using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common.GameConstants;
using UXAssist.Common.ModFeatures;

namespace UniverseGenTweaks;

[ModFeature("UniverseGenGalaxyGen", Order = 11)]
public static class GalaxyGenSettingsPatch
{
    internal static double MinDist = UniverseGenConstants.DefaultMinDist;
    internal static double MinStep = UniverseGenConstants.DefaultMinStep;
    internal static double MaxStep = UniverseGenConstants.DefaultMaxStep;
    internal static double Flatten = UniverseGenConstants.DefaultFlatten;

    internal static double GameMinDist = UniverseGenConstants.DefaultMinDist;
    internal static double GameMinStep = UniverseGenConstants.DefaultMinStep;
    internal static double GameMaxStep = UniverseGenConstants.DefaultMaxStep;
    internal static double GameFlatten = UniverseGenConstants.DefaultFlatten;

    private static Harmony _patch;
    private static Harmony _permanentPatch;

    public static void Init()
    {
        _permanentPatch ??= Harmony.CreateAndPatchAll(typeof(PermanentPatch));
        MoreSettings.Enabled.SettingChanged += OnEnabledChanged;
        Enable(MoreSettings.Enabled.Value);
    }

    public static void Uninit()
    {
        MoreSettings.Enabled.SettingChanged -= OnEnabledChanged;
        Enable(false);
        _permanentPatch?.UnpatchSelf();
        _permanentPatch = null;
    }

    private static void OnEnabledChanged(object sender, System.EventArgs e)
    {
        Enable(MoreSettings.Enabled.Value);
    }

    internal static void Enable(bool on)
    {
        if (on)
        {
            _patch ??= Harmony.CreateAndPatchAll(typeof(Patch));
            return;
        }
        _patch?.UnpatchSelf();
        _patch = null;
    }

    private static class Patch
    {
        // Harmony transpiler: UIGalaxySelect_OnStarCountSliderValueChange_Transpiler
        // Target: UIGalaxySelect.OnStarCountSliderValueChange
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect.OnStarCountSliderValueChange))]
        private static IEnumerable<CodeInstruction> UIGalaxySelect_OnStarCountSliderValueChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // Increase hard-coded maximum star count from 80 to MaxStarCount.Value
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(UniverseGenConstants.VanillaMaxStarCount))
            ).SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(MoreSettings.MaxStarCount))).Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<int>), nameof(ConfigEntry<int>.Value)))
            );
            return matcher.InstructionEnumeration();
        }
    }

    private static class PermanentPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Start))]
        private static void GameMain_Start_Prefix()
        {
            if (DSPGame.GameDesc != null)
            {
                if (GameMain.data != null) return;
                GameMinDist = MinDist;
                GameMinStep = MinStep;
                GameMaxStep = MaxStep;
                GameFlatten = Flatten;
            }
            else
            {
                GameMinDist = UniverseGenConstants.DefaultMinDist;
                GameMinStep = UniverseGenConstants.DefaultMinStep;
                GameMaxStep = UniverseGenConstants.DefaultMaxStep;
                GameFlatten = UniverseGenConstants.DefaultFlatten;
            }
        }
        // Harmony transpiler: GalaxyData_Constructor_Transpiler
        // Target: GalaxyData..ctor
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GalaxyData), MethodType.Constructor)]
        private static IEnumerable<CodeInstruction> GalaxyData_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // 25700 -> 102500
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(UniverseGenConstants.VanillaGalaxyCapacity))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, UniverseGenConstants.ExpandedGalaxyCapacity));
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: SectorModel_CreateGalaxyAstroBuffer_Transpiler
        // Target: SectorModel.CreateGalaxyAstroBuffer, SpaceColliderLogic.UpdateCollidersPose
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SectorModel), nameof(SectorModel.CreateGalaxyAstroBuffer))]
        [HarmonyPatch(typeof(SpaceColliderLogic), nameof(SpaceColliderLogic.UpdateCollidersPose))]
        private static IEnumerable<CodeInstruction> SectorModel_CreateGalaxyAstroBuffer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // 25600 -> 102500
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(UniverseGenConstants.VanillaSectorCapacity))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, UniverseGenConstants.ExpandedSectorCapacity));
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: UniverseGen_CreateGalaxy_Transpiler
        // Target: UniverseGen.CreateGalaxy
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UniverseGen), nameof(UniverseGen.CreateGalaxy))]
        private static IEnumerable<CodeInstruction> UniverseGen_CreateGalaxy_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var b0 = generator.DefineLabel();
            var b1 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UniverseGen), nameof(UniverseGen.GenerateTempPoses)))
            ).Advance(-4).RemoveInstructions(4).InsertAndAdvance(
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(UIRoot), nameof(UIRoot.instance))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIRoot), nameof(UIRoot.galaxySelect))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ManualBehaviour), nameof(ManualBehaviour.active))),
                new CodeInstruction(OpCodes.Brfalse, b0),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GalaxyGenSettingsPatch), nameof(MinDist))),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GalaxyGenSettingsPatch), nameof(MinStep))),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GalaxyGenSettingsPatch), nameof(MaxStep))),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GalaxyGenSettingsPatch), nameof(Flatten))),
                new CodeInstruction(OpCodes.Br, b1),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GalaxyGenSettingsPatch), nameof(GameMinDist))).WithLabels(b0),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GalaxyGenSettingsPatch), nameof(GameMinStep))),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GalaxyGenSettingsPatch), nameof(GameMaxStep))),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GalaxyGenSettingsPatch), nameof(GameFlatten)))
            );
            matcher.Labels.Add(b1);
            return matcher.InstructionEnumeration();
        }

        /* Patch `rand() * (maxStepLen - minStepLen) + minDist` to `rand() * (maxStepLen - minStepLen) + minStepLen`,
           this should be a bugged line in original game code. */
        // Harmony transpiler: UniverseGen_RandomPoses_Transpiler
        // Target: UniverseGen.RandomPoses
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UniverseGen), nameof(UniverseGen.RandomPoses))]
        private static IEnumerable<CodeInstruction> UniverseGen_RandomPoses_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(OpCodes.Ldarg_2)
            );
            matcher.Repeat(m => m.Advance(1).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldarg_3)));
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: UIVirtualStarmap__OnLateUpdate_Transpiler
        // Target: UIVirtualStarmap._OnLateUpdate
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIVirtualStarmap), nameof(UIVirtualStarmap._OnLateUpdate))]
        private static IEnumerable<CodeInstruction> UIVirtualStarmap__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var local1 = generator.DeclareLocal(typeof(UIVirtualStarmap.StarNode));
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIVirtualStarmap.StarNode), nameof(UIVirtualStarmap.StarNode.nameText))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(UnityEngine.Component), nameof(UnityEngine.Component.gameObject))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.SetActive)))
            ).InsertAndAdvance(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stloc, local1)
            ).Advance(3).Insert(
                new CodeInstruction(OpCodes.Ldloc, local1),
                Transpilers.EmitDelegate(bool (UIVirtualStarmap.StarNode starNode) =>
                {
                    return starNode?.starData?.type switch
                    {
                        EStarType.NeutronStar or EStarType.BlackHole => true,
                        _ => false,
                    };
                }),
                new CodeInstruction(OpCodes.Or)
            );
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: UIGalaxySelect_UpdateUIDisplay_Transpiler
        // Target: UIGalaxySelect.UpdateUIDisplay
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect.UpdateUIDisplay))]
        private static IEnumerable<CodeInstruction> UIGalaxySelect_UpdateUIDisplay_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            Label? b1 = null;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(ci => ci.Branches(out b1)),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldelem_Ref),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(ci => ci.IsLdloc())
            ).Advance(7);
            var instr = matcher.InstructionAt(0);
            matcher.Insert(
                instr,
                new CodeInstruction(OpCodes.Brfalse, b1!)
            );
            return matcher.InstructionEnumeration();
        }
    }
}
