using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using BepInEx.Configuration;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;

namespace UXAssist.Patches;

public class GamePatch: PatchImpl<GamePatch>
{
    private const string GameWindowClass = "UnityWndClass";
    private static string _gameWindowTitle = "Dyson Sphere Program";

    public static string ProfileName { get; private set; }

    public static ConfigEntry<bool> EnableWindowResizeEnabled;
    public static ConfigEntry<bool> LoadLastWindowRectEnabled;
    public static ConfigEntry<int> MouseCursorScaleUpMultiplier;
    // public static ConfigEntry<bool> AutoSaveOptEnabled;
    public static ConfigEntry<bool> ConvertSavesFromPeaceEnabled;
    public static ConfigEntry<Vector4> LastWindowRect;
    public static ConfigEntry<bool> ProfileBasedSaveFolderEnabled;
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
                key = new CombineKey((int)KeyCode.KeypadMinus, 0, ECombineKeyAction.OnceClick, false),
                conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
                name = "UPSSpeedDown",
                canOverride = true
            }
        );
        I18N.Add("KEYUPSSpeedDown", "Decrease logical frame rate", "降低逻辑帧率");
        _speedUpKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
            {
                key = new CombineKey((int)KeyCode.KeypadPlus, 0, ECombineKeyAction.OnceClick, false),
                conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.UI | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
                name = "UPSSpeedUp",
                canOverride = true
            }
        );
        I18N.Add("KEYUPSSpeedUp", "Increase logical frame rate", "提升逻辑帧率");
        I18N.Add("Logical frame rate: {0}x", "Logical frame rate: {0}x", "逻辑帧速率: {0}x");

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

        EnableWindowResizeEnabled.SettingChanged += (_, _) => EnableWindowResize.Enable(EnableWindowResizeEnabled.Value);
        LoadLastWindowRectEnabled.SettingChanged += (_, _) => LoadLastWindowRect.Enable(LoadLastWindowRectEnabled.Value);
        MouseCursorScaleUpMultiplier.SettingChanged += (_, _) =>
        {
            MouseCursorScaleUp.reload = true;
            MouseCursorScaleUp.Enable(MouseCursorScaleUpMultiplier.Value > 1);
        };
        // AutoSaveOptEnabled.SettingChanged += (_, _) => AutoSaveOpt.Enable(AutoSaveOptEnabled.Value);
        ConvertSavesFromPeaceEnabled.SettingChanged += (_, _) => ConvertSavesFromPeace.Enable(ConvertSavesFromPeaceEnabled.Value);
        ProfileBasedSaveFolderEnabled.SettingChanged += (_, _) => RefreshSavePath();
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
    }

    public static void Start()
    {
        EnableWindowResize.Enable(EnableWindowResizeEnabled.Value);
        LoadLastWindowRect.Enable(LoadLastWindowRectEnabled.Value);
        MouseCursorScaleUp.reload = false;
        MouseCursorScaleUp.Enable(MouseCursorScaleUpMultiplier.Value > 1);
        // AutoSaveOpt.Enable(AutoSaveOptEnabled.Value);
        ConvertSavesFromPeace.Enable(ConvertSavesFromPeaceEnabled.Value);
        Enable(true);
    }

    public static void Uninit()
    {
        Enable(false);
        LoadLastWindowRect.Enable(false);
        EnableWindowResize.Enable(false);
        MouseCursorScaleUp.reload = false;
        MouseCursorScaleUp.Enable(false);
        // AutoSaveOpt.Enable(false);
        ConvertSavesFromPeace.Enable(false);
    }

    public static void OnUpdate()
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

    private static void RefreshSavePath()
    {
        if (ProfileName == null) return;

        if (UIRoot.instance.loadGameWindow.gameObject.activeSelf)
        {
            UIRoot.instance.loadGameWindow._Close();
        }
        if (UIRoot.instance.saveGameWindow.gameObject.activeSelf)
        {
            UIRoot.instance.saveGameWindow._Close();
        }

        string gameSavePath;
        if (ProfileBasedSaveFolderEnabled.Value && string.Compare(DefaultProfileName.Value, ProfileName, StringComparison.OrdinalIgnoreCase) != 0)
            gameSavePath = $"{GameConfig.overrideDocumentFolder}{GameConfig.gameName}/Save/{ProfileName}/";
        else
            gameSavePath = $"{GameConfig.overrideDocumentFolder}{GameConfig.gameName}/Save/";
        if (string.Compare(GameConfig.gameSavePath, gameSavePath, StringComparison.OrdinalIgnoreCase) == 0) return;
        GameConfig.gameSavePath = gameSavePath;
        if (!Directory.Exists(GameConfig.gameSavePath))
        {
            Directory.CreateDirectory(GameConfig.gameSavePath);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.HandleApplicationQuit))]
    private static void GameMain_HandleApplicationQuit_Prefix()
    {
        var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
        if (wnd == IntPtr.Zero) return;
        WinApi.GetWindowRect(wnd, out var rect);
        LastWindowRect.Value = new Vector4(rect.Left, rect.Top, Screen.width, Screen.height);
    }

    private class EnableWindowResize: PatchImpl<EnableWindowResize>
    {

        private static bool _enabled;

        protected override void OnEnable()
        {
            var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
            if (wnd == IntPtr.Zero)
            {
                Enable(false);
                return;
            }

            _enabled = true;
            WinApi.SetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE,
                WinApi.GetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE) | (int)WindowStyles.WS_THICKFRAME | (int)WindowStyles.WS_MAXIMIZEBOX);
        }

        protected override void OnDisable()
        {
            var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
            if (wnd == IntPtr.Zero)
                return;

            _enabled = false;
            WinApi.SetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE,
                WinApi.GetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE) & ~((int)WindowStyles.WS_THICKFRAME | (int)WindowStyles.WS_MAXIMIZEBOX));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow.ApplyOptions))]
        private static void UIOptionWindow_ApplyOptions_Postfix()
        {
            var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
            if (wnd == IntPtr.Zero) return;
            if (_enabled)
                WinApi.SetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE,
                    WinApi.GetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE) | (int)WindowStyles.WS_THICKFRAME | (int)WindowStyles.WS_MAXIMIZEBOX);
            else
                WinApi.SetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE,
                    WinApi.GetWindowLong(wnd, (int)WindowLongFlags.GWL_STYLE) & ~((int)WindowStyles.WS_THICKFRAME | (int)WindowStyles.WS_MAXIMIZEBOX));
        }
    }

    private class LoadLastWindowRect: PatchImpl<LoadLastWindowRect>
    {
        private static bool _loaded;

        protected override void OnEnable()
        {
            GameLogic.OnDataLoaded += VFPreload_InvokeOnLoadWorkEnded_Postfix;
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
        }

        protected override void OnDisable()
        {
            GameLogic.OnDataLoaded -= VFPreload_InvokeOnLoadWorkEnded_Postfix;
        }

        private static void MoveWindowPosition()
        {
            if (Screen.fullScreenMode is FullScreenMode.ExclusiveFullScreen or FullScreenMode.FullScreenWindow or FullScreenMode.MaximizedWindow || GameMain.isRunning) return;
            var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
            if (wnd == IntPtr.Zero) return;
            var rect = LastWindowRect.Value;
            if (rect is { z: 0f, w: 0f }) return;
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
            if (rect is { z: 0f, w: 0f }) return;
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

        private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            if (_loaded || Screen.fullScreenMode is FullScreenMode.ExclusiveFullScreen or FullScreenMode.FullScreenWindow or FullScreenMode.MaximizedWindow) return;
            _loaded = true;
            var wnd = WinApi.FindWindow(GameWindowClass, _gameWindowTitle);
            if (wnd == IntPtr.Zero) return;
            var rect = LastWindowRect.Value;
            if (rect is { z: 0f, w: 0f }) return;
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

    private class ConvertSavesFromPeace: PatchImpl<ConvertSavesFromPeace>
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

    private class MouseCursorScaleUp: PatchImpl<MouseCursorScaleUp>
    {
        public static bool reload;

        protected override void OnEnable()
        {
            if (!reload) return;
            if (!UICursor.loaded) return;
            UICursor.loaded = false;
            UICursor.LoadCursors();
        }

        protected override void OnDisable()
        {
            if (!reload) return;
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