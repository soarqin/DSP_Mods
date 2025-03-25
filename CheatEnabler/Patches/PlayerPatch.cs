using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;

namespace CheatEnabler.Patches;

public static class PlayerPatch
{
    public static ConfigEntry<bool> InstantHandCraftEnabled;
    public static ConfigEntry<bool> InstantTeleportEnabled;
    public static ConfigEntry<bool> WarpWithoutSpaceWarpersEnabled;

    public static void Init()
    {
        InstantHandCraftEnabled.SettingChanged += (_, _) => InstantHandCraft.Enable(InstantHandCraftEnabled.Value);
        InstantTeleportEnabled.SettingChanged += (_, _) => InstantTeleport.Enable(InstantTeleportEnabled.Value);
        WarpWithoutSpaceWarpersEnabled.SettingChanged += (_, _) => WarpWithoutSpaceWarpers.Enable(WarpWithoutSpaceWarpersEnabled.Value);
    }

    public static void Start()
    {
        InstantHandCraft.Enable(InstantHandCraftEnabled.Value);
        InstantTeleport.Enable(InstantTeleportEnabled.Value);
        WarpWithoutSpaceWarpers.Enable(WarpWithoutSpaceWarpersEnabled.Value);
    }

    public static void Uninit()
    {
        InstantHandCraft.Enable(false);
        InstantTeleport.Enable(false);
        WarpWithoutSpaceWarpers.Enable(false);
    }

    private class InstantHandCraft: PatchImpl<InstantHandCraft>
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ForgeTask), MethodType.Constructor, typeof(int), typeof(int))]
        private static void ForgeTask_Ctor_Postfix(ForgeTask __instance)
        {
            __instance.tickSpend = 0;
        }
    }

    private class InstantTeleport: PatchImpl<InstantTeleport>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIGlobemap), nameof(UIGlobemap._OnUpdate))]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap.DoRightClickFastTravel))]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap.OnFastTravelButtonClick))]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap.OnScreenClick))]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap.SandboxRightClickFastTravelLogic))]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap.StartFastTravelToPlanet))]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap.StartFastTravelToUPosition))]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap.UpdateCursorView))]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap._OnUpdate))]
        private static IEnumerable<CodeInstruction> UIGlobemap__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.sandboxToolsEnabled)))
            );
            matcher.Repeat(cm => cm.SetAndAdvance(OpCodes.Ldc_I4_1, null));
            return matcher.InstructionEnumeration();
        }
    }

    private class WarpWithoutSpaceWarpers: PatchImpl<WarpWithoutSpaceWarpers>
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Mecha), nameof(Mecha.HasWarper))]
        private static bool Mecha_HasWarper_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mecha), nameof(Mecha.UseWarper))]
        private static void Mecha_UseWarper_Postfix(ref bool __result)
        {
            __result = true;
        }
    }
}
