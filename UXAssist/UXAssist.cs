using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.UI;

namespace UXAssist;

[BepInDependency(CommonAPIPlugin.GUID)]
[CommonAPISubmoduleDependency(nameof(CustomKeyBindSystem))]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class UXAssist : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private static bool _configWinInitialized;
    private static MyConfigWindow _configWin;
    private static Harmony _patch;
    private static bool _initialized;
    private static PressKeyBind _toggleKey;

    private void Awake()
    {
        _toggleKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey((int)KeyCode.BackQuote, CombineKey.ALT_COMB, ECombineKeyAction.OnceClick, false),
            conflictGroup = KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "OpenUXAssistConfigWindow",
            canOverride = true
        });
        GamePatch.EnableWindowResizeEnabled = Config.Bind("Game", "EnableWindowResize", false,
            "Enable game window resize (maximum box and thick frame)");
        GamePatch.LoadLastWindowRectEnabled = Config.Bind("Game", "LoadLastWindowRect", false,
            "Load last window position and size when game starts");
        GamePatch.LastWindowRect = Config.Bind("Game", "LastWindowRect", new Vector4(0f, 0f, 0f, 0f),
            "Last window position and size");
        /*
        GamePatch.AutoSaveOptEnabled = Config.Bind("Game", "AutoSaveOpt", false,
            "Better auto-save mechanism");
        */
        GamePatch.ConvertSavesFromPeaceEnabled = Config.Bind("Game", "ConvertSavesFromPeace", false,
            "Convert saves from Peace mode to Combat mode on save loading");
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
        FactoryPatch.OffGridBuildingEnabled = Config.Bind("Factory", "OffGridBuilding", false,
            "Enable off grid building and stepped rotation");
        FactoryPatch.LogisticsCapacityTweaksEnabled = Config.Bind("Factory", "LogisticsCapacityTweaks", true,
            "Logistics capacity related tweaks");
        FactoryPatch.TreatStackingAsSingleEnabled = Config.Bind("Factory", "TreatStackingAsSingle", false,
            "Treat stack items as single in monitor components");
        PlanetFunctions.OrbitalCollectorMaxBuildCount = Config.Bind("Factory", "OCMaxBuildCount", 0, "Maximum Orbital Collectors to build once, set to 0 to build as many as possible");
        PlayerPatch.EnhancedMechaForgeCountControlEnabled = Config.Bind("Player", "EnhancedMechaForgeCountControl", false,
                "Enhanced count control for hand-make, increases maximum of count to 1000, and you can hold Ctrl/Shift/Alt to change the count rapidly");
        DysonSpherePatch.StopEjectOnNodeCompleteEnabled = Config.Bind("DysonSphere", "StopEjectOnNodeComplete", false,
            "Stop ejectors when available nodes are all filled up");
        DysonSpherePatch.OnlyConstructNodesEnabled = Config.Bind("DysonSphere", "OnlyConstructNodes", false,
            "Construct only nodes but frames");

        I18N.Init();
        I18N.Add("UXAssist Config", "UXAssist Config", "UX助手设置");
        I18N.Add("KEYOpenUXAssistConfigWindow", "Open UXAssist Config Window", "打开UX助手设置面板");
        I18N.Apply();

        // UI Patch
        _patch ??= Harmony.CreateAndPatchAll(typeof(UXAssist));
        
        MyWindowManager.Init();
        UIConfigWindow.Init();
        GamePatch.Init();
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
        GamePatch.Uninit();
        MyWindowManager.Uninit();

        _patch?.UnpatchSelf();
        _patch = null;
    }

    private void Update()
    {
        if (VFInput.inputing) return;
        if (VFInput.onGUI)
        {
            FactoryPatch.LogisticsCapacityTweaks.OnUpdate();
        }
        if (_toggleKey.keyValue)
        {
            ToggleConfigWindow();
        }
    }

    private void LateUpdate()
    {
        FactoryPatch.NightLight.LateUpdate();
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

    // Add config button to main menu
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

    // Check for noModifier while pressing hotkeys on build bar
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

    // Patch to fix the issue that warning popup on VeinUtil upgraded to level 8000+
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ABN_VeinsUtil), nameof(ABN_VeinsUtil.CheckValue))]
    private static IEnumerable<CodeInstruction> ABN_VeinsUtil_CheckValue_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldelem_R8),
            new CodeMatch(OpCodes.Conv_R4),
            new CodeMatch(OpCodes.Add),
            new CodeMatch(OpCodes.Stloc_1)
        );
        // loc1 = Mathf.Round(n * 1000f) / 1000f;
        matcher.Advance(3).Insert(
            new CodeInstruction(OpCodes.Ldc_R4, 1000f),
            new CodeInstruction(OpCodes.Mul),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Round))),
            new CodeInstruction(OpCodes.Ldc_R4, 1000f),
            new CodeInstruction(OpCodes.Div)
        );
        return matcher.InstructionEnumeration();
    }

    // Bring popup tip window to top layer
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

    // Sort blueprint structures by item id, model index, recipe id, area index, and position before saving
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlueprintUtils), nameof(BlueprintUtils.GenerateBlueprintData))]
    private static void BlueprintUtils_GenerateBlueprintData_Postfix(BlueprintData _blueprintData)
    {
        var buildings = _blueprintData.buildings;
        Array.Sort(buildings, (a, b) =>
        {
            var tmpItemId = a.itemId - b.itemId;
            if(tmpItemId != 0)
                return tmpItemId;
            var tmpModelIndex = a.modelIndex - b.modelIndex;
            if(tmpModelIndex != 0)
                return tmpModelIndex;
            var tmpRecipeId = a.recipeId - b.recipeId;
            if(tmpRecipeId != 0)
                return tmpRecipeId;
            var tmpAreaIndex = a.areaIndex - b.areaIndex;
            if(tmpAreaIndex != 0)
                return tmpAreaIndex;
            const double ky = 256.0;
            const double kx = 1024.0;
            var scorePosA = (a.localOffset_y * ky + a.localOffset_x) * kx + a.localOffset_z;
            var scorePosB = (b.localOffset_y * ky + b.localOffset_x) * kx + b.localOffset_z;
            return scorePosA < scorePosB ? 1 : -1;
        });
        for (var i = buildings.Length - 1; i >= 0; i--)
        {
            buildings[i].index = i;
        }
    }
    
    // Increase maximum value of property realizing, 2000 -> 20000
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.UpdateUIElements))]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.OnRealizeButtonClick))]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.OnInputValueEnd))]
    private static IEnumerable<CodeInstruction> UIProductEntry_UpdateUIElements_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldc_I4, 2000)
        );
        matcher.Repeat(m =>
        {
            m.SetAndAdvance(OpCodes.Ldc_I4, 20000);
        });
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.OnInputValueEnd))]
    private static IEnumerable<CodeInstruction> UIProductEntry_OnInputValueEnd_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_R4 && ci.OperandIs(2000f))
        );
        matcher.Repeat(m =>
        {
            m.SetAndAdvance(OpCodes.Ldc_R4, 20000f);
        });
        return matcher.InstructionEnumeration();
    }

    // Increase capacity of player order queue, 16 -> 128
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlayerOrder), MethodType.Constructor, typeof(Player))]
    private static IEnumerable<CodeInstruction> PlayerOrder_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => (ci.opcode == OpCodes.Ldc_I4_S || ci.opcode == OpCodes.Ldc_I4) && ci.OperandIs(16))
        );
        matcher.Repeat(m =>
        {
            m.SetAndAdvance(OpCodes.Ldc_I4, 128);
        });
        return matcher.InstructionEnumeration();
    }

    // Increase Player Command Queue from 16 to 128
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlayerOrder), nameof(PlayerOrder._trimEnd))]
    [HarmonyPatch(typeof(PlayerOrder), nameof(PlayerOrder.Enqueue))]
    private static IEnumerable<CodeInstruction> PlayerOrder_ExtendCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => (ci.opcode == OpCodes.Ldc_I4_S || ci.opcode == OpCodes.Ldc_I4) && ci.OperandIs(16))
        );
        matcher.Repeat(m =>
        {
            m.SetAndAdvance(OpCodes.Ldc_I4, 128);
        });
        return matcher.InstructionEnumeration();
    }
    
    // Allow F11 in star map
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnLateUpdate))]
    private static IEnumerable<CodeInstruction> UIGame__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.inFullscreenGUI))),
            new CodeMatch(ci => ci.opcode == OpCodes.Brfalse || ci.opcode == OpCodes.Brfalse_S)
        );
        var jumpPos = matcher.Advance(1).Operand;
        matcher.Advance(-1).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIGame), nameof(UIGame.starmap))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ManualBehaviour), nameof(ManualBehaviour.active))),
            new CodeInstruction(OpCodes.Brtrue_S, jumpPos)
        );
        return matcher.InstructionEnumeration();
    }
}
