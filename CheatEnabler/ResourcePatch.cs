using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;

public static class ResourcePatch
{
    public static ConfigEntry<bool> InfiniteEnabled;
    public static ConfigEntry<bool> FastEnabled;
    private static Harmony _infinitePatch;
    private static Harmony _fastPatch;

    public static void Init()
    {
        InfiniteEnabled.SettingChanged += (_, _) => InfiniteValueChanged();
        FastEnabled.SettingChanged += (_, _) => FastValueChanged();
        InfiniteValueChanged();
        FastValueChanged();
    }

    public static void Uninit()
    {
        if (_infinitePatch != null)
        {
            _infinitePatch.UnpatchSelf();
            _infinitePatch = null;
        }

        if (_fastPatch != null)
        {
            _fastPatch.UnpatchSelf();
            _fastPatch = null;
        }
    }

    private static void InfiniteValueChanged()
    {
        if (InfiniteEnabled.Value)
        {
            if (_infinitePatch != null)
            {
                return;
            }

            _infinitePatch = Harmony.CreateAndPatchAll(typeof(InfiniteResource));
        }
        else if (_infinitePatch != null)
        {
            _infinitePatch.UnpatchSelf();
            _infinitePatch = null;
        }
    }
    private static void FastValueChanged()
    {
        if (FastEnabled.Value)
        {
            if (_fastPatch != null)
            {
                return;
            }

            _fastPatch = Harmony.CreateAndPatchAll(typeof(FastMining));
        }
        else if (_fastPatch != null)
        {
            _fastPatch.UnpatchSelf();
            _fastPatch = null;
        }
    }

    private static class InfiniteResource
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool))]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool), typeof(int), typeof(int),
            typeof(int))]
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
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool))]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool), typeof(int), typeof(int),
            typeof(int))]
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
                    new CodeInstruction(OpCodes.Ldc_R4, 720f)
                )
            );
            return matcher.InstructionEnumeration();
        }
    }
}