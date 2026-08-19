using System;
using System.Runtime.InteropServices;

namespace SocialDistance
{
    internal static class NativeMethods
    {
        internal const int WsExTransparent = 0x00000020;
        internal const int WsExToolWindow = 0x00000080;
        internal const int WmNclButtonDown = 0x00A1;
        internal const int HtCaption = 2;
        internal const int WmNcHitTest = 0x0084;
        internal const int HtLeft = 10;
        internal const int HtRight = 11;
        internal const int HtTop = 12;
        internal const int HtTopLeft = 13;
        internal const int HtTopRight = 14;
        internal const int HtBottom = 15;
        internal const int HtBottomLeft = 16;
        internal const int HtBottomRight = 17;

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
