using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SocialDistance
{
    internal static class GitHubReleaseParser
    {
        public static ReleaseInfo ParseStableRelease(string json)
        {
            var root = new JavaScriptSerializer().DeserializeObject(json) as IDictionary<string, object>;
            if (root == null) throw new FormatException("GitHub response is not an object.");
            var tag = GetString(root, "tag_name");
            SemVersion version;
            if (!SemVersion.TryParse(tag, out version)) throw new FormatException("Release tag is not valid SemVer.");
            var release = new ReleaseInfo
            {
                TagName = tag,
                Version = version,
                Name = GetString(root, "name"),
                Notes = GetString(root, "body"),
                Draft = GetBool(root, "draft"),
                Prerelease = GetBool(root, "prerelease")
            };
            if (release.Draft || release.Prerelease || !release.Version.IsStable)
                throw new FormatException("The release is not a stable release.");
            object assetsValue;
            var assets = root.TryGetValue("assets", out assetsValue) ? assetsValue as IEnumerable : null;
            if (assets != null)
            {
                foreach (var value in assets)
                {
                    var item = value as IDictionary<string, object>;
                    if (item == null) continue;
                    release.Assets.Add(new ReleaseAssetInfo
                    {
                        Name = GetString(item, "name"),
                        DownloadUrl = GetString(item, "browser_download_url"),
                        Size = GetLong(item, "size")
                    });
                }
            }
            return release;
        }

        private static string GetString(IDictionary<string, object> values, string key)
        {
            object value;
            return values.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : "";
        }
        private static bool GetBool(IDictionary<string, object> values, string key)
        {
            object value;
            return values.TryGetValue(key, out value) && value != null && Convert.ToBoolean(value);
        }
        private static long GetLong(IDictionary<string, object> values, string key)
        {
            object value;
            return values.TryGetValue(key, out value) && value != null ? Convert.ToInt64(value) : 0;
        }
    }

    internal sealed class GitHubReleaseClient : IDisposable
    {
        private readonly HttpClient client;

        public GitHubReleaseClient()
        {
            client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SocialDistance-ACT-Plugin");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }

        public async Task<UpdateCheckResult> CheckAsync(Version currentAssemblyVersion)
        {
            var current = ToSemVersion(currentAssemblyVersion);
            if (!UpdateRepository.IsConfigured)
                return new UpdateCheckResult { Kind = UpdateCheckKind.RepositoryMissing, CurrentVersion = current };
            try
            {
                var url = "https://api.github.com/repos/" + UpdateRepository.Owner + "/" +
                          UpdateRepository.Name + "/releases/latest";
                var json = await client.GetStringAsync(url).ConfigureAwait(false);
                var release = GitHubReleaseParser.ParseStableRelease(json);
                return Evaluate(currentAssemblyVersion, release);
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    Kind = UpdateCheckKind.Failed,
                    CurrentVersion = current,
                    Error = ex.GetBaseException().Message
                };
            }
        }

        public static UpdateCheckResult Evaluate(Version currentAssemblyVersion, ReleaseInfo release)
        {
            var current = ToSemVersion(currentAssemblyVersion);
            if (release == null || release.Version == null)
                return new UpdateCheckResult
                {
                    Kind = UpdateCheckKind.Failed,
                    CurrentVersion = current,
                    Error = "Release metadata is invalid."
                };
            return new UpdateCheckResult
            {
                Kind = release.Version.CompareTo(current) > 0
                    ? UpdateCheckKind.Available
                    : UpdateCheckKind.UpToDate,
                CurrentVersion = current,
                Release = release
            };
        }

        private static SemVersion ToSemVersion(Version version)
        {
            SemVersion result;
            SemVersion.TryParse(version.Major + "." + version.Minor + "." +
                                Math.Max(0, version.Build), out result);
            return result;
        }

        public void Dispose() => client.Dispose();
    }
}
