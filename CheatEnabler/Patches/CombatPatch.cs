using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;
using UXAssist.Common.Patching;

namespace CheatEnabler.Patches;

[ModFeature("Combat")]
public static class CombatPatch
{
    public static ConfigEntry<bool> MechaInvincibleEnabled;
    public static ConfigEntry<bool> BuildingsInvincibleEnabled;

    public static void Init()
    {
        MechaInvincibleEnabled.SettingChanged += (_, _) => MechaInvincible.Enable(MechaInvincibleEnabled.Value);
        BuildingsInvincibleEnabled.SettingChanged += (_, _) => BuildingsInvincible.Enable(BuildingsInvincibleEnabled.Value);
    }

    public static void Start()
    {
        MechaInvincible.Enable(MechaInvincibleEnabled.Value);
        BuildingsInvincible.Enable(BuildingsInvincibleEnabled.Value);
    }

    public static void Uninit()
    {
        BuildingsInvincible.Enable(false);
        MechaInvincible.Enable(false);
    }

    private class MechaInvincible : PatchImpl<MechaInvincible>
    {
        // Harmony transpiler: Player_get_invincible_Transpiler
        // Target: Player.invincible (getter)
        // Fallback: None — patch will fail loudly if the target method body changes.
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
        // Harmony transpiler: Mecha_Invincible_Transpiler
        // Target: Mecha.TakeDamage, Mecha.EnergyShieldResist (both overloads)
        // Fallback: Return original instructions if the invincibility check is not found.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Mecha), nameof(Mecha.TakeDamage))]
        [HarmonyPatch(typeof(Mecha), nameof(Mecha.EnergyShieldResist), [typeof(int)], [ArgumentType.Ref])]
        [HarmonyPatch(typeof(Mecha), nameof(Mecha.EnergyShieldResist), [typeof(int), typeof(int)], [ArgumentType.Ref, ArgumentType.Out])]
        private static IEnumerable<CodeInstruction> Mecha_Invincible_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // Repatch the consumers too: an already inlined getter bypasses its own Harmony patch.
            var original = instructions.ToList();
            var matcher = new CodeMatcher(original, generator);
            matcher.MatchForward(false, new CodeMatch(ci => ci.Calls(AccessTools.PropertyGetter(typeof(Player), nameof(Player.invincible)))));
            if (matcher.IsInvalid) return matcher.Finish(original, CheatEnabler.Logger, nameof(Mecha_Invincible_Transpiler));
            matcher.Set(OpCodes.Pop, null).Advance(1).Insert(new CodeInstruction(OpCodes.Ldc_I4_1));
            return matcher.InstructionEnumeration();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageGroundObjectByLocalCaster))]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageGroundObjectByRemoteCaster))]
        private static void SkillSystem_DamageGroundObject_Prefix(ref int damage, ref SkillTargetLocal target)
        {
            if (target.type == ETargetType.Craft) damage = 0;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageObject))]
        private static void SkillSystem_DamageObject_Prefix(ref int damage, ref SkillTarget target)
        {
            if (target.type == ETargetType.Craft) damage = 0;
        }
    }

    private class BuildingsInvincible : PatchImpl<BuildingsInvincible>
    {
        // Harmony transpiler: SkillSystem_DamageObject_Transpiler
        // Target: SkillSystem.DamageGroundObjectByLocalCaster, SkillSystem.DamageGroundObjectByRemoteCaster
        // Fallback: None — patch will fail loudly if the target method body changes.
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
