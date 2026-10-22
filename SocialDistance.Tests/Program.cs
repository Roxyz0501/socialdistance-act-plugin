using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        TestUpdateCancellation();
        Throws(() => GitHubReleaseParser.ParseStableRelease(json.Replace("\"prerelease\":false", "\"prerelease\":true")),
            "Prerelease rejected");
        Throws(() => GitHubReleaseParser.ParseStableRelease("{broken"), "Malformed response rejected");

        True(UpdatePackageManager.IsSafeZipEntry("package/SocialDistance.dll"), "Safe ZIP entry");
        True(!UpdatePackageManager.IsSafeZipEntry("../SocialDistance.dll"), "Zip Slip parent rejected");
        True(!UpdatePackageManager.IsSafeZipEntry("/SocialDistance.dll"), "Rooted ZIP entry rejected");
        True(!UpdatePackageManager.IsSafeZipEntry("C:\\SocialDistance.dll"), "Drive path rejected");

        var temp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "SocialDistanceTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var legacyConfig = Path.Combine(temp, "legacy.xml");
            File.WriteAllText(legacyConfig,
                "<PluginSettings><OverlayEnabled>false</OverlayEnabled><MaxRows>8</MaxRows></PluginSettings>");
            var legacy = PluginSettings.Load(legacyConfig);
            Equal("", legacy.Language, "Legacy Config language remains unset for one-time OS detection");
            True(legacy.CheckUpdatesOnStartup, "Legacy Config receives safe update-check default");
            bool loaded;
            PluginSettings.Load(legacyConfig, out loaded);
            True(loaded, "Existing legacy Config is reported as loaded");

            TestSettingsPersistence(temp);

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

    private static void TestSettingsPersistence(string temp)
    {
        var now = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc);
        var path = Path.Combine(temp, "settings.xml");
        var settings = new PluginSettings { OverlayX = 80 };
        SettingsPersistenceCoordinator.AtomicWrite(path, SettingsPersistenceCoordinator.Serialize(settings));
        var writes = 0;
        var coordinator = new SettingsPersistenceCoordinator(path, settings, true, now,
            TimeSpan.FromSeconds(60), SettingsPersistenceCoordinator.Serialize,
            (target, bytes) => { writes++; SettingsPersistenceCoordinator.AtomicWrite(target, bytes); });
        Equal(SettingsSaveState.Unchanged, coordinator.Poll(settings, now.AddSeconds(60)).State,
            "Unchanged settings do not write");
        Equal(0, writes, "No write for unchanged settings");

        settings.OverlayX = 100;
        Equal(SettingsSaveState.NotDue, coordinator.Poll(settings, now.AddSeconds(119)).State,
            "Continuous edits wait for 60-second cadence");
        settings.OverlayX = 120;
        Equal(SettingsSaveState.Saved, coordinator.Poll(settings, now.AddSeconds(120)).State,
            "Latest continuous edit is coalesced and saved");
        Equal(1, writes, "One write for coalesced edits");
        settings.OverlayX = 80;
        Equal(SettingsSaveState.Saved, coordinator.Flush(settings, now.AddSeconds(121)).State,
            "Returning to old value still saves against last persisted snapshot");
        Equal(2, writes, "Changed final state is persisted");

        var oldBytes = File.ReadAllBytes(path);
        var shouldFail = true;
        var failureCoordinator = new SettingsPersistenceCoordinator(path, settings, true, now,
            TimeSpan.FromSeconds(60), SettingsPersistenceCoordinator.Serialize,
            (target, bytes) =>
            {
                if (shouldFail) throw new IOException("simulated write failure");
                SettingsPersistenceCoordinator.AtomicWrite(target, bytes);
            });
        settings.OverlayX = 222;
        Equal(SettingsSaveState.Failed, failureCoordinator.Poll(settings, now.AddSeconds(60)).State,
            "Save failure is reported");
        True(BytesEqual(oldBytes, File.ReadAllBytes(path)), "Failed save preserves the previous file");
        Equal(SettingsSaveState.NotDue, failureCoordinator.Poll(settings, now.AddSeconds(90)).State,
            "Save failure does not trigger tight retry");
        shouldFail = false;
        Equal(SettingsSaveState.Saved, failureCoordinator.Poll(settings, now.AddSeconds(120)).State,
            "Failed dirty settings are retried at next cadence");

        var gateEntered = new ManualResetEventSlim(false);
        var releaseWriter = new ManualResetEventSlim(false);
        var concurrentPath = Path.Combine(temp, "shutdown-settings.xml");
        var concurrent = new PluginSettings { OverlayX = 80 };
        var firstWrite = true;
        var shutdownCoordinator = new SettingsPersistenceCoordinator(concurrentPath, concurrent, false, now,
            TimeSpan.FromSeconds(60), SettingsPersistenceCoordinator.Serialize,
            (target, bytes) =>
            {
                if (firstWrite)
                {
                    firstWrite = false;
                    gateEntered.Set();
                    releaseWriter.Wait();
                }
                SettingsPersistenceCoordinator.AtomicWrite(target, bytes);
            });
        var inFlightSave = Task.Run(() => shutdownCoordinator.Poll(concurrent, now.AddSeconds(60)));
        True(gateEntered.Wait(TimeSpan.FromSeconds(5)), "Periodic save entered writer");
        concurrent.OverlayX = 333;
        var finalSave = Task.Run(() => shutdownCoordinator.Flush(concurrent, now.AddSeconds(61)));
        releaseWriter.Set();
        True(Task.WaitAll(new Task[] { inFlightSave, finalSave }, TimeSpan.FromSeconds(5)),
            "Shutdown waits for in-flight save and performs final flush");
        var reloaded = PluginSettings.Load(concurrentPath);
        Equal(333, reloaded.OverlayX, "Shutdown flush persists latest in-memory settings");
        Equal(SettingsSaveState.Unchanged, shutdownCoordinator.Flush(concurrent, now.AddSeconds(62)).State,
            "Clean shutdown leaves no pending settings writes");
        gateEntered.Dispose();
        releaseWriter.Dispose();
    }

    private static void TestUpdateCancellation()
    {
        var handler = new BlockingHttpHandler();
        using (var client = new GitHubReleaseClient(handler))
        using (var cancellation = new CancellationTokenSource())
        {
            var task = client.CheckAsync(new Version(2, 5, 0), cancellation.Token);
            True(SpinWait.SpinUntil(() => handler.WasCalled, TimeSpan.FromSeconds(2)),
                "Update check started before shutdown cancellation");
            cancellation.Cancel();
            try
            {
                task.GetAwaiter().GetResult();
                True(false, "In-flight update check cancels cleanly");
            }
            catch (OperationCanceledException)
            {
                True(true, "In-flight update check cancels cleanly");
            }
        }
    }

    private sealed class BlockingHttpHandler : HttpMessageHandler
    {
        public volatile bool WasCalled;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length) return false;
        for (var i = 0; i < left.Length; i++)
            if (left[i] != right[i]) return false;
        return true;
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

