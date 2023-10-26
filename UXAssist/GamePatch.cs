using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;

namespace UXAssist;

public static class GamePatch
{
    private const string GameWindowClass = "UnityWndClass";
    private const string GameWindowTitle = "Dyson Sphere Program";

    public static ConfigEntry<bool> EnableWindowResizeEnabled;
    public static ConfigEntry<bool> LoadLastWindowRectEnabled;
    public static ConfigEntry<Vector4> LastWindowRect;
    private static Harmony _gamePatch;

    public static void Init()
    {
        EnableWindowResizeEnabled.SettingChanged += (_, _) => EnableWindowResize.Enable(EnableWindowResizeEnabled.Value);
        LoadLastWindowRectEnabled.SettingChanged += (_, _) => LoadLastWindowRect.Enable(LoadLastWindowRectEnabled.Value);
        EnableWindowResize.Enable(EnableWindowResizeEnabled.Value);
        LoadLastWindowRect.Enable(LoadLastWindowRectEnabled.Value);
        _gamePatch ??= Harmony.CreateAndPatchAll(typeof(GamePatch));
    }
    
    public static void Uninit()
    {
        LoadLastWindowRect.Enable(false);
        EnableWindowResize.Enable(false);
        _gamePatch?.UnpatchSelf();
        _gamePatch = null;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.HandleApplicationQuit))]
    private static void GameMain_HandleApplicationQuit_Prefix()
    {
        var wnd = WinApi.FindWindow(GameWindowClass, GameWindowTitle);
        if (wnd == IntPtr.Zero) return;
        WinApi.GetWindowRect(wnd, out var rect);
        LastWindowRect.Value = new Vector4(rect.Left, rect.Top, Screen.width, Screen.height);
    }

    private static class EnableWindowResize
    {
        public static void Enable(bool on)
        {
            var wnd = WinApi.FindWindow(GameWindowClass, GameWindowTitle);
            if (wnd == IntPtr.Zero) return;
            if (on)
                WinApi.SetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE,
                    WinApi.GetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE) | (int)WindowStyles.WS_THICKFRAME | (int)WindowStyles.WS_MAXIMIZEBOX);
            else
                WinApi.SetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE,
                    WinApi.GetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE) & ~((int)WindowStyles.WS_THICKFRAME | (int)WindowStyles.WS_MAXIMIZEBOX));
        }
    }

    private static class LoadLastWindowRect
    {
        private static Harmony _patch;
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(LoadLastWindowRect));
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution), typeof(int), typeof(int), typeof(FullScreenMode), typeof(int))]
        private static void Screen_SetResolution_Prefix(ref int width, ref int height, FullScreenMode fullscreenMode)
        {
            if (fullscreenMode is FullScreenMode.ExclusiveFullScreen or FullScreenMode.FullScreenWindow or FullScreenMode.MaximizedWindow || GameMain.isRunning) return;
            var rect = LastWindowRect.Value;
            if (rect.z == 0f && rect.w == 0f) return;
            var w = Mathf.RoundToInt(rect.z);
            var h = Mathf.RoundToInt(rect.w);
            width = w;
            height = h;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution), typeof(int), typeof(int), typeof(FullScreenMode), typeof(int))]
        private static void Screen_SetResolution_Postfix()
        {
            var wnd = WinApi.FindWindow(GameWindowClass, GameWindowTitle);
            if (wnd == IntPtr.Zero) return;
            var rect = LastWindowRect.Value;
            if (rect.z == 0f && rect.w == 0f) return;
            var x = Mathf.RoundToInt(rect.x);
            var y = Mathf.RoundToInt(rect.y);
            WinApi.SetWindowPos(wnd, IntPtr.Zero, x, y, 0, 0, 0x0235);
            if (EnableWindowResizeEnabled.Value)
                WinApi.SetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE,
                    WinApi.GetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE) | (int)WindowStyles.WS_THICKFRAME | (int)WindowStyles.WS_MAXIMIZEBOX);
        }
    }
}