using System.Collections.Generic;

namespace SocialDistance
{
    internal static class UpdateRepository
    {
        public const string Owner = "Roxyz0501";
        public const string Name = "socialdistance-act-plugin";
        public static bool IsConfigured => Owner.Length > 0 && Name.Length > 0;
    }

    internal sealed class ReleaseAssetInfo
    {
        public string Name { get; set; }
        public string DownloadUrl { get; set; }
        public long Size { get; set; }
    }

    internal sealed class ReleaseInfo
    {
        public string TagName { get; set; }
        public SemVersion Version { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
        public List<ReleaseAssetInfo> Assets { get; set; } = new List<ReleaseAssetInfo>();
    }

    internal enum UpdateCheckKind { RepositoryMissing, UpToDate, Available, Failed }

    internal sealed class UpdateCheckResult
    {
        public UpdateCheckKind Kind { get; set; }
        public string Error { get; set; }
        public SemVersion CurrentVersion { get; set; }
        public ReleaseInfo Release { get; set; }
    }
}
