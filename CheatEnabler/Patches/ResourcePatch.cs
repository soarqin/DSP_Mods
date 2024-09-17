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

    private class InfiniteResource: PatchImpl<InfiniteResource>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool))]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int))]
        [HarmonyPatch(typeof(ItemProto), "GetPropValue")]
        [HarmonyPatch(typeof(PlanetTransport), "GameTick")]
        [HarmonyPatch(typeof(UIMinerWindow), "_OnUpdate")]
        [HarmonyPatch(typeof(UIMiningUpgradeLabel), "Update")]
        [HarmonyPatch(typeof(UIPlanetDetail), "OnPlanetDataSet")]
        [HarmonyPatch(typeof(UIPlanetDetail), "RefreshDynamicProperties")]
        [HarmonyPatch(typeof(UIStarDetail), "OnStarDataSet")]
        [HarmonyPatch(typeof(UIStarDetail), "RefreshDynamicProperties")]
        [HarmonyPatch(typeof(UIStationStorage), "RefreshValues")]
        [HarmonyPatch(typeof(UIVeinCollectorPanel), "_OnUpdate")]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.miningCostRate)))
            ).Repeat(codeMatcher =>
                codeMatcher.RemoveInstruction().InsertAndAdvance(
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldc_R4, 0f)
                )
            );
            return matcher.InstructionEnumeration();
        }
    }

    private class FastMining: PatchImpl<FastMining>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool))]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int))]
        [HarmonyPatch(typeof(ItemProto), "GetPropValue")]
        [HarmonyPatch(typeof(PlanetTransport), "GameTick")]
        [HarmonyPatch(typeof(UIMinerWindow), "_OnUpdate")]
        [HarmonyPatch(typeof(UIMiningUpgradeLabel), "Update")]
        [HarmonyPatch(typeof(UIPlanetDetail), "OnPlanetDataSet")]
        [HarmonyPatch(typeof(UIPlanetDetail), "RefreshDynamicProperties")]
        [HarmonyPatch(typeof(UIStarDetail), "OnStarDataSet")]
        [HarmonyPatch(typeof(UIStarDetail), "RefreshDynamicProperties")]
        [HarmonyPatch(typeof(UIStationStorage), "RefreshValues")]
        [HarmonyPatch(typeof(UIVeinCollectorPanel), "_OnUpdate")]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.miningSpeedScale)))
            ).Repeat(codeMatcher =>
                codeMatcher.RemoveInstruction().InsertAndAdvance(
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldc_R4, 2400f)
                )
            );
            return matcher.InstructionEnumeration();
        }
    }
}