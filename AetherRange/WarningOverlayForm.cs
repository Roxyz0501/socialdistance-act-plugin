using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SocialDistance
{
    internal sealed class WarningOverlayForm : Form
    {
        private const int ResizeBorder = 7;
        private readonly Color transparencyColor = Color.FromArgb(1, 2, 3);
        private bool locked;
        private int backgroundOpacityPercent = 100;
        private string language = "en";

        public WarningOverlayForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            BackColor = transparencyColor;
            ClientSize = new Size(160, 58);
            MinimumSize = new Size(110, 40);
            MaximumSize = new Size(480, 180);
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
                if (locked == value) return;
                locked = value;
                RecreateHandle();
                Invalidate();
            }
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
                if (locked) parameters.ExStyle |= NativeMethods.WsExTransparent;
                return parameters;
            }
        }

        public void SetOpacityPercent(int percent)
        {
            Opacity = Math.Max(0.35, Math.Min(1.0, percent / 100.0));
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
            if (locked || e.Button != MouseButtons.Left) return;
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WmNclButtonDown,
                new IntPtr(NativeMethods.HtCaption), IntPtr.Zero);
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (Visible && !locked) BoundsChangedByUser?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRoundedRegion();
            if (Visible && !locked) BoundsChangedByUser?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var background = new LinearGradientBrush(
                ClientRectangle,
                BackgroundColor(238, 178, 73, 24),
                BackgroundColor(220, 104, 38, 24),
                LinearGradientMode.Horizontal))
                FillRoundedRectangle(g, background, new Rectangle(0, 0, Width - 1, Height - 1), 10);

            using (var border = new Pen(BackgroundColor(245, 255, 190, 83), 1.5f))
                DrawRoundedRectangle(g, border, new Rectangle(1, 1, Width - 3, Height - 3), 9);

            var fontSize = Math.Max(10f, Math.Min(24f, ClientSize.Height * 0.32f));
            using (var font = new Font("Yu Gothic UI Semibold", fontSize, FontStyle.Bold))
            using (var shadow = new SolidBrush(Color.FromArgb(220, 42, 16, 4)))
            using (var text = new SolidBrush(Color.FromArgb(255, 249, 224)))
            {
                var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var area = new RectangleF(5, 3, Width - 10, Height - 6);
                var shadowArea = new RectangleF(area.X + 1, area.Y + 1, area.Width, area.Height);
                g.DrawString(Localization.WarningText(language), font, shadow, shadowArea, format);
                g.DrawString(Localization.WarningText(language), font, text, area, format);
            }

            DrawResizeGrip(g);
        }

        private void DrawResizeGrip(Graphics g)
        {
            var points = new[]
            {
                new Point(Width - 24, Height - 1),
                new Point(Width - 1, Height - 24),
                new Point(Width - 1, Height - 1)
            };
            using (var fill = new SolidBrush(Color.FromArgb(68, 255, 224, 153)))
                g.FillPolygon(fill, points);
            using (var pen = new Pen(Color.FromArgb(170, 255, 235, 184)))
                g.DrawLine(pen, Width - 12, Height - 5, Width - 5, Height - 12);
        }

        private Color BackgroundColor(int alpha, int red, int green, int blue)
        {
            return Color.FromArgb(alpha * backgroundOpacityPercent / 100, red, green, blue);
        }

        private void UpdateRoundedRegion()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            using (var path = RoundedPath(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), 10))
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

        private static void FillRoundedRectangle(Graphics graphics, Brush brush, Rectangle rectangle, int radius)
        {
            using (var path = RoundedPath(rectangle, radius)) graphics.FillPath(brush, path);
        }

        private static void DrawRoundedRectangle(Graphics graphics, Pen pen, Rectangle rectangle, int radius)
        {
            using (var path = RoundedPath(rectangle, radius)) graphics.DrawPath(pen, path);
        }
    }
}
