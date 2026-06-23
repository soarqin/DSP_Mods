using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.GameConstants;

namespace UXAssist.Patches.Logistics;

internal class AutoConfigLogistics : PatchImpl<AutoConfigLogistics>
{
    protected override void OnEnable()
    {
        ToggleLimitAutoReplenishCount();
    }

    protected override void OnDisable()
    {
        ToggleLimitAutoReplenishCount();
    }

    internal static void ToggleLimitAutoReplenishCount()
    {
        LimitAutoReplenishCount.Enable(LogisticsPatch.AutoConfigLogisticsEnabled.Value && LogisticsPatch.AutoConfigLimitAutoReplenishCount.Value);
    }

    private class LimitAutoReplenishCount : PatchImpl<LimitAutoReplenishCount>
    {
        // Harmony transpiler: PlanetFactory_StationAutoReplenishIfNeeded_Transpiler
        // Target: PlanetFactory.EntityAutoReplenishIfNeeded, PlanetFactory.StationAutoReplenishIfNeeded
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityAutoReplenishIfNeeded))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.StationAutoReplenishIfNeeded))]
        private static IEnumerable<CodeInstruction> PlanetFactory_StationAutoReplenishIfNeeded_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            // Patch dispenser courier count
            matcher.Start().MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DispenserComponent), nameof(DispenserComponent.workCourierDatas))),
                new CodeMatch(OpCodes.Ldlen),
                new CodeMatch(OpCodes.Conv_I4)
            );
            matcher.Repeat(m => m.Advance(4).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(LogisticsPatch), nameof(LogisticsPatch.AutoConfigDispenserCourierCount))),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<int>), nameof(ConfigEntry<int>.Value))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), [typeof(int), typeof(int)]))
            ));

            // Patch PLS/ILS drone count
            matcher.Start().MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.workDroneDatas))),
                new CodeMatch(OpCodes.Ldlen),
                new CodeMatch(OpCodes.Conv_I4)
            );
            matcher.Repeat(m =>
            {
                var instr = m.Instruction;
                m.Advance(4).InsertAndAdvance(
                    instr,
                    Transpilers.EmitDelegate((int x, StationComponent station)
                        => Math.Min(x, station.isStellar ? LogisticsPatch.AutoConfigILSDroneCount.Value : LogisticsPatch.AutoConfigPLSDroneCount.Value))
                );
            });

            // Patch ILS ship count
            matcher.Start().MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.workShipDatas))),
                new CodeMatch(OpCodes.Ldlen),
                new CodeMatch(OpCodes.Conv_I4)
            );
            matcher.Repeat(m => m.Advance(4).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(LogisticsPatch), nameof(LogisticsPatch.AutoConfigILSShipCount))),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<int>), nameof(ConfigEntry<int>.Value))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), [typeof(int), typeof(int)]))
            ));
            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.SetDefaultParams))]
    private static void BuildTool_Addon_SetDefaultParams_Postfix(BuildTool_Addon __instance, int bpIndex)
    {
        if (__instance.handPrefabDesc.isDispenser)
        {
            __instance.handBpParams[bpIndex][2] = (int)(long)(LogisticsConstants.DispenserChargePowerMultiplier * LogisticsPatch.AutoConfigDispenserChargePower.Value + 0.5);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.NewStationComponent))]
    private static void PlanetTransport_NewStationComponent_Postfix(PlanetTransport __instance, StationComponent __result)
    {
        LogisticsPatch.DoConfigStation(__instance.factory, __result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.NewBattleBaseComponent))]
    private static void DefenseSystem_NewBattleBaseComponent_Postfix(DefenseSystem __instance, int __result)
    {
        if (__result <= 0) return;
        LogisticsPatch.BattleBaseSetChargePower(__instance.factory, __instance.battleBases[__result]);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.NewDispenserComponent))]
    private static void PlanetTransport_NewDispenserComponent_Postfix(PlanetTransport __instance, int __result)
    {
        if (__result <= 0) return;
        LogisticsPatch.DispenserFillCouriers(__instance.factory, __instance.dispenserPool[__result]);
    }
}

internal class AutoConfigLogisticsSetDefaultRemoteLogicToStorage : PatchImpl<AutoConfigLogisticsSetDefaultRemoteLogicToStorage>
{
    // Harmony transpiler: UIStationStorage_OnItemPickerReturn_Transpiler
    // Target: UIControlPanelStationStorage.OnItemPickerReturn, UIStationStorage.OnItemPickerReturn
    // Fallback: None — patch will fail loudly if the target method body changes.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.OnItemPickerReturn))]
    [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.OnItemPickerReturn))]
    private static IEnumerable<CodeInstruction> UIStationStorage_OnItemPickerReturn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.isStellar))),
            new CodeMatch(ci => ci.opcode == OpCodes.Brtrue_S || ci.opcode == OpCodes.Brtrue),
            new CodeMatch(OpCodes.Ldc_I4_0),
            new CodeMatch(ci => ci.opcode == OpCodes.Br_S || ci.opcode == OpCodes.Br),
            new CodeMatch(OpCodes.Ldc_I4_1),
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.mainPlayer)))
        );
        if (matcher.IsValid)
        {
            matcher.RemoveInstructions(7).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I4_0)
            );
        }
        return matcher.InstructionEnumeration();
    }
}
