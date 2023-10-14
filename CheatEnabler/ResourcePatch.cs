using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;

public static class ResourcePatch
{
    public static ConfigEntry<bool> InfiniteResourceEnabled;
    public static ConfigEntry<bool> FastMiningEnabled;

    public static void Init()
    {
        InfiniteResourceEnabled.SettingChanged += (_, _) => InfiniteResource.Enable(InfiniteResourceEnabled.Value);
        FastMiningEnabled.SettingChanged += (_, _) => FastMining.Enable(FastMiningEnabled.Value);
        InfiniteResource.Enable(InfiniteResourceEnabled.Value);
        FastMining.Enable(FastMiningEnabled.Value);
    }

    public static void Uninit()
    {
        InfiniteResource.Enable(false);
        FastMining.Enable(false);
    }

    private static class InfiniteResource
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(InfiniteResource));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

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

    private static class FastMining
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(FastMining));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

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