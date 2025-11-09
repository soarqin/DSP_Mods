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

    // Bring popup tip window to top layer
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIButton), nameof(UIButton.LateUpdate))]
    private static IEnumerable<CodeInstruction> UIButton_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldloc || ci.opcode == OpCodes.Ldloc_S),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject))),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.activeSelf)))
        );
        var ldLocOpr = matcher.Operand;
        var labels = matcher.Labels;
        matcher.Labels = null;
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldloc_S, ldLocOpr).WithLabels(labels),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.parent))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.parent))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), nameof(Transform.SetAsLastSibling)))
        );
        return matcher.InstructionEnumeration();
    }

    // Sort blueprint data when pasting
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildTool_BlueprintCopy), nameof(BuildTool_BlueprintCopy.UseToPasteNow))]
    private static IEnumerable<CodeInstruction> BuildTool_BlueprintCopy_UseToPasteNow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool_BlueprintCopy), nameof(BuildTool_BlueprintCopy.RefreshBlueprintData)))
        ).Advance(2).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_BlueprintCopy), nameof(BuildTool_BlueprintCopy.blueprint))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Functions.FactoryFunctions), nameof(Functions.FactoryFunctions.SortBlueprintData)))
        );
        return matcher.InstructionEnumeration();
    }

    // Sort blueprint data when saving
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlueprintData), nameof(BlueprintData.SaveBlueprintData))]
    private static void BlueprintData_SaveBlueprintData_Prefix(BlueprintData __instance)
    {
        if (!__instance.isValid) return;
        Functions.FactoryFunctions.SortBlueprintData(__instance);
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

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(VFInput), nameof(VFInput._fastTransferWithEntityDown), MethodType.Getter)]
    [HarmonyPatch(typeof(VFInput), nameof(VFInput._fastTransferWithEntityPress), MethodType.Getter)]
    private static IEnumerable<CodeInstruction> VFInput_fastTransferWithEntityDown_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))),
            new CodeMatch(ci => ci.opcode == OpCodes.Brtrue || ci.opcode == OpCodes.Brtrue_S)
        );
        var lables = matcher.Labels;
        matcher.RemoveInstructions(2);
        matcher.Labels.AddRange(lables);
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GameTick))]
    private static IEnumerable<CodeInstruction> PlayerAction_Inspect_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldc_I4_1),
            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.fastFillIn)))
        );
        matcher.SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))).Insert(
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ceq)
        );

        var label0 = generator.DefineLabel();
        var label1 = generator.DefineLabel();
        matcher.Start().MatchForward(false,
            new CodeMatch(ci => ci.IsStloc()),
            new CodeMatch(OpCodes.Ldc_I4_0),
            new CodeMatch(ci => ci.IsStloc()),
            new CodeMatch(OpCodes.Ldloc_0),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(ci => ci.IsLdloc()),
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastTakeOut)))
        ).Advance(8).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))),
            new CodeInstruction(OpCodes.Brfalse_S, label0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PersistPatch), nameof(PersistPatch.EntityFastTakeOutAlt))),
            new CodeInstruction(OpCodes.Br, label1)
        ).Labels.Add(label0);
        matcher.Advance(1).Labels.Add(label1);
        return matcher.InstructionEnumeration();
    }

    private static void EntityFastTakeOutAlt(PlanetFactory factory, int entityId, bool toPackage, out ItemBundle itemBundle, out bool full)
    {
		if (factory._tmp_items == null)
		{
			factory._tmp_items = new ItemBundle();
		}
        else
        {
			factory._tmp_items.Clear();
		}
		itemBundle = factory._tmp_items;
		full = false;
        if (entityId == 0 || factory.entityPool[entityId].id != entityId)
		{
			return;
		}
        ref var entityData = ref factory.entityPool[entityId];
        if (entityData.beltId <= 0) return;
        var cargoTraffic = factory.cargoTraffic;
        ref var belt = ref cargoTraffic.beltPool[entityData.beltId];
        if (belt.id != entityData.beltId) return;
        var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
        var end = cargoPath.bufferLength - 1;
        var buffer = cargoPath.buffer;
        Dictionary<int, long> takeOutItems = [];
		var mainPlayer = factory.gameData.mainPlayer;
        int i = 0;
        while (i <= end)
        {
            if (buffer[i] >= 246)
            {
                i += 250 - buffer[i];
                var index = buffer[i + 1] - 1 + (buffer[i + 2] - 1) * 100 + (buffer[i + 3] - 1) * 10000 + (buffer[i + 4] - 1) * 1000000;
                ref var cargo = ref cargoPath.cargoContainer.cargoPool[index];
                var item = cargo.item;
                var stack = cargo.stack;
                var inc = cargo.inc;
                takeOutItems[item] = (takeOutItems.TryGetValue(item, out var value) ? value : 0)
                    + ((long)stack | ((long)inc << 32));
                Array.Clear(buffer, i - 4, 10);
                i += 10;
                if (cargoPath.updateLen < i) cargoPath.updateLen = i;
                cargoPath.cargoContainer.RemoveCargo(index);
            }
            else
            {
                i += 5;
                if (i > end && i < end + 5)
                {
                    i = end;
                }
            }
        }
        foreach (var kvp in takeOutItems)
        {
            var added = mainPlayer.TryAddItemToPackage(kvp.Key, (int)(kvp.Value & 0xFFFFFFFF), (int)(kvp.Value >> 32), true, entityId);
            if (added > 0)
            {
                UIItemup.Up(kvp.Key, added);
            }
        }
    }
}
