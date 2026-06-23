using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Configuration;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace CheatEnabler.Patches.Factory;

[ModFeature("CheatFactory", Order = 10)]
public class FactoryPatch : PatchImpl<FactoryPatch>
{
    public static ConfigEntry<bool> ImmediateEnabled;
    public static ConfigEntry<bool> ArchitectModeEnabled;
    public static ConfigEntry<bool> NoConditionEnabled;
    public static ConfigEntry<bool> NoCollisionEnabled;
    public static ConfigEntry<bool> BeltSignalGeneratorEnabled;
    public static ConfigEntry<bool> BeltSignalNumberAltFormat;
    public static ConfigEntry<bool> BeltSignalCountGenEnabled;
    public static ConfigEntry<bool> BeltSignalCountRemEnabled;
    public static ConfigEntry<bool> BeltSignalCountRecipeEnabled;
    public static ConfigEntry<bool> BeltSignalUseProliferatorEnabled;
    public static ConfigEntry<bool> RemovePowerSpaceLimitEnabled;
    public static ConfigEntry<bool> BoostWindPowerEnabled;
    public static ConfigEntry<bool> BoostSolarPowerEnabled;
    public static ConfigEntry<bool> BoostFuelPowerEnabled;
    public static ConfigEntry<bool> BoostGeothermalPowerEnabled;
    public static ConfigEntry<bool> WindTurbinesPowerGlobalCoverageEnabled;
    public static ConfigEntry<bool> ControlPanelRemoteLogisticsEnabled;

    private static PressKeyBind _noConditionKey;
    private static PressKeyBind _noCollisionKey;
    internal static HashSet<int> BeltIds;

    public static void Init()
    {
        _noConditionKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey(0, 0, ECombineKeyAction.OnceClick, true),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ToggleNoCondition",
            canOverride = true
        }
        );
        _noCollisionKey = KeyBindings.RegisterKeyBinding(new BuiltinKey
        {
            key = new CombineKey(0, 0, ECombineKeyAction.OnceClick, true),
            conflictGroup = KeyBindConflict.MOVEMENT | KeyBindConflict.FLYING | KeyBindConflict.SAILING | KeyBindConflict.BUILD_MODE_1 | KeyBindConflict.KEYBOARD_KEYBIND,
            name = "ToggleNoCollision",
            canOverride = true
        }
        );
                                                                        ImmediateEnabled.SettingChanged += (_, _) => ImmediateBuild.Enable(ImmediateEnabled.Value);
        ArchitectModeEnabled.SettingChanged += (_, _) => ArchitectMode.Enable(ArchitectModeEnabled.Value);
        NoConditionEnabled.SettingChanged += (_, _) => NoConditionBuild.Enable(NoConditionEnabled.Value);
        NoCollisionEnabled.SettingChanged += (_, _) => NoCollisionValueChanged();
        BeltSignalGeneratorEnabled.SettingChanged += (_, _) => BeltSignalGenerator.Enable(BeltSignalGeneratorEnabled.Value);
        BeltSignalNumberAltFormat.SettingChanged += (_, _) => BeltSignalGenerator.OnAltFormatChanged();
        BeltSignalUseProliferatorEnabled.SettingChanged += (_, _) => BeltSignalGenerator.OnUseProliferatorChanged();
        RemovePowerSpaceLimitEnabled.SettingChanged += (_, _) => RemovePowerSpaceLimit.Enable(RemovePowerSpaceLimitEnabled.Value);
        BoostWindPowerEnabled.SettingChanged += (_, _) => BoostWindPower.Enable(BoostWindPowerEnabled.Value);
        BoostSolarPowerEnabled.SettingChanged += (_, _) => BoostSolarPower.Enable(BoostSolarPowerEnabled.Value);
        BoostFuelPowerEnabled.SettingChanged += (_, _) => BoostFuelPower.Enable(BoostFuelPowerEnabled.Value);
        BoostGeothermalPowerEnabled.SettingChanged += (_, _) => BoostGeothermalPower.Enable(BoostGeothermalPowerEnabled.Value);
        WindTurbinesPowerGlobalCoverageEnabled.SettingChanged += (_, _) => WindTurbinesPowerGlobalCoverage.Enable(WindTurbinesPowerGlobalCoverageEnabled.Value);
        ControlPanelRemoteLogisticsEnabled.SettingChanged += (_, _) => ControlPanelRemoteLogistics.Enable(ControlPanelRemoteLogisticsEnabled.Value);
    }

    public static void Start()
    {
        ImmediateBuild.Enable(ImmediateEnabled.Value);
        ArchitectMode.Enable(ArchitectModeEnabled.Value);
        NoConditionBuild.Enable(NoConditionEnabled.Value);
        NoCollisionValueChanged();
        BeltSignalGenerator.Enable(BeltSignalGeneratorEnabled.Value);
        RemovePowerSpaceLimit.Enable(RemovePowerSpaceLimitEnabled.Value);
        BoostWindPower.Enable(BoostWindPowerEnabled.Value);
        BoostSolarPower.Enable(BoostSolarPowerEnabled.Value);
        BoostFuelPower.Enable(BoostFuelPowerEnabled.Value);
        BoostGeothermalPower.Enable(BoostGeothermalPowerEnabled.Value);
        WindTurbinesPowerGlobalCoverage.Enable(WindTurbinesPowerGlobalCoverageEnabled.Value);
        ControlPanelRemoteLogistics.Enable(ControlPanelRemoteLogisticsEnabled.Value);
        Enable(true);
        CargoTrafficPatch.Enable(true);
        GameLogicProc.OnGameBegin += OnGameBegin_For_ImmBuild;
        GameLogicProc.OnDataLoaded += OnDataLoaded;
    }

    public static void Uninit()
    {
        GameLogicProc.OnDataLoaded -= OnDataLoaded;
        GameLogicProc.OnGameBegin -= OnGameBegin_For_ImmBuild;
        CargoTrafficPatch.Enable(false);
        Enable(false);
        ImmediateBuild.Enable(false);
        ArchitectMode.Enable(false);
        NoConditionBuild.Enable(false);
        BeltSignalGenerator.Enable(false);
        RemovePowerSpaceLimit.Enable(false);
        BoostWindPower.Enable(false);
        BoostSolarPower.Enable(false);
        BoostFuelPower.Enable(false);
        BoostGeothermalPower.Enable(false);
        WindTurbinesPowerGlobalCoverage.Enable(false);
        ControlPanelRemoteLogistics.Enable(false);
    }

    private static void OnDataLoaded()
    {
        WindTurbinesPowerGlobalCoverage.Enable(WindTurbinesPowerGlobalCoverageEnabled.Value);
        BeltIds ??= [.. LDB.items.dataArray.Where(i => i.prefabDesc.isBelt).Select(i => i.ID)];
    }

    public static void OnInputUpdate()
    {
        if (_noConditionKey.keyValue)
        {
            NoConditionEnabled.Value = !NoConditionEnabled.Value;
            if (!DSPGame.IsMenuDemo && GameMain.isRunning)
            {
                UIRoot.instance.uiGame.generalTips.InvokeRealtimeTipAhead((NoConditionEnabled.Value ? Localization.NoConditionOn : Localization.NoConditionOff).Translate());
            }
        }
        if (_noCollisionKey.keyValue)
        {
            NoCollisionEnabled.Value = !NoCollisionEnabled.Value;
            if (!DSPGame.IsMenuDemo && GameMain.isRunning)
            {
                UIRoot.instance.uiGame.generalTips.InvokeRealtimeTipAhead((NoCollisionEnabled.Value ? Localization.NoCollisionOn : Localization.NoCollisionOff).Translate());
            }
        }
    }

    internal static void NoCollisionValueChanged()
    {
        var coll = ColliderPool.instance;
        if (coll == null) return;
        var obj = coll.gameObject;
        if (obj == null) return;
        obj.gameObject.SetActive(!NoCollisionEnabled.Value);
        GameMain.data?.warningSystem?.UpdateCriticalWarningText();
    }

    public static void ArrivePlanet(PlanetFactory factory)
    {
        if (factory.prebuildCount <= 0) return;
        var imm = ImmediateEnabled.Value;
        var architect = ArchitectModeEnabled.Value;
        if ((!imm && !architect) || GameMain.gameScenario == null) return;
        var prebuilds = factory.prebuildPool;
        if (imm)
        {
            var player = GameMain.mainPlayer;
            for (var i = factory.prebuildCursor - 1; i > 0; i--)
            {
                ref var pb = ref prebuilds[i];
                if (pb.id != i || pb.isDestroyed) continue;
                if (pb.itemRequired > 0)
                {
                    if (!architect) continue;
                    pb.itemRequired = 0;
                }
                CargoTrafficPatch.InstantBuild(player, factory, i);
            }
            CargoTrafficPatch.TryEndBatchBuilding(factory);
        }
        else if (architect)
        {
            for (var i = factory.prebuildCursor - 1; i > 0; i--)
            {
                ref var pb = ref prebuilds[i];
                if (pb.id != i || pb.isDestroyed || pb.itemRequired == 0) continue;
                pb.itemRequired = 0;
                factory.AlterPrebuildModelState(i);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetData), nameof(PlanetData.NotifyFactoryLoaded))]
    private static void PlanetData_NotifyFactoryLoaded_Postfix(PlanetData __instance)
    {
        var main = GameMain.instance;
        if (main != null && main._running && __instance.factory?.planet?.data != null)
        {
            ArrivePlanet(__instance.factory);
        }
    }

    private static void OnGameBegin_For_ImmBuild()
    {
        if (DSPGame.IsMenuDemo) return;
        var factory = GameMain.mainPlayer?.factory;
        if (factory?.planet?.data != null)
        {
            ArrivePlanet(factory);
        }
        GameMain.data?.warningSystem?.UpdateCriticalWarningText();
    }
    // Harmony transpiler: WarningSystem_hasCriticalWarning_Transpiler
    // Target: WarningSystem.hasCriticalWarning (getter)
    // Fallback: None — patch will fail loudly if the target method body changes.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WarningSystem), nameof(WarningSystem.hasCriticalWarning), MethodType.Getter)]
    private static IEnumerable<CodeInstruction> WarningSystem_hasCriticalWarning_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        var label2 = generator.DefineLabel();
        matcher.End().MatchBack(false,
            new CodeMatch(OpCodes.Ret)
        ).RemoveInstructions(1);
        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Brfalse, label1),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ret),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryPatch), nameof(NoConditionEnabled))).WithLabels(label1),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), nameof(ConfigEntry<bool>.Value))),
            new CodeInstruction(OpCodes.Brfalse, label2),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ret),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryPatch), nameof(NoCollisionEnabled))).WithLabels(label2),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), nameof(ConfigEntry<bool>.Value))),
            new CodeInstruction(OpCodes.Ret)
        );
        return matcher.InstructionEnumeration();
    }
    // Harmony transpiler: WarningSystem_UpdateCriticalWarningText_Transpiler
    // Target: WarningSystem.UpdateCriticalWarningText
    // Fallback: None — patch will fail loudly if the target method body changes.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WarningSystem), nameof(WarningSystem.UpdateCriticalWarningText))]
    private static IEnumerable<CodeInstruction> WarningSystem_UpdateCriticalWarningText_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldstr, ""),
            new CodeMatch(OpCodes.Call, AccessTools.PropertySetter(typeof(WarningSystem), nameof(WarningSystem.criticalWarningTexts)))
        );
        matcher.Repeat(m =>
        {
            var label1 = generator.DefineLabel();
            m.Advance(3).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((WarningSystem w) =>
                    {
                        if (NoConditionEnabled.Value)
                        {
                            w.criticalWarningTexts = Localization.BuildWithoutConditionIsEnabled.Translate() + "\r\n";
                        }
                        else if (NoCollisionEnabled.Value)
                        {
                            w.criticalWarningTexts = Localization.NoCollisionIsEnabled.Translate() + "\r\n";
                        }
                    }
                )
            );
            if (m.Opcode == OpCodes.Ret)
            {
                m.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(WarningSystem), nameof(WarningSystem.onCriticalWarningTextChanged))),
                    new CodeInstruction(OpCodes.Brfalse_S, label1),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(WarningSystem), nameof(WarningSystem.onCriticalWarningTextChanged))),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Action), nameof(Action.Invoke)))
                );
                m.Labels.Add(label1);
            }
        });
        return matcher.InstructionEnumeration();
    }
}
