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

    [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
    public static extern bool GetProcessAffinityMask(IntPtr hProcess, out ulong lpProcessAffinityMask, out ulong lpSystemAffinityMask);

    [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32", ExactSpelling = true)]
    public static extern int GetCurrentProcessId();
    
    [DllImport("kernel32", ExactSpelling = true)]
    public static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
    public static extern bool SetProcessAffinityMask(IntPtr hProcess, ulong dwProcessAffinityMask);

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

    #region GetLogicalProcessorInformation

    [Flags]
    public enum LOGICAL_PROCESSOR_RELATIONSHIP
    {
        RelationProcessorCore,
        RelationNumaNode,
        RelationCache,
        RelationProcessorPackage,
        RelationGroup,
        RelationAll = 0xffff
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESSORCORE
    {
        public byte Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NUMANODE
    {
        public uint NodeNumber;
    }

    public enum PROCESSOR_CACHE_TYPE
    {
        CacheUnified,
        CacheInstruction,
        CacheData,
        CacheTrace
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CACHE_DESCRIPTOR
    {
        public byte Level;
        public byte Associativity;
        public ushort LineSize;
        public uint Size;
        public PROCESSOR_CACHE_TYPE Type;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION
    {
        [FieldOffset(0)]
        public PROCESSORCORE ProcessorCore;
        [FieldOffset(0)]
        public NUMANODE NumaNode;
        [FieldOffset(0)]
        public CACHE_DESCRIPTOR Cache;
        [FieldOffset(0)]
        private UInt64 Reserved1;
        [FieldOffset(8)]
        private UInt64 Reserved2;
    }

    public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
    {
        public UIntPtr ProcessorMask;
        public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
        public SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION ProcessorInformation;
    }

    [DllImport(@"kernel32", SetLastError=true)]
    private static extern bool GetLogicalProcessorInformation(
        IntPtr buffer,
        ref uint returnLength
    );

    public struct LogicalProcessorDetails
    {
        public int CoreCount;
        public int ThreadCount;
        public int PerformanceCoreCount;
        public int EfficiencyCoreCount;
        public ulong PerformanceCoreMask;
        public ulong EfficiencyCoreMask;
        public bool HybridArchitecture => PerformanceCoreCount > 0 && EfficiencyCoreCount > 0;
    }

    public static LogicalProcessorDetails GetLogicalProcessorDetails()
    {
        uint returnLength = 0;
        GetLogicalProcessorInformation(IntPtr.Zero, ref returnLength);
        var result = new LogicalProcessorDetails();
        if (Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER) return result;
        var ptr = Marshal.AllocHGlobal((int)returnLength);
        try
        {
            if (!GetLogicalProcessorInformation(ptr, ref returnLength))
                return result;
            var size = Marshal.SizeOf(typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
            var len = (int)returnLength / size;
            var item = ptr;
            for (var i = 0; i < len; i++)
            {
                var buffer = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION)Marshal.PtrToStructure(item, typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
                item += size;
                if (buffer.Relationship != LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore) continue;
                result.CoreCount++;
                var tcount = CountBitsSet((ulong)buffer.ProcessorMask);
                result.ThreadCount += tcount;
                if (tcount > 1)
                {
                    result.PerformanceCoreCount ++;
                    result.PerformanceCoreMask |= (ulong)buffer.ProcessorMask;
                }
                else
                {
                    result.EfficiencyCoreCount ++;
                    result.EfficiencyCoreMask |= (ulong)buffer.ProcessorMask;
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        return result;

        int CountBitsSet(ulong mask)
        {
            var count = 0;
            while (mask != 0)
            {
                mask &= mask - 1;
                count++;
            }
            return count;
        }
    }

    #endregion
}