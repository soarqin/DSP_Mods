using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;

namespace UXAssist.Patches.Logistics;

internal class AllowOverflowInLogistics : PatchImpl<AllowOverflowInLogistics>
{
    private static bool _blueprintPasting;

    // Do not check for overflow when try to send hand items into storages
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.OnItemIconMouseDown))]
    [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.OnItemIconMouseDown))]
    private static IEnumerable<CodeInstruction> UIStationStorage_OnItemIconMouseDown_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(LDB), nameof(LDB.items))),
            new CodeMatch(OpCodes.Ldarg_0)
        );
        var pos = matcher.Pos;
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldloc_S),
            new CodeMatch(OpCodes.Stloc_S)
        );
        var inst = matcher.InstructionAt(1).Clone();
        var pos2 = matcher.Pos + 2;
        matcher.Start().Advance(pos);
        var labels = matcher.Labels;
        matcher.RemoveInstructions(pos2 - pos).Insert(
            new CodeInstruction(OpCodes.Ldloc_1).WithLabels(labels),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.inhandItemCount))),
            inst
        );
        return matcher.InstructionEnumeration();
    }

    // Remove storage limit check
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
    private static IEnumerable<CodeInstruction> PlanetTransport_SetStationStorage_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.IsLdarg()),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(OpCodes.Add),
            new CodeMatch(ci => ci.Branches(out _)),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(OpCodes.Add),
            new CodeMatch(ci => ci.IsStarg())
        );
        var label = generator.DefineLabel();
        var oldLabels = matcher.Labels;
        matcher.Labels = [];
        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(AllowOverflowInLogistics), nameof(_blueprintPasting))).WithLabels(oldLabels),
            new CodeInstruction(OpCodes.Brfalse, label)
        );
        matcher.Advance(9).Labels.Add(label);
        return matcher.InstructionEnumeration();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
    private static void BuildTool_BlueprintPaste_CreatePrebuilds_Prefix()
    {
        _blueprintPasting = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
    private static void BuildTool_BlueprintPaste_CreatePrebuilds_Postfix()
    {
        _blueprintPasting = false;
    }
}
