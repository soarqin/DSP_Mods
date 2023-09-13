using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;

public static class BuildPatch
{
    public static ConfigEntry<bool> ImmediateEnabled;
    public static ConfigEntry<bool> NoCostEnabled;
    public static ConfigEntry<bool> NoConditionEnabled;
    public static ConfigEntry<bool> NoCollisionEnabled;

    private static Harmony _patch;
    private static Harmony _noConditionPatch;

    public static void Init()
    {
        if (_patch != null) return;
        ImmediateEnabled.SettingChanged += (_, _) => ImmediateValueChanged();
        NoCostEnabled.SettingChanged += (_, _) => NoCostValueChanged();
        NoConditionEnabled.SettingChanged += (_, _) => NoConditionValueChanged();
        NoCollisionEnabled.SettingChanged += (_, _) => NoCollisionValueChanged();
        ImmediateValueChanged();
        NoCostValueChanged();
        NoConditionValueChanged();
        NoCollisionValueChanged();
        _patch = Harmony.CreateAndPatchAll(typeof(BuildPatch));
    }

    public static void Uninit()
    {
        if (_patch != null)
        {
            _patch.UnpatchSelf();
            _patch = null;
        }
        if (_noConditionPatch != null)
        {
            _noConditionPatch.UnpatchSelf();
            _noConditionPatch = null;
        }
        ImmediateBuild.Enable(false);
        NoCostBuild.Enable(false);
    }

    private static void ImmediateValueChanged()
    {
        ImmediateBuild.Enable(ImmediateEnabled.Value);
    }
    private static void NoCostValueChanged()
    {
        NoCostBuild.Enable(NoCostEnabled.Value);
    }

    private static void NoConditionValueChanged()
    {
        if (NoConditionEnabled.Value)
        {
            if (_noConditionPatch != null)
            {
                return;
            }

            _noConditionPatch = Harmony.CreateAndPatchAll(typeof(NoConditionBuild));
        }
        else if (_noConditionPatch != null)
        {
            _noConditionPatch.UnpatchSelf();
            _noConditionPatch = null;
        }
    }

    private static void NoCollisionValueChanged()
    {
        var coll = ColliderPool.instance;
        if (coll == null) return;
        var obj = coll.gameObject;
        if (obj == null) return;
        obj.gameObject.SetActive(!NoCollisionEnabled.Value);
    }

    public static void ArrivePlanet(PlanetFactory factory)
    {
        var imm = ImmediateEnabled.Value;
        var noCost = NoCostEnabled.Value;
        if (!imm && !noCost) return;
        var prebuilds = factory.prebuildPool;
        if (imm) factory.BeginFlattenTerrain();
        for (var i = factory.prebuildCursor - 1; i > 0; i--)
        {
            if (prebuilds[i].id != i) continue;
            if (prebuilds[i].itemRequired > 0)
            {
                if (!noCost) continue;
                prebuilds[i].itemRequired = 0;
                if (imm)
                    factory.BuildFinally(GameMain.mainPlayer, i, false);
                else
                    factory.AlterPrebuildModelState(i);
            }
            else
            {
                if (imm)
                    factory.BuildFinally(GameMain.mainPlayer, i, false);
            }
        }
        if (imm) factory.EndFlattenTerrain();
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetData), nameof(PlanetData.NotifyFactoryLoaded))]
    private static void PlanetData_NotifyFactoryLoaded_Postfix(PlanetData __instance)
    {
        ArrivePlanet(__instance.factory);
    }

    private static class ImmediateBuild
    {
        private static Harmony _immediatePatch;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                if (_immediatePatch != null)
                {
                    return;
                }

                var factory = GameMain.mainPlayer?.factory;
                if (factory != null)
                {
                    ArrivePlanet(factory);
                }

                _immediatePatch = Harmony.CreateAndPatchAll(typeof(ImmediateBuild));
            }
            else if (_immediatePatch != null)
            {
                _immediatePatch.UnpatchSelf();
                _immediatePatch = null;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CreatePrebuilds))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GC), nameof(GC.Collect)))
            ).Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool), nameof(BuildTool.factory))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BuildPatch), nameof(BuildPatch.ArrivePlanet)))
            );
            return matcher.InstructionEnumeration();
        }
    }

    private static class NoCostBuild
    {
        private static Harmony _noCostPatch;
        private static bool[] _canBuildItems;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                if (_noCostPatch != null)
                {
                    return;
                }

                var factory = GameMain.mainPlayer?.factory;
                if (factory != null)
                {
                    ArrivePlanet(factory);
                }

                _noCostPatch = Harmony.CreateAndPatchAll(typeof(NoCostBuild));
            }
            else if (_noCostPatch != null)
            {
                _noCostPatch.UnpatchSelf();
                _noCostPatch = null;
            }
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.TakeTailItems), new [] { typeof(int), typeof(int), typeof(int), typeof(bool) }, new[] {ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Out, ArgumentType.Normal})]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.TakeTailItems), new [] { typeof(int), typeof(int), typeof(int[]), typeof(int), typeof(bool) }, new[] {ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal})]
        public static bool TakeTailItemsPatch(StorageComponent __instance, int itemId)
        {
            if (__instance == null || __instance.id != GameMain.mainPlayer.package.id) return true;
            if (itemId <= 0) return true;
            if (_canBuildItems == null)
            {
                DoInit();
            }
            return itemId >= 10000 || !_canBuildItems[itemId];
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StorageComponent), "GetItemCount", new Type[] { typeof(int) })]
        public static void GetItemCountPatch(StorageComponent __instance, int itemId, ref int __result)
        {
            if (__result > 99) return;
            if (__instance == null || __instance.id != GameMain.mainPlayer.package.id) return;
            if (itemId <= 0) return;
            if (_canBuildItems == null)
            {
                DoInit();
            }
            if (itemId < 10000 && _canBuildItems[itemId]) __result = 100;
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GetObjectSelectDistance))]
        private static IEnumerable<CodeInstruction> PlayerAction_Inspect_GetObjectSelectDistance_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldc_R4, 10000f);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        private static void DoInit()
        {
            _canBuildItems = new bool[10000];
            foreach (var ip in LDB.items.dataArray)
            {
                if (ip.CanBuild && ip.ID < 10000) _canBuildItems[ip.ID] = true;
            }
        }
    }

    private static class NoConditionBuild
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CheckBuildConditions))]
        // [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            yield return new CodeInstruction(OpCodes.Ret);
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> BuildTool_Click_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NoConditionBuild), nameof(NoConditionBuild.CheckForMiner)));
            yield return new CodeInstruction(OpCodes.Ret);
        }

        public static bool CheckForMiner(BuildTool tool)
        {
            var previews = tool.buildPreviews;
            foreach (var preview in previews)
            {
                var desc = preview?.item?.prefabDesc;
                if (desc == null) continue;
                if (desc.veinMiner || desc.oilMiner) return false;
            }
            return true;
        }
    }
}