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
using System.Threading.Tasks;

namespace SocialDistance
{
    internal sealed class UpdatePreparationResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string StagingDirectory { get; set; }
        public string BackupPath { get; set; }
    }

    internal sealed class UpdatePackageManager : IDisposable
    {
        private const long MaximumPackageBytes = 50L * 1024 * 1024;
        private readonly HttpClient client;

        public UpdatePackageManager()
        {
            client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SocialDistance-ACT-Plugin");
        }

        public async Task<UpdatePreparationResult> PrepareAndScheduleAsync(
            ReleaseInfo release, string pluginPath, int actProcessId)
        {
            try
            {
                if (release == null || release.Version == null || !release.Version.IsStable)
                    throw new InvalidDataException("Release metadata is invalid.");
                var fullPluginPath = Path.GetFullPath(pluginPath);
                if (!string.Equals(Path.GetFileName(fullPluginPath), "SocialDistance.dll",
                        StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPluginPath))
                    throw new InvalidDataException("The loaded plugin path is invalid.");

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
                var stage = Path.Combine(root, version + "-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(stage);
                var packagePath = Path.Combine(stage, package.Name);
                var checksumPath = Path.Combine(stage, checksum.Name);
                await DownloadAsync(package.DownloadUrl, packagePath, MaximumPackageBytes).ConfigureAwait(false);
                await DownloadAsync(checksum.DownloadUrl, checksumPath, 1024 * 1024).ConfigureAwait(false);

                var expectedHash = ParseChecksum(File.ReadAllText(checksumPath, Encoding.UTF8), package.Name);
                if (!VerifySha256(packagePath, expectedHash))
                    throw new InvalidDataException("Package SHA-256 verification failed.");

                var stagedDll = Path.Combine(stage, "SocialDistance.dll");
                ExtractValidatedPlugin(packagePath, stagedDll);
                ValidatePluginAssembly(stagedDll, release.Version);
                var stagedHash = ComputeSha256(stagedDll);
                var backupPath = fullPluginPath + ".backup-" +
                                 FileVersionInfo.GetVersionInfo(fullPluginPath).FileVersion;
                LaunchUpdater(actProcessId, stagedDll, fullPluginPath, backupPath, stagedHash, stage);
                return new UpdatePreparationResult
                {
                    Success = true,
                    StagingDirectory = stage,
                    BackupPath = backupPath
                };
            }
            catch (Exception ex)
            {
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

        private async Task DownloadAsync(string url, string destination, long maximumBytes)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || !IsAllowedGitHubUri(uri))
                throw new InvalidDataException("Release asset URL is not an allowed GitHub HTTPS URL.");
            for (var redirects = 0; redirects <= 5; redirects++)
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
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
                        while ((read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                        {
                            total += read;
                            if (total > maximumBytes) throw new InvalidDataException("Downloaded file is too large.");
                            await output.WriteAsync(buffer, 0, read).ConfigureAwait(false);
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

        private static void ExtractValidatedPlugin(string packagePath, string destination)
        {
            using (var archive = ZipFile.OpenRead(packagePath))
            {
                foreach (var entry in archive.Entries)
                    if (!IsSafeZipEntry(entry.FullName))
                        throw new InvalidDataException("Unsafe ZIP entry: " + entry.FullName);
                var candidates = archive.Entries.Where(x =>
                    string.Equals(Path.GetFileName(x.FullName), "SocialDistance.dll",
                        StringComparison.OrdinalIgnoreCase) && x.Length > 0).ToList();
                if (candidates.Count != 1) throw new InvalidDataException("Package must contain exactly one SocialDistance.dll.");
                using (var input = candidates[0].Open())
                using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    input.CopyTo(output);
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

        private static void LaunchUpdater(int processId, string source, string target, string backup,
            string expectedHash, string stage)
        {
            var resultPath = Path.Combine(stage, "update-result.txt");
            var script = "$ErrorActionPreference='Stop';" +
                         "$src=" + Ps(source) + ";$dst=" + Ps(target) + ";$bak=" + Ps(backup) +
                         ";$expected=" + Ps(expectedHash) + ";$result=" + Ps(resultPath) + ";" +
                         "Wait-Process -Id " + processId + " -ErrorAction SilentlyContinue;" +
                         "try{if((Get-FileHash -Algorithm SHA256 -LiteralPath $src).Hash.ToLowerInvariant() -ne $expected){throw 'Staged hash mismatch'};" +
                         "Copy-Item -LiteralPath $dst -Destination $bak -Force;" +
                         "try{Copy-Item -LiteralPath $src -Destination $dst -Force;" +
                         "if((Get-FileHash -Algorithm SHA256 -LiteralPath $dst).Hash.ToLowerInvariant() -ne $expected){throw 'Installed hash mismatch'}}" +
                         "catch{Copy-Item -LiteralPath $bak -Destination $dst -Force;throw};" +
                         "Set-Content -LiteralPath $result -Value 'success' -Encoding UTF8}" +
                         "catch{if((Test-Path -LiteralPath $bak) -and !(Test-Path -LiteralPath $dst)){Copy-Item -LiteralPath $bak -Destination $dst -Force};" +
                         "Set-Content -LiteralPath $result -Value ('failed: '+$_.Exception.Message) -Encoding UTF8;exit 1}";
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + encoded,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        private static string Ps(string value) => "'" + value.Replace("'", "''") + "'";
        public void Dispose() => client.Dispose();
    }
}
