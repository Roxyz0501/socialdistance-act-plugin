using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace SocialDistance
{
    internal sealed class SettingsControl : UserControl
    {
        private readonly Label headerTitle = Label("");
        private readonly Label headerSubtitle = Label("");
        private readonly TabControl tabs = new TabControl();
        private readonly TabPage displayTab = new TabPage();
        private readonly TabPage distanceTab = new TabPage();
        private readonly TabPage updateTab = new TabPage();
        private readonly TabPage supportTab = new TabPage();

        private readonly CheckBox enabled = Check();
        private readonly CheckBox hideInactive = Check();
        private readonly CheckBox showNames = Check();
        private readonly CheckBox anonymous = Check();
        private readonly CheckBox locked = Check();
        private readonly TrackBar opacity = Slider(35);
        private readonly Label opacityValue = Label("");
        private readonly TrackBar backgroundOpacity = Slider(0);
        private readonly Label backgroundOpacityValue = Label("");
        private readonly Label opacityLabel = Label("");
        private readonly Label backgroundOpacityLabel = Label("");
        private readonly Label echoSection = Label("");
        private readonly CheckBox echoEnabled = Check();
        private readonly Label echoTextLabel = Label("");
        private readonly TextBox echoText = new TextBox { Width = 200, MaxLength = 100 };
        private readonly Label echoExample = Label("");
        private readonly Label gameMessageSection = Label("");
        private readonly CheckBox gameMessageEnabled = Check();
        private readonly Label gameMessageOnLabel = Label("");
        private readonly TextBox gameMessageOnText = new TextBox { Width = 360, MaxLength = 200 };
        private readonly Label gameMessageOffLabel = Label("");
        private readonly TextBox gameMessageOffText = new TextBox { Width = 360, MaxLength = 200 };
        private readonly Label gameMessageHint = Label("");
        private readonly Label moveHint = Label("");
        private readonly Label connectionStatus = Label("");

        private readonly Label languageLabel = Label("");
        private readonly ComboBox language = Combo();
        private readonly Label unitLabel = Label("");
        private readonly ComboBox unit = Combo();
        private readonly CheckBox showDiff = Check();
        private readonly Label diffExplanation = Label("");
        private readonly CheckBox spacingAlert = Check();
        private readonly CheckBox warningOverlay = Check();
        private readonly Label rowsLabel = Label("");
        private readonly NumericUpDown rows;
        private readonly Label maxDistanceLabel = Label("");
        private readonly NumericUpDown maxDistance;
        private readonly Label alertDistanceLabel = Label("");
        private readonly NumericUpDown alertDistance;
        private readonly Label alertModeLabel = Label("");
        private readonly ComboBox alertMode = Combo();
        private readonly Label alertExplanation = Label("");
        private readonly Label supportTitle = Label("");
        private readonly Label supportDescription = Label("");
        private readonly Label supportNoDifference = Label("");
        private readonly Button supportButton = new Button();
        private readonly Label supportUrl = Label("https://ko-fi.com/roxyz0501");
        private readonly Label supportSafety = Label("");
        private readonly Label supportStatus = Label("");
        private readonly Label updateTitle = Label("");
        private readonly Label updateDescription = Label("");
        private readonly CheckBox checkUpdatesOnStartup = Check();
        private readonly Button checkNowButton = new Button();
        private readonly Label currentVersionCaption = Label("");
        private readonly Label currentVersionValue = Label("");
        private readonly Label latestVersionCaption = Label("");
        private readonly Label latestVersionValue = Label("");
        private readonly Label updateStatus = Label("");
        private readonly Label releaseNotesCaption = Label("");
        private readonly TextBox releaseNotes = new TextBox();
        private readonly Button installUpdateButton = new Button();
        private readonly Button laterButton = new Button();
        private readonly Label versionLabel;

        private bool loading = true;
        private string lastDistanceUnit;
        private int hoveredTabIndex = -1;

        public SettingsControl(PluginSettings settings)
        {
            Dock = DockStyle.Fill;
            Font = new Font("Yu Gothic UI", 9f);
            BackColor = Color.FromArgb(244, 247, 251);
            Padding = new Padding(14);

            enabled.Checked = settings.OverlayEnabled;
            hideInactive.Checked = settings.HideWhenFfxivInactive;
            showNames.Checked = settings.ShowPlayerNames;
            anonymous.Checked = settings.AnonymousMode;
            locked.Checked = settings.OverlayLocked;
            opacity.Value = Clamp(settings.OpacityPercent, 35, 100);
            backgroundOpacity.Value = Clamp(settings.BackgroundOpacityPercent, 0, 100);
            opacityValue.Text = opacity.Value + "%";
            backgroundOpacityValue.Text = backgroundOpacity.Value + "%";
            echoEnabled.Checked = settings.EchoToggleEnabled;
            echoText.Text = string.IsNullOrWhiteSpace(settings.EchoToggleText)
                ? "SocialDistance"
                : settings.EchoToggleText;
            gameMessageEnabled.Checked = settings.GameMessageTriggerEnabled;
            gameMessageOnText.Text = settings.GameMessageOnText ?? "";
            gameMessageOffText.Text = settings.GameMessageOffText ?? "";

            language.Items.AddRange(new object[] { "English", "日本語", "简体中文", "한국어" });
            language.SelectedIndex = LanguageIndex(settings.Language);
            unit.Items.AddRange(new object[] { "y", "m" });
            lastDistanceUnit = settings.DistanceUnit == "m" ? "m" : "y";
            unit.SelectedIndex = lastDistanceUnit == "m" ? 1 : 0;
            showDiff.Checked = settings.ShowLinkDistanceColumn;
            spacingAlert.Checked = settings.EnableSpacingAlert;
            warningOverlay.Checked = settings.WarningOverlayEnabled;
            checkUpdatesOnStartup.Checked = settings.CheckUpdatesOnStartup;
            rows = Numeric(settings.MaxRows, 1, 30, 0);
            maxDistance = Numeric(settings.MaxDistance, 5, 500, 0);
            alertDistance = Numeric(settings.AlertDistance, 0.5m, 500m, 1);
            alertMode.Items.AddRange(new object[] { "警告距離内の最も近いプレイヤー", "警告距離内の全プレイヤー" });
            alertMode.SelectedIndex = settings.AlertMode == "all" ? 1 : 0;

            headerTitle.Font = new Font("Yu Gothic UI Semibold", 17f, FontStyle.Bold);
            headerTitle.ForeColor = Color.FromArgb(34, 44, 61);
            headerTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            headerTitle.Location = new Point(18, 9);
            headerSubtitle.Font = new Font("Yu Gothic UI", 8.5f);
            headerSubtitle.ForeColor = Color.FromArgb(103, 116, 137);
            headerSubtitle.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            headerSubtitle.Location = new Point(20, 43);

            tabs.Dock = DockStyle.Fill;
            tabs.Font = new Font("Yu Gothic UI Semibold", 9f);
            tabs.Padding = new Point(18, 6);
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            displayTab.BackColor = Color.White;
            distanceTab.BackColor = Color.White;
            updateTab.BackColor = Color.White;
            supportTab.BackColor = Color.White;
            displayTab.Padding = new Padding(4);
            distanceTab.Padding = new Padding(4);
            updateTab.Padding = new Padding(4);
            supportTab.Padding = new Padding(4);
            displayTab.Controls.Add(BuildDisplayLayout());
            distanceTab.Controls.Add(BuildDistanceLayout());
            updateTab.Controls.Add(BuildUpdateLayout());
            supportTab.Controls.Add(BuildSupportLayout());
            tabs.TabPages.Add(displayTab);
            tabs.TabPages.Add(distanceTab);
            tabs.TabPages.Add(updateTab);
            tabs.TabPages.Add(supportTab);

            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(18, 11, 18, 8),
                Margin = new Padding(0, 0, 0, 10)
            };
            header.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (var pen = new Pen(Color.FromArgb(202, 165, 87), 2f))
                    e.Graphics.DrawLine(pen, 0, header.Height - 2, header.Width, header.Height - 2);
            };
            header.Controls.Add(headerTitle);
            header.Controls.Add(headerSubtitle);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = BackColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(tabs, 0, 1);
            Controls.Add(root);

            versionLabel = Label("v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            versionLabel.Font = new Font("Segoe UI", 8f);
            versionLabel.ForeColor = Color.FromArgb(113, 126, 146);
            versionLabel.BackColor = Color.Transparent;
            Controls.Add(versionLabel);
            PositionVersionLabel();

            opacityValue.ForeColor = Color.FromArgb(71, 84, 103);
            backgroundOpacityValue.ForeColor = Color.FromArgb(71, 84, 103);

            ApplyLanguage(CurrentLanguage);
            WireEvents();
            loading = false;
        }

        public event EventHandler SettingsChanged;
        public event EventHandler UpdateCheckRequested;
        public event EventHandler UpdateInstallRequested;
        public event EventHandler UpdateLaterRequested;
        public string CurrentLanguage => LanguageFromIndex(language.SelectedIndex);
        private string CurrentDistanceUnit => unit.SelectedIndex == 1 ? "m" : "y";

        public void ApplyTo(PluginSettings settings)
        {
            settings.OverlayEnabled = enabled.Checked;
            settings.HideWhenFfxivInactive = hideInactive.Checked;
            settings.ShowPlayerNames = showNames.Checked;
            settings.AnonymousMode = anonymous.Checked;
            settings.OverlayLocked = locked.Checked;
            settings.OpacityPercent = opacity.Value;
            settings.BackgroundOpacityPercent = backgroundOpacity.Value;
            settings.EchoToggleEnabled = echoEnabled.Checked;
            settings.EchoToggleText = echoText.Text.Trim();
            settings.GameMessageTriggerEnabled = gameMessageEnabled.Checked;
            settings.GameMessageOnText = gameMessageOnText.Text.Trim();
            settings.GameMessageOffText = gameMessageOffText.Text.Trim();
            settings.Language = CurrentLanguage;
            settings.DistanceUnit = CurrentDistanceUnit;
            settings.ShowLinkDistanceColumn = showDiff.Checked;
            settings.EnableSpacingAlert = spacingAlert.Checked;
            settings.WarningOverlayEnabled = warningOverlay.Checked;
            settings.CheckUpdatesOnStartup = checkUpdatesOnStartup.Checked;
            settings.MaxRows = (int)rows.Value;
            settings.MaxDistance = (int)maxDistance.Value;
            settings.AlertDistance = alertDistance.Value;
            settings.AlertMode = alertMode.SelectedIndex == 1 ? "all" : "nearest";
        }

        public void SetOverlayEnabled(bool value)
        {
            loading = true;
            enabled.Checked = value;
            loading = false;
        }

        public void SetConnectionStatus(bool connected, string detail)
        {
            connectionStatus.ForeColor = connected
                ? Color.FromArgb(17, 108, 75)
                : Color.FromArgb(176, 65, 56);
            connectionStatus.Text = connected
                ? "●  " + Localization.Connected(CurrentLanguage)
                : "●  " + (string.IsNullOrWhiteSpace(detail) ? Localization.Waiting(CurrentLanguage) : detail);
        }

        public void ShowUpdateChecking()
        {
            updateStatus.ForeColor = Color.FromArgb(73, 96, 128);
            updateStatus.Text = Localization.Text(CurrentLanguage, "UpdateChecking");
            checkNowButton.Enabled = false;
            installUpdateButton.Enabled = false;
            laterButton.Enabled = false;
        }

        public void ShowUpdateResult(UpdateCheckResult result)
        {
            checkNowButton.Enabled = true;
            installUpdateButton.Enabled = result != null && result.Kind == UpdateCheckKind.Available;
            laterButton.Enabled = installUpdateButton.Enabled;
            currentVersionValue.Text = result?.CurrentVersion?.ToString() ??
                                       Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            latestVersionValue.Text = result?.Release?.Version?.ToString() ?? "—";
            releaseNotes.Text = string.IsNullOrWhiteSpace(result?.Release?.Notes)
                ? Localization.Text(CurrentLanguage, "NoReleaseNotes")
                : result.Release.Notes.Trim();
            updateStatus.ForeColor = Color.FromArgb(73, 96, 128);
            if (result == null)
            {
                updateStatus.Text = Localization.Text(CurrentLanguage, "NotChecked");
                return;
            }
            switch (result.Kind)
            {
                case UpdateCheckKind.RepositoryMissing:
                    updateStatus.ForeColor = Color.FromArgb(151, 91, 29);
                    updateStatus.Text = Localization.Text(CurrentLanguage, "RepositoryMissing");
                    break;
                case UpdateCheckKind.UpToDate:
                    updateStatus.ForeColor = Color.FromArgb(17, 108, 75);
                    updateStatus.Text = Localization.Text(CurrentLanguage, "UpdateUpToDate");
                    break;
                case UpdateCheckKind.Available:
                    updateStatus.ForeColor = Color.FromArgb(176, 65, 56);
                    updateStatus.Text = Localization.Text(CurrentLanguage, "UpdateAvailable",
                        currentVersionValue.Text, latestVersionValue.Text);
                    break;
                default:
                    updateStatus.ForeColor = Color.FromArgb(176, 65, 56);
                    updateStatus.Text = Localization.Text(CurrentLanguage, "UpdateFailed",
                        string.IsNullOrWhiteSpace(result.Error)
                            ? Localization.Text(CurrentLanguage, "UnknownError")
                            : result.Error);
                    break;
            }
        }

        public void ShowUpdatePreparing()
        {
            updateStatus.ForeColor = Color.FromArgb(73, 96, 128);
            updateStatus.Text = Localization.Text(CurrentLanguage, "UpdateDownloading");
            checkNowButton.Enabled = installUpdateButton.Enabled = laterButton.Enabled = false;
        }

        public void ShowUpdatePrepared(bool success, string error)
        {
            checkNowButton.Enabled = true;
            installUpdateButton.Enabled = !success;
            laterButton.Enabled = !success;
            updateStatus.ForeColor = success ? Color.FromArgb(17, 108, 75) : Color.FromArgb(176, 65, 56);
            updateStatus.Text = success
                ? Localization.Text(CurrentLanguage, "UpdatePrepared")
                : Localization.Text(CurrentLanguage, "UpdatePrepareFailed",
                    string.IsNullOrWhiteSpace(error) ? Localization.Text(CurrentLanguage, "UnknownError") : error);
        }

        public void ShowUpdateSkipped(string version)
        {
            installUpdateButton.Enabled = false;
            laterButton.Enabled = false;
            updateStatus.ForeColor = Color.FromArgb(73, 96, 128);
            updateStatus.Text = Localization.Text(CurrentLanguage, "UpdateSkipped", version);
        }

        public void FocusUpdateTab()
        {
            tabs.SelectedTab = updateTab;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PositionVersionLabel();
        }

        private Control BuildDisplayLayout()
        {
            var layout = BaseLayout(18);
            AddWide(layout, enabled, 0);
            AddWide(layout, hideInactive, 1);
            AddWide(layout, showNames, 2);
            AddWide(layout, anonymous, 3);
            AddWide(layout, locked, 4);
            layout.Controls.Add(opacityLabel, 0, 5);
            layout.Controls.Add(SliderPanel(opacity, opacityValue), 1, 5);
            layout.Controls.Add(backgroundOpacityLabel, 0, 6);
            layout.Controls.Add(SliderPanel(backgroundOpacity, backgroundOpacityValue), 1, 6);

            echoSection.Font = new Font("Yu Gothic UI Semibold", 10f, FontStyle.Bold);
            echoSection.ForeColor = Color.FromArgb(48, 61, 80);
            echoSection.Padding = new Padding(0, 8, 0, 0);
            AddWide(layout, echoSection, 7);
            AddWide(layout, echoEnabled, 8);
            layout.Controls.Add(echoTextLabel, 0, 9);
            var echoPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = Padding.Empty
            };
            echoPanel.Controls.Add(echoText);
            echoExample.Margin = new Padding(10, 5, 0, 0);
            echoPanel.Controls.Add(echoExample);
            layout.Controls.Add(echoPanel, 1, 9);

            gameMessageSection.Font = new Font("Yu Gothic UI Semibold", 10f, FontStyle.Bold);
            gameMessageSection.ForeColor = Color.FromArgb(48, 61, 80);
            gameMessageSection.Padding = new Padding(0, 8, 0, 0);
            AddWide(layout, gameMessageSection, 10);
            AddWide(layout, gameMessageEnabled, 11);
            layout.Controls.Add(gameMessageOnLabel, 0, 12);
            layout.Controls.Add(gameMessageOnText, 1, 12);
            layout.Controls.Add(gameMessageOffLabel, 0, 13);
            layout.Controls.Add(gameMessageOffText, 1, 13);
            gameMessageHint.AutoSize = false;
            gameMessageHint.Dock = DockStyle.Fill;
            gameMessageHint.ForeColor = Color.FromArgb(73, 96, 128);
            AddWide(layout, gameMessageHint, 14);

            moveHint.AutoSize = false;
            moveHint.Dock = DockStyle.Fill;
            moveHint.ForeColor = Color.FromArgb(73, 78, 88);
            moveHint.Padding = new Padding(0, 8, 0, 0);
            AddWide(layout, moveHint, 15);
            connectionStatus.AutoSize = false;
            connectionStatus.Dock = DockStyle.Fill;
            AddWide(layout, connectionStatus, 16);

            for (var i = 0; i <= 4; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return layout;
        }

        private Control BuildDistanceLayout()
        {
            var layout = BaseLayout(12);
            layout.Controls.Add(languageLabel, 0, 0);
            layout.Controls.Add(language, 1, 0);
            layout.Controls.Add(unitLabel, 0, 1);
            layout.Controls.Add(unit, 1, 1);
            AddWide(layout, showDiff, 2);
            diffExplanation.AutoSize = false;
            diffExplanation.Dock = DockStyle.Fill;
            diffExplanation.ForeColor = Color.FromArgb(73, 96, 128);
            diffExplanation.Padding = new Padding(18, 2, 0, 0);
            AddWide(layout, diffExplanation, 3);
            AddWide(layout, spacingAlert, 4);
            alertExplanation.AutoSize = false;
            alertExplanation.Dock = DockStyle.Fill;
            alertExplanation.ForeColor = Color.FromArgb(151, 91, 29);
            alertExplanation.Padding = new Padding(18, 2, 0, 0);
            AddWide(layout, alertExplanation, 5);
            AddWide(layout, warningOverlay, 6);
            layout.Controls.Add(rowsLabel, 0, 7);
            layout.Controls.Add(rows, 1, 7);
            layout.Controls.Add(maxDistanceLabel, 0, 8);
            layout.Controls.Add(maxDistance, 1, 8);
            layout.Controls.Add(alertDistanceLabel, 0, 9);
            layout.Controls.Add(alertDistance, 1, 9);
            layout.Controls.Add(alertModeLabel, 0, 10);
            layout.Controls.Add(alertMode, 1, 10);

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            for (var i = 7; i <= 10; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return layout;
        }

        private Control BuildUpdateLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 9,
                Padding = new Padding(24),
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

            updateTitle.AutoSize = false;
            updateTitle.Dock = DockStyle.Fill;
            updateTitle.Font = new Font("Yu Gothic UI Semibold", 16f, FontStyle.Bold);
            updateTitle.ForeColor = Color.FromArgb(34, 44, 61);
            AddWide(layout, updateTitle, 0);
            updateDescription.AutoSize = false;
            updateDescription.Dock = DockStyle.Fill;
            updateDescription.ForeColor = Color.FromArgb(73, 96, 128);
            AddWide(layout, updateDescription, 1);
            layout.Controls.Add(checkUpdatesOnStartup, 0, 2);

            checkNowButton.Width = 150;
            checkNowButton.Height = 30;
            checkNowButton.Anchor = AnchorStyles.Left;
            checkNowButton.FlatStyle = FlatStyle.Flat;
            checkNowButton.BackColor = Color.FromArgb(244, 247, 251);
            layout.Controls.Add(checkNowButton, 1, 2);

            layout.Controls.Add(currentVersionCaption, 0, 3);
            currentVersionValue.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            currentVersionValue.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            layout.Controls.Add(currentVersionValue, 1, 3);
            layout.Controls.Add(latestVersionCaption, 0, 4);
            latestVersionValue.Text = "—";
            latestVersionValue.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            layout.Controls.Add(latestVersionValue, 1, 4);

            updateStatus.AutoSize = false;
            updateStatus.Dock = DockStyle.Fill;
            AddWide(layout, updateStatus, 5);
            AddWide(layout, releaseNotesCaption, 6);
            releaseNotes.Multiline = true;
            releaseNotes.ReadOnly = true;
            releaseNotes.ScrollBars = ScrollBars.Vertical;
            releaseNotes.Dock = DockStyle.Fill;
            releaseNotes.BackColor = Color.FromArgb(250, 251, 252);
            layout.Controls.Add(releaseNotes, 0, 7);
            layout.SetColumnSpan(releaseNotes, 2);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };
            installUpdateButton.Width = 250;
            installUpdateButton.Height = 40;
            installUpdateButton.FlatStyle = FlatStyle.Flat;
            installUpdateButton.BackColor = Color.FromArgb(202, 101, 20);
            installUpdateButton.ForeColor = Color.White;
            installUpdateButton.Font = new Font("Yu Gothic UI Semibold", 10f, FontStyle.Bold);
            installUpdateButton.Enabled = false;
            laterButton.Width = 110;
            laterButton.Height = 40;
            laterButton.FlatStyle = FlatStyle.Flat;
            laterButton.Enabled = false;
            buttons.Controls.Add(installUpdateButton);
            buttons.Controls.Add(laterButton);
            layout.Controls.Add(buttons, 0, 8);
            layout.SetColumnSpan(buttons, 2);
            return layout;
        }

        private Control BuildSupportLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                Padding = new Padding(28, 24, 28, 20),
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            supportTitle.AutoSize = false;
            supportTitle.Dock = DockStyle.Fill;
            supportTitle.Font = new Font("Yu Gothic UI Semibold", 17f, FontStyle.Bold);
            supportTitle.ForeColor = Color.FromArgb(166, 83, 15);
            supportTitle.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(supportTitle, 0, 0);

            var divider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 2,
                BackColor = Color.FromArgb(218, 153, 50),
                Margin = new Padding(0, 3, 0, 7)
            };
            layout.Controls.Add(divider, 0, 1);

            supportDescription.AutoSize = false;
            supportDescription.Dock = DockStyle.Fill;
            supportDescription.Font = new Font("Yu Gothic UI", 10f);
            supportDescription.ForeColor = Color.FromArgb(48, 61, 80);
            layout.Controls.Add(supportDescription, 0, 2);

            supportNoDifference.AutoSize = false;
            supportNoDifference.Dock = DockStyle.Fill;
            supportNoDifference.Font = new Font("Yu Gothic UI Semibold", 9.5f, FontStyle.Bold);
            supportNoDifference.ForeColor = Color.FromArgb(125, 72, 20);
            layout.Controls.Add(supportNoDifference, 0, 3);

            supportButton.Width = 360;
            supportButton.Height = 50;
            supportButton.Anchor = AnchorStyles.Left;
            supportButton.FlatStyle = FlatStyle.Flat;
            supportButton.FlatAppearance.BorderSize = 1;
            supportButton.FlatAppearance.BorderColor = Color.FromArgb(164, 79, 12);
            supportButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(224, 126, 31);
            supportButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(174, 83, 13);
            supportButton.BackColor = Color.FromArgb(202, 101, 20);
            supportButton.ForeColor = Color.White;
            supportButton.Font = new Font("Yu Gothic UI Semibold", 11f, FontStyle.Bold);
            supportButton.Cursor = Cursors.Hand;
            supportButton.Margin = new Padding(0, 8, 0, 8);
            layout.Controls.Add(supportButton, 0, 4);

            supportUrl.ForeColor = Color.FromArgb(140, 91, 38);
            supportUrl.Anchor = AnchorStyles.Left;
            layout.Controls.Add(supportUrl, 0, 5);

            supportSafety.AutoSize = false;
            supportSafety.Dock = DockStyle.Fill;
            supportSafety.ForeColor = Color.FromArgb(73, 96, 128);
            layout.Controls.Add(supportSafety, 0, 6);

            supportStatus.AutoSize = false;
            supportStatus.Dock = DockStyle.Fill;
            supportStatus.Font = new Font("Yu Gothic UI Semibold", 9f, FontStyle.Bold);
            layout.Controls.Add(supportStatus, 0, 7);
            return layout;
        }

        private void WireEvents()
        {
            enabled.CheckedChanged += Changed;
            hideInactive.CheckedChanged += Changed;
            showNames.CheckedChanged += Changed;
            anonymous.CheckedChanged += Changed;
            locked.CheckedChanged += Changed;
            opacity.ValueChanged += delegate { opacityValue.Text = opacity.Value + "%"; Changed(this, EventArgs.Empty); };
            backgroundOpacity.ValueChanged += delegate { backgroundOpacityValue.Text = backgroundOpacity.Value + "%"; Changed(this, EventArgs.Empty); };
            echoEnabled.CheckedChanged += Changed;
            echoText.TextChanged += delegate { UpdateEchoExample(); Changed(this, EventArgs.Empty); };
            gameMessageEnabled.CheckedChanged += Changed;
            gameMessageOnText.TextChanged += Changed;
            gameMessageOffText.TextChanged += Changed;
            language.SelectedIndexChanged += delegate { ApplyLanguage(CurrentLanguage); Changed(this, EventArgs.Empty); };
            unit.SelectedIndexChanged += OnUnitChanged;
            showDiff.CheckedChanged += Changed;
            spacingAlert.CheckedChanged += Changed;
            warningOverlay.CheckedChanged += Changed;
            rows.ValueChanged += Changed;
            maxDistance.ValueChanged += Changed;
            alertDistance.ValueChanged += Changed;
            alertMode.SelectedIndexChanged += Changed;
            checkUpdatesOnStartup.CheckedChanged += Changed;
            checkNowButton.Click += delegate { UpdateCheckRequested?.Invoke(this, EventArgs.Empty); };
            installUpdateButton.Click += delegate { UpdateInstallRequested?.Invoke(this, EventArgs.Empty); };
            laterButton.Click += delegate { UpdateLaterRequested?.Invoke(this, EventArgs.Empty); };
            supportButton.Click += OpenSupportLink;
            tabs.DrawItem += DrawTab;
            tabs.MouseMove += OnTabsMouseMove;
            tabs.MouseLeave += delegate
            {
                if (hoveredTabIndex < 0) return;
                hoveredTabIndex = -1;
                tabs.Invalidate();
            };
        }

        private void ApplyLanguage(string languageCode)
        {
            languageCode = Localization.NormalizeLanguage(languageCode);
            headerTitle.Text = "SocialDistance";
            headerSubtitle.Text = Localization.Text(languageCode, "HeaderSubtitle");
            displayTab.Text = Localization.Text(languageCode, "TabDisplay");
            distanceTab.Text = Localization.Text(languageCode, "TabDistance");
            updateTab.Text = Localization.Text(languageCode, "TabUpdate");
            supportTab.Text = Localization.Text(languageCode, "TabSupport");
            enabled.Text = Localization.Text(languageCode, "ShowOverlay");
            hideInactive.Text = Localization.Text(languageCode, "HideInactive");
            showNames.Text = Localization.Text(languageCode, "ShowNames");
            anonymous.Text = Localization.Text(languageCode, "Anonymous");
            locked.Text = Localization.Text(languageCode, "LockOverlay");
            opacityLabel.Text = Localization.Text(languageCode, "OverlayOpacity");
            backgroundOpacityLabel.Text = Localization.Text(languageCode, "BackgroundOpacity");
            echoSection.Text = Localization.Text(languageCode, "EchoSection");
            echoEnabled.Text = Localization.Text(languageCode, "EchoEnabled");
            echoTextLabel.Text = Localization.Text(languageCode, "EchoText");
            gameMessageSection.Text = Localization.Text(languageCode, "GameMessageSection");
            gameMessageEnabled.Text = Localization.Text(languageCode, "GameMessageEnabled");
            gameMessageOnLabel.Text = Localization.Text(languageCode, "GameMessageOn");
            gameMessageOffLabel.Text = Localization.Text(languageCode, "GameMessageOff");
            gameMessageHint.Text = Localization.Text(languageCode, "GameMessageHint");
            moveHint.Text = Localization.Text(languageCode, "MoveHint");
            languageLabel.Text = Localization.Text(languageCode, "Language");
            unitLabel.Text = Localization.Text(languageCode, "DistanceUnit");
            showDiff.Text = Localization.Text(languageCode, "ShowDiff");
            diffExplanation.Text = Localization.Text(languageCode, "DiffExplanation");
            spacingAlert.Text = Localization.Text(languageCode, "SpacingAlert");
            warningOverlay.Text = Localization.Text(languageCode, "WarningOverlay");
            alertExplanation.Text = Localization.Text(languageCode, "AlertExplanation");
            rowsLabel.Text = Localization.Text(languageCode, "MaxPlayers");
            maxDistanceLabel.Text = Localization.Text(languageCode, "MaxDistance", CurrentDistanceUnit);
            alertDistanceLabel.Text = Localization.Text(languageCode, "AlertDistance", CurrentDistanceUnit);
            alertModeLabel.Text = Localization.Text(languageCode, "AlertTarget");
            supportTitle.Text = Localization.Text(languageCode, "SupportTitle");
            supportDescription.Text = Localization.Text(languageCode, "SupportDescription");
            supportNoDifference.Text = Localization.Text(languageCode, "SupportOptional");
            supportButton.Text = Localization.Text(languageCode, "SupportButton");
            supportSafety.Text = Localization.Text(languageCode, "SupportSafety");
            supportStatus.Text = "";
            updateTitle.Text = Localization.Text(languageCode, "UpdateTitle");
            updateDescription.Text = Localization.Text(languageCode, "UpdateDescription");
            checkUpdatesOnStartup.Text = Localization.Text(languageCode, "CheckAtStartup");
            checkNowButton.Text = Localization.Text(languageCode, "CheckNow");
            currentVersionCaption.Text = Localization.Text(languageCode, "CurrentVersion");
            latestVersionCaption.Text = Localization.Text(languageCode, "LatestVersion");
            releaseNotesCaption.Text = Localization.Text(languageCode, "ReleaseNotes");
            installUpdateButton.Text = Localization.Text(languageCode, "UpdateButton");
            laterButton.Text = Localization.Text(languageCode, "LaterButton");
            if (string.IsNullOrWhiteSpace(updateStatus.Text))
                updateStatus.Text = Localization.Text(languageCode, "NotChecked");
            if (string.IsNullOrWhiteSpace(releaseNotes.Text))
                releaseNotes.Text = Localization.Text(languageCode, "NoReleaseNotes");

            var selected = Math.Max(0, alertMode.SelectedIndex);
            loading = true;
            alertMode.Items.Clear();
            alertMode.Items.AddRange(new object[]
            {
                Localization.Text(languageCode, "NearestInRange"),
                Localization.Text(languageCode, "AllInRange")
            });
            alertMode.SelectedIndex = selected;
            loading = false;
            UpdateEchoExample();
            tabs.Invalidate();
        }

        private void OpenSupportLink(object sender, EventArgs e)
        {
            string error;
            if (ExternalLinkLauncher.TryOpen("https://ko-fi.com/roxyz0501", out error))
            {
                supportStatus.ForeColor = Color.FromArgb(17, 108, 75);
                supportStatus.Text = Localization.Text(CurrentLanguage, "SupportOpened");
                return;
            }

            supportStatus.ForeColor = Color.FromArgb(176, 65, 56);
            supportStatus.Text = Localization.Text(CurrentLanguage, "SupportFailed",
                string.Equals(error, "Invalid web address.", StringComparison.Ordinal)
                    ? Localization.Text(CurrentLanguage, "InvalidAddress")
                    : error);
        }

        private void OnTabsMouseMove(object sender, MouseEventArgs e)
        {
            var index = -1;
            for (var i = 0; i < tabs.TabCount; i++)
            {
                if (!tabs.GetTabRect(i).Contains(e.Location)) continue;
                index = i;
                break;
            }
            if (hoveredTabIndex == index) return;
            hoveredTabIndex = index;
            tabs.Invalidate();
        }

        private void DrawTab(object sender, DrawItemEventArgs e)
        {
            var isSelected = e.Index == tabs.SelectedIndex;
            var isHovered = e.Index == hoveredTabIndex;
            var isSupport = tabs.TabPages[e.Index] == supportTab;
            var background = isSelected ? Color.White : Color.FromArgb(240, 242, 245);
            var foreground = Color.FromArgb(42, 53, 69);

            if (isSupport)
            {
                background = isSelected
                    ? Color.FromArgb(255, 245, 226)
                    : (isHovered ? Color.FromArgb(255, 238, 207) : Color.FromArgb(250, 231, 197));
                foreground = isSelected
                    ? Color.FromArgb(137, 62, 7)
                    : (isHovered ? Color.FromArgb(157, 72, 8) : Color.FromArgb(174, 84, 13));
            }
            else if (isHovered && !isSelected)
            {
                background = Color.FromArgb(247, 248, 250);
            }

            using (var brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds,
                foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            if (isSupport && isSelected)
            {
                using (var pen = new Pen(Color.FromArgb(213, 137, 32), 2f))
                    e.Graphics.DrawLine(pen, e.Bounds.Left + 4, e.Bounds.Bottom - 2,
                        e.Bounds.Right - 4, e.Bounds.Bottom - 2);
            }
        }

        private void UpdateEchoExample()
        {
            var value = string.IsNullOrWhiteSpace(echoText.Text) ? "SocialDistance" : echoText.Text;
            echoExample.Text = Localization.Text(CurrentLanguage, "EchoExample", value);
        }

        private void OnUnitChanged(object sender, EventArgs e)
        {
            if (loading) return;
            var newUnit = CurrentDistanceUnit;
            if (newUnit == lastDistanceUnit) return;
            loading = true;
            var factor = (lastDistanceUnit == "y" ? 0.9144m : 1m) *
                         (newUnit == "y" ? 1m / 0.9144m : 1m);
            maxDistance.Value = Bound(maxDistance, Math.Round(maxDistance.Value * factor, 0, MidpointRounding.AwayFromZero));
            alertDistance.Value = Bound(alertDistance, Math.Round(alertDistance.Value * factor, 1, MidpointRounding.AwayFromZero));
            lastDistanceUnit = newUnit;
            loading = false;
            ApplyLanguage(CurrentLanguage);
            Changed(this, EventArgs.Empty);
        }

        private void Changed(object sender, EventArgs e)
        {
            if (!loading) SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PositionVersionLabel()
        {
            if (versionLabel == null) return;
            versionLabel.Location = new Point(
                Math.Max(8, ClientSize.Width - versionLabel.PreferredWidth - 12),
                Math.Max(8, ClientSize.Height - versionLabel.PreferredHeight - 8));
            versionLabel.BringToFront();
        }

        private static TableLayoutPanel BaseLayout(int rows)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = rows,
                Padding = new Padding(12),
                AutoScroll = true,
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return layout;
        }

        private static void AddWide(TableLayoutPanel layout, Control control, int row)
        {
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }

        private static Control SliderPanel(TrackBar slider, Label value)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };
            slider.Width = 185;
            slider.Height = 36;
            value.Margin = new Padding(8, 8, 0, 0);
            panel.Controls.Add(slider);
            panel.Controls.Add(value);
            return panel;
        }

        private static CheckBox Check() => new CheckBox { AutoSize = true, ForeColor = Color.FromArgb(48, 61, 80) };
        private static Label Label(string text) => new Label { AutoSize = true, Text = text, ForeColor = Color.FromArgb(45, 58, 77), Anchor = AnchorStyles.Left };
        private static ComboBox Combo() => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Anchor = AnchorStyles.Left };
        private static TrackBar Slider(int minimum) => new TrackBar { Minimum = minimum, Maximum = 100, TickFrequency = 5, AutoSize = false, Height = 36 };
        private static NumericUpDown Numeric(decimal value, decimal min, decimal max, int decimals) => new NumericUpDown
        {
            DecimalPlaces = decimals,
            Increment = decimals == 0 ? 1m : 0.5m,
            Minimum = min,
            Maximum = max,
            Value = Math.Max(min, Math.Min(max, value)),
            Width = 90,
            Anchor = AnchorStyles.Left
        };
        private static decimal Bound(NumericUpDown control, decimal value) => Math.Max(control.Minimum, Math.Min(control.Maximum, value));
        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        private static int LanguageIndex(string value)
        {
            switch (Localization.NormalizeLanguage(value))
            {
                case Localization.Japanese: return 1;
                case Localization.SimplifiedChinese: return 2;
                case Localization.Korean: return 3;
                default: return 0;
            }
        }

        private static string LanguageFromIndex(int index)
        {
            switch (index)
            {
                case 1: return Localization.Japanese;
                case 2: return Localization.SimplifiedChinese;
                case 3: return Localization.Korean;
                default: return Localization.English;
            }
        }
    }
}
