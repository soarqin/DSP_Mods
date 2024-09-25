using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BepInEx.Configuration;
using UnityEngine;
using UXAssist.Common;

namespace UXAssist.Functions;

public static class WindowFunctions
{
    public static string ProfileName { get; private set; }

    private const string GameWindowClass = "UnityWndClass";
    private static string _gameWindowTitle = "Dyson Sphere Program";

    private static IntPtr _oldWndProc = IntPtr.Zero;
    private static IntPtr _gameWindowHandle = IntPtr.Zero;

    private static bool _gameLoaded;
    public static WinApi.LogicalProcessorDetails ProcessorDetails { get; private set; }

    public static ConfigEntry<int> ProcessPriority;
    public static ConfigEntry<int> ProcessAffinity;

    private static readonly int[] ProrityFlags = [
        WinApi.HIGH_PRIORITY_CLASS,
        WinApi.ABOVE_NORMAL_PRIORITY_CLASS,
        WinApi.NORMAL_PRIORITY_CLASS,
        WinApi.BELOW_NORMAL_PRIORITY_CLASS,
        WinApi.IDLE_PRIORITY_CLASS
    ];

    public static void Init()
    {
        ProcessorDetails = WinApi.GetLogicalProcessorDetails();
    }

    public static void Start()
    {
        GameLogic.OnDataLoaded += () => { _gameLoaded = true; };
        var wndProc = new WinApi.WndProc(GameWndProc);
        var gameWnd = FindGameWindow();
        if (gameWnd != IntPtr.Zero)
        {
            _oldWndProc = WinApi.SetWindowLongPtr(gameWnd, WinApi.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(wndProc));
        }
        Patches.GamePatch.LoadLastWindowRect.MoveWindowPosition(true);

        ProcessPriority.SettingChanged += (_, _) => WinApi.SetPriorityClass(WinApi.GetCurrentProcess(), ProrityFlags[ProcessPriority.Value]);
        WinApi.SetPriorityClass(WinApi.GetCurrentProcess(), ProrityFlags[ProcessPriority.Value]);
        ProcessAffinity.SettingChanged += (_, _) => UpdateAffinity();
        UpdateAffinity();
        return;

        void UpdateAffinity()
        {
            var process = WinApi.GetCurrentProcess();
            if (!WinApi.GetProcessAffinityMask(process, out _, out var systemMask))
            {
                systemMask = ulong.MaxValue;
            }
            switch (ProcessAffinity.Value)
            {
                case 0:
                    WinApi.SetProcessAffinityMask(process, systemMask);
                    break;
                case 1:
                    WinApi.SetProcessAffinityMask(process, systemMask & ((1UL << (ProcessorDetails.ThreadCount / 2)) - 1UL));
                    break;
                case 2:
                    WinApi.SetProcessAffinityMask(process, systemMask & (ProcessorDetails.ThreadCount > 16 ? 0xFFUL : 1UL));
                    break;
                case 3:
                    WinApi.SetProcessAffinityMask(process, systemMask & ProcessorDetails.PerformanceCoreMask);
                    break;
                case 4:
                    WinApi.SetProcessAffinityMask(process, systemMask & ProcessorDetails.EfficiencyCoreMask);
                    break;
            }
        }
    }

    private static IntPtr GameWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        switch (uMsg)
        {
            case WinApi.WM_ACTIVATE:
                WinApi.SetPriorityClass(WinApi.GetCurrentProcess(), ProrityFlags[ProcessPriority.Value]);
                break;
            case WinApi.WM_DESTROY:
                if (_oldWndProc != IntPtr.Zero && _gameWindowHandle != IntPtr.Zero)
                {
                    WinApi.SetWindowLongPtr(_gameWindowHandle, WinApi.GWLP_WNDPROC, _oldWndProc);
                }
                break;
            case WinApi.WM_SYSCOMMAND:
                switch ((long)wParam & 0xFFF0L)
                {
                    case WinApi.SC_MOVE:
                        if (!_gameLoaded) return (IntPtr)1L;
                        break;
                }
                break;
            case WinApi.WM_MOVING:
                if (_gameLoaded) break;
                var rect = Patches.GamePatch.LastWindowRect.Value;
                if (rect is { z: 0f, w: 0f }) break;
                var x = Mathf.RoundToInt(rect.x);
                var y = Mathf.RoundToInt(rect.y);
                var rect2 = Marshal.PtrToStructure<WinApi.Rect>(lParam);
                rect2.Left = x;
                rect2.Top = y;
                Marshal.StructureToPtr(rect2, lParam, false);
                break;
            case WinApi.WM_SIZING:
                if (_gameLoaded) break;
                rect = Patches.GamePatch.LastWindowRect.Value;
                if (rect is { z: 0f, w: 0f }) break;
                x = Mathf.RoundToInt(rect.x);
                y = Mathf.RoundToInt(rect.y);
                var w = Mathf.RoundToInt(rect.z);
                var h = Mathf.RoundToInt(rect.w);
                rect2 = Marshal.PtrToStructure<WinApi.Rect>(lParam);
                rect2.Left = x;
                rect2.Top = y;
                rect2.Right = x + w;
                rect2.Bottom = y + h;
                Marshal.StructureToPtr(rect2, lParam, false);
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
