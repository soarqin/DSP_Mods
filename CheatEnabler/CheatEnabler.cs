using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CheatEnabler;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class CheatEnabler : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    public static ConfigEntry<KeyboardShortcut> Hotkey;
    private static bool _configWinInitialized = false;
    private static UIConfigWindow _configWin;
    
    private static Harmony _windowPatch;
    private static Harmony _patch;

    private static bool _initialized;

    private void Awake()
    {
        Hotkey = Config.Bind("General", "Shortcut", KeyboardShortcut.Deserialize("BackQuote + LeftAlt"), "Shortcut to open config window");
        DevShortcuts.Enabled = Config.Bind("General", "DevShortcuts", false, "Enable DevMode shortcuts");
        AbnormalDisabler.Enabled = Config.Bind("General", "DisableAbnormalChecks", false,
            "disable all abnormal checks");
        TechPatch.Enabled = Config.Bind("General", "UnlockTech", false,
            "Unlock clicked tech by holding key-modifilers(Shift/Alt/Ctrl)");
        FactoryPatch.ImmediateEnabled = Config.Bind("Build", "ImmediateBuild", false,
            "Build immediately");
        FactoryPatch.ArchitectModeEnabled = Config.Bind("Build", "Architect", false,
            "Architect Mode");
        FactoryPatch.UnlimitInteractiveEnabled = Config.Bind("Build", "UnlimitInteractive", false,
            "Unlimit interactive range");
        FactoryPatch.NoConditionEnabled = Config.Bind("Build", "BuildWithoutCondition", false,
            "Build without condition");
        FactoryPatch.NoCollisionEnabled = Config.Bind("Build", "NoCollision", false,
            "No collision");
        FactoryPatch.BeltSignalGeneratorEnabled = Config.Bind("Build", "BeltSignalGenerator", false,
            "Belt signal generator");
        FactoryPatch.BeltSignalCountRecipeEnabled = Config.Bind("Build", "BeltSignalCountRecipe", false,
            "Belt signal count all raws and intermediates in statistics");
        FactoryPatch.NightLightEnabled = Config.Bind("Build", "NightLight", false,
            "Night light");
        FactoryPatch.RemovePowerSpaceLimitEnabled = Config.Bind("Build", "RemovePowerDistanceLimit", false,
            "Remove distance limit for wind turbines and geothermals");
        FactoryPatch.BoostWindPowerEnabled = Config.Bind("Build", "BoostWindPower", false,
            "Boost wind power");
        FactoryPatch.BoostSolarPowerEnabled = Config.Bind("Build", "BoostSolarPower", false,
            "Boost solar power");
        FactoryPatch.BoostFuelPowerEnabled = Config.Bind("Build", "BoostFuelPower", false,
            "Boost fuel power");
        FactoryPatch.BoostGeothermalPowerEnabled = Config.Bind("Build", "BoostGeothermalPower", false,
            "Boost geothermal power");
        ResourcePatch.InfiniteEnabled = Config.Bind("Planet", "AlwaysInfiniteResource", false,
            "always infinite natural resource");
        ResourcePatch.FastEnabled = Config.Bind("Planet", "FastMining", false,
            "super-fast mining speed");
        WaterPumperPatch.Enabled = Config.Bind("Planet", "WaterPumpAnywhere", false,
            "Can pump water anywhere (while water type is not None)");
        TerraformPatch.Enabled = Config.Bind("Planet", "TerraformAnyway", false,
            "Can do terraform without enough sands");
        DysonSpherePatch.SkipBulletEnabled = Config.Bind("DysonSphere", "SkipBullet", false,
            "Skip bullet");
        DysonSpherePatch.SkipAbsorbEnabled = Config.Bind("DysonSphere", "SkipAbsorb", false,
            "Skip absorption");
        DysonSpherePatch.QuickAbsortEnabled = Config.Bind("DysonSphere", "QuickAbsorb", false,
            "Quick absorb");
        DysonSpherePatch.EjectAnywayEnabled = Config.Bind("DysonSphere", "EjectAnyway", false,
            "Eject anyway");
        DysonSpherePatch.OverclockEjectorEnabled = Config.Bind("DysonSphere", "OverclockEjector", false,
            "Overclock ejector");
        DysonSpherePatch.OverclockSiloEnabled = Config.Bind("DysonSphere", "OverclockSilo", false,
            "Overclock silo");
        BirthPlanetPatch.SitiVeinsOnBirthPlanet = Config.Bind("Birth", "SiTiVeinsOnBirthPlanet", false,
            "Silicon/Titanium on birth planet");
        BirthPlanetPatch.FireIceOnBirthPlanet = Config.Bind("Birth", "FireIceOnBirthPlanet", false,
            "Fire ice on birth planet");
        BirthPlanetPatch.KimberliteOnBirthPlanet = Config.Bind("Birth", "KimberliteOnBirthPlanet", false,
            "Kimberlite on birth planet");
        BirthPlanetPatch.FractalOnBirthPlanet = Config.Bind("Birth", "FractalOnBirthPlanet", false,
            "Fractal silicon on birth planet");
        BirthPlanetPatch.OrganicOnBirthPlanet = Config.Bind("Birth", "OrganicOnBirthPlanet", false,
            "Organic crystal on birth planet");
        BirthPlanetPatch.OpticalOnBirthPlanet = Config.Bind("Birth", "OpticalOnBirthPlanet", false,
            "Optical grating crystal on birth planet");
        BirthPlanetPatch.SpiniformOnBirthPlanet = Config.Bind("Birth", "SpiniformOnBirthPlanet", false,
            "Spiniform stalagmite crystal on birth planet");
        BirthPlanetPatch.UnipolarOnBirthPlanet = Config.Bind("Birth", "UnipolarOnBirthPlanet", false,
            "Unipolar magnet on birth planet");
        BirthPlanetPatch.FlatBirthPlanet = Config.Bind("Birth", "FlatBirthPlanet", false,
            "Birth planet is solid flat (no water at all)");
        BirthPlanetPatch.HighLuminosityBirthStar = Config.Bind("Birth", "HighLuminosityBirthStar", false,
            "Birth star has high luminosity");

        I18N.Init();
        I18N.Add("CheatEnabler Config", "CheatEnabler Config", "CheatEnabler设置");
        I18N.Apply();

        // UI Patch
        _windowPatch = Harmony.CreateAndPatchAll(typeof(UI.MyWindowManager.Patch));
        _patch = Harmony.CreateAndPatchAll(typeof(CheatEnabler));

        DevShortcuts.Init();
        AbnormalDisabler.Init();
        TechPatch.Init();
        FactoryPatch.Init();
        ResourcePatch.Init();
        WaterPumperPatch.Init();
        TerraformPatch.Init();
        DysonSpherePatch.Init();
        BirthPlanetPatch.Init();
    }

    private void OnDestroy()
    {
        BirthPlanetPatch.Uninit();
        DysonSpherePatch.Uninit();
        TerraformPatch.Uninit();
        WaterPumperPatch.Uninit();
        ResourcePatch.Uninit();
        FactoryPatch.Uninit();
        TechPatch.Uninit();
        AbnormalDisabler.Uninit();
        DevShortcuts.Uninit();
        _patch?.UnpatchSelf();
        _patch = null;
        _windowPatch?.UnpatchSelf();
        _windowPatch = null;
    }

    private void Update()
    {
        if (VFInput.inputing)
        {
            return;
        }
        
        if (Hotkey.Value.IsDown())
            ToggleConfigWindow();
    }

    private void LateUpdate()
    {
        FactoryPatch.NightLight.LateUpdate();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), nameof(UIRoot.OpenMainMenuUI))]
    public static void UIRoot_OpenMainMenuUI_Postfix()
    {
        if (_initialized) return;
        {
            var mainMenu = UIRoot.instance.uiMainMenu;
            var src = mainMenu.newGameButton;
            var parent = src.transform.parent;
            var btn = Instantiate(src, parent);
            btn.name = "button-cheatenabler-config";
            var l = btn.text.GetComponent<Localizer>();
            if (l != null)
            {
                l.stringKey = "CheatEnabler Config";
                l.translation = "CheatEnabler Config".Translate();
            }
            btn.text.text = "CheatEnabler Config".Translate();
            btn.text.fontSize = btn.text.fontSize * 7 / 8;
            I18N.OnInitialized += () => { btn.text.text = "CheatEnabler Config".Translate(); };
            var vec = ((RectTransform)mainMenu.exitButton.transform).anchoredPosition3D;
            var vec2 = ((RectTransform)mainMenu.creditsButton.transform).anchoredPosition3D;
            var transform1 = (RectTransform)btn.transform;
            transform1.anchoredPosition3D = new Vector3(vec.x, vec.y + (vec.y - vec2.y) * 2, vec.z);
            btn.button.onClick.RemoveAllListeners();
            btn.button.onClick.AddListener(ToggleConfigWindow);
        }
        {
            var panel = UIRoot.instance.uiGame.planetGlobe;
            var src = panel.button2;
            var sandboxMenu = UIRoot.instance.uiGame.sandboxMenu;
            var icon = sandboxMenu.categoryButtons[6].transform.Find("icon")?.GetComponent<Image>()?.sprite;
            var b = Instantiate(src, src.transform.parent);
            var panelButtonGo = b.gameObject;
            var rect = (RectTransform)panelButtonGo.transform;
            var btn = panelButtonGo.GetComponent<UIButton>();
            var img = panelButtonGo.transform.Find("button-2/icon")?.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = icon;
            }
            if (panelButtonGo != null && btn != null)
            {
                panelButtonGo.name = "open-cheatenabler-config";
                rect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                rect.anchoredPosition3D = new Vector3(128f, -105f, 0f);
                b.onClick.RemoveAllListeners();
                btn.onClick += _ => { ToggleConfigWindow(); };
                btn.tips.tipTitle = "CheatEnabler Config";
                I18N.OnInitialized += () => { btn.tips.tipTitle = "CheatEnabler Config".Translate(); };
                btn.tips.tipText = null;
                btn.tips.corner = 9;
                btn.tips.offset = new Vector2(-20f, -20f);
                panelButtonGo.SetActive(true);
            }
        }
        _initialized = true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIBuildMenu), nameof(UIBuildMenu._OnUpdate))]
    private static IEnumerable<CodeInstruction> UIBuildMenu__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.inScreen)))
        );
        matcher.Repeat(codeMatcher =>
        {
            var jumpPos = codeMatcher.Advance(1).Operand;
            codeMatcher.Advance(-1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.noModifier))),
                new CodeInstruction(OpCodes.Brfalse_S, jumpPos)
            ).Advance(2);
        });
        return matcher.InstructionEnumeration();
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIButton), nameof(UIButton.LateUpdate))]
    private static IEnumerable<CodeInstruction> UIButton_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldloc_2),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject))),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.activeSelf)))
        );
        var labels = matcher.Labels;
        matcher.Labels = null;
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldloc_2).WithLabels(labels),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.parent))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.parent))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), nameof(Transform.SetAsLastSibling)))
        );
        return matcher.InstructionEnumeration();
    }

    private static void ToggleConfigWindow()
    {
        if (!_configWinInitialized)
        {
            if (!I18N.Initialized()) return;
            _configWinInitialized = true;
            _configWin = UIConfigWindow.CreateInstance();
        }

        if (_configWin.active)
        {
            _configWin._Close();
        }
        else
        {
            UIRoot.instance.uiGame.ShutPlayerInventory();
            _configWin.Open();
        }
    }
}
