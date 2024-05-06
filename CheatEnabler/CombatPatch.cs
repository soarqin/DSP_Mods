using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;

public static class CombatPatch
{
    public static ConfigEntry<bool> MechaInvincibleEnabled;
    public static ConfigEntry<bool> BuildingsInvincibleEnabled;

    public static void Init()
    {
        MechaInvincibleEnabled.SettingChanged += (_, _) => MechaInvincible.Enable(MechaInvincibleEnabled.Value);
        BuildingsInvincibleEnabled.SettingChanged += (_, _) => BuildingsInvincible.Enable(BuildingsInvincibleEnabled.Value);
        MechaInvincible.Enable(MechaInvincibleEnabled.Value);
        BuildingsInvincible.Enable(BuildingsInvincibleEnabled.Value);
    }

    public static void Uninit()
    {
        BuildingsInvincible.Enable(false);
        MechaInvincible.Enable(false);
    }

    private static class MechaInvincible
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(MechaInvincible));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), nameof(Player.invincible), MethodType.Getter)]
        private static IEnumerable<CodeInstruction> Player_get_invincible_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().RemoveInstructions(matcher.Length).Insert(
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ret)
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageGroundObjectByLocalCaster))]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageGroundObjectByRemoteCaster))]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageObject))]
        private static IEnumerable<CodeInstruction> SkillSystem_DamageObject_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsLdarg()),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SkillTargetLocal), nameof(SkillTargetLocal.type))),
                new CodeMatch(OpCodes.Ldc_I4_6),
                new CodeMatch(ci => ci.opcode == OpCodes.Bne_Un || ci.opcode == OpCodes.Bne_Un_S)
            );
            matcher.Repeat(m => m.Advance(4).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Starg_S, __originalMethod.Name == "DamageObject" ? 1 : 2)
            ));
            return matcher.InstructionEnumeration();
        }
    }

    private static class BuildingsInvincible
    {
        private static Harmony _patch;
        
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(BuildingsInvincible));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageGroundObjectByLocalCaster))]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageGroundObjectByRemoteCaster))]
        private static IEnumerable<CodeInstruction> SkillSystem_DamageObject_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsLdarg()),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SkillTargetLocal), nameof(SkillTargetLocal.type))),
                new CodeMatch(ci => ci.opcode == OpCodes.Brtrue || ci.opcode == OpCodes.Brtrue_S),
                new CodeMatch(OpCodes.Ldarg_1)
            ).Advance(3).Insert(
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Starg_S, 2)
            );
            return matcher.InstructionEnumeration();
        }
    }
}