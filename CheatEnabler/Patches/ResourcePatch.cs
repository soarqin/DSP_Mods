using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;

namespace CheatEnabler.Patches;

public static class ResourcePatch
{
    public static ConfigEntry<bool> InfiniteResourceEnabled;
    public static ConfigEntry<bool> FastMiningEnabled;

    public static void Init()
    {
        InfiniteResourceEnabled.SettingChanged += (_, _) => InfiniteResource.Enable(InfiniteResourceEnabled.Value);
        FastMiningEnabled.SettingChanged += (_, _) => FastMining.Enable(FastMiningEnabled.Value);
    }

    public static void Start()
    {
        InfiniteResource.Enable(InfiniteResourceEnabled.Value);
        FastMining.Enable(FastMiningEnabled.Value);
    }

    public static void Uninit()
    {
        InfiniteResource.Enable(false);
        FastMining.Enable(false);
    }

    private class InfiniteResource : PatchImpl<InfiniteResource>
    {
        static private readonly float InfiniteResourceRate = 0f;
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTick), typeof(long), typeof(bool))]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic._miner_parallel))]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.GameTick))]
        [HarmonyPatch(typeof(UIChartAstroResource), nameof(UIChartAstroResource.CalculateMaxAmount))]
        [HarmonyPatch(typeof(UIChartVeinGroup), nameof(UIChartVeinGroup.CalculateMaxAmount))]
        [HarmonyPatch(typeof(UIControlPanelAdvancedMinerEntry), nameof(UIControlPanelAdvancedMinerEntry._OnUpdate))]
        [HarmonyPatch(typeof(UIControlPanelVeinCollectorPanel), nameof(UIControlPanelVeinCollectorPanel._OnUpdate))]
        [HarmonyPatch(typeof(UIMinerWindow), nameof(UIMinerWindow._OnUpdate))]
        [HarmonyPatch(typeof(UIMiningUpgradeLabel), nameof(UIMiningUpgradeLabel.Update))]
        [HarmonyPatch(typeof(UIVeinCollectorPanel), nameof(UIVeinCollectorPanel._OnUpdate))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => (ci.opcode == OpCodes.Ldfld || ci.opcode == OpCodes.Ldflda) && ci.OperandIs(AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.miningCostRate))))
            ).Repeat(codeMatcher =>
                {
                    if (codeMatcher.Instruction.opcode == OpCodes.Ldfld)
                    {
                        codeMatcher.RemoveInstruction().InsertAndAdvance(
                            new CodeInstruction(OpCodes.Pop),
                            new CodeInstruction(OpCodes.Ldc_R4, InfiniteResourceRate)
                        );
                    }
                    else
                    {
                        codeMatcher.RemoveInstruction().InsertAndAdvance(
                            new CodeInstruction(OpCodes.Pop),
                            new CodeInstruction(OpCodes.Ldsflda, AccessTools.Field(typeof(InfiniteResource), nameof(InfiniteResourceRate)))
                        );
                    }
                }
            );
            return matcher.InstructionEnumeration();
        }
    }

    private class FastMining : PatchImpl<FastMining>
    {
        static private readonly float FastMiningSpeed = 2400f;

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(AstroResourceStatPlan), nameof(AstroResourceStatPlan.AddPlanetResources))]
        [HarmonyPatch(typeof(BuildingGizmo), nameof(BuildingGizmo.Update))]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTick), typeof(long), typeof(bool))]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic._miner_parallel))]
        [HarmonyPatch(typeof(ItemProto), nameof(ItemProto.GetPropValue))]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.GameTick))]
        [HarmonyPatch(typeof(ProductionExtraInfoCalculator), nameof(ProductionExtraInfoCalculator.CalculateFactory))]
        [HarmonyPatch(typeof(UIChartAstroResource), nameof(UIChartAstroResource.CalculateMaxAmount))]
        [HarmonyPatch(typeof(UIChartVeinGroup), nameof(UIChartVeinGroup.CalculateMaxAmount))]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.RefreshValues))]
        [HarmonyPatch(typeof(UIControlPanelVeinCollectorPanel), nameof(UIControlPanelVeinCollectorPanel._OnUpdate))]
        [HarmonyPatch(typeof(UIMinerWindow), nameof(UIMinerWindow._OnUpdate))]
        [HarmonyPatch(typeof(UIMiningUpgradeLabel), nameof(UIMiningUpgradeLabel.Update))]
        [HarmonyPatch(typeof(UIPlanetDetail), nameof(UIPlanetDetail.OnPlanetDataSet))]
        [HarmonyPatch(typeof(UIPlanetDetail), nameof(UIPlanetDetail.RefreshDynamicProperties))]
        [HarmonyPatch(typeof(UIReferenceSpeedTip), nameof(UIReferenceSpeedTip.AddEntryDataWithFactory))]
        [HarmonyPatch(typeof(UIStarDetail), nameof(UIStarDetail.OnStarDataSet))]
        [HarmonyPatch(typeof(UIStarDetail), nameof(UIStarDetail.RefreshDynamicProperties))]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.RefreshValues))]
        [HarmonyPatch(typeof(UIVeinCollectorPanel), nameof(UIVeinCollectorPanel._OnUpdate))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => (ci.opcode == OpCodes.Ldfld || ci.opcode == OpCodes.Ldflda) && ci.OperandIs(AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.miningSpeedScale))))
            ).Repeat(codeMatcher =>
                {
                    if (codeMatcher.Instruction.opcode == OpCodes.Ldfld)
                    {
                        codeMatcher.RemoveInstruction().InsertAndAdvance(
                            new CodeInstruction(OpCodes.Pop),
                            new CodeInstruction(OpCodes.Ldc_R4, FastMiningSpeed)
                        );
                    }
                    else
                    {
                        codeMatcher.RemoveInstruction().InsertAndAdvance(
                            new CodeInstruction(OpCodes.Pop),
                            new CodeInstruction(OpCodes.Ldsflda, AccessTools.Field(typeof(FastMining), nameof(FastMiningSpeed)))
                        );
                    }
                }
            );
            return matcher.InstructionEnumeration();
        }
    }
}