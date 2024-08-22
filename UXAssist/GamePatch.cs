using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;

namespace UXAssist;

public static class GamePatch
{
    private const string GameWindowClass = "UnityWndClass";
    private static string _gameWindowTitle = "Dyson Sphere Program";

    public static ConfigEntry<bool> EnableWindowResizeEnabled;
    public static ConfigEntry<bool> LoadLastWindowRectEnabled;
    // public static ConfigEntry<bool> AutoSaveOptEnabled;
    public static ConfigEntry<bool> ConvertSavesFromPeaceEnabled;
    public static ConfigEntry<Vector4> LastWindowRect;
    private static Harmony _gamePatch;

    public static void Init()
    {
        // Get profile name from command line arguments, and set window title accordingly
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
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
            _gameWindowTitle = $"Dyson Sphere Program - {arg}";
            WinApi.SetWindowText(wnd, _gameWindowTitle);
            break;
        }

        EnableWindowResizeEnabled.SettingChanged += (_, _) => EnableWindowResize.Enable(EnableWindowResizeEnabled.Value);
        LoadLastWindowRectEnabled.SettingChanged += (_, _) => LoadLastWindowRect.Enable(LoadLastWindowRectEnabled.Value);
        // AutoSaveOptEnabled.SettingChanged += (_, _) => AutoSaveOpt.Enable(AutoSaveOptEnabled.Value);
        ConvertSavesFromPeaceEnabled.SettingChanged += (_, _) => ConvertSavesFromPeace.Enable(ConvertSavesFromPeaceEnabled.Value);
        EnableWindowResize.Enable(EnableWindowResizeEnabled.Value);
        LoadLastWindowRect.Enable(LoadLastWindowRectEnabled.Value);
        // AutoSaveOpt.Enable(AutoSaveOptEnabled.Value);
        ConvertSavesFromPeace.Enable(ConvertSavesFromPeaceEnabled.Value);
        _gamePatch ??= Harmony.CreateAndPatchAll(typeof(GamePatch));
    }

    public static void Uninit()
    {
        LoadLastWindowRect.Enable(false);
        EnableWindowResize.Enable(false);
        // AutoSaveOpt.Enable(false);
        ConvertSavesFromPeace.Enable(false);
        _gamePatch?.UnpatchSelf();
        _gamePatch = null;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.HandleApplicationQuit))]
    private static void GameMain_HandleApplicationQuit_Prefix()
    {
        var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
        if (wnd == IntPtr.Zero) return;
        WinApi.GetWindowRect(wnd, out var rect);
        LastWindowRect.Value = new Vector4(rect.Left, rect.Top, Screen.width, Screen.height);
    }

    private static class EnableWindowResize
    {
        public static void Enable(bool on)
        {
            var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
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
        private static bool _loaded;
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(LoadLastWindowRect));
                if (Screen.fullScreenMode is not (FullScreenMode.ExclusiveFullScreen or FullScreenMode.FullScreenWindow or FullScreenMode.MaximizedWindow))
                {
                    var rect = LastWindowRect.Value;
                    var x = Mathf.RoundToInt(rect.x);
                    var y = Mathf.RoundToInt(rect.y);
                    var w = Mathf.RoundToInt(rect.z);
                    var h = Mathf.RoundToInt(rect.w);
                    var needFix = false;
                    if (w < 100)
                    {
                        w = 1280;
                        needFix = true;
                    }
                    if (h < 100)
                    {
                        h = 720;
                        needFix = true;
                    }
                    var sw = Screen.currentResolution.width;
                    var sh = Screen.currentResolution.height;
                    if (x + w > sw)
                    {
                        x = sw - w;
                        needFix = true;
                    }
                    if (y + h > sh)
                    {
                        y = sh - h;
                        needFix = true;
                    }
                    if (x < 0)
                    {
                        x = 0;
                        needFix = true;
                    }
                    if (y < 0)
                    {
                        y = 0;
                        needFix = true;
                    }
                    if (needFix)
                    {
                        LastWindowRect.Value = new Vector4(x, y, w, h);
                    }
                }
                MoveWindowPosition();
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }

        private static void MoveWindowPosition()
        {
            if (Screen.fullScreenMode is FullScreenMode.ExclusiveFullScreen or FullScreenMode.FullScreenWindow or FullScreenMode.MaximizedWindow || GameMain.isRunning) return;
            var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
            if (wnd == IntPtr.Zero) return;
            var rect = LastWindowRect.Value;
            if (rect.z == 0f && rect.w == 0f) return;
            var x = Mathf.RoundToInt(rect.x);
            var y = Mathf.RoundToInt(rect.y);
            WinApi.SetWindowPos(wnd, IntPtr.Zero, x, y, 0, 0, 0x0235);
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
        private static void Screen_SetResolution_Postfix(FullScreenMode fullscreenMode)
        {
            MoveWindowPosition();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            if (_loaded || Screen.fullScreenMode is FullScreenMode.ExclusiveFullScreen or FullScreenMode.FullScreenWindow or FullScreenMode.MaximizedWindow) return;
            _loaded = true;
            var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
            if (wnd == IntPtr.Zero) return;
            var rect = LastWindowRect.Value;
            if (rect.z == 0f && rect.w == 0f) return;
            var x = Mathf.RoundToInt(rect.x);
            var y = Mathf.RoundToInt(rect.y);
            var w = Mathf.RoundToInt(rect.z);
            var h = Mathf.RoundToInt(rect.w);
            Screen.SetResolution(w, h, false);
            WinApi.SetWindowPos(wnd, IntPtr.Zero, x, y, 0, 0, 0x0235);
            if (EnableWindowResizeEnabled.Value)
                WinApi.SetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE,
                    WinApi.GetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE) | (int)WindowStyles.WS_THICKFRAME | (int)WindowStyles.WS_MAXIMIZEBOX);
        }
    }

    /*
    private static class AutoSaveOpt
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            if (on)
            {
                Directory.CreateDirectory(GameConfig.gameSaveFolder + "AutoSaves/");
                _patch ??= Harmony.CreateAndPatchAll(typeof(AutoSaveOpt));
                return;
            }

            _patch?.UnpatchSelf();
            _patch = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.AutoSave))]
        private static bool GameSave_AutoSave_Prefix(ref bool __result)
        {
            if (!GameSave.SaveCurrentGame(GameSave.AutoSaveTmp))
            {
                GlobalObject.SaveOpCounter();
                __result = false;
                return false;
            }

            var tmpFilename = GameConfig.gameSaveFolder + GameSave.AutoSaveTmp + GameSave.saveExt;
            var targetFilename = $"{GameConfig.gameSaveFolder}AutoSaves/[{GameMain.data.gameDesc.clusterString}] {DateTime.Now:yyyy-MM-dd_hh-mm-ss}{GameSave.saveExt}";
            File.Move(tmpFilename, targetFilename);
            __result = true;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UILoadGameWindow), nameof(UILoadGameWindow.RefreshList))]
        public static void UILoadGameWindow_RefreshList_Postfix(UILoadGameWindow __instance)
        {
            var baseDir = GameConfig.gameSaveFolder + "AutoSaves/";
            var files = Directory.GetFiles(baseDir, "*" + GameSave.saveExt, SearchOption.TopDirectoryOnly);
            var entries = __instance.entries;
            var entries2 = new List<UIGameSaveEntry>();
            var entryPrefab = __instance.entryPrefab;
            var entryPrefabParent = entryPrefab.transform.parent;
            foreach (var f in files)
            {
                var fileInfo = new FileInfo(f);
                var entry = Object.Instantiate(entryPrefab, entryPrefabParent);
                entry.fileInfo = fileInfo;
                entries2.Add(entry);
            }
            entries2.Sort((x, y) => -x.fileDate.CompareTo(y.fileDate));
            if (entries2.Count > 10)
                entries2.RemoveRange(10, entries2.Count - 10);
            var autoSaveText = ">>  " + "自动存档条目".Translate();
            foreach (var entry in entries2)
            {
                entry.indexText.text = "";
                var saveName = entry.saveName;
                entry._saveName = $"AutoSaves/{saveName}";
                var quoteIndex = saveName.IndexOf('[');
                if (quoteIndex >= 0)
                {
                    var quoteIndex2 = saveName.IndexOf(']', quoteIndex + 1);
                    if (quoteIndex2 > 0) saveName = saveName.Substring(quoteIndex, quoteIndex2 + 1 - quoteIndex);
                }
                entry.nameText.text = $"{autoSaveText} {saveName}";
                entry.nameText.fontStyle = FontStyle.Italic;
                entry.nameText.color = new Color(1f, 1f, 1f, 0.7f);
                entry.timeText.text = $"{entry.fileDate:yyyy-MM-dd HH:mm:ss}";
                GameSave.ReadModes(entry.fileInfo.FullName, out var isSandbox, out var isPeace);
                if (entry.sandboxIcon != null)
                {
                    entry.sandboxIcon.gameObject.SetActive(isSandbox);
                }
                if (entry.combatIcon != null)
                {
                    entry.combatIcon.gameObject.SetActive(!isPeace);
                }
                entry.selected = false;
                entry.gameObject.SetActive(true);
            }
            entries.AddRange(entries2);
            entries.Sort((x, y) => -x.fileDate.CompareTo(y.fileDate));
            var displayIndex = 1;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                entry.index = i + 1;
                entry.rectTrans.anchoredPosition = new Vector2(entry.rectTrans.anchoredPosition.x, -40 * i);
                if (string.IsNullOrEmpty(entry.indexText.text)) continue;
                entry.indexText.text = displayIndex.ToString();
                displayIndex++;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISaveGameWindow), nameof(UISaveGameWindow.RefreshList))]
        public static void UISaveGameWindow_RefreshList_Postfix(UISaveGameWindow __instance)
        {
            var entries = __instance.entries;
            entries.Sort((x, y) => -x.fileDate.CompareTo(y.fileDate));
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                entry.index = i + 1;
                entry.rectTrans.anchoredPosition = new Vector2(entry.rectTrans.anchoredPosition.x, -40 * i);
                entry.indexText.text = (i + 1).ToString();
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UILoadGameWindow), nameof(UILoadGameWindow.DoLoadSelectedGame))]
        [HarmonyPatch(typeof(UILoadGameWindow), nameof(UILoadGameWindow.OnSelectedChange))]
        private static IEnumerable<CodeInstruction> UILoadGameWindow_ReplaceSaveName_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(UIGameSaveEntry), nameof(UIGameSaveEntry.saveName)))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldfld, AccessTools.Field(typeof(UIGameSaveEntry), nameof(UIGameSaveEntry._saveName))));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame))]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadGameDesc))]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.ReadHeader))]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.ReadHeaderAndDescAndProperty))]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveExist))]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.SavePath))]
        private static IEnumerable<CodeInstruction> GameSave_RemoveValidateOnLoad_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(CommonUtils), nameof(CommonUtils.ValidFileName)))
            );
            matcher.RemoveInstruction();
            return matcher.InstructionEnumeration();
        }
    }
    */

    private static class ConvertSavesFromPeace
    {
        private static Harmony _patch;
        private static bool _needConvert;
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(ConvertSavesFromPeace));
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameDesc), nameof(GameDesc.Import))]
        private static void GameDesc_Import_Postfix(GameDesc __instance)
        {
            if (DSPGame.IsMenuDemo || !__instance.isPeaceMode) return;
            __instance.combatSettings = UIRoot.instance.galaxySelect.uiCombat.combatSettings;
            __instance.isPeaceMode = false;
            _needConvert = true;
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameData), nameof(GameData.Import))]
        private static IEnumerable<CodeInstruction> GameData_Import_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.mecha))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Mecha), nameof(Mecha.CheckCombatModuleDataIsValidPatch)))
            );
            matcher.Advance(2).Opcode = OpCodes.Brfalse;
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ConvertSavesFromPeace), nameof(ConvertSavesFromPeace._needConvert)))
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), nameof(GameData.Import))]
        private static void GameData_Import_Postfix()
        {
            _needConvert = false;
        }
    }
}