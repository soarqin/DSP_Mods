using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches.Factory;

internal static class FactoryBuildPatches
{
    internal class AutoConstructPatch : PatchImpl<AutoConstructPatch>
    {
        private const long AutoConstructTickInterval = 15L;
        private const float ConstructionStopMargin = 1.5f;
        private const float AvoidanceAltitude = 35f;
        private const float AvoidanceClearance = 1.5f;
        private const float StuckDistance = 1.5f;
        private const int StuckTickThreshold = 45;

        private static int _targetPrebuildId;
        private static OrderNode _autoOrder;
        private static Vector3 _lastPosition;
        private static int _stuckTicks;
        private static int _detourCount;
        private static bool _isAvoidingObstacle;
        private static Vector3 _pendingWaypoint;
        private static bool _hasPendingWaypoint;

        protected override void OnEnable()
        {
            GameLogicProc.OnGameEnd += ResetNavigation;
            Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
            // Diagnostic aid for reports of the auto-construct button not showing up:
            // log the visibility predicate inputs once per enable.
            var planet = GameMain.localPlanet;
            var factoryLoaded = planet != null && planet.factoryLoaded;
            UXAssist.Logger.LogInfo(
                $"AutoConstruct button enabled: buttonCreated={Functions.UI.AutoConstructUI.ToggleAutoConstruct != null}, " +
                $"localPlanet={planet != null}, factoryLoaded={factoryLoaded}, " +
                $"prebuildCount={(factoryLoaded ? planet.factory.prebuildCount : 0)}");
        }

        protected override void OnDisable()
        {
            GameLogicProc.OnGameEnd -= ResetNavigation;
            ResetNavigation();
            Functions.UIFunctions.UpdateToggleAutoConstructCheckButtonVisiblility();
        }

        // Button visibility and the pending-construction count text are reconciled periodically
        // in AutoConstructUI.OnUpdate() (independent of Harmony patch state), so this postfix
        // only implements the auto-construct fly-to-target behavior. This also keeps the patch
        // surface small: PlanetData.NotifyFactoryLoaded/UnloadFactory no longer need postfixes.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerAction_Rts), nameof(PlayerAction_Rts.GameTick))]
        private static void PlayerAction_Rts_GameTick_Postfix(PlayerAction_Rts __instance, long timei)
        {
            if (timei % AutoConstructTickInterval != 0) return;
            if (!FactoryPatch.AutoConstructEnabled.Value) return;
            var planet = GameMain.localPlanet;
            if (planet == null || !planet.factoryLoaded) return;
            var factory = planet.factory;
            var prebuildCount = factory.prebuildCount;
            if (prebuildCount <= 0) return;
            var player = __instance.player;
            if (player.planetData != planet)
            {
                ResetNavigation();
                return;
            }

            if (_targetPrebuildId != 0)
            {
                if (!TryGetPrebuild(factory, _targetPrebuildId, out var target) ||
                    target.itemRequired > 0 && player.package.GetItemCount(target.protoId) < target.itemRequired)
                {
                    ResetNavigation();
                }
                else if (player.orders.orderCount > 0 ||
                         player.orders.currentOrder != null && player.orders.currentOrder != _autoOrder)
                {
                    ResetNavigation();
                }
            }

            if (_targetPrebuildId == 0)
            {
                if (prebuildCount <= player.mecha.constructionModule.buildTargetTotalCount ||
                    player.orders.orderCount > 0 || player.orders.currentOrder != null ||
                    player.controller.horzVelocity.sqrMagnitude > 0.01f)
                {
                    return;
                }

                if (!TryFindTarget(factory, player, out _targetPrebuildId)) return;
                _lastPosition = player.position;
                _stuckTicks = 0;
                _detourCount = 0;
                _isAvoidingObstacle = false;
            }

            if (!TryGetPrebuild(factory, _targetPrebuildId, out var prebuild))
            {
                ResetNavigation();
                return;
            }

            var targetPosition = prebuild.pos;
            var constructionStopDistance = Mathf.Max(2f, player.mecha.buildArea - ConstructionStopMargin);
            if ((targetPosition - player.position).sqrMagnitude <= constructionStopDistance * constructionStopDistance)
            {
                player.ClearOrders();
                ResetNavigation();
                return;
            }

            if (player.movementState == EMovementState.Walk && player.mecha.thrusterLevel >= 1)
            {
                player.controller.actionWalk.SwitchToFly();
            }

            if (_autoOrder != null && player.orders.currentOrder == null)
            {
                _autoOrder = null;
                _stuckTicks = 0;
                _lastPosition = player.position;
                if (_hasPendingWaypoint)
                {
                    var pendingWaypoint = _pendingWaypoint;
                    _pendingWaypoint = Vector3.zero;
                    _hasPendingWaypoint = false;
                    IssueRoute(player, pendingWaypoint, true);
                    return;
                }
            }

            if (_autoOrder == null)
            {
                IssueRoute(player, targetPosition, false);
                return;
            }

            if ((player.position - _lastPosition).sqrMagnitude < StuckDistance * StuckDistance)
            {
                _stuckTicks += (int)AutoConstructTickInterval;
            }
            else
            {
                _stuckTicks = 0;
                _lastPosition = player.position;
            }

            var routeTarget = _autoOrder.target;
            var altitude = _isAvoidingObstacle ? AvoidanceAltitude : GetCurrentAltitude(player);
            if (_stuckTicks >= StuckTickThreshold || !IsPathClear(player, routeTarget.normalized, altitude))
            {
                if (_detourCount < 4 && TryFindDetour(player, routeTarget, altitude, out var detour, out var pendingWaypoint))
                {
                    _detourCount++;
                    _isAvoidingObstacle = true;
                    _pendingWaypoint = pendingWaypoint;
                    _hasPendingWaypoint = pendingWaypoint.sqrMagnitude > 0.01f;
                    player.controller.actionFly.targetAltitude = AvoidanceAltitude;
                    IssueRoute(player, detour, true);
                }
                else
                {
                    _detourCount = 0;
                    _isAvoidingObstacle = true;
                    _pendingWaypoint = Vector3.zero;
                    _hasPendingWaypoint = false;
                    player.controller.actionFly.targetAltitude = AvoidanceAltitude;
                    IssueRoute(player, targetPosition, true);
                }

                _stuckTicks = 0;
                _lastPosition = player.position;
            }
        }

        private static bool TryFindTarget(PlanetFactory factory, Player player, out int targetId)
        {
            var prebuilds = factory.prebuildPool;
            var minDistance = float.MaxValue;
            targetId = 0;
            for (var i = factory.prebuildCursor - 1; i > 0; i--)
            {
                ref var prebuild = ref prebuilds[i];
                if (prebuild.id != i || prebuild.isDestroyed) continue;
                if (prebuild.itemRequired > 0 && player.package.GetItemCount(prebuild.protoId) < prebuild.itemRequired) continue;

                var distance = (prebuild.pos - player.position).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetId = i;
                }
            }

            return targetId != 0;
        }

        private static bool TryGetPrebuild(PlanetFactory factory, int id, out PrebuildData prebuild)
        {
            if (id > 0 && id < factory.prebuildPool.Length)
            {
                prebuild = factory.prebuildPool[id];
                return prebuild.id == id && !prebuild.isDestroyed;
            }

            prebuild = default;
            return false;
        }

        private static void IssueRoute(Player player, Vector3 targetPosition, bool keepAltitude)
        {
            var direction = targetPosition - player.position;
            if (direction.sqrMagnitude < 0.01f) return;

            var target = targetPosition + direction.normalized * 6f;
            _autoOrder = OrderNode.MoveTo(target);
            player.Order(_autoOrder, false);
            if (keepAltitude && player.movementState == EMovementState.Fly)
            {
                player.controller.actionFly.targetAltitude = AvoidanceAltitude;
            }
        }

        private static bool TryFindDetour(Player player, Vector3 routeTarget, float altitude, out Vector3 detour, out Vector3 pendingWaypoint)
        {
            detour = default;
            pendingWaypoint = default;
            var planet = player.planetData;
            if (planet == null) return false;

            var origin = GetFlightPoint(player.position.normalized, altitude, planet.realRadius);
            var finalDirection = routeTarget.normalized;
            var path = GetFlightPoint(finalDirection, altitude, planet.realRadius) - origin;
            if (path.sqrMagnitude < 0.01f) return false;
            if (!Physics.SphereCast(origin, AvoidanceClearance, path.normalized, out var hit, path.magnitude, 8704, QueryTriggerInteraction.Collide) ||
                hit.distance + 4f >= path.magnitude)
            {
                return false;
            }

            var obstacleBounds = hit.collider != null ? hit.collider.bounds : new Bounds(hit.point, Vector3.one * 2f);
            var anchor = obstacleBounds.center.normalized;
            var forward = Vector3.ProjectOnPlane(finalDirection, anchor).normalized;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.ProjectOnPlane(player.position, anchor).normalized;
            }

            var side = Vector3.Cross(anchor, forward).normalized;
            if (side.sqrMagnitude < 0.01f) return false;

            var sideExtent = Mathf.Min(GetProjectedExtent(obstacleBounds.extents, side), 48f);
            var forwardExtent = Mathf.Min(GetProjectedExtent(obstacleBounds.extents, forward), 48f);
            var sideOffset = Mathf.Max(6f, sideExtent + AvoidanceClearance + 2f);
            var forwardOffset = Mathf.Max(5f, forwardExtent + AvoidanceClearance + 2f);
            var sideOffsets = new[] { sideOffset, -sideOffset, sideOffset + 8f, -sideOffset - 8f, sideOffset + 16f, -sideOffset - 16f };
            foreach (var lateralOffset in sideOffsets)
            {
                var entry = SurfaceOffset(anchor, side * lateralOffset - forward * forwardOffset, planet.realRadius);
                var exit = SurfaceOffset(anchor, side * lateralOffset + forward * forwardOffset, planet.realRadius);
                if (IsPathClear(player, entry, altitude) &&
                    IsPathClear(entry, exit, altitude, planet.realRadius) &&
                    IsPathClear(exit, finalDirection, altitude, planet.realRadius))
                {
                    detour = entry * planet.realRadius;
                    pendingWaypoint = exit * planet.realRadius;
                    return true;
                }

                var candidate = SurfaceOffset(anchor, side * lateralOffset, planet.realRadius);
                if (!IsPathClear(player, candidate, altitude) ||
                    !IsPathClear(candidate, finalDirection, altitude, planet.realRadius)) continue;

                detour = candidate * planet.realRadius;
                return true;
            }

            return false;
        }

        private static float GetProjectedExtent(Vector3 extents, Vector3 axis)
        {
            return Mathf.Abs(axis.x) * extents.x + Mathf.Abs(axis.y) * extents.y + Mathf.Abs(axis.z) * extents.z;
        }

        private static bool IsPathClear(Player player, Vector3 targetDirection, float altitude)
        {
            var planet = player.planetData;
            return planet != null && IsPathClear(player.position.normalized, targetDirection, altitude, planet.realRadius);
        }

        private static bool IsPathClear(Vector3 startDirection, Vector3 targetDirection, float altitude, float realRadius)
        {
            var origin = GetFlightPoint(startDirection, altitude, realRadius);
            var target = GetFlightPoint(targetDirection, altitude, realRadius);
            var path = target - origin;
            if (path.sqrMagnitude < 0.01f) return true;

            return !Physics.SphereCast(origin, AvoidanceClearance, path.normalized, out var hit, path.magnitude, 8704, QueryTriggerInteraction.Collide) ||
                   hit.distance + 4f >= path.magnitude;
        }

        private static Vector3 SurfaceOffset(Vector3 surfaceDirection, Vector3 tangentOffset, float realRadius)
        {
            var distance = tangentOffset.magnitude;
            if (distance < 0.01f) return surfaceDirection.normalized;

            var tangent = Vector3.ProjectOnPlane(tangentOffset, surfaceDirection).normalized;
            var angle = distance / Mathf.Max(1f, realRadius);
            return (surfaceDirection.normalized * Mathf.Cos(angle) + tangent * Mathf.Sin(angle)).normalized;
        }

        private static Vector3 GetFlightPoint(Vector3 direction, float altitude, float realRadius)
        {
            return direction.normalized * (realRadius + Mathf.Max(2f, altitude));
        }

        private static float GetCurrentAltitude(Player player)
        {
            return Mathf.Clamp(player.position.magnitude - player.planetData.realRadius, 15f, AvoidanceAltitude);
        }

        private static void ResetNavigation()
        {
            _targetPrebuildId = 0;
            _autoOrder = null;
            _lastPosition = Vector3.zero;
            _stuckTicks = 0;
            _detourCount = 0;
            _isAvoidingObstacle = false;
            _pendingWaypoint = Vector3.zero;
            _hasPendingWaypoint = false;
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
