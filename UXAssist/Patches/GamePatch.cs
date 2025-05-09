using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Xml;
using BepInEx;
using BepInEx.Configuration;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Functions;

namespace UXAssist.Patches;

public class GamePatch : PatchImpl<GamePatch>
{
    public static ConfigEntry<bool> EnableWindowResizeEnabled;
    public static ConfigEntry<bool> LoadLastWindowRectEnabled;

    public static ConfigEntry<int> MouseCursorScaleUpMultiplier;

    // public static ConfigEntry<bool> AutoSaveOptEnabled;
    public static ConfigEntry<bool> ConvertSavesFromPeaceEnabled;
    public static ConfigEntry<Vector4> LastWindowRect;
    public static ConfigEntry<bool> ProfileBasedSaveFolderEnabled;
    public static ConfigEntry<bool> ProfileBasedOptionEnabled;
    public static ConfigEntry<string> DefaultProfileName;
    public static ConfigEntry<double> GameUpsFactor;

    private static PressKeyBind _speedDownKey;
    private static PressKeyBind _speedUpKey;
    private static bool _enableGameUpsFactor = true;

    public static bool EnableGameUpsFactor
    {
        get => _enableGameUpsFactor;
        set
        {
            _enableGameUpsFactor = value;
            if (value)
            {
                var oldFixUps = FPSController.instance.fixUPS;
                if (oldFixUps <= 1.0)
                {
                    GameUpsFactor.Value = 1.0;
                    return;
                }

                GameUpsFactor.Value = Maths.Clamp(FPSController.instance.fixUPS / GameMain.tickPerSec, 0.1, 10.0);
            }
            else
            {
                GameUpsFactor.Value = 1.0;
            }
        }
    }

    public static void Init()
    {
        _speedDownKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.Minus, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "UPSSpeedDown",
            canOverride = true
        }
        );
        I18N.Add("KEYUPSSpeedDown", "[UXA] Decrease logical frame rate", "[UXA] 降低逻辑帧率");
        _speedUpKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.Equals, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.UI | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "UPSSpeedUp",
            canOverride = true
        }
        );
        I18N.Add("KEYUPSSpeedUp", "[UXA] Increase logical frame rate", "[UXA] 提升逻辑帧率");
        I18N.Add("Logical frame rate: {0}x", "[UXA] Logical frame rate: {0}x", "[UXA] 逻辑帧速率: {0}x");

        EnableWindowResizeEnabled.SettingChanged += (_, _) => EnableWindowResize.Enable(EnableWindowResizeEnabled.Value);
        LoadLastWindowRectEnabled.SettingChanged += (_, _) => {
            if (LoadLastWindowRectEnabled.Value)
            {
                FixLastWindowRect();
            }
        };
        MouseCursorScaleUpMultiplier.SettingChanged += (_, _) =>
        {
            MouseCursorScaleUp.NeedReloadCursors = true;
            MouseCursorScaleUp.Enable(MouseCursorScaleUpMultiplier.Value > 1);
        };
        // AutoSaveOptEnabled.SettingChanged += (_, _) => AutoSaveOpt.Enable(AutoSaveOptEnabled.Value);
        ConvertSavesFromPeaceEnabled.SettingChanged += (_, _) => ConvertSavesFromPeace.Enable(ConvertSavesFromPeaceEnabled.Value);
        ProfileBasedSaveFolderEnabled.SettingChanged += (_, _) => RefreshSavePath();
        ProfileBasedOptionEnabled.SettingChanged += (_, _) => ProfileBasedOption.Enable(ProfileBasedOptionEnabled.Value);
        DefaultProfileName.SettingChanged += (_, _) => RefreshSavePath();
        GameUpsFactor.SettingChanged += (_, _) =>
        {
            if (!EnableGameUpsFactor || GameUpsFactor.Value == 0.0) return;
            if (Math.Abs(GameUpsFactor.Value - 1.0) < 0.001)
            {
                FPSController.SetFixUPS(0.0);
                return;
            }

            FPSController.SetFixUPS(GameMain.tickPerSec * GameUpsFactor.Value);
        };
        ProfileBasedOption.Enable(ProfileBasedOptionEnabled.Value);
    }

    public static void Start()
    {
        RefreshSavePath();
        if (LoadLastWindowRectEnabled.Value)
        {
            FixLastWindowRect();
            var wnd = WindowFunctions.FindGameWindow();
            if (wnd != IntPtr.Zero)
            {
                ThreadingHelper.Instance.StartCoroutine(SetWindowPositionCoroutine(wnd, (int)LastWindowRect.Value.x, (int)LastWindowRect.Value.y));
            }
        }
        EnableWindowResize.Enable(EnableWindowResizeEnabled.Value);
        MouseCursorScaleUp.NeedReloadCursors = false;
        MouseCursorScaleUp.Enable(MouseCursorScaleUpMultiplier.Value > 1);
        // AutoSaveOpt.Enable(AutoSaveOptEnabled.Value);
        ConvertSavesFromPeace.Enable(ConvertSavesFromPeaceEnabled.Value);
        Enable(true);
    }

    public static void Uninit()
    {
        Enable(false);
        EnableWindowResize.Enable(false);
        MouseCursorScaleUp.NeedReloadCursors = false;
        MouseCursorScaleUp.Enable(false);
        // AutoSaveOpt.Enable(false);
        ConvertSavesFromPeace.Enable(false);
    }

    public static void OnInputUpdate()
    {
        if (!_enableGameUpsFactor) return;
        if (_speedDownKey.keyValue)
        {
            GameUpsFactor.Value = Maths.Clamp(Math.Round((GameUpsFactor.Value - 0.5) * 2.0) / 2.0, 0.1, 10.0);
            UIRoot.instance.uiGame.generalTips.InvokeRealtimeTipAhead(string.Format("Logical frame rate: {0}x".Translate(), GameUpsFactor.Value));
        }

        if (_speedUpKey.keyValue)
        {
            GameUpsFactor.Value = Maths.Clamp(Math.Round((GameUpsFactor.Value + 0.5) * 2.0) / 2.0, 0.1, 10.0);
            UIRoot.instance.uiGame.generalTips.InvokeRealtimeTipAhead(string.Format("Logical frame rate: {0}x".Translate(), GameUpsFactor.Value));
        }
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(GameConfig), "gameSaveFolder", MethodType.Getter)]
    public static void GameConfig_gameSaveFolder_Postfix(ref string __result)
    {
        if (!ProfileBasedSaveFolderEnabled.Value || string.IsNullOrEmpty(WindowFunctions.ProfileName)) return;
        __result = $"{__result}{WindowFunctions.ProfileName}/";
    }

    private static void RefreshSavePath()
    {
        var gameSaveFolder = GameConfig.gameSaveFolder;
        if (!Directory.Exists(gameSaveFolder))
            Directory.CreateDirectory(gameSaveFolder);
        if (UIRoot.instance?.loadGameWindow?.active == true) UIRoot.instance.loadGameWindow.RefreshList();
        if (UIRoot.instance?.saveGameWindow?.active == true) UIRoot.instance.saveGameWindow.RefreshList();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.HandleApplicationQuit))]
    private static void GameMain_HandleApplicationQuit_Prefix()
    {
        if (!LoadLastWindowRectEnabled.Value) return;
        var wnd = WindowFunctions.FindGameWindow();
        if (wnd == IntPtr.Zero) return;
        WinApi.GetWindowRect(wnd, out var rect);
        LastWindowRect.Value = new Vector4(rect.Left, rect.Top, Screen.width, Screen.height);
    }
    private static void FixLastWindowRect()
    {
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

            var rc = new WinApi.Rect { Left = x, Top = y, Right = x + w, Bottom = y + h };
            if (WinApi.MonitorFromRect(ref rc, 0) == IntPtr.Zero)
            {
                x = 0;
                y = 0;
                w = 1280;
                h = 720;
                needFix = true;
            }
            if (needFix)
            {
                LastWindowRect.Value = new Vector4(x, y, w, h);
            }
        }
    }

    public static IEnumerator SetWindowPositionCoroutine(IntPtr wnd, int x, int y)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        WinApi.SetWindowPos(wnd, IntPtr.Zero, x, y, 0, 0, 0x0235);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution), typeof(int), typeof(int), typeof(FullScreenMode), typeof(int))]
    private static void Screen_SetResolution_Prefix(ref int width, ref int height, FullScreenMode fullscreenMode, ref Vector2Int __state)
    {
        if (fullscreenMode is FullScreenMode.ExclusiveFullScreen or FullScreenMode.FullScreenWindow or FullScreenMode.MaximizedWindow) return;
        if (GameMain.isRunning)
        {
            var wnd = WindowFunctions.FindGameWindow();
            if (wnd == IntPtr.Zero) return;
            WinApi.GetWindowRect(wnd, out var rc);
            __state = new Vector2Int(rc.Left, rc.Top);
            return;
        }
        else if (!LoadLastWindowRectEnabled.Value) return;
        int x = 0, y = 0, w = 0, h = 0;
        var rect = LastWindowRect.Value;
        if (rect is not { z: 0f, w: 0f })
        {
            x = Mathf.RoundToInt(rect.x);
            y = Mathf.RoundToInt(rect.y);
            w = Mathf.RoundToInt(rect.z);
            h = Mathf.RoundToInt(rect.w);
        }
        width = w;
        height = h;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution), typeof(int), typeof(int), typeof(FullScreenMode), typeof(int))]
    private static void Screen_SetResolution_Postfix(FullScreenMode fullscreenMode, Vector2Int __state)
    {
        if (fullscreenMode is FullScreenMode.ExclusiveFullScreen or FullScreenMode.FullScreenWindow or FullScreenMode.MaximizedWindow) return;
        var gameRunning = GameMain.isRunning;
        if (!LoadLastWindowRectEnabled.Value && !gameRunning) return;
        var wnd = WindowFunctions.FindGameWindow();
        if (wnd == IntPtr.Zero) return;
        int x, y;
        if (gameRunning)
        {
            x = __state.x;
            y = __state.y;
        }
        else
        {
            var rect = LastWindowRect.Value;
            if (rect is { z: 0f, w: 0f }) return;
            x = Mathf.RoundToInt(rect.x);
            y = Mathf.RoundToInt(rect.y);
        }
        ThreadingHelper.Instance.StartCoroutine(SetWindowPositionCoroutine(wnd, x, y));
    }

    private static GameOption _gameOption;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow._OnOpen))]
    private static void UIOptionWindow__OnOpen_Postfix()
    {
        _gameOption = DSPGame.globalOption;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameOption), nameof(GameOption.Apply))]
    private static IEnumerable<CodeInstruction> UIOptionWindow_ApplyOptions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Screen), nameof(Screen.SetResolution), [typeof(int), typeof(int), typeof(bool), typeof(int)]))
        ).Advance(1).Labels.Add(label1);
        matcher.Start().Insert(
            Transpilers.EmitDelegate(() =>
                _gameOption.fullscreen == DSPGame.globalOption.fullscreen &&
                _gameOption.resolution.width == DSPGame.globalOption.resolution.width &&
                _gameOption.resolution.height == DSPGame.globalOption.resolution.height &&
                _gameOption.resolution.refreshRate == DSPGame.globalOption.resolution.refreshRate
            ),
            new CodeInstruction(OpCodes.Brtrue, label1)
        );
        return matcher.InstructionEnumeration();
    }

    private class EnableWindowResize : PatchImpl<EnableWindowResize>
    {
        private static bool _enabled;

        protected override void OnEnable()
        {
            var wnd = WindowFunctions.FindGameWindow();
            if (wnd == IntPtr.Zero)
            {
                Enable(false);
                return;
            }

            _enabled = true;
            WinApi.SetWindowLong(wnd, WinApi.GWL_STYLE,
                WinApi.GetWindowLong(wnd, WinApi.GWL_STYLE) | WinApi.WS_THICKFRAME | WinApi.WS_MAXIMIZEBOX);
        }

        protected override void OnDisable()
        {
            var wnd = WindowFunctions.FindGameWindow();
            if (wnd == IntPtr.Zero)
                return;

            _enabled = false;
            WinApi.SetWindowLong(wnd, WinApi.GWL_STYLE,
                WinApi.GetWindowLong(wnd, WinApi.GWL_STYLE) & ~(WinApi.WS_THICKFRAME | WinApi.WS_MAXIMIZEBOX));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow.ApplyOptions))]
        private static void UIOptionWindow_ApplyOptions_Postfix()
        {
            var wnd = WindowFunctions.FindGameWindow();
            if (wnd == IntPtr.Zero) return;
            if (_enabled)
                WinApi.SetWindowLong(wnd, WinApi.GWL_STYLE,
                    WinApi.GetWindowLong(wnd, WinApi.GWL_STYLE) | WinApi.WS_THICKFRAME | WinApi.WS_MAXIMIZEBOX);
            else
                WinApi.SetWindowLong(wnd, WinApi.GWL_STYLE,
                    WinApi.GetWindowLong(wnd, WinApi.GWL_STYLE) & ~(WinApi.WS_THICKFRAME | WinApi.WS_MAXIMIZEBOX));
        }
    }

    /*
    private class AutoSaveOpt: PatchImpl<AutoSaveOpt>
    {
        protected override void OnEnable()
        {
            Directory.CreateDirectory(GameConfig.gameSaveFolder + "AutoSaves/");
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

    private class ConvertSavesFromPeace : PatchImpl<ConvertSavesFromPeace>
    {
        private static bool _needConvert;

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
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ConvertSavesFromPeace), nameof(_needConvert)))
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

    private class ProfileBasedOption : PatchImpl<ProfileBasedOption>
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameOption), nameof(GameOption.LoadGlobal))]
        private static bool GameOption_LoadGlobal_Prefix(ref GameOption __instance)
        {
            UXAssist.Logger.LogDebug("Loading global option");
            var profileName = WindowFunctions.ProfileName;
            if (profileName == null)
            {
                // We should initialize WindowFunctions before using WindowFunctions.ProfileName
                WindowFunctions.Init();
                profileName = WindowFunctions.ProfileName;
                if (profileName == null) return true;
            }
            if (string.Compare(DefaultProfileName.Value, profileName, StringComparison.OrdinalIgnoreCase) == 0) return true;
            var optionPath = $"{GameConfig.gameDocumentFolder}Option/{profileName}.xml";
            if (File.Exists(optionPath))
            {
                try
                {
                    __instance.ImportXML(optionPath);
                    return false;
                }
                catch
                {
                }
            }
            var gameXMLOptionPath = GameConfig.gameXMLOptionPath;
            if (File.Exists(gameXMLOptionPath))
            {
                try
                {
                    __instance.ImportXML(gameXMLOptionPath);
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            GameOption.newlyCreated = true;
            __instance.SetDefault();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameOption), nameof(GameOption.SaveGlobal))]
        private static bool GameOption_SaveGlobal_Prefix(ref GameOption __instance)
        {
            var profileName = WindowFunctions.ProfileName;
            if (profileName == null) return true;
            if (string.Compare(DefaultProfileName.Value, profileName, StringComparison.OrdinalIgnoreCase) == 0) return true;
            var path = $"{GameConfig.gameDocumentFolder}Option";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            try
            {
                using FileStream fileStream = new($"{path}/{profileName}.xml", FileMode.Create, FileAccess.Write, FileShare.None);
                using XmlTextWriter xmlTextWriter = new(fileStream, Console.OutputEncoding);
                xmlTextWriter.Formatting = Formatting.Indented;
                __instance.ExportXML(xmlTextWriter);
                xmlTextWriter.Close();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            return false;
        }
    }

    [PatchSetCallbackFlag(PatchCallbackFlag.CallOnDisableAfterUnpatch)]
    private class MouseCursorScaleUp : PatchImpl<MouseCursorScaleUp>
    {
        public static bool NeedReloadCursors;

        protected override void OnEnable()
        {
            if (!NeedReloadCursors) return;
            if (!UICursor.loaded) return;
            UICursor.loaded = false;
            UICursor.LoadCursors();
        }

        protected override void OnDisable()
        {
            if (!NeedReloadCursors) return;
            if (!UICursor.loaded) return;
            UICursor.loaded = false;
            UICursor.LoadCursors();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UICursor), nameof(UICursor.LoadCursors))]
        private static IEnumerable<CodeInstruction> UICursor_LoadCursors_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /*
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_S),
                new CodeMatch(OpCodes.Newarr)
            );
            var startPos = matcher.Pos;
            matcher.Advance(2).MatchForward(false,
                new CodeMatch(OpCodes.Stsfld, AccessTools.Field(typeof(UICursor), nameof(UICursor.cursorTexs)))
            );
            var endPos = matcher.Pos + 1;
            matcher.Start().Advance(startPos).RemoveInstructions(endPos - startPos);
            matcher.InsertAndAdvance(
                Transpilers.EmitDelegate(() =>
                {
                    var pluginfolder = Util.PluginFolder;
                    UICursor.cursorTexs =
                    [
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-transfer.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-target-in.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-target-out.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-target-a.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-target-b.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-ban.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-delete.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-reform.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-dyson-node-create.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-painter.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-eyedropper.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-eraser.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-upgrade.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-downgrade.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-blank.png"),
                        Util.LoadTexture($"{pluginfolder}/assets/cursor/cursor-remove.png")
                    ];
                })
            );
            */
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stsfld, AccessTools.Field(typeof(UICursor), nameof(UICursor.cursorHots))),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Stsfld, AccessTools.Field(typeof(UICursor), nameof(UICursor.loaded)))
            ).Advance(1).InsertAndAdvance(
                Transpilers.EmitDelegate(() =>
                {
                    var multiplier = MouseCursorScaleUpMultiplier.Value;
                    for (var i = 0; i < UICursor.cursorTexs.Length; i++)
                    {
                        var cursor = UICursor.cursorTexs[i];
                        if (cursor == null) continue;
                        var newWidth = 32 * multiplier;
                        var newHeight = 32 * multiplier;
                        if (cursor.width == newWidth && cursor.height == newHeight) continue;
                        UICursor.cursorTexs[i] = ResizeTexture2D(cursor, newWidth, newHeight);
                    }

                    if (multiplier <= 1) return;
                    for (var i = UICursor.cursorHots.Length - 1; i >= 0; i--)
                    {
                        UICursor.cursorHots[i] = new Vector2(UICursor.cursorHots[i].x * multiplier, UICursor.cursorHots[i].y * multiplier);
                    }
                })
            ).MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Cursor), nameof(Cursor.SetCursor), [typeof(Texture2D), typeof(Vector2), typeof(CursorMode)]))
            ).SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_1));
            return matcher.InstructionEnumeration();

            Texture2D ResizeTexture2D(Texture2D texture2D, int targetWidth, int targetHeight)
            {
                var oldActive = RenderTexture.active;
                var rt = new RenderTexture(targetWidth, targetHeight, 32)
                {
                    antiAliasing = 8
                };
                RenderTexture.active = rt;
                Graphics.Blit(texture2D, rt);
                rt.ResolveAntiAliasedSurface();
                var result = new Texture2D(targetWidth, targetHeight, texture2D.format, false);
                result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                result.filterMode = FilterMode.Trilinear;
                result.Apply();
                RenderTexture.active = oldActive;
                rt.Release();
                return result;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UICursor), nameof(UICursor.cursorIndexApply), MethodType.Setter)]
        private static IEnumerable<CodeInstruction> UICursor_set_cursorIndexApply_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Cursor), nameof(Cursor.SetCursor), [typeof(Texture2D), typeof(Vector2), typeof(CursorMode)]))
            ).SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_1));
            return matcher.InstructionEnumeration();
        }
    }
}