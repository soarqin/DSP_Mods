using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;
public static class DevShortcuts
{
    public static ConfigEntry<bool> Enabled;
    private static Harmony _patch;
    private static PlayerAction_Test _test;

    public static void Init()
    {
        _patch ??= Harmony.CreateAndPatchAll(typeof(DevShortcuts));
        Enabled.SettingChanged += (_, _) =>
        {
            if (_test != null) _test.active = Enabled.Value;
        };
    }

    public static void Uninit()
    {
        _patch?.UnpatchSelf();
        _patch = null;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.Init))]
    private static void PlayerController_Init_Postfix(PlayerController __instance)
    {
        var cnt = __instance.actions.Length;
        var newActions = new PlayerAction[cnt + 1];
        for (var i = 0; i < cnt; i++)
        {
            newActions[i] = __instance.actions[i];
        }

        _test = new PlayerAction_Test();
        _test.Init(__instance.player);
        _test.active = Enabled.Value;
        newActions[cnt] = _test;
        __instance.actions = newActions;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAction_Test), nameof(PlayerAction_Test.GameTick))]
    private static void PlayerAction_Test_GameTick_Postfix(PlayerAction_Test __instance)
    {
        __instance.Update();
    }

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
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GameMain), "get_sandboxToolsEnabled")),
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
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Input), nameof(UnityEngine.Input.GetKeyDown), new[] { typeof(UnityEngine.KeyCode) }))
        );
        labels = matcher.Labels;
        matcher.Labels = null;
        matcher.RemoveInstructions(2);
        matcher.Opcode = OpCodes.Br;
        matcher.Labels = labels;
        return matcher.InstructionEnumeration();
    }
    
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
