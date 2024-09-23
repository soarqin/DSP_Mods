using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UXAssist.Common;

namespace UXAssist.Functions;

public static class WindowFunctions
{
    public static string ProfileName { get; private set; }

    private const string GameWindowClass = "UnityWndClass";
    private static string _gameWindowTitle = "Dyson Sphere Program";

    private static IntPtr _oldWndProc = IntPtr.Zero;
    private static IntPtr _gameWindowHandle = IntPtr.Zero;

    public static void Start()
    {
        var wndProc = new WinApi.WndProc(GameWndProc);
        var gameWnd = FindGameWindow();
        if (gameWnd != IntPtr.Zero)
        {
            _oldWndProc = WinApi.SetWindowLongPtr(gameWnd, WinApi.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(wndProc));
        }
    }

    private static IntPtr GameWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        switch (uMsg)
        {
            case WinApi.WM_ACTIVATE:
                // UXAssist.Logger.LogDebug($"Activate: {wParam.ToInt32()}, {lParam.ToInt32()}");
                // TODO: Set Priority like: WinApi.SetPriorityClass(WinApi.GetCurrentProcess(), 0x00000080);
                break;
            case WinApi.WM_DESTROY:
                if (_oldWndProc != IntPtr.Zero && _gameWindowHandle != IntPtr.Zero)
                {
                    WinApi.SetWindowLongPtr(_gameWindowHandle, WinApi.GWLP_WNDPROC, _oldWndProc);
                }
                break;
        }
        return WinApi.CallWindowProc(_oldWndProc, hWnd, uMsg, wParam, lParam);
    }

    public static void ShowCPUInfo()
    {
        var details = WinApi.GetLogicalProcessorDetails();
        var msg = $"Cores: {details.CoreCount}\nThreads: {details.ThreadCount}";
        var hybrid = details.HybridArchitecture;
        if (hybrid)
        {
            msg += $"\nP-Cores: {details.PerformanceCoreCount}\nE-Cores: {details.EfficiencyCoreCount}";
        }

        var handle = WinApi.GetCurrentProcess();
        var prio = (ProcessPriorityClass)WinApi.GetPriorityClass(handle);
        msg += $"\nPriority: {prio}";

        var aff = 0UL;
        if (WinApi.GetProcessAffinityMask(handle, out var processMask, out var systemMask))
            aff = (ulong)processMask & (ulong)systemMask;

        msg += $"\nEnabled CPUs: ";
        var first = true;
        for (var i = 0; aff != 0UL; i++)
        {
            if ((aff & 1UL) != 0)
            {
                if (first)
                    first = false;
                else
                    msg += ",";
                msg += i;
                if (hybrid)
                {
                    if ((details.PerformanceCoreMask & (1UL << i)) != 0)
                        msg += "(P)";
                    else if ((details.EfficiencyCoreMask & (1UL << i)) != 0)
                        msg += "(E)";
                }
            }

            aff >>= 1;
        }

        UIMessageBox.Show("CPU Info".Translate(), msg, "OK".Translate(), -1);
    }

    public static void SetWindowTitle()
    {
        // Get profile name from command line arguments, and set window title accordingly
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] != "--doorstop-target") continue;
            var arg = args[i + 1];
            const string doorstopPathSuffix = @"\BepInEx\core\BepInEx.Preloader.dll";
            if (!arg.EndsWith(doorstopPathSuffix, StringComparison.OrdinalIgnoreCase))
                break;
            arg = arg.Substring(0, arg.Length - doorstopPathSuffix.Length);
            const string profileSuffix = @"\profiles\";
            var index = arg.LastIndexOf(profileSuffix, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                break;
            arg = arg.Substring(index + profileSuffix.Length);
            var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
            if (wnd == IntPtr.Zero) return;
            ProfileName = arg;
            _gameWindowTitle = $"Dyson Sphere Program - {arg}";
            WinApi.SetWindowText(wnd, _gameWindowTitle);
            break;
        }
    }

    public static IntPtr FindGameWindow()
    {
        if (_gameWindowHandle == IntPtr.Zero)
            _gameWindowHandle = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
        return _gameWindowHandle;
    }
}
