using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UXAssist.Common;

namespace CheatEnabler.Patches.Factory;

internal class ControlPanelRemoteLogistics : PatchImpl<ControlPanelRemoteLogistics>
{
    // Harmony transpiler: UIControlPanelDispenserInspector_OnItemIconMouseDown_Transpiler
    // Target: UIControlPanelDispenserInspector.OnItemIconMouseDown, UIControlPanelDispenserInspector.OnHoldupItemClick, UIControlPanelDispenserInspector.OnCourierIconClick
    // Fallback: None — patch will fail loudly if the target method body changes.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIControlPanelDispenserInspector), nameof(UIControlPanelDispenserInspector.OnItemIconMouseDown))]
    [HarmonyPatch(typeof(UIControlPanelDispenserInspector), nameof(UIControlPanelDispenserInspector.OnHoldupItemClick))]
    [HarmonyPatch(typeof(UIControlPanelDispenserInspector), nameof(UIControlPanelDispenserInspector.OnCourierIconClick))]
    private static IEnumerable<CodeInstruction> UIControlPanelDispenserInspector_OnItemIconMouseDown_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        Label? branch = null;
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UIControlPanelDispenserInspector), nameof(UIControlPanelDispenserInspector.isLocal))),
            new CodeMatch(ci => ci.Branches(out branch))
        ).Repeat(
            m =>
            {
                if (branch == null)
                {
                    m.Advance(3);
                    return;
                }
                var labels = m.Labels;
                m.RemoveInstructions(3).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Br, branch.Value).WithLabels(labels)
                );
            }
        );
        return matcher.InstructionEnumeration();
    }
    // Harmony transpiler: UIControlPanelStationInspector_OnShipIconClick_Transpiler
    // Target: UIControlPanelStationInspector.OnShipIconClick, UIControlPanelStationInspector.OnWarperIconClick, UIControlPanelStationInspector.OnDroneIconClick
    // Fallback: None — patch will fail loudly if the target method body changes.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIControlPanelStationInspector), nameof(UIControlPanelStationInspector.OnShipIconClick))]
    [HarmonyPatch(typeof(UIControlPanelStationInspector), nameof(UIControlPanelStationInspector.OnWarperIconClick))]
    [HarmonyPatch(typeof(UIControlPanelStationInspector), nameof(UIControlPanelStationInspector.OnDroneIconClick))]
    private static IEnumerable<CodeInstruction> UIControlPanelStationInspector_OnShipIconClick_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        Label? branch = null;
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UIControlPanelStationInspector), nameof(UIControlPanelStationInspector.isLocal))),
            new CodeMatch(ci => ci.Branches(out branch))
        ).Repeat(
            m =>
            {
                if (branch == null)
                {
                    m.Advance(3);
                    return;
                }
                var labels = m.Labels;
                m.RemoveInstructions(3).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Br, branch.Value).WithLabels(labels)
                );
            }
        );
        return matcher.InstructionEnumeration();
    }
    // Harmony transpiler: UIControlPanelStationStorage_OnItemIconMouseDown_Transpiler
    // Target: UIControlPanelStationStorage.OnItemIconMouseDown
    // Fallback: None — patch will fail loudly if the target method body changes.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.OnItemIconMouseDown))]
    private static IEnumerable<CodeInstruction> UIControlPanelStationStorage_OnItemIconMouseDown_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        Label? branch = null;
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.isLocal))),
            new CodeMatch(ci => ci.Branches(out branch))
        ).Repeat(
            m =>
            {
                if (branch == null)
                {
                    m.Advance(3);
                    return;
                }
                var labels = m.Labels;
                m.RemoveInstructions(3).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Br, branch.Value).WithLabels(labels)
                );
            }
        );
        return matcher.InstructionEnumeration();
    }
    // Harmony transpiler: UIControlPanelStationStorage_OnTakeBackButtonClick_Transpiler
    // Target: UIControlPanelStationStorage.OnTakeBackButtonClick
    // Fallback: None — patch will fail loudly if the target method body changes.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.OnTakeBackButtonClick))]
    private static IEnumerable<CodeInstruction> UIControlPanelStationStorage_OnTakeBackButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.isLocal))),
            new CodeMatch(ci => ci.Branches(out _))
        ).Repeat(
            m =>
            {
                var labels = m.Labels;
                m.RemoveInstructions(3).Labels.AddRange(labels);
            }
        );
        return matcher.InstructionEnumeration();
    }
    // Harmony transpiler: UIControlPanelVeinCollectorPanel_OnProductIconClick_Transpiler
    // Target: UIControlPanelVeinCollectorPanel.OnProductIconClick
    // Fallback: None — patch will fail loudly if the target method body changes.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIControlPanelVeinCollectorPanel), nameof(UIControlPanelVeinCollectorPanel.OnProductIconClick))]
    private static IEnumerable<CodeInstruction> UIControlPanelVeinCollectorPanel_OnProductIconClick_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        Label? branch = null;
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UIControlPanelVeinCollectorPanel), nameof(UIControlPanelVeinCollectorPanel.isLocal))),
            new CodeMatch(ci => ci.Branches(out branch))
        ).Repeat(
            m =>
            {
                if (branch == null)
                {
                    m.Advance(3);
                    return;
                }
                var labels = m.Labels;
                m.RemoveInstructions(3).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Br, branch.Value).WithLabels(labels)
                );
            }
        );
        return matcher.InstructionEnumeration();
    }
}
