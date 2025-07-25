﻿using System;
using System.Runtime.InteropServices;
using BepInEx.Configuration;
using UXAssist.Common;

namespace UXAssist.Functions;

public static class WindowFunctions
{
    private static bool _initialized;
    public static string ProfileName { get; private set; }

    private const string GameWindowClass = "UnityWndClass";
    private static string _gameWindowTitle = "Dyson Sphere Program";

    private static IntPtr _oldWndProc = IntPtr.Zero;
    private static IntPtr _gameWindowHandle = IntPtr.Zero;

    public static WinApi.LogicalProcessorDetails ProcessorDetails { get; private set; }

    public static ConfigEntry<int> ProcessPriority;
    public static ConfigEntry<int> ProcessAffinity;

    private static readonly int[] ProrityFlags =
    [
        WinApi.HIGH_PRIORITY_CLASS,
        WinApi.ABOVE_NORMAL_PRIORITY_CLASS,
        WinApi.NORMAL_PRIORITY_CLASS,
        WinApi.BELOW_NORMAL_PRIORITY_CLASS,
        WinApi.IDLE_PRIORITY_CLASS
    ];

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;
        I18N.Add("Cores: {0}\nThreads: {1}", "Cores: {0}\nThreads: {1}", "核心数: {0}\n线程数: {1}");
        I18N.Add("\nP-Cores: {0}\nE-Cores: {1}", "\nP-Cores: {0}\nE-Cores: {1}", "\n性能核心: {0}\n能效核心: {1}");
        I18N.Add("\nPriority: {0}", "\nProcess priority: {0}", "\n进程优先级: {0}");
        I18N.Add("\nEnabled CPUs: ", "\nEnabled CPUs: ", "\n使用的CPU: ");
        I18N.Add("Unknown", "Unknown", "未知");
        ProcessorDetails = WinApi.GetLogicalProcessorDetails();
        SetWindowTitle();
    }

    public static void Start()
    {
        var wndProc = new WinApi.WndProc(GameWndProc);
        var gameWnd = FindGameWindow();
        if (gameWnd != IntPtr.Zero)
        {
            _oldWndProc = WinApi.SetWindowLongPtr(gameWnd, WinApi.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(wndProc));
        }

        ProcessPriority.SettingChanged += (_, _) => WinApi.SetPriorityClass(WinApi.GetCurrentProcess(), ProrityFlags[ProcessPriority.Value]);
        WinApi.SetPriorityClass(WinApi.GetCurrentProcess(), ProrityFlags[ProcessPriority.Value]);
        ProcessAffinity.SettingChanged += (_, _) => UpdateAffinity();
        UpdateAffinity();
        return;

        static void UpdateAffinity()
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
        }

        return WinApi.CallWindowProc(_oldWndProc, hWnd, uMsg, wParam, lParam);
    }

    private static string GetPriorityName(int priority)
    {
        return priority switch
        {
            WinApi.HIGH_PRIORITY_CLASS => "High".Translate(),
            WinApi.ABOVE_NORMAL_PRIORITY_CLASS => "Above Normal".Translate(),
            WinApi.NORMAL_PRIORITY_CLASS => "Normal".Translate(),
            WinApi.BELOW_NORMAL_PRIORITY_CLASS => "Below Normal".Translate(),
            WinApi.IDLE_PRIORITY_CLASS => "Idle".Translate(),
            _ => "Unknown".Translate()
        };
    }

    public static void ShowCPUInfo()
    {
        var details = ProcessorDetails;
        var msg = string.Format("Cores: {0}\nThreads: {1}".Translate(), details.CoreCount, details.ThreadCount);
        var hybrid = details.HybridArchitecture;
        if (hybrid)
        {
            msg += string.Format("\nP-Cores: {0}\nE-Cores: {1}".Translate(), details.PerformanceCoreCount, details.EfficiencyCoreCount);
        }

        var handle = WinApi.GetCurrentProcess();
        var prio = GetPriorityName(WinApi.GetPriorityClass(handle));
        msg += string.Format("\nPriority: {0}".Translate(), prio);

        var aff = 0UL;
        if (WinApi.GetProcessAffinityMask(handle, out var processMask, out var systemMask))
            aff = processMask & systemMask;

        msg += "\nEnabled CPUs: ".Translate();
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

        UIMessageBox.Show("CPU Info".Translate(), msg, "确定".Translate(), -1);
    }

    public static void SetWindowTitle()
    {
        // Get profile name from command line arguments, and set window title accordingly
        var args = Environment.GetCommandLineArgs();
        for (var i = args.Length - 2; i >= 0; i--)
        {
            if (args[i] == "--gale-profile")
            {
                // We use gale profile name directly
                ProfileName = args[i + 1];
            }
            else
            {
                // Doorstop 3.x and 4.x use different arguments to pass the target assembly path
                if (args[i] != "--doorstop-target" && args[i] != "--doorstop-target-assembly") continue;
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
                ProfileName = arg;
            }
            var wnd = FindGameWindow();
            if (wnd == IntPtr.Zero) return;
            _gameWindowTitle = $"Dyson Sphere Program - {ProfileName}";
            WinApi.SetWindowText(wnd, _gameWindowTitle);
            break;
        }
    }

    public static IntPtr FindGameWindow()
    {
        if (_gameWindowHandle != IntPtr.Zero)
            return _gameWindowHandle;
        var wnd = IntPtr.Zero;
        var consoleWnd = WinApi.GetConsoleWindow();
        var currentProcessId = WinApi.GetCurrentProcessId();
        while (true)
        {
            wnd = WinApi.FindWindowEx(IntPtr.Zero, wnd, GameWindowClass, _gameWindowTitle);
            if (wnd == IntPtr.Zero)
                return IntPtr.Zero;
            if (wnd == consoleWnd)
                continue;
            WinApi.GetWindowThreadProcessId(wnd, out var pid);
            if (pid == currentProcessId)
                break;
        }

        _gameWindowHandle = wnd;
        return _gameWindowHandle;
    }
}