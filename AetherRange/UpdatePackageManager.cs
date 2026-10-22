using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocialDistance
{
    internal sealed class UpdatePreparationResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string StagingDirectory { get; set; }
        public string StagedPluginPath { get; set; }
    }

    internal sealed class UpdatePackageManager : IDisposable
    {
        private const long MaximumPackageBytes = 50L * 1024 * 1024;
        private const long MaximumPluginBytes = 50L * 1024 * 1024;
        private readonly HttpClient client;

        public UpdatePackageManager()
        {
            client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SocialDistance-ACT-Plugin");
        }

        public async Task<UpdatePreparationResult> PrepareAsync(
            ReleaseInfo release, CancellationToken cancellationToken)
        {
            string stage = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (release == null || release.Version == null || !release.Version.IsStable)
                    throw new InvalidDataException("Release metadata is invalid.");

                var version = release.Version.ToString();
                var expectedNames = new[]
                {
                    "SocialDistance-v" + version + ".zip",
                    "SocialDistance-" + version + ".zip"
                };
                var package = release.Assets.FirstOrDefault(x =>
                    expectedNames.Any(name => string.Equals(name, x.Name, StringComparison.OrdinalIgnoreCase)));
                if (package == null) throw new InvalidDataException("Expected SocialDistance release asset was not found.");
                var checksum = release.Assets.FirstOrDefault(x =>
                    string.Equals(x.Name, package.Name + ".sha256", StringComparison.OrdinalIgnoreCase)) ??
                    release.Assets.FirstOrDefault(x =>
                        string.Equals(x.Name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase));
                if (checksum == null) throw new InvalidDataException("SHA-256 manifest was not found.");

                var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SocialDistance", "Updates");
                Directory.CreateDirectory(root);
                stage = Path.Combine(root, version + "-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(stage);
                var packagePath = Path.Combine(stage, package.Name);
                var checksumPath = Path.Combine(stage, checksum.Name);
                await DownloadAsync(package.DownloadUrl, packagePath, MaximumPackageBytes, cancellationToken).ConfigureAwait(false);
                await DownloadAsync(checksum.DownloadUrl, checksumPath, 1024 * 1024, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
                var expectedHash = ParseChecksum(File.ReadAllText(checksumPath, Encoding.UTF8), package.Name);
                if (!VerifySha256(packagePath, expectedHash))
                    throw new InvalidDataException("Package SHA-256 verification failed.");

                var stagedDll = Path.Combine(stage, "SocialDistance.dll");
                await ExtractValidatedPluginAsync(packagePath, stagedDll, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                ValidatePluginAssembly(stagedDll, release.Version);
                cancellationToken.ThrowIfCancellationRequested();
                return new UpdatePreparationResult
                {
                    Success = true,
                    StagingDirectory = stage,
                    StagedPluginPath = stagedDll
                };
            }
            catch (OperationCanceledException)
            {
                TryDeleteStage(stage);
                throw;
            }
            catch (Exception ex)
            {
                TryDeleteStage(stage);
                return new UpdatePreparationResult { Success = false, Error = ex.GetBaseException().Message };
            }
        }

        public static bool IsSafeZipEntry(string entryName)
        {
            if (string.IsNullOrWhiteSpace(entryName)) return false;
            var normalized = entryName.Replace('\\', '/');
            if (normalized.StartsWith("/", StringComparison.Ordinal) ||
                normalized.Contains(":") ||
                normalized.Split('/').Any(part => part == ".."))
                return false;
            return !Path.IsPathRooted(entryName);
        }

        public static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        public static bool VerifySha256(string path, string expected)
        {
            return !string.IsNullOrWhiteSpace(expected) &&
                   string.Equals(ComputeSha256(path), expected.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static string ParseChecksum(string manifest, string assetName)
        {
            foreach (var rawLine in (manifest ?? "").Split(new[] { '\r', '\n' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length < 64) continue;
                var hash = line.Substring(0, 64);
                if (!hash.All(Uri.IsHexDigit)) continue;
                var remainder = line.Substring(64).TrimStart(' ', '\t', '*');
                if (remainder.Length == 0 ||
                    string.Equals(Path.GetFileName(remainder), assetName, StringComparison.OrdinalIgnoreCase))
                    return hash.ToLowerInvariant();
            }
            throw new InvalidDataException("The SHA-256 manifest does not contain the package.");
        }

        private async Task DownloadAsync(string url, string destination, long maximumBytes,
            CancellationToken cancellationToken)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || !IsAllowedGitHubUri(uri))
                throw new InvalidDataException("Release asset URL is not an allowed GitHub HTTPS URL.");
            for (var redirects = 0; redirects <= 5; redirects++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                           cancellationToken)
                           .ConfigureAwait(false))
                {
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                    {
                        if (response.Headers.Location == null) throw new HttpRequestException("Redirect location is missing.");
                        uri = response.Headers.Location.IsAbsoluteUri
                            ? response.Headers.Location
                            : new Uri(uri, response.Headers.Location);
                        if (!IsAllowedGitHubUri(uri))
                            throw new InvalidDataException("Redirected asset URL is not an allowed GitHub HTTPS URL.");
                        continue;
                    }
                    response.EnsureSuccessStatusCode();
                    if (response.Content.Headers.ContentLength.HasValue &&
                        response.Content.Headers.ContentLength.Value > maximumBytes)
                        throw new InvalidDataException("Downloaded file is too large.");
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        long total = 0;
                        int read;
                        while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                                   .ConfigureAwait(false)) > 0)
                        {
                            total += read;
                            if (total > maximumBytes) throw new InvalidDataException("Downloaded file is too large.");
                            await output.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    return;
                }
            }
            throw new HttpRequestException("Too many redirects.");
        }

        private static bool IsAllowedGitHubUri(Uri uri)
        {
            if (uri == null || uri.Scheme != Uri.UriSchemeHttps) return false;
            var host = uri.Host.ToLowerInvariant();
            return host == "api.github.com" || host == "github.com" ||
                   host == "objects.githubusercontent.com" ||
                   host == "release-assets.githubusercontent.com";
        }

        private static async Task ExtractValidatedPluginAsync(string packagePath, string destination,
            CancellationToken cancellationToken)
        {
            using (var archive = ZipFile.OpenRead(packagePath))
            {
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!IsSafeZipEntry(entry.FullName))
                        throw new InvalidDataException("Unsafe ZIP entry: " + entry.FullName);
                }
                var candidates = archive.Entries.Where(x =>
                    string.Equals(Path.GetFileName(x.FullName), "SocialDistance.dll",
                        StringComparison.OrdinalIgnoreCase) && x.Length > 0 && x.Length <= MaximumPluginBytes).ToList();
                if (candidates.Count != 1) throw new InvalidDataException("Package must contain exactly one SocialDistance.dll.");
                using (var input = candidates[0].Open())
                using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await input.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
            }
        }

        private static void ValidatePluginAssembly(string path, SemVersion releaseVersion)
        {
            var name = AssemblyName.GetAssemblyName(path);
            if (!string.Equals(name.Name, "SocialDistance", StringComparison.Ordinal))
                throw new InvalidDataException("Package contains a different plugin.");
            SemVersion assemblyVersion;
            if (!SemVersion.TryParse(name.Version.Major + "." + name.Version.Minor + "." +
                                     Math.Max(0, name.Version.Build), out assemblyVersion) ||
                assemblyVersion.CompareTo(releaseVersion) != 0)
                throw new InvalidDataException("Package version does not match the Release tag.");
            var company = FileVersionInfo.GetVersionInfo(path).CompanyName;
            if (!string.Equals(company, "Roxyz0501", StringComparison.Ordinal))
                throw new InvalidDataException("Package author metadata is invalid.");
        }

        private static void TryDeleteStage(string stage)
        {
            if (string.IsNullOrWhiteSpace(stage)) return;
            try
            {
                if (Directory.Exists(stage))
                    Directory.Delete(stage, true);
            }
            catch
            {
                // A leftover staged package has no process or retry scheduled after return.
            }
        }

        public void Dispose() => client.Dispose();
    }
}

