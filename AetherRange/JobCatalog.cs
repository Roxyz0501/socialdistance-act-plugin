using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace SocialDistance
{
    internal sealed class JobInfo
    {
        public JobInfo(string abbreviation, Color color, string iconName)
        {
            Abbreviation = abbreviation;
            Color = color;
            IconName = iconName;
        }

        public string Abbreviation { get; private set; }
        public Color Color { get; private set; }
        public string IconName { get; private set; }
    }

    internal static class JobCatalog
    {
        private static readonly Color Tank = Color.FromArgb(82, 132, 255);
        private static readonly Color Healer = Color.FromArgb(82, 204, 142);
        private static readonly Color Dps = Color.FromArgb(244, 99, 115);
        private static readonly Color Crafter = Color.FromArgb(211, 158, 83);
        private static readonly Color Gatherer = Color.FromArgb(126, 190, 120);
        private static readonly Color Unknown = Color.FromArgb(145, 153, 170);

        private static readonly Dictionary<int, JobInfo> Jobs = new Dictionary<int, JobInfo>
        {
            { 1, J("GLA", Tank, "gladiator") }, { 2, J("PGL", Dps, "pugilist") },
            { 3, J("MRD", Tank, "marauder") }, { 4, J("LNC", Dps, "lancer") },
            { 5, J("ARC", Dps, "archer") }, { 6, J("CNJ", Healer, "conjurer") },
            { 7, J("THM", Dps, "thaumaturge") }, { 8, J("CRP", Crafter, "carpenter") },
            { 9, J("BSM", Crafter, "blacksmith") }, { 10, J("ARM", Crafter, "armorer") },
            { 11, J("GSM", Crafter, "goldsmith") }, { 12, J("LTW", Crafter, "leatherworker") },
            { 13, J("WVR", Crafter, "weaver") }, { 14, J("ALC", Crafter, "alchemist") },
            { 15, J("CUL", Crafter, "culinarian") }, { 16, J("MIN", Gatherer, "miner") },
            { 17, J("BTN", Gatherer, "botanist") }, { 18, J("FSH", Gatherer, "fisher") },
            { 19, J("PLD", Tank, "paladin") }, { 20, J("MNK", Dps, "monk") },
            { 21, J("WAR", Tank, "warrior") }, { 22, J("DRG", Dps, "dragoon") },
            { 23, J("BRD", Dps, "bard") }, { 24, J("WHM", Healer, "whitemage") },
            { 25, J("BLM", Dps, "blackmage") }, { 26, J("ACN", Dps, "arcanist") },
            { 27, J("SMN", Dps, "summoner") }, { 28, J("SCH", Healer, "scholar") },
            { 29, J("ROG", Dps, "rogue") }, { 30, J("NIN", Dps, "ninja") },
            { 31, J("MCH", Dps, "machinist") }, { 32, J("DRK", Tank, "darkknight") },
            { 33, J("AST", Healer, "astrologian") }, { 34, J("SAM", Dps, "samurai") },
            { 35, J("RDM", Dps, "redmage") }, { 36, J("BLU", Dps, "bluemage") },
            { 37, J("GNB", Tank, "gunbreaker") }, { 38, J("DNC", Dps, "dancer") },
            { 39, J("RPR", Dps, "reaper") }, { 40, J("SGE", Healer, "sage") },
            { 41, J("VPR", Dps, "viper") }, { 42, J("PCT", Dps, "pictomancer") }
        };

        private static readonly Dictionary<int, Image> IconCache = new Dictionary<int, Image>();

        public static JobInfo Get(int jobId)
        {
            JobInfo info;
            return Jobs.TryGetValue(jobId, out info) ? info : new JobInfo("???", Unknown, null);
        }

        public static Image GetIcon(int jobId)
        {
            Image cached;
            if (IconCache.TryGetValue(jobId, out cached))
                return cached;

            var info = Get(jobId);
            if (string.IsNullOrEmpty(info.IconName))
                return null;

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SocialDistance.Assets.Jobs." + info.IconName + ".png";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;
                using (var source = Image.FromStream(stream))
                    cached = new Bitmap(source);
            }

            IconCache[jobId] = cached;
            return cached;
        }

        private static JobInfo J(string abbreviation, Color color, string iconName)
            => new JobInfo(abbreviation, color, iconName);
    }
}
