using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.Bindings;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace CheatEnabler.Patches;

[ModFeature("Game")]
public static class GamePatch
{
    public static ConfigEntry<bool> DevShortcutsEnabled;
    public static ConfigEntry<bool> AbnormalDisablerEnabled;

    public static void Init()
    {
        DevShortcutsEnabled.SettingChanged += (_, _) => DevShortcuts.Enable(DevShortcutsEnabled.Value);
        AbnormalDisablerEnabled.SettingChanged += (_, _) => AbnormalDisabler.Enable(AbnormalDisablerEnabled.Value);
        GameLogicProc.OnGameEnd += ResetState;
    }

    public static void Start()
    {
        DevShortcuts.Enable(DevShortcutsEnabled.Value);
        AbnormalDisabler.Enable(AbnormalDisablerEnabled.Value);
    }

    public static void Uninit()
    {
        GameLogicProc.OnGameEnd -= ResetState;
        AbnormalDisabler.Enable(false);
        DevShortcuts.Enable(false);
    }

    public static void OnInputUpdate()
    {
        if (!DevShortcutsEnabled.Value || DSPGame.IsMenuDemo || GameMain.isPaused || !GameMain.isRunning) return;
        DevShortcuts.OnInputUpdate();
    }

    private static void ResetState()
    {
        AbnormalDisabler.ResetState();
        DevShortcuts.ResetState();
    }

    public class AbnormalDisabler : PatchImpl<AbnormalDisabler>
    {
        private static Dictionary<int, AbnormalityDeterminator> _savedDeterminators;

        protected override void OnEnable()
        {
            if (_savedDeterminators == null) return;
            var abnormalLogic = GameMain.gameScenario?.abnormalityLogic;
            if (abnormalLogic == null) return;
            foreach (var p in _savedDeterminators)
            {
                p.Value.OnUnregEvent();
            }

            abnormalLogic.determinators = [];
        }

        protected override void OnDisable()
        {
            if (_savedDeterminators == null) return;
            var abnormalLogic = GameMain.gameScenario?.abnormalityLogic;
            if (abnormalLogic?.determinators == null) return;
            abnormalLogic.determinators = _savedDeterminators;
            foreach (var p in _savedDeterminators)
            {
                p.Value.OnRegEvent();
            }
        }

        internal static void ResetState()
        {
            _savedDeterminators = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AbnormalityLogic), nameof(AbnormalityLogic.NotifyBeforeGameSave))]
        [HarmonyPatch(typeof(AbnormalityLogic), nameof(AbnormalityLogic.NotifyOnAssemblerRecipePick))]
        [HarmonyPatch(typeof(AbnormalityLogic), nameof(AbnormalityLogic.NotifyOnGameBegin))]
        [HarmonyPatch(typeof(AbnormalityLogic), nameof(AbnormalityLogic.NotifyOnMechaForgeTaskComplete))]
        [HarmonyPatch(typeof(AbnormalityLogic), nameof(AbnormalityLogic.NotifyOnUnlockTech))]
        [HarmonyPatch(typeof(AbnormalityLogic), nameof(AbnormalityLogic.NotifyOnUseConsole))]
        private static bool DisableAbnormalLogic()
        {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AbnormalityLogic), nameof(AbnormalityLogic.InitDeterminators))]
        private static void DisableAbnormalDeterminators(AbnormalityLogic __instance)
        {
            _savedDeterminators = __instance.determinators;
            if (!AbnormalDisablerEnabled.Value) return;
            __instance.determinators = [];
            foreach (var p in _savedDeterminators)
            {
                p.Value.OnUnregEvent();
            }
        }
    }

    public class DevShortcuts : PatchImpl<DevShortcuts>
    {
        private static PlayerAction_Test _test;

        protected override void OnEnable()
        {
            AttachToController(GameMain.mainPlayer?.controller);
            if (_test != null) _test.active = true;
        }

        protected override void OnDisable()
        {
            if (_test != null) _test.active = false;
        }

        internal static void ResetState()
        {
            _test = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.Init))]
        private static void PlayerController_Init_Postfix(PlayerController __instance)
        {
            AttachToController(__instance);
        }

        private static void AttachToController(PlayerController controller)
        {
            if (controller?.actions == null) return;
            foreach (var action in controller.actions)
            {
                if (action is not PlayerAction_Test test) continue;
                _test = test;
                _test.active = DevShortcutsEnabled.Value;
                return;
            }
            var cnt = controller.actions.Length;
            var newActions = new PlayerAction[cnt + 1];
            for (var i = 0; i < cnt; i++)
            {
                newActions[i] = controller.actions[i];
            }

            _test = new PlayerAction_Test();
            _test.Init(controller.player);
            _test.active = DevShortcutsEnabled.Value;
            newActions[cnt] = _test;
            controller.actions = newActions;
        }

        internal static void OnInputUpdate()
        {
            if (GetHarmony() != null && _test?.active == true) _test.Update();
        }
        // Harmony transpiler: PlayerAction_Test_Update_Transpiler
        // Target: PlayerAction_Test.Update
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerAction_Test), nameof(PlayerAction_Test.Update))]
        private static IEnumerable<CodeInstruction> PlayerAction_Test_Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.End().MatchBack(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerAction_Test), nameof(PlayerAction_Test.active)))
            );
            var pos = matcher.Pos;
            /* Remove Shift+F4 part of the method */
            matcher.Start().RemoveInstructions(pos).MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.sandboxToolsEnabled))),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Ceq)
            );
            var labels = matcher.Labels;
            matcher.SetInstructionAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(labels)
            ).RemoveInstructions(2);
            /* Remove Ctrl+A */
            matcher.Start().MatchForward(false,
                new CodeMatch(instr => (instr.opcode == OpCodes.Ldc_I4_S || instr.opcode == OpCodes.Ldc_I4) && instr.OperandIs(0x61)),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Input), nameof(UnityEngine.Input.GetKeyDown), [typeof(UnityEngine.KeyCode)]))
            );
            labels = matcher.Labels;
            matcher.Labels = null;
            matcher.RemoveInstructions(2);
            matcher.Opcode = OpCodes.Br;
            matcher.Labels = labels;
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: GameCamera_Logic_Transpiler
        // Target: GameCamera.FrameLogic
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.FrameLogic))]
        private static IEnumerable<CodeInstruction> GameCamera_Logic_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GameCamera), nameof(GameCamera.finalPoser))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(CameraPoser), nameof(CameraPoser.cameraPose)))
            );
            var labels = matcher.Labels;
            matcher.Labels = null;
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                Transpilers.EmitDelegate((GameCamera camera) =>
                {
                    if (PlayerAction_Test.lockCam)
                    {
                        camera.finalPoser.cameraPose = PlayerAction_Test.camPose;
                    }
                })
            );
            return matcher.InstructionEnumeration();
        }
    }

}
