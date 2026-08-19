using System;
using System.IO;
using SocialDistance;

internal static class Program
{
    private static int failures;

    private static void Main()
    {
        Equal("ja", Localization.ResolveInitialLanguage("", "ja-JP"), "OS Japanese");
        Equal("zh-CN", Localization.ResolveInitialLanguage(null, "zh-Hans"), "OS Chinese");
        Equal("ko", Localization.ResolveInitialLanguage("", "ko-KR"), "OS Korean");
        Equal("en", Localization.ResolveInitialLanguage("", "fr-FR"), "OS fallback");
        Equal("ja", Localization.ResolveInitialLanguage("ja", "en-US"), "Configured language retained");
        Equal("SOCIAL DISTANCE", Localization.Text("ja", "OverlayTitle"), "Missing key falls back to English");

        SemVersion v123, v124, prerelease, stable;
        True(SemVersion.TryParse("v1.2.3", out v123), "SemVer v prefix");
        True(SemVersion.TryParse("1.2.4", out v124) && v123.CompareTo(v124) < 0, "SemVer compare");
        SemVersion.TryParse("2.0.0-rc.1", out prerelease);
        SemVersion.TryParse("2.0.0", out stable);
        True(prerelease.CompareTo(stable) < 0, "Prerelease ordering");

        var json = "{\"tag_name\":\"v2.5.0\",\"name\":\"Stable\",\"body\":\"Notes\"," +
                   "\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"SocialDistance-v2.5.0.zip\"," +
                   "\"browser_download_url\":\"https://github.com/Roxyz0501/repo/releases/download/v2.5.0/file.zip\",\"size\":123}]}";
        var release = GitHubReleaseParser.ParseStableRelease(json);
        Equal("2.5.0", release.Version.ToString(), "Release response parsing");
        Equal(1, release.Assets.Count, "Release asset parsing");
        Equal(UpdateCheckKind.Available, GitHubReleaseClient.Evaluate(new Version(2, 4, 0), release).Kind,
            "Update available classification");
        Equal(UpdateCheckKind.UpToDate, GitHubReleaseClient.Evaluate(new Version(2, 5, 0), release).Kind,
            "No update classification");
        True(UpdateRepository.IsConfigured, "Update repository configured");
        Equal("Roxyz0501", UpdateRepository.Owner, "Update repository owner");
        Equal("socialdistance-act-plugin", UpdateRepository.Name, "Update repository name");
        Throws(() => GitHubReleaseParser.ParseStableRelease(json.Replace("\"prerelease\":false", "\"prerelease\":true")),
            "Prerelease rejected");
        Throws(() => GitHubReleaseParser.ParseStableRelease("{broken"), "Malformed response rejected");

        True(UpdatePackageManager.IsSafeZipEntry("package/SocialDistance.dll"), "Safe ZIP entry");
        True(!UpdatePackageManager.IsSafeZipEntry("../SocialDistance.dll"), "Zip Slip parent rejected");
        True(!UpdatePackageManager.IsSafeZipEntry("/SocialDistance.dll"), "Rooted ZIP entry rejected");
        True(!UpdatePackageManager.IsSafeZipEntry("C:\\SocialDistance.dll"), "Drive path rejected");

        var temp = Path.Combine(Path.GetTempPath(), "SocialDistanceTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var legacyConfig = Path.Combine(temp, "legacy.xml");
            File.WriteAllText(legacyConfig,
                "<PluginSettings><OverlayEnabled>false</OverlayEnabled><MaxRows>8</MaxRows></PluginSettings>");
            var legacy = PluginSettings.Load(legacyConfig);
            Equal("", legacy.Language, "Legacy Config language remains unset for one-time OS detection");
            True(legacy.CheckUpdatesOnStartup, "Legacy Config receives safe update-check default");

            var file = Path.Combine(temp, "asset.bin");
            File.WriteAllText(file, "verified content");
            var hash = UpdatePackageManager.ComputeSha256(file);
            True(UpdatePackageManager.VerifySha256(file, hash), "SHA-256 verified");
            File.AppendAllText(file, "corrupt");
            True(!UpdatePackageManager.VerifySha256(file, hash), "Corrupt asset rejected");
            Equal(hash, UpdatePackageManager.ParseChecksum(hash + "  SocialDistance-v2.5.0.zip",
                "SocialDistance-v2.5.0.zip"), "SHA-256 manifest parsing");
        }
        finally
        {
            Directory.Delete(temp, true);
        }

        if (failures > 0)
        {
            Console.Error.WriteLine(failures + " test(s) failed.");
            Environment.Exit(1);
        }
        Console.WriteLine("All SocialDistance tests passed.");
    }

    private static void True(bool value, string name)
    {
        if (value) { Console.WriteLine("PASS " + name); return; }
        failures++;
        Console.Error.WriteLine("FAIL " + name);
    }
    private static void Equal<T>(T expected, T actual, string name)
    {
        True(Equals(expected, actual), name + " (expected " + expected + ", actual " + actual + ")");
    }
    private static void Throws(Action action, string name)
    {
        try { action(); True(false, name); }
        catch { True(true, name); }
    }
}
