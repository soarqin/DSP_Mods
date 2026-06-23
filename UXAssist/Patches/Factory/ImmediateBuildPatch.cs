using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches.Factory;

internal static class ImmediateBuildPatch
{
    public static void Enable(bool enable)
    {
        AutoConstructButton.Enable(enable);
        QuickBuildAndDismantleLab.Enable(enable);
    }

    internal class AutoConstructButton : PatchImpl<AutoConstructButton>
    {
        private static int _lastPrebuildCount = -1;

        protected override void OnEnable()
        {
            Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
        }

        protected override void OnDisable()
        {
            Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
            _lastPrebuildCount = -1;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetData), nameof(PlanetData.NotifyFactoryLoaded))]
        private static void PlanetData_NotifyFactoryLoaded_Postfix()
        {
            Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
            _lastPrebuildCount = -1;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetData), nameof(PlanetData.UnloadFactory))]
        private static void PlanetData_UnloadFactory_Postfix()
        {
            Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
            _lastPrebuildCount = -1;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerAction_Rts), nameof(PlayerAction_Rts.GameTick))]
        private static void PlayerAction_Rts_GameTick_Postfix(PlayerAction_Rts __instance, long timei)
        {
            if (timei % 60L != 0) return;
            var planet = GameMain.localPlanet;
            if (planet == null || !planet.factoryLoaded) return;
            var factory = planet.factory;
            var prebuildCount = factory.prebuildCount;
            if (_lastPrebuildCount != prebuildCount)
            {
                if (_lastPrebuildCount <= 0 || prebuildCount == 0)
                {
                    Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
                }
                _lastPrebuildCount = prebuildCount;
                Functions.UIFunctions.UpdateConstructCountText(prebuildCount);
            }
            if (prebuildCount <= 0) return;
            if (!FactoryPatch.AutoConstructEnabled.Value) return;
            var player = __instance.player;
            if (prebuildCount <= player.mecha.constructionModule.buildTargetTotalCount) return;
            if (player.orders.orderCount > 0) return;
            if (player.controller.horzVelocity.sqrMagnitude > 0.01f)
            {
                return;
            }
            var prebuilds = factory.prebuildPool;
            var minDist = float.MaxValue;
            var minIndex = 0;
            var playerPos = player.position;
            for (var i = factory.prebuildCursor - 1; i > 0; i--)
            {
                ref var prebuild = ref prebuilds[i];
                if (prebuild.id != i || prebuild.isDestroyed) continue;
                if (prebuild.itemRequired > 0)
                {
                    if (player.package.GetItemCount(prebuild.protoId) < prebuild.itemRequired) continue;
                }
                var dist = (prebuild.pos - playerPos).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    minIndex = i;
                }
            }
            if (minIndex == 0) return;
            var diff = prebuilds[minIndex].pos - playerPos;
            if (diff.sqrMagnitude < 400f) return;
            if (player.movementState == EMovementState.Walk && player.mecha.thrusterLevel >= 1)
            {
                player.controller.actionWalk.SwitchToFly();
                return;
            }
            player.Order(OrderNode.MoveTo(prebuilds[minIndex].pos + diff.normalized * 6f), false);
        }
    }

    internal class QuickBuildAndDismantleLab : PatchImpl<QuickBuildAndDismantleLab>
    {
        private static bool DetermineMoreLabsForDismantle(BuildTool dismantle, int id)
        {
            if (!VFInput._chainReaction) return true;
            var factory = dismantle.factory;
            var proto = dismantle.GetItemProto(id);
            var protoId = proto.ID;
            var prefDesc = proto.prefabDesc;
            if (!prefDesc.isLab && !prefDesc.isTank && (!prefDesc.isStorage || prefDesc.isBattleBase)) return true;
            factory.ReadObjectConn(id, 14, out _, out var nextId, out _);
            /* We keep last lab if selected lab is not the ground one */
            if (nextId > 0)
            {
                while (true)
                {
                    factory.ReadObjectConn(nextId, 14, out _, out var nextNextId, out _);
                    if (nextNextId <= 0) break;
                    var itemProto = dismantle.GetItemProto(nextId);
                    if (itemProto.ID != protoId) break;
                    var desc = itemProto.prefabDesc;
                    var pose = dismantle.GetObjectPose(nextId);
                    var preview = new BuildPreview
                    {
                        item = itemProto,
                        desc = desc,
                        lpos = pose.position,
                        lrot = pose.rotation,
                        lpos2 = pose.position,
                        lrot2 = pose.rotation,
                        objId = nextId,
                        needModel = desc.lodCount > 0 && desc.lodMeshes[0] != null,
                        isConnNode = true
                    };
                    dismantle.buildPreviews.Add(preview);
                    nextId = nextNextId;
                }
            }

            nextId = id;
            while (true)
            {
                factory.ReadObjectConn(nextId, 15, out _, out var nextId2, out _);
                if (nextId2 <= 0)
                {
                    factory.ReadObjectConn(nextId, 13, out _, out nextId2, out _);
                    if (nextId2 <= 0) break;
                }

                nextId = nextId2;
                var itemProto = dismantle.GetItemProto(nextId);
                var desc = itemProto.prefabDesc;
                var pose = dismantle.GetObjectPose(nextId);
                var preview = new BuildPreview
                {
                    item = itemProto,
                    desc = desc,
                    lpos = pose.position,
                    lrot = pose.rotation,
                    lpos2 = pose.position,
                    lrot2 = pose.rotation,
                    objId = nextId,
                    needModel = desc.lodCount > 0 && desc.lodMeshes[0] != null,
                    isConnNode = true
                };
                dismantle.buildPreviews.Add(preview);
            }

            return false;
        }

        private static void BuildLabsToTop(BuildTool_Click click)
        {
            if (!click.multiLevelCovering || !VFInput._chainReaction) return;
            var prefDesc = click.GetPrefabDesc(click.castObjectId);
            if (!prefDesc.isLab && !prefDesc.isTank && (!prefDesc.isStorage || prefDesc.isBattleBase)) return;
            var levelMax = prefDesc.isLab ? GameMain.history.labLevel : GameMain.history.storageLevel;
            var factory = click.factory;
            var currLevel = 2;
            var nid = click.castObjectId;
            do
            {
                factory.ReadObjectConn(nid, 14, out _, out nid, out _);
                if (nid <= 0) break;
                currLevel++;
            } while (true);

            while (currLevel < levelMax)
            {
                click.UpdateRaycast();
                click.DeterminePreviews();
                click.UpdateCollidersForCursor();
                click.UpdateCollidersForGiantBp();
                var model = click.actionBuild.model;
                click.UpdatePreviewModels(model);
                if (!click.CheckBuildConditions())
                {
                    model.ClearAllPreviewsModels();
                    model.EarlyGameTickIgnoreActive();
                    return;
                }

                click.UpdatePreviewModelConditions(model);
                click.UpdateGizmos(model);
                click.CreatePrebuilds();
                currLevel++;
            }
        }
        // Harmony transpiler: BuildTool_Dismantle_DeterminePreviews_Transpiler
        // Target: BuildTool_Dismantle.DeterminePreviews
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildTool_Dismantle_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.desc))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isBattleBase))),
                new CodeMatch(OpCodes.Brfalse)
            ).Advance(-1);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.objId))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuickBuildAndDismantleLab), nameof(DetermineMoreLabsForDismantle))),
                new CodeInstruction(OpCodes.And)
            );
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: BuildTool_Click__OnTick_Transpiler
        // Target: BuildTool_Click._OnTick
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click._OnTick))]
        private static IEnumerable<CodeInstruction> BuildTool_Click__OnTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds)))
            ).Advance(2);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuickBuildAndDismantleLab), nameof(BuildLabsToTop)))
            );
            return matcher.InstructionEnumeration();
        }
    }
}
