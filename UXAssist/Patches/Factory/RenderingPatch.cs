using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches.Factory;

internal static class RenderingPatch
{
    public static void Enable(bool enable)
    {
        DoNotRenderEntities.Enable(enable);
        NightLight.Enable(enable);
    }

    internal class DoNotRenderEntities : PatchImpl<DoNotRenderEntities>
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ObjectRenderer), nameof(ObjectRenderer.Render))]
        [HarmonyPatch(typeof(DynamicRenderer), nameof(DynamicRenderer.Render))]
        private static bool ObjectRenderer_Render_Prefix()
        {
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabRenderer), nameof(LabRenderer.Render))]
        private static bool LabRenderer_Render_Prefix()
        {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GPUInstancingManager), nameof(GPUInstancingManager.Render))]
        private static void FactoryModel_DrawInstancedBatches_Postfix(GPUInstancingManager __instance)
        {
            __instance.renderEntity = true;
        }
        // Harmony transpiler: RaycastLogic_GameTick_Transpiler
        // Target: RaycastLogic.GameTick
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(RaycastLogic), nameof(RaycastLogic.GameTick))]
        private static IEnumerable<CodeInstruction> RaycastLogic_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Brtrue));
            var branch1 = (Label)matcher.Advance(1).Operand;
            var branch2 = generator.DefineLabel();
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Call),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldarg_0)
            ).Advance(8).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, 45),
                new CodeInstruction(OpCodes.Brtrue, branch2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, 33),
                new CodeInstruction(OpCodes.Ldloc_S, 35),
                Transpilers.EmitDelegate((RaycastLogic l, ColliderData[] colliderPool, int j) => l.factory.entityPool[colliderPool[j].objId].inserterId > 0),
                new CodeInstruction(OpCodes.Brfalse, branch1)
            );
            matcher.Labels.Add(branch2);
            return matcher.InstructionEnumeration();
        }
    }

    internal class NightLight : PatchImpl<NightLight>
    {
        private static bool _nightlightInitialized;
        private static bool _mechaOnEarth;
        private static AnimationState _sail;
        private static Light _sunlight;

        protected override void OnEnable()
        {
            GameLogicProc.OnGameEnd += OnGameEnd;
        }

        protected override void OnDisable()
        {
            GameLogicProc.OnGameEnd -= OnGameEnd;
            if (_sunlight)
            {
                _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
                _sunlight = null;
            }
            _sail = null;
            _mechaOnEarth = false;
            _nightlightInitialized = false;
        }

        private static void OnGameEnd()
        {
            if (_sunlight)
            {
                _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
                _sunlight = null;
            }
            _sail = null;
            _mechaOnEarth = false;
            _nightlightInitialized = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateSunlightAngle()
        {
            if (!_sunlight) return;
            _sunlight.transform.rotation =
                Quaternion.LookRotation(-GameMain.mainPlayer.transform.up + GameMain.mainPlayer.transform.forward * FactoryPatch.NightLightAngleX.Value / 10f +
                                        GameMain.mainPlayer.transform.right * FactoryPatch.NightLightAngleY.Value / 10f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.LateUpdate))]
        public static void GameMain_LateUpdate_Postfix(GameMain __instance)
        {
            if (__instance.isMenuDemo || !__instance._running) return;

            if (!_nightlightInitialized)
            {
                if (!GameMain.mainPlayer.controller.model.gameObject.activeInHierarchy) return;
                if (_sail == null) _sail = GameMain.mainPlayer.animator.sails[GameMain.mainPlayer.animator.sailAnimIndex];
                _nightlightInitialized = true;
            }

            var sailing = _sail && _sail.enabled;
            if (_mechaOnEarth)
            {
                if (!sailing)
                {
                    UpdateSunlightAngle();
                    return;
                }
                _mechaOnEarth = false;
                if (!_sunlight) return;
                _sunlight.transform.localEulerAngles = new Vector3(0f, 180f);
                _sunlight = null;
                return;
            }

            if (sailing) return;
            _mechaOnEarth = true;
            if (_sunlight == null)
            {
                _sunlight = GameMain.universeSimulator?.LocalStarSimulator()?.sunLight;
            }
        }
        // Harmony transpiler: StarSimulator_LateUpdate_Transpiler
        // Target: StarSimulator.LateUpdate
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StarSimulator), nameof(StarSimulator.LateUpdate))]
        private static IEnumerable<CodeInstruction> StarSimulator_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform)))
            ).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(NightLight), nameof(_mechaOnEarth))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.mainPlayer))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.transform))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.up))),
                new CodeInstruction(OpCodes.Stloc_0),
                new CodeInstruction(OpCodes.Br_S, label2)
            );
            matcher.Labels.Add(label1);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_0)
            ).Advance(1).Labels.Add(label2);
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: PlanetSimulator_LateRefresh_Transpiler
        // Target: PlanetSimulator.LateRefresh
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetSimulator), nameof(PlanetSimulator.LateRefresh))]
        private static IEnumerable<CodeInstruction> PlanetSimulator_LateRefresh_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // var vec = (NightlightEnabled ? GameMain.mainPlayer.transform.up : (Quaternion.Inverse(localPlanet.runtimeRotation) * (__instance.planetData.star.uPosition - __instance.planetData.uPosition).normalized));
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_1)
            ).Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(NightLight), nameof(_mechaOnEarth))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.mainPlayer))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.transform))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.up))),
                new CodeInstruction(OpCodes.Stloc_2),
                new CodeInstruction(OpCodes.Br_S, label2)
            );
            matcher.Labels.Add(label1);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryModel), nameof(FactoryModel.whiteMode0)))
            ).Labels.Add(label2);
            return matcher.InstructionEnumeration();
        }
    }
}
