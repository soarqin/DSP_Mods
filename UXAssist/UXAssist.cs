using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CommonAPI;
using CommonAPI.Systems;
using crecheng.DSPModSave;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.Functions;
using UXAssist.Patches;
using UXAssist.UI;
using Util = UXAssist.Common.Util;

namespace UXAssist;

[BepInDependency(CommonAPIPlugin.GUID)]
[BepInDependency(DSPModSavePlugin.MODGUID)]
[CommonAPISubmoduleDependency(nameof(CustomKeyBindSystem))]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class UXAssist : BaseUnityPlugin, IModCanSave
{
    public new static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private static bool _configWinInitialized;
    private static MyConfigWindow _configWin;
    private static bool _initialized;
    private static PressKeyBind _toggleKey;
    private static ConfigFile _dummyConfig;
    private Type[] _patches, _compats;

    #region IModCanSave
    private const ushort ModSaveVersion = 1;

    public void Export(BinaryWriter w)
    {
        w.Write(ModSaveVersion);
        FactoryPatch.Export(w);
    }

    public void Import(BinaryReader r)
    {
        var version = r.ReadUInt16();
        if (version <= 0) return;
        FactoryPatch.Import(r);
    }

    public void IntoOtherSave()
    {
    }
    #endregion

    private void Awake()
    {
        _dummyConfig = new ConfigFile(Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_GUID + "_dummy.cfg"), false)
        {
            SaveOnConfigSet = false
        };
        _toggleKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.BackQuote, CombineKey.ALT_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "OpenUXAssistConfigWindow",
            canOverride = true
        });
        GamePatch.EnableWindowResizeEnabled = Config.Bind("Game", "EnableWindowResize", false,
            "Enable game window resize (maximum box and thick frame)");
        GamePatch.LoadLastWindowRectEnabled = Config.Bind("Game", "LoadLastWindowRect", false,
            "Load last window position and size when game starts");
        GamePatch.LastWindowRect = Config.Bind("Game", "LastWindowRect", new Vector4(0f, 0f, 0f, 0f),
            "Last window position and size");
        GamePatch.MouseCursorScaleUpMultiplier = Config.Bind("Game", "MouseCursorScaleUpMultiplier", 1,
            "Mouse cursor scale up multiplier");
        GamePatch.ProfileBasedSaveFolderEnabled = Config.Bind("Game", "ProfileBasedSaveFolder", false,
            "Profile-based save folder");
        GamePatch.ProfileBasedOptionEnabled = Config.Bind("Game", "ProfileBasedOption", false,
            "Profile-based option");
        GamePatch.DefaultProfileName = Config.Bind("Game", "DefaultProfileName", "Default",
            "Default profile name, used when profile-based save folder is enabled. Use original game save folder if matched");
        /*
        GamePatch.AutoSaveOptEnabled = Config.Bind("Game", "AutoSaveOpt", false,
            "Better auto-save mechanism");
        */
        GamePatch.ConvertSavesFromPeaceEnabled = Config.Bind("Game", "ConvertSavesFromPeace", false,
            "Convert saves from Peace mode to Combat mode on save loading");
        GamePatch.GameUpsFactor = _dummyConfig.Bind("Game", "GameUpsFactor", 1.0,
            "Game UPS factor (1.0 for normal speed)");
        WindowFunctions.ProcessPriority = Config.Bind("Game", "ProcessPriority", 2,
            new ConfigDescription("Game process priority\n  0: High  1: Above Normal  2: Normal  3: Below Normal  4: Idle", new AcceptableValueRange<int>(0, 4)));
        WindowFunctions.ProcessAffinity = Config.Bind("Game", "CPUAffinity", -1,
            new ConfigDescription("""
                                  Game process CPU affinity
                                    0: All  1: First-half CPUs  2. First 8 CPUs (if total CPUs are greater than 16)
                                    3. All Performance Cores(If Intel 13th or greater)  4. All Efficiency Cores(If Intel 13th or greater)
                                  """, new AcceptableValueRange<int>(0, 4)));

        FactoryPatch.UnlimitInteractiveEnabled = Config.Bind("Factory", "UnlimitInteractive", false,
            "Unlimit interactive range");
        FactoryPatch.RemoveSomeConditionEnabled = Config.Bind("Factory", "RemoveSomeBuildConditionCheck", false,
            "Remove part of build condition checks that does not affect game logic");
        FactoryPatch.NightLightEnabled = Config.Bind("Factory", "NightLight", false,
            "Night light");
        FactoryPatch.NightLightAngleX = Config.Bind("Factory", "NightLightAngleX", -8f, "Night light angle X");
        FactoryPatch.NightLightAngleY = Config.Bind("Factory", "NightLightAngleY", -2f, "Night light angle Y");
        PlanetPatch.PlayerActionsInGlobeViewEnabled = Config.Bind("Planet", "PlayerActionsInGlobeView", false,
            "Enable player actions in globe view");
        FactoryPatch.RemoveBuildRangeLimitEnabled = Config.Bind("Factory", "RemoveBuildRangeLimit", false,
                "Remove limit for build range and maximum count of drag building belts/buildings\nNote: this does not affect range limit for mecha drones' action");
        FactoryPatch.LargerAreaForUpgradeAndDismantleEnabled = Config.Bind("Factory", "LargerAreaForUpgradeAndDismantle", false,
            "Increase maximum area size for upgrade and dismantle to 31x31 (from 11x11)");
        FactoryPatch.LargerAreaForTerraformEnabled = Config.Bind("Factory", "LargerAreaForTerraform", false,
                "Increase maximum area size for terraform to 30x30 (from 10x10)\nNote: this may impact game performance while using large area");
        FactoryPatch.OffGridBuildingEnabled = Config.Bind("Factory", "OffGridBuilding", false,
            "Enable off grid building and stepped rotation");
        FactoryPatch.TreatStackingAsSingleEnabled = Config.Bind("Factory", "TreatStackingAsSingle", false,
            "Treat stack items as single in monitor components");
        FactoryPatch.QuickBuildAndDismantleLabsEnabled = Config.Bind("Factory", "QuickBuildAndDismantleLab", false,
            "Quick build and dismantle stacking labs");
        FactoryPatch.ProtectVeinsFromExhaustionEnabled = Config.Bind("Factory", "ProtectVeinsFromExhaustion", false,
            "Protect veins from exhaustion");
        FactoryPatch.ProtectVeinsFromExhaustion.KeepVeinAmount = Config.Bind("Factory", "KeepVeinAmount", 1000, new ConfigDescription("Keep veins amount (0 to disable)", new AcceptableValueRange<int>(0, 10000))).Value;
        FactoryPatch.ProtectVeinsFromExhaustion.KeepOilSpeed = Config.Bind("Factory", "KeepOilSpeed", 1.0f, new ConfigDescription("Keep minimal oil speed (< 0.1 to disable)", new AcceptableValueRange<float>(0.0f, 10.0f))).Value;
        FactoryPatch.DoNotRenderEntitiesEnabled = Config.Bind("Factory", "DoNotRenderEntities", false,
            "Do not render factory entities");
        FactoryPatch.DragBuildPowerPolesEnabled = Config.Bind("Factory", "DragBuildPowerPoles", false,
            "Drag building power poles in maximum connection range");
        FactoryPatch.DragBuildPowerPolesAlternatelyEnabled = Config.Bind("Factory", "DragBuildPowerPolesAlternately", true,
            "Build Tesla Tower and Wireless Power Tower alternately");
        FactoryPatch.BeltSignalsForBuyOutEnabled = Config.Bind("Factory", "BeltSignalsForBuyOut", false,
            "Belt signals for buy out dark fog items automatically");
        FactoryPatch.TankFastFillInAndTakeOutEnabled = Config.Bind("Factory", "TankFastFillInAndTakeOut", false,
            "Fast fill in to and take out from tanks");
        FactoryPatch.TankFastFillInAndTakeOutMultiplier = Config.Bind("Factory", "TankFastFillInAndTakeOutMultiplier", 1000, "Speed multiplier for fast filling in to and takeing out from tanks");
        FactoryPatch.CutConveyorBeltEnabled = Config.Bind("Factory", "CutConveyorBeltShortcut", false,
            "Cut conveyor belt (with shortcut key)");
        FactoryPatch.TweakBuildingBufferEnabled = Config.Bind("Factory", "TweakBuildingBuffer", false,
            "Tweak buffer count for assemblers and power generators");
        FactoryPatch.AssemblerBufferTimeMultiplier = Config.Bind("Factory", "AssemblerBufferTimeMultiplier", 4, new ConfigDescription("Assembler buffer time multiplier in seconds", new AcceptableValueRange<int>(2, 10)));
        FactoryPatch.AssemblerBufferMininumMultiplier = Config.Bind("Factory", "AssemblerBufferMininumMultiplier", 4, new ConfigDescription("Assembler buffer minimum multiplier", new AcceptableValueRange<int>(2, 10)));
        FactoryPatch.ReceiverBufferCount = Config.Bind("Factory", "ReceiverBufferCount", 1, new ConfigDescription("Ray Receiver Graviton Lens buffer count", new AcceptableValueRange<int>(1, 20)));
        LogisticsPatch.LogisticsCapacityTweaksEnabled = Config.Bind("Factory", "LogisticsCapacityTweaks", true,
            "Logistics capacity related tweaks");
        LogisticsPatch.AllowOverflowInLogisticsEnabled = Config.Bind("Factory", "AllowOverflowInLogistics", false,
            "Allow overflow in logistic stations");
        LogisticsPatch.LogisticsConstrolPanelImprovementEnabled = Config.Bind("Factory", "LogisticsConstrolPanelImprovement", false,
            "Logistics control panel improvement");
        LogisticsPatch.RealtimeLogisticsInfoPanelEnabled = Config.Bind("Factory", "RealtimeLogisticsInfoPanel", false,
            "Realtime logistics info panel");
        LogisticsPatch.RealtimeLogisticsInfoPanelBarsEnabled = Config.Bind("Factory", "RealtimeLogisticsInfoPanelBars", false,
            "Realtime logistics info panel - Show status bars for storage item");
        PlanetFunctions.OrbitalCollectorMaxBuildCount = Config.Bind("Factory", "OCMaxBuildCount", 0, "Maximum Orbital Collectors to build once, set to 0 to build as many as possible");
        PlayerPatch.EnhancedMechaForgeCountControlEnabled = Config.Bind("Player", "EnhancedMechaForgeCountControl", false,
            "Enhanced count control for hand-make, increases maximum of count to 1000, and you can hold Ctrl/Shift/Alt to change the count rapidly");
        PlayerPatch.HideTipsForSandsChangesEnabled = Config.Bind("Player", "HideTipsForGettingSands", false,
            "Hide tips for getting soil piles");
        PlayerPatch.ShortcutKeysForStarsNameEnabled = Config.Bind("Player", "ShortcutKeysForStarsName", false,
            "Shortcut keys for showing stars' name");
        PlayerPatch.AutoNavigationEnabled = Config.Bind("Player", "AutoNavigation", false,
            "Auto navigation");
        PlayerPatch.AutoCruiseEnabled = Config.Bind("Player", "AutoCruise", false,
            "Auto-cruise enabled");
        PlayerPatch.AutoBoostEnabled = Config.Bind("Player", "AutoBoost", false,
            "Auto boost speed with auto-cruise enabled");
        PlayerPatch.DistanceToWarp = Config.Bind("Player", "DistanceToWarp", 5.0, "Distance to warp (in AU)");
        TechPatch.SorterCargoStackingEnabled = Config.Bind("Tech", "SorterCargoStacking", false,
            "Restore upgrades of `Sorter Cargo Stacking` on panel");
        TechPatch.BatchBuyoutTechEnabled = Config.Bind("Tech", "BatchBuyoutTech", false,
            "Can buy out techs with their prerequisites");
        DysonSpherePatch.StopEjectOnNodeCompleteEnabled = Config.Bind("DysonSphere", "StopEjectOnNodeComplete", false,
            "Stop ejectors when available nodes are all filled up");
        DysonSpherePatch.OnlyConstructNodesEnabled = Config.Bind("DysonSphere", "OnlyConstructNodes", false,
            "Construct only nodes but frames");
        DysonSpherePatch.AutoConstructMultiplier = Config.Bind("DysonSphere", "AutoConstructMultiplier", 1, "Dyson Sphere auto-construct speed multiplier");

        I18N.Init();
        I18N.Add("UXAssist Config", "UXAssist Config", "UX助手设置");
        I18N.Add("KEYOpenUXAssistConfigWindow", "Open UXAssist Config Window", "打开UX助手设置面板");

        // UI Patches
        GameLogic.Enable(true);

        UIConfigWindow.Init();

        _patches = Util.GetTypesFiltered(Assembly.GetExecutingAssembly(),
            t => string.Equals(t.Namespace, "UXAssist.Patches", StringComparison.Ordinal) || string.Equals(t.Namespace, "UXAssist.Functions", StringComparison.Ordinal));
        _patches?.Do(type => type.GetMethod("Init")?.Invoke(null, null));
        _compats = Util.GetTypesInNamespace(Assembly.GetExecutingAssembly(), "UXAssist.ModsCompat");
        _compats?.Do(type => type.GetMethod("Init")?.Invoke(null, null));

        I18N.Apply();
        I18N.OnInitialized += RecreateConfigWindow;
    }

    private void Start()
    {
        MyWindowManager.InitBaseObjects();
        MyWindowManager.Enable(true);
        UIPatch.Enable(true);

        _patches?.Do(type => type.GetMethod("Start")?.Invoke(null, null));

        object[] parameters = [UIPatch.GetHarmony()];
        _compats?.Do(type => type.GetMethod("Start")?.Invoke(null, parameters));
    }

    private void OnDestroy()
    {
        _patches?.Do(type => type.GetMethod("Uninit")?.Invoke(null, null));

        UIPatch.Enable(false);
        MyWindowManager.Enable(false);
        GameLogic.Enable(false);
    }

    private void Update()
    {
        if (VFInput.inputing) return;
        if (VFInput.onGUI)
        {
            LogisticsPatch.LogisticsCapacityTweaks.UpdateInput();
        }
        if (_toggleKey.keyValue)
        {
            ToggleConfigWindow();
        }
        GamePatch.OnUpdate();
        FactoryPatch.OnUpdate();
        PlayerPatch.OnUpdate();
        LogisticsPatch.OnUpdate();
    }

    private static void ToggleConfigWindow()
    {
        if (!_configWinInitialized)
        {
            if (!I18N.Initialized()) return;
            _configWinInitialized = true;
            _configWin = MyConfigWindow.CreateInstance();
        }

        if (_configWin.active)
        {
            _configWin._Close();
        }
        else
        {
            _configWin.Open();
        }
    }

    private static void RecreateConfigWindow()
    {
        if (!_configWinInitialized) return;
        var wasActive = _configWin.active;
        if (wasActive) _configWin._Close();
        MyConfigWindow.DestroyInstance(_configWin);
        _configWinInitialized = false;
        if (wasActive) ToggleConfigWindow();
    }

    [PatchGuid(PluginInfo.PLUGIN_GUID)]
    private class UIPatch: PatchImpl<UIPatch>
    {
        private static GameObject _buttonOnPlanetGlobe;

        protected override void OnEnable()
        {
            InitMenuButtons();
        }

        private static void InitMenuButtons()
        {
            if (_initialized) return;
            var uiRoot = UIRoot.instance;
            if (!uiRoot) return;
            {
                var mainMenu = uiRoot.uiMainMenu;
                var src = mainMenu.newGameButton;
                var parent = src.transform.parent;
                var btn = Instantiate(src, parent);
                btn.name = "button-uxassist-config";
                var l = btn.text.GetComponent<Localizer>();
                if (l != null)
                {
                    l.stringKey = "UXAssist Config";
                    l.translation = "UXAssist Config".Translate();
                }

                btn.text.text = "UXAssist Config".Translate();
                btn.text.fontSize = btn.text.fontSize * 7 / 8;
                I18N.OnInitialized += () => { btn.text.text = "UXAssist Config".Translate(); };
                var vec = ((RectTransform)mainMenu.exitButton.transform).anchoredPosition3D;
                var vec2 = ((RectTransform)mainMenu.creditsButton.transform).anchoredPosition3D;
                var transform1 = (RectTransform)btn.transform;
                transform1.anchoredPosition3D = new Vector3(vec.x, vec.y + (vec.y - vec2.y) * 2, vec.z);
                btn.button.onClick.RemoveAllListeners();
                btn.button.onClick.AddListener(ToggleConfigWindow);
            }
            {
                var panel = uiRoot.uiGame.planetGlobe;
                var src = panel.button2;
                var sandboxMenu = uiRoot.uiGame.sandboxMenu;
                var icon = sandboxMenu.categoryButtons[6].transform.Find("icon")?.GetComponent<Image>()?.sprite;
                var b = Instantiate(src, src.transform.parent);
                _buttonOnPlanetGlobe = b.gameObject;
                var rect = (RectTransform)_buttonOnPlanetGlobe.transform;
                var btn = _buttonOnPlanetGlobe.GetComponent<UIButton>();
                var img = _buttonOnPlanetGlobe.transform.Find("button-2/icon")?.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = icon;
                }

                if (_buttonOnPlanetGlobe != null && btn != null)
                {
                    _buttonOnPlanetGlobe.name = "open-uxassist-config";
                    rect.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                    rect.anchoredPosition3D = new Vector3(64f, -5f, 0f);
                    b.onClick.RemoveAllListeners();
                    btn.onClick += _ => { ToggleConfigWindow(); };
                    btn.tips.tipTitle = "UXAssist Config";
                    I18N.OnInitialized += () => { btn.tips.tipTitle = "UXAssist Config".Translate(); };
                    btn.tips.tipText = null;
                    btn.tips.corner = 9;
                    btn.tips.offset = new Vector2(-20f, -20f);
                    _buttonOnPlanetGlobe.SetActive(true);
                }
            }
            _initialized = true;
        }

        // Add config button to main menu
        [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), nameof(UIRoot._OnOpen))]
        public static void UIRoot__OnOpen_Postfix()
        {
            InitMenuButtons();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPlanetGlobe), nameof(UIPlanetGlobe.DistributeButtons))]
        private static void UIPlanetGlobe_DistributeButtons_Postfix(UIPlanetGlobe __instance)
        {
            if (_buttonOnPlanetGlobe == null) return;
            var rect = (RectTransform)_buttonOnPlanetGlobe.transform;
            if (__instance.dysonSphereSystemUnlocked || __instance.logisticsSystemUnlocked)
            {
                rect.anchoredPosition3D = new Vector3(64f, -5f, 0f);
            }
            else
            {
                rect.anchoredPosition3D = new Vector3(128f, -100f, 0f);
            }
        }
    }
}
