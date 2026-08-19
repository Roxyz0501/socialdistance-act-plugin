using System;
using System.IO;
using System.Xml.Serialization;

namespace SocialDistance
{
    [Serializable]
    public sealed class PluginSettings
    {
        public bool OverlayEnabled { get; set; } = true;
        public bool OverlayLocked { get; set; } = false;
        public bool HideWhenFfxivInactive { get; set; } = false;
        public bool ShowPlayerNames { get; set; } = true;
        public bool AnonymousMode { get; set; } = false;
        public bool ShowLinkDistanceColumn { get; set; } = false;
        public bool EnableSpacingAlert { get; set; } = false;
        public bool WarningOverlayEnabled { get; set; } = true;
        public bool EchoToggleEnabled { get; set; } = false;
        public string EchoToggleText { get; set; } = "SocialDistance";
        public bool GameMessageTriggerEnabled { get; set; } = false;
        public string GameMessageOnText { get; set; } = "";
        public string GameMessageOffText { get; set; } = "";
        public int OverlayX { get; set; } = 80;
        public int OverlayY { get; set; } = 120;
        public int MaxRows { get; set; } = 12;
        public int MaxDistance { get; set; } = 100;
        public decimal AlertDistance { get; set; } = 5m;
        public string AlertMode { get; set; } = "nearest";
        public string DistanceUnit { get; set; } = "y";
        public int OpacityPercent { get; set; } = 94;
        public int BackgroundOpacityPercent { get; set; } = 100;
        public int OverlayWidth { get; set; } = 292;
        public int OverlayHeight { get; set; } = 390;
        public int WarningOverlayX { get; set; } = 390;
        public int WarningOverlayY { get; set; } = 120;
        public int WarningOverlayWidth { get; set; } = 160;
        public int WarningOverlayHeight { get; set; } = 58;
        public string Language { get; set; } = "";
        public bool CheckUpdatesOnStartup { get; set; } = true;
        public string LastUpdateCheckUtc { get; set; } = "";
        public string SkippedUpdateVersion { get; set; } = "";

        public static PluginSettings Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return new PluginSettings();

                using (var stream = File.OpenRead(path))
                    return (PluginSettings)new XmlSerializer(typeof(PluginSettings)).Deserialize(stream);
            }
            catch
            {
                return new PluginSettings();
            }
        }

        public void Save(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var stream = File.Create(path))
                new XmlSerializer(typeof(PluginSettings)).Serialize(stream, this);
        }
    }
}
