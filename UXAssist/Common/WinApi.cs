using System;
using System.Runtime.InteropServices;

namespace UXAssist.Common;

[Flags]
public enum WindowStyles: int
{
    WS_BORDER = 0x00800000,
    WS_CAPTION = 0x00C00000,
    WS_CHILD = 0x40000000,
    WS_CHILDWINDOW = 0x40000000,
    WS_CLIPCHILDREN = 0x02000000,
    WS_CLIPSIBLINGS = 0x04000000,
    WS_DISABLED = 0x08000000,
    WS_DLGFRAME = 0x00400000,
    WS_GROUP = 0x00020000,
    WS_HSCROLL = 0x00100000,
    WS_ICONIC = 0x20000000,
    WS_MAXIMIZE = 0x01000000,
    WS_MAXIMIZEBOX = 0x00010000,
    WS_MINIMIZE = 0x20000000,
    WS_MINIMIZEBOX = 0x00020000,
    WS_OVERLAPPED = 0x00000000,
    WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
    WS_POPUP = unchecked((int)0x80000000),
    WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
    WS_SIZEBOX = 0x00040000,
    WS_SYSMENU = 0x00080000,
    WS_TABSTOP = 0x00010000,
    WS_THICKFRAME = 0x00040000,
    WS_TILED = 0x00000000,
    WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
    WS_VISIBLE = 0x10000000,
    WS_VSCROLL = 0x00200000
}

[Flags]
public enum WindowLongFlags: int
{
    GWL_EXSTYLE = -20,
    GWLP_HINSTANCE = -6,
    GWLP_HWNDPARENT = -8,
    GWLP_ID = -12,
    GWL_STYLE = -16,
    GWLP_USERDATA = -21,
    GWLP_WNDPROC = -4,
    DWLP_DLGPROC = 0x4,
    DWLP_MSGRESULT = 0,
    DWLP_USER = 0x8
}

public static class WinApi
{

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern int GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    
    [DllImport("user32", ExactSpelling = true)]
    public static extern bool GetWindowRect(IntPtr hwnd, out Rect lpRect);
    
    [DllImport("user32", ExactSpelling = true)]
    public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);
    
    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern bool SetWindowText(IntPtr hwnd, string lpString);
    
    [DllImport("user32", ExactSpelling = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);
}