using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.GameConstants;

namespace CheatEnabler.Patches.Factory;

internal class RemovePowerSpaceLimit : PatchImpl<RemovePowerSpaceLimit>
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
    [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
    private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Ldc_R4, 110.25f)
        );
        if (matcher.IsValid)
        {
            matcher.Repeat(codeMatcher => codeMatcher.SetAndAdvance(
                OpCodes.Ldc_R4, 1f
            ));
        }
        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Ldc_R4, 144f)
        );
        if (matcher.IsValid)
        {
            matcher.Repeat(codeMatcher => codeMatcher.SetAndAdvance(
                OpCodes.Ldc_R4, 1f
            ));
        }
        return matcher.InstructionEnumeration();
    }
}

internal class BoostWindPower : PatchImpl<BoostWindPower>
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_Wind))]
    private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_Wind_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.Start().RemoveInstructions(matcher.Length);
        matcher.Insert(
            // this.currentStrength = windStrength
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.currentStrength))),
            // this.capacityCurrentTick = 500000000L
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldc_I8, 500000000L),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.capacityCurrentTick))),
            // return 500000000L
            new CodeInstruction(OpCodes.Ldc_I8, 500000000L),
            new CodeInstruction(OpCodes.Ret)
        );
        return matcher.InstructionEnumeration();
    }
}

internal class BoostSolarPower : PatchImpl<BoostSolarPower>
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_PV))]
    private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_PV_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.Start().RemoveInstructions(matcher.Length).Insert(
            // this.currentStrength = lumino
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_S, 4),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.currentStrength))),
            // this.capacityCurrentTick = 600000000L
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldc_I8, 600000000L),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.capacityCurrentTick))),
            // return 600000000L
            new CodeInstruction(OpCodes.Ldc_I8, 600000000L),
            new CodeInstruction(OpCodes.Ret)
        );
        return matcher.InstructionEnumeration();
    }
}

internal class BoostFuelPower : PatchImpl<BoostFuelPower>
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_Fuel))]
    private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_Fuel_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        var label2 = generator.DefineLabel();
        var label3 = generator.DefineLabel();
        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.capacityCurrentTick)))
        );
        var labels = matcher.Labels;
        matcher.Labels = [];
        matcher.Insert(
            // if (this.fuelMask == 4)
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.fuelMask))),
            new CodeInstruction(OpCodes.Ldc_I4_4),
            new CodeInstruction(OpCodes.Bne_Un_S, label1),
            // multiplier = 10000L
            new CodeInstruction(OpCodes.Ldc_I8, 10000L),
            new CodeInstruction(OpCodes.Br_S, label3),
            // else if (this.fuelMask == 2)
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(label1),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.fuelMask))),
            new CodeInstruction(OpCodes.Ldc_I4_2),
            new CodeInstruction(OpCodes.Bne_Un_S, label2),
            // multiplier = 20000L
            new CodeInstruction(OpCodes.Ldc_I8, 20000L),
            new CodeInstruction(OpCodes.Br_S, label3),
            // else multiplier = 50000L
            new CodeInstruction(OpCodes.Ldc_I8, 50000L).WithLabels(label2),
            // do multiplier before store to this.capacityCurrentTick
            new CodeInstruction(OpCodes.Mul).WithLabels(label3)
        );
        return matcher.InstructionEnumeration();
    }
}

internal class BoostGeothermalPower : PatchImpl<BoostGeothermalPower>
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_GTH))]
    private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_GTH_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.Start().RemoveInstructions(matcher.Length).Insert(
            // this.currentStrength = this.gthStrength
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.gthStrength))),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.currentStrength))),
            // this.capacityCurrentTick = 2000000000L
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldc_I8, 2000000000L),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.capacityCurrentTick))),
            // return 2000000000L
            new CodeInstruction(OpCodes.Ldc_I8, 2000000000L),
            new CodeInstruction(OpCodes.Ret)
        );
        return matcher.InstructionEnumeration();
    }
}

internal static class WindTurbinesPowerGlobalCoverage
{
    private static bool _patched;
    private static PrefabDesc _prefabdesc;
    private static float _oldCoverRadius;
    private static float _oldConnectDistance;
    private const int WindTurbineId = ItemIds.WindTurbine;
    private const float WindTurbineNewCoverageDistance = 500f;

    public static void Enable(bool enable)
    {
        if (enable)
        {
            if (_patched) return;
            _patched = true;
            var itemProto = LDB.items.Select(WindTurbineId);
            _oldCoverRadius = itemProto.prefabDesc.powerCoverRadius;
            _oldConnectDistance = itemProto.prefabDesc.powerConnectDistance;
            itemProto.prefabDesc.powerCoverRadius = WindTurbineNewCoverageDistance;
            itemProto.prefabDesc.powerConnectDistance = WindTurbineNewCoverageDistance;
            _prefabdesc = itemProto.prefabDesc;
        }
        else
        {
            if (!_patched) return;
            _patched = false;
            _prefabdesc.powerCoverRadius = _oldCoverRadius;
            _prefabdesc.powerConnectDistance = _oldConnectDistance;
        }

        // Iterate all factories and update wind turbines power nodes
        if (GameMain.data == null) return;
        foreach (var factory in GameMain.data.factories)
        {
            var powerSystem = factory?.powerSystem;
            if (powerSystem == null) continue;
            for (var i = powerSystem.nodeCursor - 1; i >= 0; i--)
            {
                ref var node = ref powerSystem.nodePool[i];
                if (node.id != i) continue;
                ref var entity = ref factory.entityPool[node.entityId];
                if (entity.protoId != WindTurbineId) continue;
                // Disconnect from power system
                powerSystem.OnNodeRemoving(i);
                // Set new properties
                node.connectDistance = _prefabdesc.powerConnectDistance;
                node.coverRadius = _prefabdesc.powerCoverRadius;
                // Connect back to power system
                powerSystem.OnNodeAdded(i);
            }
            // Refresh power nodes rendering if factory is loaded
            if (factory.planet.factoryLoaded)
            {
                factory.planet.factoryModel.RefreshPowerNodes();
            }
        }
    }
}
