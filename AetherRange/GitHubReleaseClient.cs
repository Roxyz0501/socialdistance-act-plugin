using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
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
        private sealed class RateLimitException : HttpRequestException
        {
            public RateLimitException(string message) : base(message) { }
        }

        private readonly HttpClient client;

        public GitHubReleaseClient()
            : this(new HttpClientHandler { AllowAutoRedirect = false })
        {
        }

        internal GitHubReleaseClient(HttpMessageHandler handler)
        {
            client = new HttpClient(handler, true) { Timeout = TimeSpan.FromSeconds(12) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SocialDistance-ACT-Plugin");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }

        public async Task<UpdateCheckResult> CheckAsync(Version currentAssemblyVersion, CancellationToken cancellationToken)
        {
            var current = ToSemVersion(currentAssemblyVersion);
            cancellationToken.ThrowIfCancellationRequested();
            if (!UpdateRepository.IsConfigured)
                return new UpdateCheckResult { Kind = UpdateCheckKind.RepositoryMissing, CurrentVersion = current };
            try
            {
                var latest = await ResolveLatestStableTagAsync(cancellationToken).ConfigureAwait(false);
                if (latest.Version.CompareTo(current) <= 0)
                {
                    return new UpdateCheckResult { Kind = UpdateCheckKind.UpToDate, CurrentVersion = current, Release = latest };
                }

                var apiUrl = "https://api.github.com/repos/" + UpdateRepository.Owner + "/" +
                             UpdateRepository.Name + "/releases/tags/" + Uri.EscapeDataString(latest.TagName);
                using (var request = new HttpRequestMessage(HttpMethod.Get, apiUrl))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead,
                           cancellationToken).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden || (int)response.StatusCode == 429)
                    {
                        var fallback = CreateTagPinnedRelease(latest.Version, latest.TagName);
                        return new UpdateCheckResult
                        {
                            Kind = UpdateCheckKind.RateLimitedAvailable,
                            CurrentVersion = current,
                            Release = fallback,
                            Error = "GitHub API rate limit reached.",
                            FailureKind = UpdateCheckFailureKind.RateLimit
                        };
                    }
                    response.EnsureSuccessStatusCode();
                    cancellationToken.ThrowIfCancellationRequested();
                    var release = GitHubReleaseParser.ParseStableRelease(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    if (!string.Equals(release.TagName, latest.TagName, StringComparison.OrdinalIgnoreCase))
                        throw new FormatException("GitHub API returned a different Release tag.");
                    return Evaluate(currentAssemblyVersion, release);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                return new UpdateCheckResult
                {
                    Kind = UpdateCheckKind.Failed,
                    CurrentVersion = current,
                    Error = ex.GetBaseException().Message,
                    FailureKind = UpdateCheckFailureKind.Network
                };
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    Kind = UpdateCheckKind.Failed,
                    CurrentVersion = current,
                    Error = ex.GetBaseException().Message,
                    FailureKind = ex is RateLimitException ? UpdateCheckFailureKind.RateLimit :
                        ex is HttpRequestException || ex is TaskCanceledException
                            ? UpdateCheckFailureKind.Network : UpdateCheckFailureKind.InvalidResponse
                };
            }
        }

        private async Task<ReleaseInfo> ResolveLatestStableTagAsync(CancellationToken cancellationToken)
        {
            var uri = new Uri("https://github.com/" + UpdateRepository.Owner + "/" + UpdateRepository.Name + "/releases/latest");
            for (var redirects = 0; redirects <= 5; redirects++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var request = new HttpRequestMessage(HttpMethod.Head, uri))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                           cancellationToken).ConfigureAwait(false))
                {
                    if (IsRedirect(response.StatusCode))
                    {
                        if (response.Headers.Location == null) throw new HttpRequestException("GitHub latest-release redirect had no location.");
                        uri = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location : new Uri(uri, response.Headers.Location);
                        if (!TryParseAllowedTag(uri, out var tag, out var version))
                        {
                            if (!IsLatestUri(uri)) throw new FormatException("GitHub redirected outside the configured repository.");
                            continue;
                        }
                        return new ReleaseInfo { TagName = tag, Version = version };
                    }
                    if (response.StatusCode == HttpStatusCode.Forbidden || (int)response.StatusCode == 429)
                        throw new RateLimitException("GitHub is rate-limiting release checks.");
                    response.EnsureSuccessStatusCode();
                    if (TryParseAllowedTag(response.RequestMessage.RequestUri, out var finalTag, out var finalVersion))
                        return new ReleaseInfo { TagName = finalTag, Version = finalVersion };
                    if (!IsLatestUri(response.RequestMessage.RequestUri))
                        throw new FormatException("GitHub latest-release page is not in the configured repository.");
                    uri = response.RequestMessage.RequestUri;
                }

                // Some GitHub front ends answer HEAD with the generic page. Read the canonical URL only in that case.
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead,
                           cancellationToken).ConfigureAwait(false))
                {
                    if (IsRedirect(response.StatusCode))
                    {
                        if (response.Headers.Location == null) throw new HttpRequestException("GitHub latest-release redirect had no location.");
                        uri = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location : new Uri(uri, response.Headers.Location);
                        if (TryParseAllowedTag(uri, out var redirectedTag, out var redirectedVersion))
                            return new ReleaseInfo { TagName = redirectedTag, Version = redirectedVersion };
                        throw new FormatException("GitHub redirected outside the configured repository.");
                    }
                    if (response.StatusCode == HttpStatusCode.Forbidden || (int)response.StatusCode == 429)
                        throw new RateLimitException("GitHub is rate-limiting release checks.");
                    response.EnsureSuccessStatusCode();
                    var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var match = Regex.Match(html, "<link[^>]+rel=[\\\"']canonical[\\\"'][^>]+href=[\\\"'](?<url>[^\\\"']+)", RegexOptions.IgnoreCase);
                    if (!match.Success) match = Regex.Match(html, "<meta[^>]+property=[\\\"']og:url[\\\"'][^>]+content=[\\\"'](?<url>[^\\\"']+)", RegexOptions.IgnoreCase);
                    Uri canonical;
                    if (!match.Success || !Uri.TryCreate(match.Groups["url"].Value, UriKind.Absolute, out canonical) ||
                        !TryParseAllowedTag(canonical, out var htmlTag, out var htmlVersion))
                        throw new FormatException("Could not identify the stable tag from GitHub releases/latest.");
                    return new ReleaseInfo { TagName = htmlTag, Version = htmlVersion };
                }
            }
            throw new HttpRequestException("Too many GitHub latest-release redirects.");
        }

        private static ReleaseInfo CreateTagPinnedRelease(SemVersion version, string tag)
        {
            var zipName = "SocialDistance-v" + version + ".zip";
            var root = "https://github.com/" + UpdateRepository.Owner + "/" + UpdateRepository.Name +
                       "/releases/download/" + Uri.EscapeDataString(tag) + "/";
            return new ReleaseInfo
            {
                TagName = tag,
                Version = version,
                Name = tag,
                Notes = "",
                Assets = new System.Collections.Generic.List<ReleaseAssetInfo>
                {
                    new ReleaseAssetInfo { Name = zipName, DownloadUrl = root + zipName },
                    new ReleaseAssetInfo { Name = zipName + ".sha256", DownloadUrl = root + zipName + ".sha256" }
                }
            };
        }

        private static bool TryParseAllowedTag(Uri uri, out string tag, out SemVersion version)
        {
            tag = null;
            version = null;
            if (uri == null || uri.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)) return false;
            var path = Uri.UnescapeDataString(uri.AbsolutePath);
            var prefix = "/" + UpdateRepository.Owner + "/" + UpdateRepository.Name + "/releases/tag/";
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            tag = path.Substring(prefix.Length);
            if (tag.Length == 0 || tag.Contains("/")) return false;
            return SemVersion.TryParse(tag, out version) && version.IsStable;
        }

        private static bool IsLatestUri(Uri uri)
        {
            if (uri == null || uri.Scheme != Uri.UriSchemeHttps || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)) return false;
            return string.Equals(Uri.UnescapeDataString(uri.AbsolutePath),
                "/" + UpdateRepository.Owner + "/" + UpdateRepository.Name + "/releases/latest", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRedirect(HttpStatusCode code)
        {
            return code == HttpStatusCode.MovedPermanently || code == HttpStatusCode.Redirect ||
                   code == HttpStatusCode.SeeOther || code == HttpStatusCode.TemporaryRedirect || (int)code == 308;
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
