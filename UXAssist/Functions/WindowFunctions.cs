using System;
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

    public static ConfigEntry<int> ProcessPriority;

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