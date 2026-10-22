using Advanced_Combat_Tracker;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        private System.Windows.Forms.Timer refreshTimer;
        private System.Windows.Forms.Timer settingsSaveTimer;
        private string settingsPath;
        private SettingsPersistenceCoordinator settingsPersistence;
        private GitHubReleaseClient releaseClient;
        private UpdatePackageManager updatePackageManager;
        private UpdateCheckResult lastUpdateResult;
        private bool updateCheckRunning;
        private bool updateInstallRunning;
        private volatile bool acceptingWork;
        private CancellationTokenSource lifecycleCancellation;
        private Task activeUpdateTask = Task.CompletedTask;

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
            var currentSettingsExist = File.Exists(settingsPath);
            var loadPath = currentSettingsExist ? settingsPath : legacySettingsPath;
            bool settingsLoaded;
            settings = PluginSettings.Load(loadPath, out settingsLoaded);
            settingsPersistence = new SettingsPersistenceCoordinator(
                settingsPath, settings, currentSettingsExist && settingsLoaded, DateTime.UtcNow);
            settings.Language = Localization.ResolveInitialLanguage(
                settings.Language, CultureInfo.CurrentUICulture.Name);
            if (settings.EchoToggleText == "AetherRange")
                settings.EchoToggleText = "SocialDistance";
            acceptingWork = true;
            lifecycleCancellation = new CancellationTokenSource();
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

            refreshTimer = new System.Windows.Forms.Timer { Interval = 125 };
            refreshTimer.Tick += OnRefresh;
            refreshTimer.Start();
            settingsSaveTimer = new System.Windows.Forms.Timer
            {
                Interval = (int)SettingsPersistenceCoordinator.DefaultInterval.TotalMilliseconds
            };
            settingsSaveTimer.Tick += OnSettingsSaveTick;
            settingsSaveTimer.Start();
            ActGlobals.oFormActMain.OnLogLineRead += OnLogLineRead;

            ApplyVisibility();
            actStatusLabel.Text = Localization.Text(settings.Language, "Started");
            settingsControl.ShowUpdateResult(null);
            if (settings.CheckUpdatesOnStartup)
                BeginUpdateCheck(false);
        }

        public void DeInitPlugin()
        {
            if (!acceptingWork)
                return;
            acceptingWork = false;

            ActGlobals.oFormActMain.OnLogLineRead -= OnLogLineRead;
            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Tick -= OnRefresh;
                refreshTimer.Dispose();
                refreshTimer = null;
            }

            if (settingsSaveTimer != null)
            {
                settingsSaveTimer.Stop();
                settingsSaveTimer.Tick -= OnSettingsSaveTick;
                settingsSaveTimer.Dispose();
                settingsSaveTimer = null;
            }

            if (settingsControl != null)
            {
                settingsControl.SettingsChanged -= OnSettingsChanged;
                settingsControl.UpdateCheckRequested -= OnUpdateCheckRequested;
                settingsControl.UpdateInstallRequested -= OnUpdateInstallRequested;
                settingsControl.UpdateLaterRequested -= OnUpdateLaterRequested;
            }

            // Both update paths use ConfigureAwait(false) and post UI work without waiting,
            // so waiting here on ACT's UI thread cannot deadlock.
            lifecycleCancellation?.Cancel();
            try
            {
                activeUpdateTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                SetStatus(Localization.Text(settings?.Language, "UpdatePrepareFailed", ex.GetBaseException().Message));
            }

            var finalSave = settingsPersistence?.Flush(settings, DateTime.UtcNow);
            if (finalSave != null && finalSave.State == SettingsSaveState.Failed)
                SetStatus(Localization.Text(settings?.Language, "SaveFailed",
                    finalSave.Error == null ? Localization.Text(settings?.Language, "UnknownError") : finalSave.Error.Message));

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

            releaseClient?.Dispose();
            releaseClient = null;
            updatePackageManager?.Dispose();
            updatePackageManager = null;
            lifecycleCancellation?.Dispose();
            lifecycleCancellation = null;

            if (actStatusLabel != null)
            {
                if (finalSave == null || finalSave.State != SettingsSaveState.Failed)
                    actStatusLabel.Text = Localization.Text(settings?.Language, "Stopped");
            }
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            if (!acceptingWork)
                return;
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
        }

        private void OnSettingsSaveTick(object sender, EventArgs e)
        {
            if (!acceptingWork || settingsPersistence == null || settings == null)
                return;
            var result = settingsPersistence.Poll(settings, DateTime.UtcNow);
            if (result.State == SettingsSaveState.Failed)
                SetStatus(Localization.Text(settings.Language, "SaveFailed",
                    result.Error == null ? Localization.Text(settings.Language, "UnknownError") : result.Error.Message));
        }

        private void OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (!acceptingWork || isImport || settings == null || logInfo == null)
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
            if (!acceptingWork || settingsControl == null || settingsControl.IsDisposed || !settingsControl.IsHandleCreated)
                return;
            if (settingsControl.InvokeRequired)
            {
                try
                {
                    settingsControl.BeginInvoke((Action)delegate
                    {
                        if (acceptingWork)
                            action();
                    });
                }
                catch (ObjectDisposedException)
                {
                }
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
            if (!acceptingWork || settings == null)
                return;
            SetOverlayEnabled(!settings.OverlayEnabled, "SourceEcho");
        }

        private void SetOverlayEnabled(bool enabled, string sourceKey)
        {
            if (!acceptingWork || settings == null)
                return;
            if (settings.OverlayEnabled == enabled)
                return;

            settings.OverlayEnabled = enabled;
            settingsControl?.SetOverlayEnabled(settings.OverlayEnabled);
            ApplyVisibility();
            if (actStatusLabel != null)
                actStatusLabel.Text = Localization.Text(settings.Language, "OverlayChanged",
                    Localization.Text(settings.Language, settings.OverlayEnabled ? "Enabled" : "Disabled"),
                    Localization.Text(settings.Language, sourceKey));
        }

        private void OnOverlayBoundsChanged(object sender, EventArgs e)
        {
            if (!acceptingWork || overlay == null || settings == null || settings.OverlayLocked)
                return;

            settings.OverlayX = overlay.Left;
            settings.OverlayY = overlay.Top;
            settings.OverlayWidth = overlay.ClientSize.Width;
            settings.OverlayHeight = overlay.ClientSize.Height;

        }

        private void OnWarningOverlayBoundsChanged(object sender, EventArgs e)
        {
            if (!acceptingWork || warningOverlay == null || settings == null || settings.OverlayLocked)
                return;

            settings.WarningOverlayX = warningOverlay.Left;
            settings.WarningOverlayY = warningOverlay.Top;
            settings.WarningOverlayWidth = warningOverlay.ClientSize.Width;
            settings.WarningOverlayHeight = warningOverlay.ClientSize.Height;

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

        private void BeginUpdateCheck(bool manual)
        {
            if (!acceptingWork || updateCheckRunning || updateInstallRunning || releaseClient == null || settingsControl == null)
                return;
            updateCheckRunning = true;
            settingsControl.ShowUpdateChecking();
            var token = lifecycleCancellation.Token;
            activeUpdateTask = RunUpdateCheckAsync(manual, token);
        }

        private async Task RunUpdateCheckAsync(bool manual, CancellationToken cancellationToken)
        {
            UpdateCheckResult result;
            try
            {
                result = await releaseClient.CheckAsync(
                    Assembly.GetExecutingAssembly().GetName().Version, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                result = new UpdateCheckResult
                {
                    Kind = UpdateCheckKind.Failed,
                    Error = ex.GetBaseException().Message
                };
            }

            PostToUi(delegate
            {
                updateCheckRunning = false;
                lastUpdateResult = result;
                settings.LastUpdateCheckUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
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
                        SetStatus(Localization.Text(settings.Language, "UpdateAvailable",
                            result.CurrentVersion, result.Release.Version));
                    }
                }
            });
        }

        private void OnUpdateInstallRequested(object sender, EventArgs e)
        {
            if (!acceptingWork || updateCheckRunning || updateInstallRunning || lastUpdateResult == null || lastUpdateResult.Kind != UpdateCheckKind.Available ||
                lastUpdateResult.Release == null || updatePackageManager == null)
                return;
            settingsControl.ShowUpdatePreparing();
            updateInstallRunning = true;
            var token = lifecycleCancellation.Token;
            activeUpdateTask = RunUpdatePreparationAsync(lastUpdateResult.Release, token);
        }

        private async Task RunUpdatePreparationAsync(ReleaseInfo release, CancellationToken cancellationToken)
        {
            UpdatePreparationResult result;
            try
            {
                result = await updatePackageManager.PrepareAsync(release, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                result = new UpdatePreparationResult { Error = ex.GetBaseException().Message };
            }

            PostToUi(delegate
            {
                updateInstallRunning = false;
                settingsControl.ShowUpdatePrepared(result.Success, result.Error, result.StagedPluginPath);
                SetStatus(result.Success
                    ? Localization.Text(settings.Language, "UpdatePrepared", result.StagedPluginPath)
                    : Localization.Text(settings.Language, "UpdatePrepareFailed",
                        string.IsNullOrWhiteSpace(result.Error)
                            ? Localization.Text(settings.Language, "UnknownError")
                            : result.Error));
            });
        }

        private void OnUpdateLaterRequested(object sender, EventArgs e)
        {
            if (!acceptingWork || lastUpdateResult?.Release?.Version == null)
                return;
            settings.SkippedUpdateVersion = lastUpdateResult.Release.Version.ToString();
            settingsControl.ShowUpdateSkipped(settings.SkippedUpdateVersion);
        }

        private void PostToUi(Action action)
        {
            if (!acceptingWork || settingsControl == null || settingsControl.IsDisposed || !settingsControl.IsHandleCreated)
                return;
            if (settingsControl.InvokeRequired)
            {
                try
                {
                    settingsControl.BeginInvoke((Action)delegate
                    {
                        if (acceptingWork)
                            action();
                    });
                }
                catch (ObjectDisposedException)
                {
                }
                return;
            }
            if (acceptingWork)
                action();
        }

        private void SetStatus(string message)
        {
            if (actStatusLabel != null && !actStatusLabel.IsDisposed)
                actStatusLabel.Text = message;
        }
    }
}

