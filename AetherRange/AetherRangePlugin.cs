using Advanced_Combat_Tracker;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SocialDistance
{
    public sealed class SocialDistancePlugin : IActPluginV1
    {
        private Label actStatusLabel;
        private SettingsControl settingsControl;
        private OverlayForm overlay;
        private WarningOverlayForm warningOverlay;
        private FfxivDataSource dataSource;
        private PluginSettings settings;
        private Timer refreshTimer;
        private string settingsPath;
        private DateTime lastPositionSave = DateTime.MinValue;
        private GitHubReleaseClient releaseClient;
        private UpdatePackageManager updatePackageManager;
        private UpdateCheckResult lastUpdateResult;
        private bool updateCheckRunning;

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            actStatusLabel = pluginStatusText;
            pluginScreenSpace.Text = "SocialDistance";

            settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Advanced Combat Tracker", "Config", "SocialDistance.config.xml");
            var legacySettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Advanced Combat Tracker", "Config", "AetherRange.config.xml");
            settings = PluginSettings.Load(File.Exists(settingsPath) ? settingsPath : legacySettingsPath);
            var languageWasUnset = string.IsNullOrWhiteSpace(settings.Language);
            settings.Language = Localization.ResolveInitialLanguage(
                settings.Language, CultureInfo.CurrentUICulture.Name);
            if (settings.EchoToggleText == "AetherRange")
                settings.EchoToggleText = "SocialDistance";
            dataSource = new FfxivDataSource();

            settingsControl = new SettingsControl(settings);
            settingsControl.SettingsChanged += OnSettingsChanged;
            settingsControl.UpdateCheckRequested += OnUpdateCheckRequested;
            settingsControl.UpdateInstallRequested += OnUpdateInstallRequested;
            settingsControl.UpdateLaterRequested += OnUpdateLaterRequested;
            pluginScreenSpace.Controls.Add(settingsControl);
            releaseClient = new GitHubReleaseClient();
            updatePackageManager = new UpdatePackageManager();

            overlay = new OverlayForm
            {
                Location = new System.Drawing.Point(settings.OverlayX, settings.OverlayY),
                ClientSize = new System.Drawing.Size(
                    Math.Max(220, Math.Min(720, settings.OverlayWidth)),
                    Math.Max(110, Math.Min(1000, settings.OverlayHeight))),
                Locked = settings.OverlayLocked,
                AlertDistance = settings.AlertDistance,
                AlertAllPlayers = settings.AlertMode == "all",
                ShowPlayerNames = settings.ShowPlayerNames,
                AnonymousMode = settings.AnonymousMode,
                ShowLinkDistanceColumn = settings.ShowLinkDistanceColumn,
                EnableSpacingAlert = settings.EnableSpacingAlert,
                MaximumRows = settings.MaxRows,
                MaximumDistance = settings.MaxDistance,
                DistanceUnit = settings.DistanceUnit,
                BackgroundOpacityPercent = settings.BackgroundOpacityPercent,
                Language = settings.Language
            };
            overlay.SetOpacityPercent(settings.OpacityPercent);
            overlay.BoundsChangedByUser += OnOverlayBoundsChanged;

            warningOverlay = new WarningOverlayForm
            {
                Location = new System.Drawing.Point(settings.WarningOverlayX, settings.WarningOverlayY),
                ClientSize = new System.Drawing.Size(
                    Math.Max(110, Math.Min(480, settings.WarningOverlayWidth)),
                    Math.Max(40, Math.Min(180, settings.WarningOverlayHeight))),
                Locked = settings.OverlayLocked,
                BackgroundOpacityPercent = settings.BackgroundOpacityPercent,
                Language = settings.Language
            };
            warningOverlay.SetOpacityPercent(settings.OpacityPercent);
            warningOverlay.BoundsChangedByUser += OnWarningOverlayBoundsChanged;

            refreshTimer = new Timer { Interval = 125 };
            refreshTimer.Tick += OnRefresh;
            refreshTimer.Start();
            ActGlobals.oFormActMain.OnLogLineRead += OnLogLineRead;

            ApplyVisibility();
            actStatusLabel.Text = Localization.Text(settings.Language, "Started");
            if (languageWasUnset)
                SaveSettings();
            settingsControl.ShowUpdateResult(null);
            if (settings.CheckUpdatesOnStartup)
                BeginUpdateCheck(false);
        }

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.OnLogLineRead -= OnLogLineRead;
            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Tick -= OnRefresh;
                refreshTimer.Dispose();
                refreshTimer = null;
            }

            SaveSettings();

            if (overlay != null)
            {
                overlay.BoundsChangedByUser -= OnOverlayBoundsChanged;
                overlay.Close();
                overlay.Dispose();
                overlay = null;
            }

            if (warningOverlay != null)
            {
                warningOverlay.BoundsChangedByUser -= OnWarningOverlayBoundsChanged;
                warningOverlay.Close();
                warningOverlay.Dispose();
                warningOverlay = null;
            }

            if (settingsControl != null)
            {
                settingsControl.SettingsChanged -= OnSettingsChanged;
                settingsControl.UpdateCheckRequested -= OnUpdateCheckRequested;
                settingsControl.UpdateInstallRequested -= OnUpdateInstallRequested;
                settingsControl.UpdateLaterRequested -= OnUpdateLaterRequested;
            }

            releaseClient?.Dispose();
            releaseClient = null;
            updatePackageManager?.Dispose();
            updatePackageManager = null;

            if (actStatusLabel != null)
                actStatusLabel.Text = Localization.Text(settings?.Language, "Stopped");
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            if (!settings.OverlayEnabled)
            {
                HideOverlay();
                return;
            }

            var process = dataSource.GetGameProcess();
            var connected = process != null && !process.HasExited;
            settingsControl.SetConnectionStatus(connected,
                settings.Language == "en" ? dataSource.LastError : null);

            if (!connected || (settings.HideWhenFfxivInactive && !IsGameForeground(process)))
            {
                HideOverlay();
                return;
            }

            var readRows = settings.EnableSpacingAlert ? Math.Max(2, settings.MaxRows) : settings.MaxRows;
            var readDistance = settings.EnableSpacingAlert
                ? int.MaxValue
                : (int)Math.Ceiling(ToMeters(settings.MaxDistance, settings.DistanceUnit));
            overlay.SetPlayers(dataSource.ReadPlayers(readRows, readDistance));
            if (!overlay.Visible)
                overlay.Show();
            ApplyWarningVisibility();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            settingsControl.ApplyTo(settings);
            overlay.Locked = settings.OverlayLocked;
            overlay.SetOpacityPercent(settings.OpacityPercent);
            overlay.AlertDistance = settings.AlertDistance;
            overlay.AlertAllPlayers = settings.AlertMode == "all";
            overlay.ShowPlayerNames = settings.ShowPlayerNames;
            overlay.AnonymousMode = settings.AnonymousMode;
            overlay.ShowLinkDistanceColumn = settings.ShowLinkDistanceColumn;
            overlay.EnableSpacingAlert = settings.EnableSpacingAlert;
            overlay.MaximumRows = settings.MaxRows;
            overlay.MaximumDistance = settings.MaxDistance;
            overlay.DistanceUnit = settings.DistanceUnit;
            overlay.BackgroundOpacityPercent = settings.BackgroundOpacityPercent;
            overlay.Language = settings.Language;
            warningOverlay.Locked = settings.OverlayLocked;
            warningOverlay.SetOpacityPercent(settings.OpacityPercent);
            warningOverlay.BackgroundOpacityPercent = settings.BackgroundOpacityPercent;
            warningOverlay.Language = settings.Language;
            settingsControl.ShowUpdateResult(lastUpdateResult);
            ApplyVisibility();
            SaveSettings();
        }

        private void OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (isImport || settings == null || logInfo == null)
                return;

            if (settings.EchoToggleEnabled && !string.IsNullOrWhiteSpace(settings.EchoToggleText))
            {
                string message;
                var echoMatched = EchoCommandParser.TryGetMessage(logInfo.logLine, out message) ||
                                  EchoCommandParser.TryGetMessage(logInfo.originalLogLine, out message);
                if (echoMatched && string.Equals(message.Trim(), settings.EchoToggleText.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    InvokeOnUiThread(ToggleOverlayFromEcho);
                    return;
                }
            }

            if (!settings.GameMessageTriggerEnabled)
                return;

            var showMatched = LogMessageTriggerMatcher.IsMatch(
                logInfo.logLine, logInfo.originalLogLine, settings.GameMessageOnText);
            var hideMatched = LogMessageTriggerMatcher.IsMatch(
                logInfo.logLine, logInfo.originalLogLine, settings.GameMessageOffText);
            if (!showMatched && !hideMatched)
                return;

            var enable = ResolveGameMessageState(showMatched, hideMatched);
            InvokeOnUiThread(delegate { SetOverlayEnabled(enable, "SourceGameMessage"); });
        }

        private void InvokeOnUiThread(Action action)
        {
            if (settingsControl != null && settingsControl.InvokeRequired)
            {
                settingsControl.BeginInvoke(action);
                return;
            }
            action();
        }

        private bool ResolveGameMessageState(bool showMatched, bool hideMatched)
        {
            if (showMatched && hideMatched)
            {
                var showLength = (settings.GameMessageOnText ?? "").Trim().Length;
                var hideLength = (settings.GameMessageOffText ?? "").Trim().Length;
                return showLength > hideLength;
            }
            return showMatched;
        }

        private void ToggleOverlayFromEcho()
        {
            if (settings == null)
                return;
            SetOverlayEnabled(!settings.OverlayEnabled, "SourceEcho");
        }

        private void SetOverlayEnabled(bool enabled, string sourceKey)
        {
            if (settings == null)
                return;
            if (settings.OverlayEnabled == enabled)
                return;

            settings.OverlayEnabled = enabled;
            settingsControl?.SetOverlayEnabled(settings.OverlayEnabled);
            ApplyVisibility();
            SaveSettings();
            if (actStatusLabel != null)
                actStatusLabel.Text = Localization.Text(settings.Language, "OverlayChanged",
                    Localization.Text(settings.Language, settings.OverlayEnabled ? "Enabled" : "Disabled"),
                    Localization.Text(settings.Language, sourceKey));
        }

        private void OnOverlayBoundsChanged(object sender, EventArgs e)
        {
            if (overlay == null || settings == null || settings.OverlayLocked)
                return;

            settings.OverlayX = overlay.Left;
            settings.OverlayY = overlay.Top;
            settings.OverlayWidth = overlay.ClientSize.Width;
            settings.OverlayHeight = overlay.ClientSize.Height;

            if ((DateTime.UtcNow - lastPositionSave).TotalMilliseconds >= 500)
            {
                SaveSettings();
                lastPositionSave = DateTime.UtcNow;
            }
        }

        private void OnWarningOverlayBoundsChanged(object sender, EventArgs e)
        {
            if (warningOverlay == null || settings == null || settings.OverlayLocked)
                return;

            settings.WarningOverlayX = warningOverlay.Left;
            settings.WarningOverlayY = warningOverlay.Top;
            settings.WarningOverlayWidth = warningOverlay.ClientSize.Width;
            settings.WarningOverlayHeight = warningOverlay.ClientSize.Height;

            if ((DateTime.UtcNow - lastPositionSave).TotalMilliseconds >= 500)
            {
                SaveSettings();
                lastPositionSave = DateTime.UtcNow;
            }
        }

        private void ApplyVisibility()
        {
            if (!settings.OverlayEnabled)
                HideOverlay();
            else
                ApplyWarningVisibility();
        }

        private void ApplyWarningVisibility()
        {
            if (warningOverlay == null)
                return;

            var shouldShow = settings.OverlayEnabled && settings.WarningOverlayEnabled &&
                             overlay != null && overlay.Visible && overlay.HasActiveAlert;
            if (shouldShow)
            {
                if (!warningOverlay.Visible)
                    warningOverlay.Show();
            }
            else if (warningOverlay.Visible)
            {
                warningOverlay.Hide();
            }
        }

        private void HideOverlay()
        {
            if (overlay != null && overlay.Visible)
                overlay.Hide();
            if (warningOverlay != null && warningOverlay.Visible)
                warningOverlay.Hide();
        }

        private static bool IsGameForeground(Process process)
        {
            if (process == null || process.HasExited)
                return false;

            return process.MainWindowHandle != IntPtr.Zero &&
                   NativeMethods.GetForegroundWindow() == process.MainWindowHandle;
        }

        private static double ToMeters(double value, string unit)
        {
            return unit == "m" ? value : value * 0.9144d;
        }

        private void OnUpdateCheckRequested(object sender, EventArgs e)
        {
            BeginUpdateCheck(true);
        }

        private async void BeginUpdateCheck(bool manual)
        {
            if (updateCheckRunning || releaseClient == null || settingsControl == null)
                return;
            updateCheckRunning = true;
            settingsControl.ShowUpdateChecking();
            var result = await releaseClient.CheckAsync(
                Assembly.GetExecutingAssembly().GetName().Version);
            updateCheckRunning = false;
            lastUpdateResult = result;
            settings.LastUpdateCheckUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            InvokeOnUiThread(delegate
            {
                if (result.Kind == UpdateCheckKind.Available && !manual &&
                    string.Equals(settings.SkippedUpdateVersion, result.Release.Version.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    settingsControl.ShowUpdateResult(result);
                    settingsControl.ShowUpdateSkipped(result.Release.Version.ToString());
                }
                else
                {
                    settingsControl.ShowUpdateResult(result);
                    if (result.Kind == UpdateCheckKind.Available)
                    {
                        settingsControl.FocusUpdateTab();
                        if (actStatusLabel != null)
                            actStatusLabel.Text = Localization.Text(settings.Language, "UpdateAvailable",
                                result.CurrentVersion, result.Release.Version);
                    }
                }
                SaveSettings();
            });
        }

        private async void OnUpdateInstallRequested(object sender, EventArgs e)
        {
            if (lastUpdateResult == null || lastUpdateResult.Kind != UpdateCheckKind.Available ||
                lastUpdateResult.Release == null || updatePackageManager == null)
                return;
            settingsControl.ShowUpdatePreparing();
            var result = await updatePackageManager.PrepareAndScheduleAsync(
                lastUpdateResult.Release,
                Assembly.GetExecutingAssembly().Location,
                Process.GetCurrentProcess().Id);
            InvokeOnUiThread(delegate
            {
                settingsControl.ShowUpdatePrepared(result.Success, result.Error);
                if (actStatusLabel != null)
                    actStatusLabel.Text = result.Success
                        ? Localization.Text(settings.Language, "UpdatePrepared")
                        : Localization.Text(settings.Language, "UpdatePrepareFailed",
                            string.IsNullOrWhiteSpace(result.Error)
                                ? Localization.Text(settings.Language, "UnknownError")
                                : result.Error);
            });
        }

        private void OnUpdateLaterRequested(object sender, EventArgs e)
        {
            if (lastUpdateResult?.Release?.Version == null)
                return;
            settings.SkippedUpdateVersion = lastUpdateResult.Release.Version.ToString();
            settingsControl.ShowUpdateSkipped(settings.SkippedUpdateVersion);
            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                settings?.Save(settingsPath);
            }
            catch (Exception ex)
            {
                if (actStatusLabel != null)
                    actStatusLabel.Text = Localization.Text(settings?.Language, "SaveFailed", ex.Message);
            }
        }
    }
}
