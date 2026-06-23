using System;
using System.Diagnostics;
using BepInEx.Configuration;
using UXAssist.Common;

namespace UXAssist.Functions;

public static class WindowFunctions
{
    private static bool _initialized;
    public static string ProfileName { get; private set; }

    private const string GameWindowClass = "UnityWndClass";
    private static string _gameWindowTitle = "Dyson Sphere Program";

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
        ProcessPriority.SettingChanged += (_, _) => ApplyProcessPriority();
        ApplyProcessPriority();
    }

    private static void ApplyProcessPriority()
    {
        // Use the managed BCL wrapper instead of a raw kernel32 SetPriorityClass P/Invoke.
        using var process = Process.GetCurrentProcess();
        process.PriorityClass = (ProcessPriorityClass)ProrityFlags[ProcessPriority.Value];
    }

    private static string GetPriorityName(int priority)
    {
        return priority switch
        {
            WinApi.HIGH_PRIORITY_CLASS => I18NKeys.High.Translate(),
            WinApi.ABOVE_NORMAL_PRIORITY_CLASS => I18NKeys.AboveNormal.Translate(),
            WinApi.NORMAL_PRIORITY_CLASS => I18NKeys.Normal.Translate(),
            WinApi.BELOW_NORMAL_PRIORITY_CLASS => I18NKeys.BelowNormal.Translate(),
            WinApi.IDLE_PRIORITY_CLASS => I18NKeys.Idle.Translate(),
            _ => I18NKeys.Unknown.Translate()
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
        // Match the game window by its Unity window class plus our own process id. Matching the
        // class excludes the BepInEx console window (class "ConsoleWindowClass"), which
        // Process.MainWindowHandle would otherwise return when the console is enabled; the pid
        // check guards against other running Unity apps. Process.Id supplies the pid so no extra
        // kernel32 P/Invoke (GetCurrentProcessId/GetConsoleWindow) is needed.
        int currentProcessId;
        using (var process = Process.GetCurrentProcess())
            currentProcessId = process.Id;
        var wnd = IntPtr.Zero;
        while (true)
        {
            wnd = WinApi.FindWindowEx(IntPtr.Zero, wnd, GameWindowClass, null);
            if (wnd == IntPtr.Zero)
                return IntPtr.Zero;
            WinApi.GetWindowThreadProcessId(wnd, out var pid);
            if (pid == currentProcessId)
                break;
        }

        _gameWindowHandle = wnd;
        return _gameWindowHandle;
    }
}