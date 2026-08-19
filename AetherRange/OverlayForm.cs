using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SocialDistance
{
    internal sealed class OverlayForm : Form
    {
        private const int HeaderHeight = 46;
        private const int RowHeight = 40;
        private const int FooterHeight = 10;
        private const int ResizeBorder = 7;
        private readonly Color transparencyColor = Color.FromArgb(1, 2, 3);
        private IList<PlayerDistance> players = new List<PlayerDistance>();
        private bool locked;
        private decimal alertDistance = 5m;
        private bool alertAllPlayers;
        private bool showPlayerNames = true;
        private bool anonymousMode;
        private bool showLinkDistanceColumn;
        private bool enableSpacingAlert;
        private int maximumRows = 12;
        private int maximumDistance = 100;
        private string distanceUnit = "y";
        private int backgroundOpacityPercent = 100;
        private string language = "en";

        public OverlayForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            BackColor = transparencyColor;
            ClientSize = new Size(292, 390);
            MinimumSize = new Size(220, 110);
            MaximumSize = new Size(720, 1000);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            TransparencyKey = transparencyColor;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            UpdateRoundedRegion();
        }

        public event EventHandler BoundsChangedByUser;

        public bool Locked
        {
            get { return locked; }
            set
            {
                if (locked == value)
                    return;
                locked = value;
                RecreateHandle();
                Invalidate();
            }
        }

        public decimal AlertDistance
        {
            get { return alertDistance; }
            set { alertDistance = value; Invalidate(); }
        }

        public bool AlertAllPlayers
        {
            get { return alertAllPlayers; }
            set { alertAllPlayers = value; Invalidate(); }
        }

        public bool ShowPlayerNames
        {
            get { return showPlayerNames; }
            set { showPlayerNames = value; Invalidate(); }
        }

        public bool AnonymousMode
        {
            get { return anonymousMode; }
            set { anonymousMode = value; Invalidate(); }
        }

        public bool ShowLinkDistanceColumn
        {
            get { return showLinkDistanceColumn; }
            set { showLinkDistanceColumn = value; Invalidate(); }
        }

        public bool EnableSpacingAlert
        {
            get { return enableSpacingAlert; }
            set { enableSpacingAlert = value; Invalidate(); }
        }

        public int MaximumRows
        {
            get { return maximumRows; }
            set { maximumRows = Math.Max(1, value); Invalidate(); }
        }

        public int MaximumDistance
        {
            get { return maximumDistance; }
            set { maximumDistance = Math.Max(1, value); Invalidate(); }
        }

        public string DistanceUnit
        {
            get { return distanceUnit; }
            set { distanceUnit = value == "m" ? "m" : "y"; Invalidate(); }
        }

        public int BackgroundOpacityPercent
        {
            get { return backgroundOpacityPercent; }
            set
            {
                backgroundOpacityPercent = Math.Max(0, Math.Min(100, value));
                Invalidate();
            }
        }

        public string Language
        {
            get { return language; }
            set { language = Localization.NormalizeLanguage(value); Invalidate(); }
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= NativeMethods.WsExToolWindow;
                if (locked)
                    parameters.ExStyle |= NativeMethods.WsExTransparent;
                return parameters;
            }
        }

        public void SetOpacityPercent(int percent)
        {
            Opacity = Math.Max(0.35, Math.Min(1.0, percent / 100.0));
        }

        public void SetPlayers(IList<PlayerDistance> newPlayers)
        {
            players = newPlayers ?? new List<PlayerDistance>();
            Invalidate();
        }

        public bool HasActiveAlert
        {
            get
            {
                var displayedPlayers = players
                    .Where(player => DisplayDistance(player.Distance) <= maximumDistance)
                    .Take(maximumRows)
                    .ToList();
                var alertCandidates = alertAllPlayers ? displayedPlayers : displayedPlayers.Take(1);
                var hasDistanceAlert = alertCandidates
                    .Any(player => (decimal)DisplayDistance(player.Distance) < alertDistance);
                var hasSpacingAlert = enableSpacingAlert && players.Count >= 2 &&
                                      players[1].LinkDistance < players[0].Distance;
                return hasDistanceAlert || hasSpacingAlert;
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == NativeMethods.WmNcHitTest && !locked)
            {
                var raw = message.LParam.ToInt64();
                var screenPoint = new Point(unchecked((short)(raw & 0xffff)), unchecked((short)((raw >> 16) & 0xffff)));
                var point = PointToClient(screenPoint);
                var left = point.X <= ResizeBorder;
                var right = point.X >= ClientSize.Width - ResizeBorder;
                var top = point.Y <= ResizeBorder;
                var bottom = point.Y >= ClientSize.Height - ResizeBorder;

                if (left && top) { message.Result = new IntPtr(NativeMethods.HtTopLeft); return; }
                if (right && top) { message.Result = new IntPtr(NativeMethods.HtTopRight); return; }
                if (left && bottom) { message.Result = new IntPtr(NativeMethods.HtBottomLeft); return; }
                if (right && bottom) { message.Result = new IntPtr(NativeMethods.HtBottomRight); return; }
                if (left) { message.Result = new IntPtr(NativeMethods.HtLeft); return; }
                if (right) { message.Result = new IntPtr(NativeMethods.HtRight); return; }
                if (top) { message.Result = new IntPtr(NativeMethods.HtTop); return; }
                if (bottom) { message.Result = new IntPtr(NativeMethods.HtBottom); return; }
            }

            base.WndProc(ref message);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (locked || e.Button != MouseButtons.Left || e.Y > HeaderHeight)
                return;

            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WmNclButtonDown,
                new IntPtr(NativeMethods.HtCaption), IntPtr.Zero);
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (Visible && !locked)
                BoundsChangedByUser?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRoundedRegion();
            if (Visible && !locked)
                BoundsChangedByUser?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var body = new SolidBrush(BackgroundColor(242, 13, 16, 23)))
                FillRoundedRectangle(g, body, new Rectangle(0, 0, Width - 1, Height - 1), 12);

            using (var headerBrush = new LinearGradientBrush(
                new Rectangle(0, 0, Width, HeaderHeight),
                BackgroundColor(250, 38, 34, 30),
                BackgroundColor(246, 18, 20, 27),
                LinearGradientMode.Horizontal))
                FillTopRoundedRectangle(g, headerBrush, new Rectangle(0, 0, Width - 1, HeaderHeight), 12);

            using (var gold = new Pen(BackgroundColor(190, 194, 155, 82)))
            {
                g.DrawLine(gold, 12, HeaderHeight - 1, Width - 12, HeaderHeight - 1);
                g.DrawLine(gold, 15, 9, Width - 15, 9);
            }
            using (var diamond = new SolidBrush(Color.FromArgb(225, 219, 180, 102)))
            {
                var points = new[] { new Point(14, 18), new Point(18, 22), new Point(14, 26), new Point(10, 22) };
                g.FillPolygon(diamond, points);
            }

            var narrow = Width < 270;
            using (var titleFont = new Font("Georgia", narrow ? 8.5f : 9.5f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(239, 221, 178)))
                g.DrawString(Localization.OverlayTitle(language), titleFont, titleBrush, 25, 14);

            var displayedPlayers = players
                .Where(player => DisplayDistance(player.Distance) <= maximumDistance)
                .Take(maximumRows)
                .ToList();
            var visibleCount = Math.Min(displayedPlayers.Count, VisibleRowCapacity());
            var spacingAlert = enableSpacingAlert && players.Count >= 2 &&
                               players[1].LinkDistance < players[0].Distance;
            if (!narrow && !spacingAlert)
            {
                using (var countFont = new Font("Segoe UI", 7.7f))
                using (var countBrush = new SolidBrush(Color.FromArgb(137, 151, 171)))
                {
                    var count = Localization.PlayerCount(language, visibleCount);
                    var size = g.MeasureString(count, countFont);
                    g.DrawString(count, countFont, countBrush, Width - size.Width - 14, 13);
                }
            }

            if (spacingAlert)
            {
                using (var alertFont = new Font("Segoe UI Semibold", 7.2f, FontStyle.Bold))
                using (var alertBrush = new SolidBrush(Color.FromArgb(255, 202, 82)))
                {
                    var label = "DIFF !";
                    var size = g.MeasureString(label, alertFont);
                    g.DrawString(label, alertFont, alertBrush, Width - size.Width - 14, 13);
                }
            }

            using (var columnFont = new Font("Segoe UI Semibold", 6.5f, FontStyle.Bold))
            using (var columnBrush = new SolidBrush(Color.FromArgb(175, 206, 187, 142)))
            {
                var format = new StringFormat { Alignment = StringAlignment.Far };
                var selfColumnX = showLinkDistanceColumn ? Width - 121 : Width - 65;
                g.DrawString("toME", columnFont, columnBrush,
                    new RectangleF(selfColumnX, 29, 52, 14), format);
                if (showLinkDistanceColumn)
                    g.DrawString("DIFF", columnFont, columnBrush,
                        new RectangleF(Width - 65, 29, 52, 14), format);
            }

            for (var index = 0; index < visibleCount; index++)
                DrawPlayerRow(g, displayedPlayers[index], HeaderHeight + (index * RowHeight), index,
                    spacingAlert);

            DrawResizeGrip(g);

            using (var border = new Pen(BackgroundColor(175, 161, 126, 70)))
                DrawRoundedRectangle(g, border, new Rectangle(0, 0, Width - 1, Height - 1), 12);
        }

        private int VisibleRowCapacity()
        {
            return Math.Max(0, (ClientSize.Height - HeaderHeight - FooterHeight) / RowHeight);
        }

        private void DrawPlayerRow(Graphics g, PlayerDistance player, int y, int index, bool spacingAlert)
        {
            var isDistanceAlert = (decimal)DisplayDistance(player.Distance) < alertDistance &&
                                  (alertAllPlayers || index == 0);
            var isSpacingAlert = spacingAlert && index == 1;
            var isAlert = isDistanceAlert || isSpacingAlert;
            var job = JobCatalog.Get(player.JobId);
            var rowColor = isSpacingAlert
                ? Color.FromArgb(242, 174, 48)
                : (isDistanceAlert ? Color.FromArgb(220, 105, 32) : job.Color);
            using (var rowBackground = new LinearGradientBrush(
                new Rectangle(7, y + 2, Width - 14, RowHeight - 4),
                BackgroundColor(isAlert ? 178 : 118, rowColor),
                BackgroundColor(isAlert ? 105 : 58, rowColor),
                LinearGradientMode.Horizontal))
                FillRoundedRectangle(g, rowBackground, new Rectangle(7, y + 2, Width - 14, RowHeight - 4), 6);

            if (isAlert)
            {
                using (var alertBar = new SolidBrush(isSpacingAlert
                    ? BackgroundColor(255, 255, 226, 116)
                    : BackgroundColor(255, 255, 174, 69)))
                    g.FillRectangle(alertBar, 7, y + 8, 3, RowHeight - 16);
            }
            else if (index > 0)
            {
                using (var separator = new Pen(BackgroundColor(40, 230, 218, 188)))
                    g.DrawLine(separator, 14, y, Width - 14, y);
            }

            var iconRect = new Rectangle(14, y + 6, 28, 28);
            using (var iconBackground = new SolidBrush(Color.FromArgb(150, 8, 10, 15)))
                g.FillEllipse(iconBackground, iconRect);

            var icon = JobCatalog.GetIcon(player.JobId);
            if (icon != null)
            {
                g.DrawImage(icon, iconRect);
            }
            else
            {
                using (var iconFont = new Font("Segoe UI", 6.4f, FontStyle.Bold))
                using (var iconText = new SolidBrush(Color.White))
                {
                    var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(job.Abbreviation, iconFont, iconText, iconRect, format);
                }
            }

            var distanceText = DisplayDistance(player.Distance).ToString("0.0") + " " + distanceUnit;
            using (var distanceFont = new Font("Segoe UI Semibold", 9.3f, FontStyle.Bold))
            {
                var selfRect = showLinkDistanceColumn
                    ? new RectangleF(Width - 121, y + 8, 52, 24)
                    : new RectangleF(Width - 65, y + 8, 52, 24);
                var linkRect = new RectangleF(Width - 65, y + 8, 52, 24);
                var nameRight = selfRect.Left - 6;
                var nameWidth = Math.Max(25, nameRight - 50);
                var textColor = Color.FromArgb(250, 249, 242);

                if (showPlayerNames)
                {
                    using (var nameFont = new Font("Segoe UI Semibold", 9f, isAlert ? FontStyle.Bold : FontStyle.Regular))
                    {
                        var nameRect = new RectangleF(50, y + 9, nameWidth, 23);
                        var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                        var displayName = anonymousMode ? "Player " + (index + 1).ToString("00") : player.Name;
                        DrawShadowedString(g, displayName, nameFont, textColor, nameRect, format);
                    }
                }

                var numberFormat = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                if (showLinkDistanceColumn)
                {
                    DrawShadowedString(g, DisplayDistance(player.LinkDistance).ToString("0.0") + " " + distanceUnit,
                        distanceFont,
                        isSpacingAlert ? Color.FromArgb(255, 249, 211) : Color.White,
                        linkRect, numberFormat);
                }
                DrawShadowedString(g, distanceText, distanceFont,
                    isAlert ? Color.FromArgb(255, 244, 195) : Color.White,
                    selfRect, numberFormat);
            }
        }

        private void DrawResizeGrip(Graphics g)
        {
            var points = new[]
            {
                new Point(Width - 28, Height - 1),
                new Point(Width - 1, Height - 28),
                new Point(Width - 1, Height - 1)
            };
            using (var fill = new SolidBrush(Color.FromArgb(58, 214, 190, 132)))
                g.FillPolygon(fill, points);
            using (var pen = new Pen(Color.FromArgb(145, 229, 207, 151), 1f))
            {
                g.DrawLine(pen, Width - 14, Height - 6, Width - 6, Height - 14);
                g.DrawLine(pen, Width - 10, Height - 6, Width - 6, Height - 10);
            }
        }

        private Color BackgroundColor(int alpha, int red, int green, int blue)
        {
            return Color.FromArgb(alpha * backgroundOpacityPercent / 100, red, green, blue);
        }

        private Color BackgroundColor(int alpha, Color color)
        {
            return Color.FromArgb(alpha * backgroundOpacityPercent / 100, color);
        }

        private static void DrawShadowedString(Graphics g, string text, Font font, Color color,
            RectangleF rectangle, StringFormat format)
        {
            using (var shadow = new SolidBrush(Color.FromArgb(210, 0, 0, 0)))
            using (var foreground = new SolidBrush(color))
            {
                var shadowRect = new RectangleF(rectangle.X + 1, rectangle.Y + 1, rectangle.Width, rectangle.Height);
                g.DrawString(text, font, shadow, shadowRect, format);
                g.DrawString(text, font, foreground, rectangle, format);
            }
        }

        private float DisplayDistance(float meters)
        {
            return distanceUnit == "m" ? meters : meters / 0.9144f;
        }

        private static void DrawShadowedString(Graphics g, string text, Font font, Color color, PointF point)
        {
            using (var shadow = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
            using (var foreground = new SolidBrush(color))
            {
                g.DrawString(text, font, shadow, point.X + 1, point.Y + 1);
                g.DrawString(text, font, foreground, point);
            }
        }

        private void UpdateRoundedRegion()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;
            using (var path = RoundedPath(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), 12))
                Region = new Region(path);
        }

        private static GraphicsPath RoundedPath(Rectangle rectangle, int radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void FillRoundedRectangle(Graphics g, Brush brush, Rectangle rectangle, int radius)
        {
            using (var path = RoundedPath(rectangle, radius)) g.FillPath(brush, path);
        }

        private static void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle rectangle, int radius)
        {
            using (var path = RoundedPath(rectangle, radius)) g.DrawPath(pen, path);
        }

        private static void FillTopRoundedRectangle(Graphics g, Brush brush, Rectangle rectangle, int radius)
        {
            var diameter = radius * 2;
            using (var path = new GraphicsPath())
            {
                path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
                path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
                path.AddLine(rectangle.Right, rectangle.Bottom, rectangle.Left, rectangle.Bottom);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }
    }
}
