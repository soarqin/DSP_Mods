using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.Bindings;
using UXAssist.Common;

namespace CheatEnabler.Patches;

public static class GamePatch
{
    public static ConfigEntry<bool> DevShortcutsEnabled;
    public static ConfigEntry<bool> AbnormalDisablerEnabled;
    public static ConfigEntry<bool> UnlockTechEnabled;

    public static void Init()
    {
        DevShortcutsEnabled.SettingChanged += (_, _) => DevShortcuts.Enable(DevShortcutsEnabled.Value);
        AbnormalDisablerEnabled.SettingChanged += (_, _) => AbnormalDisabler.Enable(AbnormalDisablerEnabled.Value);
        UnlockTechEnabled.SettingChanged += (_, _) => UnlockTech.Enable(UnlockTechEnabled.Value);
    }

    public static void Start()
    {
        DevShortcuts.Enable(DevShortcutsEnabled.Value);
        AbnormalDisabler.Enable(AbnormalDisablerEnabled.Value);
        UnlockTech.Enable(UnlockTechEnabled.Value);
    }

    public static void Uninit()
    {
        UnlockTech.Enable(false);
        AbnormalDisabler.Enable(false);
        DevShortcuts.Enable(false);
    }

    public class AbnormalDisabler : PatchImpl<AbnormalDisabler>
    {
        private static Dictionary<int, AbnormalityDeterminator> _savedDeterminators;

        protected override void OnEnable()
        {
            if (_savedDeterminators == null) return;
            var abnormalLogic = GameMain.gameScenario.abnormalityLogic;
            foreach (var p in _savedDeterminators)
            {
                p.Value.OnUnregEvent();
            }

            abnormalLogic.determinators = new Dictionary<int, AbnormalityDeterminator>();
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
            __instance.determinators = new Dictionary<int, AbnormalityDeterminator>();
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
            if (_test != null) _test.active = true;
        }

        protected override void OnDisable()
        {
            if (_test != null) _test.active = false;
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
            _test.active = DevShortcutsEnabled.Value;
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
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Input), nameof(UnityEngine.Input.GetKeyDown), [typeof(UnityEngine.KeyCode)]))
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

    public class UnlockTech : PatchImpl<UnlockTech>
    {
        private static void UnlockTechRecursive(GameHistoryData history, [NotNull] TechProto techProto, int maxLevel = 10000)
        {
            var techStates = history.techStates;
            var techID = techProto.ID;
            if (techStates == null || !techStates.TryGetValue(techID, out var value))
            {
                return;
            }

            if (value.unlocked)
            {
                return;
            }

            var maxLvl = Math.Min(maxLevel < 0 ? value.curLevel - maxLevel - 1 : maxLevel, value.maxLevel);

            foreach (var preid in techProto.PreTechs)
            {
                var preProto = LDB.techs.Select(preid);
                if (preProto != null)
                    UnlockTechRecursive(history, preProto, techProto.PreTechsMax ? 10000 : -1);
            }

            foreach (var preid in techProto.PreTechsImplicit)
            {
                var preProto = LDB.techs.Select(preid);
                if (preProto != null)
                    UnlockTechRecursive(history, preProto, techProto.PreTechsMax ? 10000 : -1);
            }

            if (value.curLevel < techProto.Level) value.curLevel = techProto.Level;
            while (value.curLevel <= maxLvl)
            {
                if (value.curLevel == 0)
                {
                    foreach (var recipe in techProto.UnlockRecipes)
                    {
                        history.UnlockRecipe(recipe);
                    }
                }

                for (var j = 0; j < techProto.UnlockFunctions.Length; j++)
                {
                    history.UnlockTechFunction(techProto.UnlockFunctions[j], techProto.UnlockValues[j], value.curLevel);
                }

                for (var k = 0; k < techProto.AddItems.Length; k++)
                {
                    history.GainTechAwards(techProto.AddItems[k], techProto.AddItemCounts[k]);
                }

                value.curLevel++;
            }

            value.unlocked = maxLvl >= value.maxLevel;
            value.curLevel = value.unlocked ? maxLvl : maxLvl + 1;
            value.hashNeeded = techProto.GetHashNeeded(value.curLevel);
            value.hashUploaded = value.unlocked ? value.hashNeeded : 0;
            techStates[techID] = value;
            history.RegFeatureKey(1000100);
            history.NotifyTechUnlock(techID, maxLvl, true);
        }

        private static void OnClickTech(UITechNode node)
        {
            var history = GameMain.history;
            if (VFInput.shift)
            {
                if (VFInput.alt) return;
                if (VFInput.control)
                    UnlockTechRecursive(history, node.techProto, -100);
                else
                    UnlockTechRecursive(history, node.techProto, -1);
            }
            else
            {
                if (VFInput.control)
                {
                    if (!VFInput.alt)
                        UnlockTechRecursive(history, node.techProto, -10);
                    else
                        return;
                }
                else if (VFInput.alt)
                {
                    UnlockTechRecursive(history, node.techProto);
                }
                else
                {
                    return;
                }
            }

            history.VerifyTechQueue();
            if (history.currentTech != history.techQueue[0])
            {
                history.currentTech = history.techQueue[0];
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UITechNode), nameof(UITechNode.OnPointerDown))]
        private static IEnumerable<CodeInstruction> UITechNode_OnPointerDown_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UITechNode), nameof(UITechNode.tree))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(UITechTree), "get_selected"))
            );
            var labels = matcher.Labels;
            matcher.Labels = null;
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnlockTech), nameof(UnlockTech.OnClickTech)))
            );
            return matcher.InstructionEnumeration();
        }
    }
}