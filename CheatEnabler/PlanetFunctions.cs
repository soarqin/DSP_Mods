using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using BepInEx.Configuration;
using HarmonyLib;
using Random = UnityEngine.Random;

namespace CheatEnabler;
public static class PlanetFunctions
{
    public static ConfigEntry<bool> PlayerActionsInGlobeViewEnabled;

    public static void Init()
    {
        PlayerActionsInGlobeViewEnabled.SettingChanged += (_, _) => PlayerActionInGlobeViewValueChanged();
        PlayerActionInGlobeViewValueChanged();
    }

    public static void Uninit()
    {
        PlayerActionInGlobeView.Enable(false);
    }

    private static void PlayerActionInGlobeViewValueChanged()
    {
        PlayerActionInGlobeView.Enable(PlayerActionsInGlobeViewEnabled.Value);
    }

    public static class PlayerActionInGlobeView
    {
        private static Harmony _patch;
        
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(PlayerActionInGlobeView));
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
                codeMatcher.Labels = new List<Label>();
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
            matcher.Labels = new List<Label>();
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
                matcher.Advance(1);
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc, local1),
                    new CodeInstruction(OpCodes.Or)
                );
            });
            return matcher.InstructionEnumeration();
        }
    }
    public static void DismantleAll(bool toBag)
    {
        var player = GameMain.mainPlayer;
        if (player == null) return;
        var planet = GameMain.localPlanet;
        var factory = planet?.factory;
        if (factory == null) return;
        foreach (var etd in factory.entityPool)
        {
            var stationId = etd.stationId;
            if (stationId > 0)
            {
                var sc = GameMain.localPlanet.factory.transport.stationPool[stationId];
                if (toBag)
                {
                    for (var i = 0; i < sc.storage.Length; i++)
                    {
                        var package = player.TryAddItemToPackage(sc.storage[i].itemId, sc.storage[i].count, 0, true, etd.id);
                        UIItemup.Up(sc.storage[i].itemId, package);
                    }
                }
                sc.storage = new StationStore[sc.storage.Length];
                sc.needs = new int[sc.needs.Length];
            }
            if (toBag)
            {
                player.controller.actionBuild.DoDismantleObject(etd.id);
            }
            else
            {
                factory.RemoveEntityWithComponents(etd.id);
            }
        }
    }

    public static void BuryAllVeins(bool bury)
    {
        var planet = GameMain.localPlanet;
        var factory = planet?.factory;
        if (factory == null) return;
        var physics = planet.physics;
        var height = bury ? planet.realRadius - 50f : planet.realRadius + 0.07f;
        var array = factory.veinPool;
        var num = factory.veinCursor;
        for (var m = 1; m < num; m++)
        {
            var pos = array[m].pos;
            var colliderId = array[m].colliderId;
            var colliderData = physics.GetColliderData(colliderId);
            var vector = colliderData.pos.normalized * (height + 0.4f);
            physics.colChunks[colliderId >> 20].colliderPool[colliderId & 0xFFFFF].pos = vector;
            array[m].pos = pos.normalized * height;
            var quaternion = Maths.SphericalRotation(array[m].pos, Random.value * 360f);
            physics.SetPlanetPhysicsColliderDirty();
            GameMain.gpuiManager.AlterModel(array[m].modelIndex, array[m].modelId, m, array[m].pos, quaternion, false);
        }
        GameMain.gpuiManager.SyncAllGPUBuffer();
    }

    public static void RecreatePlanet(bool revertReform)
    {
        var player = GameMain.mainPlayer;
        if (player == null) return;
        var planet = GameMain.localPlanet;
        var factory = planet?.factory;
        if (factory == null) return;
        //planet.data = new PlanetRawData(planet.precision);
        //planet.data.CalcVerts();
        for (var id = factory.entityCursor - 1; id > 0; id--)
        {
            var ed = factory.entityPool[id];
            if (ed.id != id) continue;
            if (ed.colliderId != 0)
            {
                planet.physics.RemoveLinkedColliderData(ed.colliderId);
                planet.physics.NotifyObjectRemove(EObjectType.Entity, ed.id);
            }

            if (ed.modelId != 0)
            {
                GameMain.gpuiManager.RemoveModel(ed.modelIndex, ed.modelId);
            }

            if (ed.mmblockId != 0)
            {
                factory.blockContainer.RemoveMiniBlock(ed.mmblockId);
            }

            if (ed.audioId != 0)
            {
                if (planet.audio != null)
                {
                    planet.audio.RemoveAudioData(ed.audioId);
                }
            }
        }

        var stationPool = factory.transport?.stationPool;
        if (stationPool != null)
        {
            foreach (var sc in stationPool)
            {
                if (sc == null || sc.id <= 0) continue;
                sc.storage = new StationStore[sc.storage.Length];
                sc.needs = new int[sc.needs.Length];
                int protoId = factory.entityPool[sc.entityId].protoId;
                factory.DismantleFinally(player, sc.entityId, ref protoId);
            }
        }

        var gameScenario = GameMain.gameScenario;
        if (gameScenario != null)
        {
            var genPool = factory.powerSystem?.genPool;
            if (genPool != null)
            {
                foreach (var pgc in genPool)
                {
                    if (pgc.id <= 0) continue;
                    int protoId = factory.entityPool[pgc.entityId].protoId;
                    gameScenario.achievementLogic.NotifyBeforeDismantleEntity(planet.id, protoId, pgc.entityId);
                    gameScenario.NotifyOnDismantleEntity(planet.id, protoId, pgc.entityId);
                }
            }
        }

        if (revertReform)
        {
            factory.PlanetReformRevert();
        }

        planet.UnloadFactory();
        var index = factory.index;
        var warningSystem = GameMain.data.warningSystem;
        var warningPool = warningSystem.warningPool;
        for (var i = warningSystem.warningCursor - 1; i > 0; i--)
        {
            if (warningPool[i].id == i && warningPool[i].factoryId == index)
                warningSystem.RemoveWarningData(warningPool[i].id);
        }
        factory.entityCursor = 1;
        factory.entityRecycleCursor = 0;
        factory.entityCapacity = 0;
        factory.SetEntityCapacity(1024);
        factory.prebuildCursor = 1;
        factory.prebuildRecycleCursor = 0;
        factory.prebuildCapacity = 0;
        factory.SetPrebuildCapacity(256);
        factory.cargoContainer = new CargoContainer();
        factory.cargoTraffic = new CargoTraffic(planet);
        factory.blockContainer = new MiniBlockContainer();
        factory.factoryStorage = new FactoryStorage(planet);
        factory.powerSystem = new PowerSystem(planet);
        factory.factorySystem = new FactorySystem(planet);
        factory.transport = new PlanetTransport(GameMain.data, planet);
        factory.transport.Init();
        factory.digitalSystem = new DigitalSystem(planet);
        //GameMain.data.statistics.production.CreateFactoryStat(index);
        planet.LoadFactory();
        while (!planet.factoryLoaded)
        {
            PlanetModelingManager.Update();
            Thread.Sleep(0);
        }
    }
}