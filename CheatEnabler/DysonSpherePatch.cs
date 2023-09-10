using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using PowerNetworkStructures;

namespace CheatEnabler;

public static class DysonSpherePatch
{
    public static ConfigEntry<bool> SkipBulletEnabled;
    public static ConfigEntry<bool> SkipAbsorbEnabled;
    public static ConfigEntry<bool> QuickAbsortEnabled;
    public static ConfigEntry<bool> EjectAnywayEnabled;
    private static Harmony _skipBulletPatch;
    private static Harmony _skipAbsorbPatch;
    private static Harmony _quickAbsortPatch;
    private static Harmony _ejectAnywayPatch;
    
    public static void Init()
    {
        SkipBulletEnabled.SettingChanged += (_, _) => SkipBulletValueChanged();
        SkipAbsorbEnabled.SettingChanged += (_, _) => SkipAbsorbValueChanged();
        QuickAbsortEnabled.SettingChanged += (_, _) => QuickAbsortValueChanged();
        EjectAnywayEnabled.SettingChanged += (_, _) => EjectAnywayValueChanged();
        SkipBulletValueChanged();
        SkipAbsorbValueChanged();
        QuickAbsortValueChanged();
        EjectAnywayValueChanged();
    }
    
    public static void Uninit()
    {
        if (_skipBulletPatch != null)
        {
            _skipBulletPatch.UnpatchSelf();
            _skipBulletPatch = null;
        }
        if (_skipAbsorbPatch != null)
        {
            _skipAbsorbPatch.UnpatchSelf();
            _skipAbsorbPatch = null;
        }
        if (_quickAbsortPatch != null)
        {
            _quickAbsortPatch.UnpatchSelf();
            _quickAbsortPatch = null;
        }
        if (_ejectAnywayPatch != null)
        {
            _ejectAnywayPatch.UnpatchSelf();
            _ejectAnywayPatch = null;
        }
    }
    
    private static void SkipBulletValueChanged()
    {
        if (SkipBulletEnabled.Value)
        {
            if (_skipBulletPatch != null)
            {
                _skipBulletPatch.UnpatchSelf();
                _skipBulletPatch = null;
            }
            _skipBulletPatch = Harmony.CreateAndPatchAll(typeof(SkipBulletPatch));
        }
        else if (_skipBulletPatch != null)
        {
            _skipBulletPatch.UnpatchSelf();
            _skipBulletPatch = null;
        }
    }
    
    private static void SkipAbsorbValueChanged()
    {
        if (SkipAbsorbEnabled.Value)
        {
            if (_skipAbsorbPatch != null)
            {
                _skipAbsorbPatch.UnpatchSelf();
                _skipAbsorbPatch = null;
            }
            _skipAbsorbPatch = Harmony.CreateAndPatchAll(typeof(SkipAbsorbPatch));
        }
        else if (_skipAbsorbPatch != null)
        {
            _skipAbsorbPatch.UnpatchSelf();
            _skipAbsorbPatch = null;
        }
    }
    
    private static void QuickAbsortValueChanged()
    {
        if (QuickAbsortEnabled.Value)
        {
            if (_quickAbsortPatch != null)
            {
                _quickAbsortPatch.UnpatchSelf();
                _quickAbsortPatch = null;
            }
            _quickAbsortPatch = Harmony.CreateAndPatchAll(typeof(QuickAbsortPatch));
        }
        else if (_quickAbsortPatch != null)
        {
            _quickAbsortPatch.UnpatchSelf();
            _quickAbsortPatch = null;
        }
    }
    
    private static void EjectAnywayValueChanged()
    {
        if (EjectAnywayEnabled.Value)
        {
            if (_ejectAnywayPatch != null)
            {
                _ejectAnywayPatch.UnpatchSelf();
                _ejectAnywayPatch = null;
            }
            _ejectAnywayPatch = Harmony.CreateAndPatchAll(typeof(EjectAnywayPatch));
        }
        else if (_ejectAnywayPatch != null)
        {
            _ejectAnywayPatch.UnpatchSelf();
            _ejectAnywayPatch = null;
        }
    }

    private static class SkipBulletPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            return matcher.InstructionEnumeration();
        }
    }
    
    private static class SkipAbsorbPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.AbsorbSail))]
        private static IEnumerable<CodeInstruction> DysonSwarm_AbsorbSail_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(ExpiryOrder), nameof(ExpiryOrder.index)))
            ).Advance(1).RemoveInstructions(matcher.Length - matcher.Pos).Insert(
                // node.cpOrdered = node.cpOrdered + 1;
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode.cpOrdered))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode.cpOrdered))),
                
                // if (node.ConstructCp() != null)
                // {
                //     this.dysonSphere.productRegister[11903]++;
                // }
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(DysonNode), nameof(DysonNode.ConstructCp))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSwarm), nameof(DysonSwarm.dysonSphere))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphere), nameof(DysonSphere.productRegister))),
                new CodeInstruction(OpCodes.Ldc_I4, 11903),
                new CodeInstruction(OpCodes.Ldelema, typeof(int)),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldind_I4),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stind_I4),
                
                // this.RemoveSolarSail(index);
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(label1),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DysonSwarm), nameof(DysonSwarm.RemoveSolarSail))),
                
                // return false;
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ret)
            );
            return matcher.InstructionEnumeration();
        }
    }
    
    private static class QuickAbsortPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonSphereLayer), nameof(DysonSphereLayer.GameTick))]
        private static IEnumerable<CodeInstruction> DysonSphereLayer_GameTick_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* Remove `dysonNode.id % 120 == num` */
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_S, 120)
            ).Advance(-2).RemoveInstructions(6);
            return matcher.InstructionEnumeration();
        }
    }
    
    private static class EjectAnywayPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.localRot)))
            );
            var start = matcher.Pos - 6;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.And)
            ).Advance(1).MatchForward(false,
                new CodeMatch(OpCodes.And)
            );
            var end = matcher.Pos - 2;
            /* Remove angle checking codes, then add:
             *   V_13 = this.bulletCount > 0;
             */
            matcher.Start().Advance(start).RemoveInstructions(end - start).Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.bulletCount))),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.Stloc_S, 13)
            );
            return matcher.InstructionEnumeration();
        }
    }
}
