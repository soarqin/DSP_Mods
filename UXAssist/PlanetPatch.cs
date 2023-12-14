using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace UXAssist;
public static class PlanetPatch
{
    public static ConfigEntry<bool> PlayerActionsInGlobeViewEnabled;

    public static void Init()
    {
        PlayerActionsInGlobeViewEnabled.SettingChanged += (_, _) => PlayerActionsInGlobeView.Enable(PlayerActionsInGlobeViewEnabled.Value);
        PlayerActionsInGlobeView.Enable(PlayerActionsInGlobeViewEnabled.Value);
    }

    public static void Uninit()
    {
        PlayerActionsInGlobeView.Enable(false);
    }

    public static class PlayerActionsInGlobeView
    {
        private static Harmony _patch;
        
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(PlayerActionsInGlobeView));
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput.UpdateGameStates))]
        private static IEnumerable<CodeInstruction> VFInput_UpdateGameStates_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* remove UIGame.viewMode != EViewMode.Globe in two places:
             * so search for:
             *   ldsfld bool VFInput::viewMode
             *   ldc.i4.3
             */
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(UIGame), nameof(UIGame.viewMode))),
                new CodeMatch(OpCodes.Ldc_I4_3)
            );
            matcher.Repeat(codeMatcher =>
            {
                var labels = codeMatcher.Labels;
                codeMatcher.Labels = [];
                codeMatcher.RemoveInstructions(3).Labels.AddRange(labels);
            });
            return matcher.InstructionEnumeration();
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetInput))]
        private static IEnumerable<CodeInstruction> PlayerController_GetInput_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            // replace `UIGame.viewMode >= EViewMode.Globe` with `UIGame.viewMode >= EViewMode.Starmap`
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(UIGame), nameof(UIGame.viewMode))),
                new CodeMatch(OpCodes.Ldc_I4_3)
            ).Advance(1).Opcode = OpCodes.Ldc_I4_4;
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerAction_Rts), nameof(PlayerAction_Rts.GameTick))]
        private static IEnumerable<CodeInstruction> PlayerAction_Rts_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var local1 = generator.DeclareLocal(typeof(bool));
            // var local1 = UIGame.viewMode == 3;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(VFInput), nameof(VFInput.rtsMoveCameraConflict))),
                new CodeMatch(OpCodes.Stloc_1)
            );
            var labels = matcher.Labels;
            matcher.Labels = [];
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UIGame), nameof(UIGame.viewMode))).WithLabels(labels),
                new CodeInstruction(OpCodes.Ldc_I4_3),
                new CodeInstruction(OpCodes.Ceq),
                new CodeInstruction(OpCodes.Stloc, local1)
            );
            // Add extra condition:
            //   VFInput.rtsMoveCameraConflict / VFInput.rtsMineCameraConflict `|| local1` 
            matcher.MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Ldloc_1 || instr.opcode == OpCodes.Ldloc_2)
            );
            matcher.Repeat(codeMatcher =>
            {
                codeMatcher.Advance(1).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc, local1),
                    new CodeInstruction(OpCodes.Or)
                );
            });
            return matcher.InstructionEnumeration();
        }
    }
}