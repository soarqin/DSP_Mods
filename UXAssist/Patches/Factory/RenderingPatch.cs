using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.Patching;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches.Factory;

internal static class RenderingPatch
{
    internal class DoNotRenderEntities : PatchImpl<DoNotRenderEntities>
    {
        public static bool HideSorters;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ObjectRenderer), nameof(ObjectRenderer.Render))]
        [HarmonyPatch(typeof(DynamicRenderer), nameof(DynamicRenderer.Render))]
        private static bool ObjectRenderer_Render_Prefix()
        {
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InserterRenderer), nameof(InserterRenderer.Render))]
        private static bool InserterRenderer_Render_Prefix()
        {
            return !HideSorters;
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
        // Fallback: TranspilerGuard returns original instructions when the collider skip or entity lookup is not found.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(RaycastLogic), nameof(RaycastLogic.GameTick))]
        private static IEnumerable<CodeInstruction> RaycastLogic_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Phys), nameof(Phys.RayCastOBB))),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S));
            if (matcher.IsInvalid)
                return matcher.Finish(instructions, UXAssist.Logger, nameof(RaycastLogic_GameTick_Transpiler));
            var skipCollider = (Label)matcher.InstructionAt(1).operand;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.entityPool))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldelema, typeof(EntityData)),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EntityData), nameof(EntityData.modelIndex))));
            if (matcher.IsInvalid)
                return matcher.Finish(instructions, UXAssist.Logger, nameof(RaycastLogic_GameTick_Transpiler));
            var visibleEntity = generator.DefineLabel();
            matcher.Advance(3).InsertAndAdvance(
                new CodeInstruction(OpCodes.Dup).MoveLabelsFrom(matcher.Instruction),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DoNotRenderEntities), nameof(IsVisibleEntity))),
                new CodeInstruction(OpCodes.Brtrue, visibleEntity),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Br, skipCollider)
            );
            matcher.Labels.Add(visibleEntity);
            return matcher.InstructionEnumeration();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVisibleEntity(ref EntityData entity)
        {
            return entity.beltId > 0 || !HideSorters && entity.inserterId > 0;
        }
    }

    internal class NightLight : PatchImpl<NightLight>
    {
        private static Light _sunlight;
        private static Quaternion _sunlightLocalRotation;
        private static PlanetSimulator _planetSimulator;
        private static int _sunDirectionId;
        private static int _globalSunDirectionId;

        protected override void OnEnable()
        {
            _sunDirectionId = Shader.PropertyToID("_SunDir");
            _globalSunDirectionId = Shader.PropertyToID("_Global_SunDir");
            GameLogicProc.OnGameEnd += RestoreSunlight;
        }

        protected override void OnDisable()
        {
            GameLogicProc.OnGameEnd -= RestoreSunlight;
            RestoreSunlight();
        }

        private static void RestoreSunlight()
        {
            var sunlight = _sunlight;
            if (sunlight)
                sunlight.transform.localRotation = _sunlightLocalRotation;
            var planetSimulator = _planetSimulator;
            _sunlight = null;
            _planetSimulator = null;

            if ((!sunlight && !planetSimulator) || !GameMain.instance || !GameMain.instance._running || GameMain.instance.isMenuDemo || FactoryModel.whiteMode0) return;
            var starSimulator = GameMain.universeSimulator?.LocalStarSimulator();
            if (starSimulator)
                Shader.SetGlobalVector(_globalSunDirectionId, starSimulator.transform.forward);
            if (planetSimulator && planetSimulator.isActiveAndEnabled && planetSimulator.planetData != null)
            {
                var rotation = GameMain.localPlanet?.runtimeRotation ?? Quaternion.identity;
                var planet = planetSimulator.planetData;
                Vector3 direction = Quaternion.Inverse(rotation) * (planet.star.uPosition - planet.uPosition).normalized;
                ApplyPlanetSunlightDirection(planetSimulator, direction);
            }
        }

        internal static Vector3 CalculateSunlightDirection(Vector3 surfaceUp, Vector3 cameraForward, Vector3 cameraUp, float angleX, float angleY)
        {
            surfaceUp.Normalize();
            // Screen-right stays stable when the native globe camera's offset crosses its viewing axis.
            var right = Vector3.ProjectOnPlane(Vector3.Cross(cameraUp, cameraForward), surfaceUp);
            if (right.sqrMagnitude < 0.000001f)
                right = Vector3.Cross(-cameraForward, surfaceUp);
            right.Normalize();
            var towardCamera = Vector3.Cross(surfaceUp, right);
            return (surfaceUp - towardCamera * (angleX / 10f) - right * (angleY / 10f)).normalized;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.FrameLogic))]
        private static void UpdateLighting()
        {
            var camera = GameCamera.main;
            if (!camera) return;
            var game = GameMain.instance;
            var player = GameMain.mainPlayer;
            var planet = GameMain.localPlanet;
            if (!game || !game._running || game.isMenuDemo || player == null || planet == null ||
                player.movementState == EMovementState.Sail || FactoryModel.whiteMode0)
            {
                RestoreSunlight();
                return;
            }

            var universe = GameMain.universeSimulator;
            var sunlight = universe?.LocalStarSimulator()?.sunLight;
            var planetSimulator = universe?.FindPlanetSimulator(planet);
            if (!sunlight || !planetSimulator)
            {
                RestoreSunlight();
                return;
            }

            var cameraTransform = camera.transform;
            var target = GameCamera.generalTarget;
            var surfaceUp = GameCamera.instance && GameCamera.instance.isPlanetMode
                ? cameraTransform.position
                : target ? target.position : player.position;
            if (surfaceUp.sqrMagnitude < 0.000001f)
            {
                RestoreSunlight();
                return;
            }

            if (_sunlight != sunlight || _planetSimulator != planetSimulator)
            {
                RestoreSunlight();
                _sunlight = sunlight;
                _sunlightLocalRotation = sunlight.transform.localRotation;
                _planetSimulator = planetSimulator;
            }

            var direction = CalculateSunlightDirection(surfaceUp, cameraTransform.forward, cameraTransform.up,
                FactoryPatch.NightLightAngleX.Value, FactoryPatch.NightLightAngleY.Value);
            // Native camera updates run after the star and planet callbacks.
            sunlight.transform.rotation = Quaternion.FromToRotation(Vector3.forward, -direction);
            Shader.SetGlobalVector(_globalSunDirectionId, direction);
            ApplyPlanetSunlightDirection(planetSimulator, direction);
        }

        private static void ApplyPlanetSunlightDirection(PlanetSimulator planetSimulator, Vector3 direction)
        {
            if (planetSimulator.surfaceRenderer != null && planetSimulator.surfaceRenderer.Length > 0 && planetSimulator.surfaceRenderer[0])
                SetSunlightDirection(planetSimulator.surfaceRenderer[0].sharedMaterial, direction);
            SetSunlightDirection(planetSimulator.reformMat0, direction);
            SetSunlightDirection(planetSimulator.reformMat1, direction);
            SetSunlightDirection(planetSimulator.atmoMat, direction);
            SetSunlightDirection(planetSimulator.atmoMatLate, direction);
        }

        private static void SetSunlightDirection(Material material, Vector3 direction)
        {
            if (material) material.SetVector(_sunDirectionId, direction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetNativeGlobalSunDirection(string _, Vector4 direction, StarSimulator simulator)
        {
            if (_sunlight && simulator.sunLight == _sunlight && !FactoryModel.whiteMode0) return;
            Shader.SetGlobalVector(_globalSunDirectionId, direction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetNativePlanetSunDirection(Material material, string _, Vector4 direction, PlanetSimulator simulator)
        {
            if (simulator == _planetSimulator && simulator.planetData == GameMain.localPlanet && !FactoryModel.whiteMode0 && _sunlight) return;
            material.SetVector(_sunDirectionId, direction);
        }

        // Harmony transpiler: DeferNativeSunDirectionWrites
        // Targets: StarSimulator.LateUpdate and PlanetSimulator.LateRefresh sun-direction setters.
        // Fallback: TranspilerGuard returns original instructions when a sun-direction setter is not found.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StarSimulator), nameof(StarSimulator.LateUpdate))]
        [HarmonyPatch(typeof(PlanetSimulator), nameof(PlanetSimulator.LateRefresh))]
        private static IEnumerable<CodeInstruction> DeferNativeSunDirectionWrites(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var isPlanet = original.DeclaringType == typeof(PlanetSimulator);
            var property = isPlanet ? "_SunDir" : "_Global_SunDir";
            var setter = isPlanet
                ? AccessTools.Method(typeof(Material), nameof(Material.SetVector), [typeof(string), typeof(Vector4)])
                : AccessTools.Method(typeof(Shader), nameof(Shader.SetGlobalVector), [typeof(string), typeof(Vector4)]);
            var replacement = AccessTools.Method(typeof(NightLight), isPlanet ? nameof(SetNativePlanetSunDirection) : nameof(SetNativeGlobalSunDirection));
            var matcher = new CodeMatcher(instructions);
            var matched = false;
            while (true)
            {
                matcher.MatchForward(false, new CodeMatch(OpCodes.Ldstr, property));
                if (matcher.IsInvalid) break;
                matcher.Advance(1).MatchForward(false, new CodeMatch(instruction => instruction.Calls(setter) || instruction.opcode == OpCodes.Ldstr));
                if (matcher.IsInvalid)
                    return matcher.Finish(instructions, UXAssist.Logger, nameof(DeferNativeSunDirectionWrites));
                if (!matcher.Instruction.Calls(setter))
                {
                    UXAssist.Logger?.LogWarning($"Transpiler '{nameof(DeferNativeSunDirectionWrites)}' found an unexpected sun-direction write; returning original instructions.");
                    return instructions;
                }
                matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(matcher.Instruction));
                matcher.Set(OpCodes.Call, replacement).Advance(1);
                matched = true;
            }
            return matched ? matcher.InstructionEnumeration() : matcher.Finish(instructions, UXAssist.Logger, nameof(DeferNativeSunDirectionWrites));
        }
    }
}
