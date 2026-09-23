using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;
using UXAssist.Common.Patching;

namespace UXAssist.Patches;

public static class PlanetPatch
{
    public static ConfigEntry<bool> PlayerActionsInGlobeViewEnabled;

    public static void Init()
    {
        PlayerActionsInGlobeViewEnabled.SettingChanged += (_, _) => PlayerActionsInGlobeView.Enable(PlayerActionsInGlobeViewEnabled.Value);
    }

    public static void Start()
    {
        PlayerActionsInGlobeView.Enable(PlayerActionsInGlobeViewEnabled.Value);
    }

    public static void Uninit()
    {
        PlayerActionsInGlobeView.Enable(false);
    }

    public class PlayerActionsInGlobeView : PatchImpl<PlayerActionsInGlobeView>
    {
        // Harmony transpiler: VFInput_UpdateGameStates_Transpiler
        // Target: VFInput.UpdateGameStates
        // Fallback: TranspilerGuard returns original instructions when the globe-view checks are not found.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput.UpdateGameStates))]
        private static IEnumerable<CodeInstruction> VFInput_UpdateGameStates_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(UIGame), nameof(UIGame.viewMode))),
                new CodeMatch(instruction => instruction.LoadsConstant((int)EViewMode.Globe)),
                new CodeMatch(instruction => instruction.Branches(out _))
            );
            if (matcher.IsInvalid)
                return matcher.Finish(instructions, UXAssist.Logger, nameof(VFInput_UpdateGameStates_Transpiler));
            matcher.Repeat(codeMatcher =>
            {
                var labels = codeMatcher.Labels;
                codeMatcher.Labels = [];
                codeMatcher.RemoveInstructions(3).Labels.AddRange(labels);
            });
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: PlayerController_GetInput_Transpiler
        // Target: PlayerController.GetInput
        // Fallback: TranspilerGuard returns original instructions when the view-mode input limit is not found.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetInput))]
        private static IEnumerable<CodeInstruction> PlayerController_GetInput_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(UIGame), nameof(UIGame.viewMode))),
                new CodeMatch(instruction => instruction.LoadsConstant((int)EViewMode.Globe))
            );
            if (matcher.IsInvalid)
                return matcher.Finish(instructions, UXAssist.Logger, nameof(PlayerController_GetInput_Transpiler));
            matcher.Advance(1).Set(OpCodes.Ldc_I4, (int)EViewMode.Starmap);
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: PlayerAction_Rts_GameTick_Transpiler
        // Target: PlayerAction_Rts.GameTick
        // Fallback: TranspilerGuard returns original instructions when the camera-conflict getters are not found.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerAction_Rts), nameof(PlayerAction_Rts.GameTick))]
        private static IEnumerable<CodeInstruction> PlayerAction_Rts_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var moveConflict = AccessTools.PropertyGetter(typeof(VFInput), nameof(VFInput.rtsMoveCameraConflict));
            var mineConflict = AccessTools.PropertyGetter(typeof(VFInput), nameof(VFInput.rtsMineCameraConflict));
            matcher.MatchForward(false,
                new CodeMatch(instruction => instruction.Calls(moveConflict) || instruction.Calls(mineConflict))
            );
            if (matcher.IsInvalid)
                return matcher.Finish(instructions, UXAssist.Logger, nameof(PlayerAction_Rts_GameTick_Transpiler));
            matcher.Repeat(codeMatcher =>
            {
                codeMatcher.Advance(1).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UIGame), nameof(UIGame.viewMode))),
                    new CodeInstruction(OpCodes.Ldc_I4, (int)EViewMode.Globe),
                    new CodeInstruction(OpCodes.Ceq),
                    new CodeInstruction(OpCodes.Or)
                );
            });
            return matcher.InstructionEnumeration();
        }
    }
}
