using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SocialDistance
{
    internal enum SettingsSaveState
    {
        NotDue,
        Unchanged,
        Saved,
        Failed
    }

    internal sealed class SettingsSaveResult
    {
        public SettingsSaveState State { get; private set; }
        public Exception Error { get; private set; }

        public SettingsSaveResult(SettingsSaveState state, Exception error = null)
        {
            State = state;
            Error = error;
        }
    }

    internal sealed class SettingsPersistenceCoordinator
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(60);

        private readonly object gate = new object();
        private readonly string path;
        private readonly TimeSpan interval;
        private readonly Func<PluginSettings, byte[]> serialize;
        private readonly Action<string, byte[]> write;
        private byte[] lastSavedSnapshot;
        private bool hasSavedSnapshot;
        private DateTime lastCheckUtc;

        public SettingsPersistenceCoordinator(
            string path,
            PluginSettings initialSettings,
            bool initialSettingsAreSaved,
            DateTime nowUtc)
            : this(path, initialSettings, initialSettingsAreSaved, nowUtc,
                DefaultInterval, Serialize, AtomicWrite)
        {
        }

        internal SettingsPersistenceCoordinator(
            string path,
            PluginSettings initialSettings,
            bool initialSettingsAreSaved,
            DateTime nowUtc,
            TimeSpan interval,
            Func<PluginSettings, byte[]> serialize,
            Action<string, byte[]> write)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A settings path is required.", nameof(path));
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
            this.path = path;
            this.interval = interval;
            this.serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
            this.write = write ?? throw new ArgumentNullException(nameof(write));
            lastCheckUtc = nowUtc;
            hasSavedSnapshot = initialSettingsAreSaved;
            if (initialSettingsAreSaved)
                lastSavedSnapshot = this.serialize(initialSettings);
        }

        public SettingsSaveResult Poll(PluginSettings settings, DateTime nowUtc)
        {
            lock (gate)
            {
                if (nowUtc < lastCheckUtc || nowUtc - lastCheckUtc < interval)
                    return new SettingsSaveResult(SettingsSaveState.NotDue);
                lastCheckUtc = nowUtc;
                return SaveIfChanged(settings);
            }
        }

        public SettingsSaveResult Flush(PluginSettings settings, DateTime nowUtc)
        {
            lock (gate)
            {
                lastCheckUtc = nowUtc;
                return SaveIfChanged(settings);
            }
        }

        private SettingsSaveResult SaveIfChanged(PluginSettings settings)
        {
            byte[] snapshot;
            try
            {
                snapshot = serialize(settings);
            }
            catch (Exception ex)
            {
                return new SettingsSaveResult(SettingsSaveState.Failed, ex);
            }

            if (hasSavedSnapshot && Equal(lastSavedSnapshot, snapshot))
                return new SettingsSaveResult(SettingsSaveState.Unchanged);

            try
            {
                write(path, snapshot);
                lastSavedSnapshot = snapshot;
                hasSavedSnapshot = true;
                return new SettingsSaveResult(SettingsSaveState.Saved);
            }
            catch (Exception ex)
            {
                // Preserve the last successful snapshot so the next scheduled check retries.
                return new SettingsSaveResult(SettingsSaveState.Failed, ex);
            }
        }

        internal static byte[] Serialize(PluginSettings settings)
        {
            using (var stream = new MemoryStream())
            {
                new XmlSerializer(typeof(PluginSettings)).Serialize(stream, settings);
                return stream.ToArray();
            }
        }

        internal static void AtomicWrite(string path, byte[] contents)
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(directory);
            var temporaryPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew,
                           FileAccess.Write, FileShare.None))
                {
                    stream.Write(contents, 0, contents.Length);
                    stream.Flush(true);
                }

                if (File.Exists(fullPath))
                    File.Replace(temporaryPath, fullPath, null);
                else
                    File.Move(temporaryPath, fullPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static bool Equal(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            return !left.Where((value, index) => value != right[index]).Any();
        }
    }
}

