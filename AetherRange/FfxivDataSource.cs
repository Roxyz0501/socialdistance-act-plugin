using Advanced_Combat_Tracker;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SocialDistance
{
    internal sealed class FfxivDataSource
    {
        private object repository;
        private MethodInfo getPlayerId;
        private MethodInfo getCombatants;
        private MethodInfo getProcess;
        private Type combatantType;
        private PropertyInfo idProperty;
        private PropertyInfo typeProperty;
        private PropertyInfo jobProperty;
        private PropertyInfo nameProperty;
        private PropertyInfo xProperty;
        private PropertyInfo yProperty;
        private PropertyInfo zProperty;

        public string LastError { get; private set; }

        public bool TryConnect()
        {
            try
            {
                var plugin = ActGlobals.oFormActMain.ActPlugins
                    .Select(p => p.pluginObj)
                    .FirstOrDefault(p => p != null &&
                        p.GetType().FullName == "FFXIV_ACT_Plugin.FFXIV_ACT_Plugin");

                if (plugin == null)
                {
                    LastError = "FFXIV_ACT_Plugin is not enabled.";
                    return false;
                }

                var repositoryProperty = plugin.GetType().GetProperty("DataRepository");
                repository = repositoryProperty == null ? null : repositoryProperty.GetValue(plugin, null);
                if (repository == null)
                {
                    LastError = "FFXIV data repository is not ready.";
                    return false;
                }

                var repositoryType = repository.GetType().GetInterfaces()
                    .FirstOrDefault(t => t.FullName == "FFXIV_ACT_Plugin.Common.IDataRepository")
                    ?? repository.GetType();

                getPlayerId = repositoryType.GetMethod("GetCurrentPlayerID");
                getCombatants = repositoryType.GetMethod("GetCombatantList");
                getProcess = repositoryType.GetMethod("GetCurrentFFXIVProcess");

                if (getPlayerId == null || getCombatants == null)
                {
                    LastError = "The installed FFXIV plugin does not expose combatant data.";
                    repository = null;
                    return false;
                }

                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.GetBaseException().Message;
                repository = null;
                return false;
            }
        }

        public Process GetGameProcess()
        {
            if (repository == null && !TryConnect())
                return null;

            try
            {
                return getProcess == null ? null : getProcess.Invoke(repository, null) as Process;
            }
            catch
            {
                return null;
            }
        }

        public IList<PlayerDistance> ReadPlayers(int maxRows, int maxDistance)
        {
            if (repository == null && !TryConnect())
                return new List<PlayerDistance>();

            try
            {
                var selfId = Convert.ToUInt32(getPlayerId.Invoke(repository, null));
                var items = getCombatants.Invoke(repository, null) as IEnumerable;
                if (selfId == 0 || items == null)
                    return new List<PlayerDistance>();

                object self = null;
                var snapshot = new List<object>();
                foreach (var item in items)
                {
                    if (item == null)
                        continue;

                    EnsureCombatantProperties(item.GetType());
                    snapshot.Add(item);
                    if (GetUInt(idProperty, item) == selfId)
                        self = item;
                }

                if (self == null)
                    return new List<PlayerDistance>();

                var selfX = GetFloat(xProperty, self);
                var selfY = GetFloat(yProperty, self);
                var selfZ = GetFloat(zProperty, self);

                var result = snapshot
                    .Where(item => GetUInt(idProperty, item) != selfId)
                    .Where(item => GetByte(typeProperty, item) == 1)
                    .Select(item => new PlayerDistance
                    {
                        Id = GetUInt(idProperty, item),
                        JobId = GetInt(jobProperty, item),
                        Name = Convert.ToString(nameProperty.GetValue(item, null)),
                        X = GetFloat(xProperty, item),
                        Y = GetFloat(yProperty, item),
                        Z = GetFloat(zProperty, item),
                        Distance = Distance3D(selfX, selfY, selfZ,
                            GetFloat(xProperty, item), GetFloat(yProperty, item), GetFloat(zProperty, item))
                    })
                    .Where(player => !string.IsNullOrWhiteSpace(player.Name))
                    .Where(player => player.Distance <= maxDistance)
                    .OrderBy(player => player.Distance)
                    .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                    .Take(maxRows)
                    .ToList();

                for (var index = 0; index < result.Count; index++)
                {
                    if (index == 0)
                    {
                        result[index].LinkDistance = result[index].Distance;
                        continue;
                    }

                    var previous = result[index - 1];
                    var current = result[index];
                    current.LinkDistance = Distance3D(
                        previous.X, previous.Y, previous.Z,
                        current.X, current.Y, current.Z);
                }

                return result;
            }
            catch (Exception ex)
            {
                LastError = ex.GetBaseException().Message;
                repository = null;
                return new List<PlayerDistance>();
            }
        }

        private void EnsureCombatantProperties(Type type)
        {
            if (combatantType == type)
                return;

            combatantType = type;
            idProperty = RequiredProperty(type, "ID");
            typeProperty = RequiredProperty(type, "type");
            jobProperty = RequiredProperty(type, "Job");
            nameProperty = RequiredProperty(type, "Name");
            xProperty = RequiredProperty(type, "PosX");
            yProperty = RequiredProperty(type, "PosY");
            zProperty = RequiredProperty(type, "PosZ");
        }

        private static PropertyInfo RequiredProperty(Type type, string name)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                throw new MissingMemberException(type.FullName, name);
            return property;
        }

        internal static float Distance3D(float ax, float ay, float az, float bx, float by, float bz)
        {
            var dx = ax - bx;
            var dy = ay - by;
            var dz = az - bz;
            return (float)Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }

        private static uint GetUInt(PropertyInfo property, object value) => Convert.ToUInt32(property.GetValue(value, null));
        private static int GetInt(PropertyInfo property, object value) => Convert.ToInt32(property.GetValue(value, null));
        private static byte GetByte(PropertyInfo property, object value) => Convert.ToByte(property.GetValue(value, null));
        private static float GetFloat(PropertyInfo property, object value) => Convert.ToSingle(property.GetValue(value, null));
    }
}
