using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CheatEnabler;

public static class BuildPatch
{
    public static ConfigEntry<bool> ImmediateEnabled;
    public static ConfigEntry<bool> ArchitectModeEnabled;
    public static ConfigEntry<bool> NoConditionEnabled;
    public static ConfigEntry<bool> NoCollisionEnabled;

    private static Harmony _patch;
    private static Harmony _noConditionPatch;

    public static void Init()
    {
        if (_patch != null) return;
        ImmediateEnabled.SettingChanged += (_, _) => ImmediateValueChanged();
        ArchitectModeEnabled.SettingChanged += (_, _) => ArchitectModeValueChanged();
        NoConditionEnabled.SettingChanged += (_, _) => NoConditionValueChanged();
        NoCollisionEnabled.SettingChanged += (_, _) => NoCollisionValueChanged();
        ImmediateValueChanged();
        ArchitectModeValueChanged();
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
        ArchitectMode.Enable(false);
        NightLightEnd();
    }

    private static void ImmediateValueChanged()
    {
        ImmediateBuild.Enable(ImmediateEnabled.Value);
    }
    private static void ArchitectModeValueChanged()
    {
        ArchitectMode.Enable(ArchitectModeEnabled.Value);
        NightLightUpdateState();
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
        var architect = ArchitectModeEnabled.Value;
        if (!imm && !architect) return;
        var prebuilds = factory.prebuildPool;
        if (imm) factory.BeginFlattenTerrain();
        for (var i = factory.prebuildCursor - 1; i > 0; i--)
        {
            if (prebuilds[i].id != i) continue;
            if (prebuilds[i].itemRequired > 0)
            {
                if (!architect) continue;
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

    /* Night Light Begin */
    private const float NightLightAngleX = -8;
    private const float NightLightAngleY = -2;
    public static bool NightlightEnabled;
    private static bool _nightlightInitialized;
    private static bool _mechaOnEarth;
    private static AnimationState _sail;
    private static Light _sunlight;

    private static void NightLightUpdateState()
    {
        if (ArchitectModeEnabled.Value)
        {
            NightlightEnabled = _mechaOnEarth;
            return;
        }
        NightlightEnabled = false;
        if (_sunlight == null) return;
        _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
    }

	public static void NightLightLateUpdate()
    {
        switch (_nightlightInitialized)
        {
            case false:
                NightLightReady();
                break;
            case true:
                NightLightGo();
                break;
        }
    }

	private static void NightLightReady()
	{
        if (!GameMain.isRunning || !GameMain.mainPlayer.controller.model.gameObject.activeInHierarchy) return;
        if (_sail == null)
        {
            _sail = GameMain.mainPlayer.animator.sails[GameMain.mainPlayer.animator.sailAnimIndex];
        }

        _nightlightInitialized = true;
    }

	private static void NightLightGo()
	{
		if (!GameMain.isRunning)
		{
			NightLightEnd();
            return;
        }

		if (_sail.enabled)
        {
            _mechaOnEarth = false;
            NightlightEnabled = false;
            if (_sunlight == null) return;
            _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
            _sunlight = null;
            return;
        }

        if (!_mechaOnEarth)
        {
            if (_sunlight == null)
            {
                _sunlight = GameMain.universeSimulator.LocalStarSimulator().sunLight;
                if (_sunlight == null) return;
            }
            _mechaOnEarth = true;
            NightlightEnabled = ArchitectModeEnabled.Value;
        }
        if (NightlightEnabled)
        {
            _sunlight.transform.rotation =
                Quaternion.LookRotation(-GameMain.mainPlayer.transform.up + GameMain.mainPlayer.transform.forward * NightLightAngleX / 10f + GameMain.mainPlayer.transform.right * NightLightAngleY / 10f);
        }
    }

	private static void NightLightEnd()
	{
		_mechaOnEarth = false;
        NightlightEnabled = false;
		if (_sunlight != null)
		{
			_sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
			_sunlight = null;
		}
		_sail = null;
		_nightlightInitialized = false;
	}

	[HarmonyTranspiler]
	[HarmonyPatch(typeof(StarSimulator), "LateUpdate")]
    private static IEnumerable<CodeInstruction> StarSimulator_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        // var vec = NightlightEnabled ? GameMain.mainPlayer.transform.up : __instance.transform.forward;
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        var label2 = generator.DefineLabel();
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform)))
        ).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(BuildPatch), nameof(BuildPatch.NightlightEnabled))),
            new CodeInstruction(OpCodes.Brfalse_S, label1),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.mainPlayer))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.transform))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.up))),
            new CodeInstruction(OpCodes.Stloc_0),
            new CodeInstruction(OpCodes.Br_S, label2)
        );
        matcher.Labels.Add(label1);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Stloc_0)
        ).Advance(1).Labels.Add(label2);
        return matcher.InstructionEnumeration();
    }

	[HarmonyTranspiler]
	[HarmonyPatch(typeof(PlanetSimulator), "LateUpdate")]
    private static IEnumerable<CodeInstruction> PlanetSimulator_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        // var vec = (NightlightEnabled ? GameMain.mainPlayer.transform.up : (Quaternion.Inverse(localPlanet.runtimeRotation) * (__instance.planetData.star.uPosition - __instance.planetData.uPosition).normalized));
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        var label2 = generator.DefineLabel();
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Pop)
        ).Advance(1).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(BuildPatch), nameof(BuildPatch.NightlightEnabled))),
            new CodeInstruction(OpCodes.Brfalse_S, label1),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.mainPlayer))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.transform))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.up))),
            new CodeInstruction(OpCodes.Stloc_1),
            new CodeInstruction(OpCodes.Br_S, label2)
        );
        matcher.Labels.Add(label1);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryModel), nameof(FactoryModel.whiteMode0)))
        ).Labels.Add(label2);
        return matcher.InstructionEnumeration();
    }
    /* Night Light End */

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
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BuildPatch), nameof(ArrivePlanet)))
            );
            return matcher.InstructionEnumeration();
        }
    }

    private static class ArchitectMode
    {
        private static Harmony _architectPatch;
        private static bool[] _canBuildItems;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                if (_architectPatch != null)
                {
                    return;
                }

                var factory = GameMain.mainPlayer?.factory;
                if (factory != null)
                {
                    ArrivePlanet(factory);
                }

                _architectPatch = Harmony.CreateAndPatchAll(typeof(ArchitectMode));
            }
            else if (_architectPatch != null)
            {
                _architectPatch.UnpatchSelf();
                _architectPatch = null;
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
            return itemId >= 12000 || !_canBuildItems[itemId];
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
            if (itemId < 12000 && _canBuildItems[itemId]) __result = 100;
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
            _canBuildItems = new bool[12000];
            foreach (var ip in LDB.items.dataArray)
            {
                if (ip.CanBuild && ip.ID < 12000) _canBuildItems[ip.ID] = true;
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
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NoConditionBuild), nameof(CheckForMiner)));
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