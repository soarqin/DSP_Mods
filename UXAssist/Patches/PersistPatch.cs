using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;

namespace UXAssist.Patches;

public class PersistPatch : PatchImpl<PersistPatch>
{
    public static void Start()
    {
        Enable(true);
    }

    public static void Uninit()
    {
        Enable(false);
    }

    // Check for noModifier while pressing hotkeys on build bar
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIBuildMenu), nameof(UIBuildMenu._OnUpdate))]
    private static IEnumerable<CodeInstruction> UIBuildMenu__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.inScreen)))
        );
        matcher.Repeat(codeMatcher =>
        {
            var jumpPos = codeMatcher.Advance(1).Operand;
            codeMatcher.Advance(-1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.noModifier))),
                new CodeInstruction(OpCodes.Brfalse_S, jumpPos)
            ).Advance(2);
        });
        return matcher.InstructionEnumeration();
    }

    // Patch to fix the issue that warning popup on VeinUtil upgraded to level 8000+
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ABN_VeinsUtil), nameof(ABN_VeinsUtil.CheckValue))]
    private static IEnumerable<CodeInstruction> ABN_VeinsUtil_CheckValue_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldelem_R8),
            new CodeMatch(OpCodes.Conv_R4),
            new CodeMatch(OpCodes.Add),
            new CodeMatch(OpCodes.Stloc_1)
        );
        // loc1 = Mathf.Round(n * 1000f) / 1000f;
        matcher.Advance(3).Insert(
            new CodeInstruction(OpCodes.Ldc_R4, 1000f),
            new CodeInstruction(OpCodes.Mul),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Round))),
            new CodeInstruction(OpCodes.Ldc_R4, 1000f),
            new CodeInstruction(OpCodes.Div)
        );
        return matcher.InstructionEnumeration();
    }

    // Bring popup tip window to top layer
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIButton), nameof(UIButton.LateUpdate))]
    private static IEnumerable<CodeInstruction> UIButton_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldloc_2),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject))),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.activeSelf)))
        );
        var labels = matcher.Labels;
        matcher.Labels = null;
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldloc_2).WithLabels(labels),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.parent))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.parent))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), nameof(Transform.SetAsLastSibling)))
        );
        return matcher.InstructionEnumeration();
    }

    // Sort blueprint structures by item id, model index, recipe id, area index, and position before saving
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlueprintUtils), nameof(BlueprintUtils.GenerateBlueprintData))]
    private static void BlueprintUtils_GenerateBlueprintData_Postfix(BlueprintData _blueprintData)
    {
        var buildings = _blueprintData.buildings;
        Array.Sort(buildings, (a, b) =>
        {
            var tmpItemId = a.itemId - b.itemId;
            if (tmpItemId != 0)
                return tmpItemId;
            var tmpModelIndex = a.modelIndex - b.modelIndex;
            if (tmpModelIndex != 0)
                return tmpModelIndex;
            var tmpRecipeId = a.recipeId - b.recipeId;
            if (tmpRecipeId != 0)
                return tmpRecipeId;
            var tmpAreaIndex = a.areaIndex - b.areaIndex;
            if (tmpAreaIndex != 0)
                return tmpAreaIndex;
            const double ky = 256.0;
            const double kx = 1024.0;
            var scorePosA = (a.localOffset_y * ky + a.localOffset_x) * kx + a.localOffset_z;
            var scorePosB = (b.localOffset_y * ky + b.localOffset_x) * kx + b.localOffset_z;
            return scorePosA < scorePosB ? 1 : -1;
        });
        for (var i = buildings.Length - 1; i >= 0; i--)
        {
            buildings[i].index = i;
        }
    }

    // Increase maximum value of property realizing, 2000 -> 20000
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.UpdateUIElements))]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.OnRealizeButtonClick))]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.OnInputValueEnd))]
    private static IEnumerable<CodeInstruction> UIProductEntry_UpdateUIElements_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldc_I4, 2000)
        );
        matcher.Repeat(m => { m.SetAndAdvance(OpCodes.Ldc_I4, 20000); });
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.OnInputValueEnd))]
    private static IEnumerable<CodeInstruction> UIProductEntry_OnInputValueEnd_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_R4 && ci.OperandIs(2000f))
        );
        matcher.Repeat(m => { m.SetAndAdvance(OpCodes.Ldc_R4, 20000f); });
        return matcher.InstructionEnumeration();
    }

    // Increase capacity of player order queue, 16 -> 128
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlayerOrder), MethodType.Constructor, typeof(Player))]
    private static IEnumerable<CodeInstruction> PlayerOrder_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => (ci.opcode == OpCodes.Ldc_I4_S || ci.opcode == OpCodes.Ldc_I4) && ci.OperandIs(16))
        );
        matcher.Repeat(m => { m.SetAndAdvance(OpCodes.Ldc_I4, 128); });
        return matcher.InstructionEnumeration();
    }

    // Increase Player Command Queue from 16 to 128
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlayerOrder), nameof(PlayerOrder._trimEnd))]
    [HarmonyPatch(typeof(PlayerOrder), nameof(PlayerOrder.Enqueue))]
    private static IEnumerable<CodeInstruction> PlayerOrder_ExtendCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => (ci.opcode == OpCodes.Ldc_I4_S || ci.opcode == OpCodes.Ldc_I4) && ci.OperandIs(16))
        );
        matcher.Repeat(m => { m.SetAndAdvance(OpCodes.Ldc_I4, 128); });
        return matcher.InstructionEnumeration();
    }

    // Allow F11 in star map
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnLateUpdate))]
    private static IEnumerable<CodeInstruction> UIGame__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.inFullscreenGUI))),
            new CodeMatch(ci => ci.opcode == OpCodes.Brfalse || ci.opcode == OpCodes.Brfalse_S)
        );
        var jumpPos = matcher.Advance(1).Operand;
        matcher.Advance(-1).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIGame), nameof(UIGame.starmap))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ManualBehaviour), nameof(ManualBehaviour.active))),
            new CodeInstruction(OpCodes.Brtrue_S, jumpPos)
        );
        return matcher.InstructionEnumeration();
    }

    // Ignore UIDFCommunicatorWindow.Determine()
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIDFCommunicatorWindow), nameof(UIDFCommunicatorWindow.Determine))]
    private static bool UIDFCommunicatorWindow_Determine_Prefix()
    {
        return false;
    }
}
