using System;
using System.Runtime.InteropServices;

namespace UXAssist.Common;

public static class WinApi
{
    #region Styles

    public const int WS_BORDER = 0x00800000;
    public const int WS_CAPTION = 0x00C00000;
    public const int WS_CHILD = 0x40000000;
    public const int WS_CHILDWINDOW = 0x40000000;
    public const int WS_CLIPCHILDREN = 0x02000000;
    public const int WS_CLIPSIBLINGS = 0x04000000;
    public const int WS_DISABLED = 0x08000000;
    public const int WS_DLGFRAME = 0x00400000;
    public const int WS_GROUP = 0x00020000;
    public const int WS_HSCROLL = 0x00100000;
    public const int WS_ICONIC = 0x20000000;
    public const int WS_MAXIMIZE = 0x01000000;
    public const int WS_MAXIMIZEBOX = 0x00010000;
    public const int WS_MINIMIZE = 0x20000000;
    public const int WS_MINIMIZEBOX = 0x00020000;
    public const int WS_OVERLAPPED = 0x00000000;
    public const int WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
    public const int WS_POPUP = unchecked((int)0x80000000);
    public const int WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU;
    public const int WS_SIZEBOX = 0x00040000;
    public const int WS_SYSMENU = 0x00080000;
    public const int WS_TABSTOP = 0x00010000;
    public const int WS_THICKFRAME = 0x00040000;
    public const int WS_TILED = 0x00000000;
    public const int WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
    public const int WS_VISIBLE = 0x10000000;
    public const int WS_VSCROLL = 0x00200000;

    #endregion

    #region GetWindowLong and SetWindowLong Flags

    public const int GWL_EXSTYLE = -20;
    public const int GWLP_HINSTANCE = -6;
    public const int GWLP_HWNDPARENT = -8;
    public const int GWLP_ID = -12;
    public const int GWL_STYLE = -16;
    public const int GWLP_USERDATA = -21;
    public const int GWLP_WNDPROC = -4;
    public const int DWLP_DLGPROC = 0x4;
    public const int DWLP_MSGRESULT = 0;
    public const int DWLP_USER = 0x8;

    #endregion

    #region Priorities

    public const int HIGH_PRIORITY_CLASS = 0x00000080;
    public const int ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000;
    public const int NORMAL_PRIORITY_CLASS = 0x00000020;
    public const int BELOW_NORMAL_PRIORITY_CLASS = 0x00004000;
    public const int IDLE_PRIORITY_CLASS = 0x00000040;

    #endregion

    #region Messages

    public const int WM_CREATE = 0x0001;
    public const int WM_DESTROY = 0x0002;
    public const int WM_MOVE = 0x0003;
    public const int WM_SIZE = 0x0005;
    public const int WM_ACTIVATE = 0x0006;
    public const int WM_SETFOCUS = 0x0007;
    public const int WM_KILLFOCUS = 0x0008;
    public const int WM_ENABLE = 0x000A;
    public const int WM_CLOSE = 0x0010;
    public const int WM_QUIT = 0x0012;
    public const int WM_SYSCOMMAND = 0x0112;
    public const int WM_SIZING = 0x0214;
    public const int WM_MOVING = 0x0216;
    public const long SC_MOVE = 0xF010L;

    #endregion

    #region Errors

    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    #endregion

    #region Structs

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    #endregion

    #region Functions

    public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32", ExactSpelling = true)]
    public static extern int GetLastError();

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern int GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

    [DllImport("user32", ExactSpelling = true, SetLastError = true)]
    public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int lpdwProcessId);

    [DllImport("user32", ExactSpelling = true)]
    public static extern bool GetWindowRect(IntPtr hwnd, out Rect lpRect);

    [DllImport("user32", ExactSpelling = true)]
    public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern bool SetWindowText(IntPtr hwnd, string lpString);

    [DllImport("user32", ExactSpelling = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32", ExactSpelling = true)]
    public static extern IntPtr MonitorFromRect([In] ref Rect lpRect, uint dwFlags);

    [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32", ExactSpelling = true)]
    public static extern int GetCurrentProcessId();

    [DllImport("kernel32", ExactSpelling = true)]
    public static extern IntPtr GetConsoleWindow();

    // GetPriorityClass and SetPriorityClass
    [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
    public static extern int GetPriorityClass(IntPtr hProcess);

    [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
    public static extern bool SetPriorityClass(IntPtr hProcess, int dwPriorityClass);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    #endregion
}