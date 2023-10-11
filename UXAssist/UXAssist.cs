using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.UI;

namespace UXAssist;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class UXAssist : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    public static ConfigEntry<KeyboardShortcut> Hotkey;
    private static bool _configWinInitialized = false;
    private static MyConfigWindow _configWin;
    
    private static Harmony _windowPatch;
    private static Harmony _patch;

    private static bool _initialized;

    private void Awake()
    {
        Hotkey = Config.Bind("General", "Shortcut", KeyboardShortcut.Deserialize("BackQuote + LeftAlt"), "Shortcut to open config window");
        FactoryPatch.UnlimitInteractiveEnabled = Config.Bind("Factory", "UnlimitInteractive", false,
            "Unlimit interactive range");
        FactoryPatch.RemoveSomeConditionEnabled = Config.Bind("Factory", "RemoveSomeBuildConditionCheck", false,
            "Remove part of build condition checks that does not affect game logic");
        FactoryPatch.NightLightEnabled = Config.Bind("Factory", "NightLight", false,
            "Night light");
        PlanetPatch.PlayerActionsInGlobeViewEnabled = Config.Bind("Planet", "PlayerActionsInGlobeView", false,
            "Enable player actions in globe view");
        FactoryPatch.RemoveBuildRangeLimitEnabled = Config.Bind("Factory", "RemoveBuildRangeLimit", false,
                "Remove limit for build range and maximum count of drag building belts/buildings\nNote: this does not affect range limit for mecha drones' action");
        FactoryPatch.LargerAreaForUpgradeAndDismantleEnabled = Config.Bind("Factory", "LargerAreaForUpgradeAndDismantle", false,
            "Increase maximum area size for upgrade and dismantle to 31x31 (from 11x11)");
        FactoryPatch.LargerAreaForTerraformEnabled = Config.Bind("Factory", "LargerAreaForTerraform", false,
                "Increase maximum area size for terraform to 30x30 (from 10x10)\nNote: this may impact game performance while using large area");
        PlayerPatch.EnhancedMechaForgeCountControlEnabled = Config.Bind("Player", "EnhancedMechaForgeCountControl", false,
                "Enhanced count control for hand-make, increases maximum of count to 1000, and you can hold Ctrl/Shift/Alt to change the count rapidly");
        DysonSpherePatch.StopEjectOnNodeCompleteEnabled = Config.Bind("DysonSphere", "StopEjectOnNodeComplete", false,
            "Stop ejectors when available nodes are all filled up");
        DysonSpherePatch.OnlyConstructNodesEnabled = Config.Bind("DysonSphere", "OnlyConstructNodes", false,
            "Construct only nodes but frames");

        I18N.Init();
        I18N.Add("UXAssist Config", "UXAssist Config", "UX助手设置");
        I18N.Apply();

        // UI Patch
        _windowPatch ??= Harmony.CreateAndPatchAll(typeof(UI.MyWindowManager.Patch));
        _patch ??= Harmony.CreateAndPatchAll(typeof(UXAssist));
        
        UIConfigWindow.Init();
        FactoryPatch.Init();
        PlanetPatch.Init();
        PlayerPatch.Init();
        DysonSpherePatch.Init();
    }

    private void OnDestroy()
    {
        DysonSpherePatch.Uninit();
        PlayerPatch.Uninit();
        PlanetPatch.Uninit();
        FactoryPatch.Uninit();

        _patch?.UnpatchSelf();
        _patch = null;
        _windowPatch?.UnpatchSelf();
        _windowPatch = null;
    }

    private void Update()
    {
        if (VFInput.inputing) return;
        if (Hotkey.Value.IsDown()) ToggleConfigWindow();
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
                panelButtonGo.name = "open-uxassist-config";
                rect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                rect.anchoredPosition3D = new Vector3(128f, -105f, 0f);
                b.onClick.RemoveAllListeners();
                btn.onClick += _ => { ToggleConfigWindow(); };
                btn.tips.tipTitle = "UXAssist Config";
                I18N.OnInitialized += () => { btn.tips.tipTitle = "UXAssist Config".Translate(); };
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
}
