using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches.Factory;

internal static class BuildToolPatch
{
    public static void Enable(bool enable)
    {
        BuildGizmoPatch.Enable(enable);
        OffGridBuilding.Enable(enable);
        TreatStackingAsSingle.Enable(enable);
        DragBuildPowerPoles.Enable(enable);
        TankFastFillInAndTakeOut.Enable(enable);
        PressShiftToTakeWholeBeltItems.Enable(enable);
    }

    private class BuildGizmoPatch : PatchImpl<BuildGizmoPatch>
    {
        // Harmony transpiler: ConnGizmoGraph_Constructor_Transpiler
        // Target: ConnGizmoGraph..ctor
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ConnGizmoGraph), MethodType.Constructor)]
        private static IEnumerable<CodeInstruction> ConnGizmoGraph_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(256))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 2048));
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: ConnGizmoGraph_SetPointCount_Transpiler
        // Target: ConnGizmoGraph.SetPointCount
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ConnGizmoGraph), nameof(ConnGizmoGraph.SetPointCount))]
        private static IEnumerable<CodeInstruction> ConnGizmoGraph_SetPointCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(256))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 2048));
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: BuildTool_Path__OnInit_Transpiler
        // Target: BuildTool_Path._OnInit
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path._OnInit))]
        private static IEnumerable<CodeInstruction> BuildTool_Path__OnInit_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(160))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 2048));
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: BuildTool_Reform_Constructor_Transpiler
        // Target: BuildTool_Reform..ctor
        // Fallback: Checks CodeMatcher.IsInvalid/IsValid and returns original instructions on mismatch.
        [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Reform), MethodType.Constructor)]
        private static IEnumerable<CodeInstruction> BuildTool_Reform_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(100))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 900));
            return matcher.InstructionEnumeration();
        }
    }

    internal class OffGridBuilding : PatchImpl<OffGridBuilding>
    {
        // private const float SteppedRotationDegrees = 15f;

        private static bool _initialized;

        private static void SetupRichTextSupport()
        {
            if (_initialized) return;
            UIGeneralTips.instance.buildCursorTextComp.supportRichText = true;
            UIGeneralTips.instance.entityBriefInfo.entityNameText.supportRichText = true;
            _initialized = true;
        }

        private static void CalculateGridOffset(PlanetData planet, Vector3 pos, out float x, out float y, out float z)
        {
            var npos = pos.normalized;
            var segment = planet.aux.activeGrid?.segment ?? 200;
            var latitudeRadPerGrid = BlueprintUtils.GetLatitudeRadPerGrid(segment);
            var longitudeSegmentCount = BlueprintUtils.GetLongitudeSegmentCount(npos, segment);
            var longitudeRadPerGrid = BlueprintUtils.GetLongitudeRadPerGrid(longitudeSegmentCount, segment);
            var latitudeRad = BlueprintUtils.GetLatitudeRad(npos);
            var longitudeRad = BlueprintUtils.GetLongitudeRad(npos);
            x = longitudeRad / longitudeRadPerGrid;
            y = latitudeRad / latitudeRadPerGrid;
            z = (pos.magnitude - planet.realRadius - 0.2f) / 1.3333333f;
        }

        private static string FormatOffsetFloat(float f)
        {
            return f.ToString("0.0000").TrimEnd('0').TrimEnd('.');
        }

        private static PlanetData _lastPlanet;
        private static Vector3 _lastPos;
        private static string _lastOffsetText;

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        private static void BuildTool_Click_CheckBuildConditions_Postfix(BuildTool __instance)
        {
            var cnt = __instance.buildPreviews.Count;
            if (cnt == 0) return;
            var preview = __instance.buildPreviews[cnt - 1];
            if (preview.desc.isInserter) return;
            var planet = __instance.planet;
            if (_lastPlanet != planet || _lastPos != preview.lpos)
            {
                SetupRichTextSupport();
                CalculateGridOffset(__instance.planet, preview.lpos, out var x, out var y, out var z);
                _lastPlanet = planet;
                _lastPos = preview.lpos;
                _lastOffsetText = z is < 0.001f and > -0.001f
                    ? $"<color=#ffbfbfff>{FormatOffsetFloat(x)}</color>,<color=#bfffbfff>{FormatOffsetFloat(y)}</color>"
                    : $"<color=#ffbfbfff>{FormatOffsetFloat(x)}</color>,<color=#bfffbfff>{FormatOffsetFloat(y)}</color>,<color=#bfbfffff>{FormatOffsetFloat(z)}</color>";
            }

            __instance.actionBuild.model.cursorText = $"({_lastOffsetText})\n" + __instance.actionBuild.model.cursorText;
        }
        // Harmony transpiler: UIEntityBriefInfo__OnUpdate_Transpiler
        // Target: UIEntityBriefInfo._OnUpdate
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIEntityBriefInfo), nameof(UIEntityBriefInfo._OnUpdate))]
        private static IEnumerable<CodeInstruction> UIEntityBriefInfo__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIEntityBriefInfo), nameof(UIEntityBriefInfo.entityNameText))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Text), nameof(Text.preferredWidth)))
            );
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((UIEntityBriefInfo entityBriefInfo) =>
                    {
                        var entity = entityBriefInfo.factory.entityPool[entityBriefInfo.entityId];
                        if (entity.inserterId > 0) return;
                        var planet = entityBriefInfo.factory.planet;
                        if (_lastPlanet != planet || _lastPos != entity.pos)
                        {
                            SetupRichTextSupport();
                            CalculateGridOffset(planet, entity.pos, out var x, out var y, out var z);
                            _lastPlanet = planet;
                            _lastPos = entity.pos;
                            _lastOffsetText = $"<color=#ffbfbfff>{FormatOffsetFloat(x)}</color>,<color=#bfffbfff>{FormatOffsetFloat(y)}</color>,<color=#bfbfffff>{FormatOffsetFloat(z)}</color>";
                        }

                        entityBriefInfo.entityNameText.text += $" ({_lastOffsetText})";
                    }
                )
            );
            return matcher.InstructionEnumeration();
        }

        private static void MatchIgnoreGridAndCheckIfRotatable(CodeMatcher matcher, out Label? ifBlockEntryLabel, out Label? elseBlockEntryLabel)
        {
            Label? thisIfBlockEntryLabel = null;
            Label? thisElseBlockEntryLabel = null;

            matcher.MatchForward(false,
                new CodeMatch(ci => ci.Calls(AccessTools.PropertyGetter(typeof(VFInput), nameof(VFInput._switchGridSnap)))),
                new CodeMatch(ci => ci.Branches(out thisElseBlockEntryLabel)),
                new CodeMatch(ci => ci.IsLdarg()),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(ci => ci.LoadsConstant(EMinerType.Vein)),
                new CodeMatch(ci => ci.Branches(out thisIfBlockEntryLabel)),
                new CodeMatch(ci => ci.IsLdarg()),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldfld)
            );

            ifBlockEntryLabel = thisIfBlockEntryLabel;
            elseBlockEntryLabel = thisElseBlockEntryLabel;
        }
        // Harmony transpiler: AllowOffGridConstruction
        // Target: BuildTool_Click.UpdateRaycast, BuildTool_Click.DeterminePreviews
        // Fallback: Checks CodeMatcher.IsInvalid/IsValid and returns original instructions on mismatch.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.UpdateRaycast))]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.DeterminePreviews))]
        public static IEnumerable<CodeInstruction> AllowOffGridConstruction(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            MatchIgnoreGridAndCheckIfRotatable(matcher, out var entryLabel, out _);

            if (matcher.IsInvalid)
                return instructions;

            matcher.Advance(2);
            matcher.Insert(new CodeInstruction(OpCodes.Br, entryLabel.Value));

            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: PreventDraggingWhenOffGrid
        // Target: BuildTool_Click.DeterminePreviews
        // Fallback: Checks CodeMatcher.IsInvalid/IsValid and returns original instructions on mismatch.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.DeterminePreviews))]
        public static IEnumerable<CodeInstruction> PreventDraggingWhenOffGrid(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            Label? exitLabel = null;

            matcher.MatchForward(false,
                new CodeMatch(ci => ci.Branches(out exitLabel)),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(ci => ci.LoadsConstant(1)),
                new CodeMatch(ci => ci.StoresField(AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.isDragging))))
            );

            if (matcher.IsInvalid)
                return instructions;

            matcher.Advance(1);
            matcher.Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(VFInput), nameof(VFInput._switchGridSnap))),
                new CodeInstruction(OpCodes.Brtrue, exitLabel)
            );

            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: AllowOffGridConstructionForPath
        // Target: BuildTool_Path.UpdateRaycast
        // Fallback: Checks CodeMatcher.IsInvalid/IsValid and returns original instructions on mismatch.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.UpdateRaycast))]
        public static IEnumerable<CodeInstruction> AllowOffGridConstructionForPath(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.actionBuild))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerAction_Build), nameof(PlayerAction_Build.planetAux))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPos))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castTerrain))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetAuxData), nameof(PlanetAuxData.Snap), [typeof(Vector3), typeof(bool)])),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPosSnapped)))
            );

            if (matcher.IsInvalid)
                return matcher.InstructionEnumeration();

            var jmp0 = generator.DefineLabel();
            var jmp1 = generator.DefineLabel();
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryPatch), nameof(FactoryPatch._offgridfForPathsKey))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KeyBindings), nameof(KeyBindings.IsKeyPressing))),
                new CodeInstruction(OpCodes.Brfalse, jmp0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPos))),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.normalized))),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.planet))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlanetData), nameof(PlanetData.realRadius))),
                new CodeInstruction(OpCodes.Ldc_R4, 0.2f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Multiply", [typeof(Vector3), typeof(float)])),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(BuildTool_Path), nameof(BuildTool_Path.castGroundPosSnapped))),
                new CodeInstruction(OpCodes.Br, jmp1)
            ).Labels.Add(jmp0);
            matcher.Advance(10).Labels.Add(jmp1);

            return matcher.InstructionEnumeration();
        }

        /*
        public static IEnumerable<CodeInstruction> PatchToPerformSteppedRotate(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            MatchIgnoreGridAndCheckIfRotatable(matcher, out var ifBlockEntryLabel, out var elseBlockEntryLabel);

            if (matcher.IsInvalid)
                return instructions;

            while (!matcher.Labels.Contains(elseBlockEntryLabel.Value))
                matcher.Advance(1);

            Label? ifBlockExitLabel = null;

            matcher.MatchBack(false, new CodeMatch(ci => ci.Branches(out ifBlockExitLabel)));

            if (matcher.IsInvalid)
                return instructions;

            while (!matcher.Labels.Contains(ifBlockEntryLabel.Value))
                matcher.Advance(-1);

            var instructionToClone = matcher.Instruction.Clone();
            var overwriteWith = CodeInstruction.LoadField(typeof(VFInput), nameof(VFInput.control));

            matcher.SetAndAdvance(overwriteWith.opcode, overwriteWith.operand);
            matcher.Insert(instructionToClone);
            matcher.CreateLabel(out var existingEntryLabel);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Brfalse, existingEntryLabel),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(OffGridBuilding), nameof(RotateStepped)),
                new CodeInstruction(OpCodes.Br, ifBlockExitLabel)
            );

            return matcher.InstructionEnumeration();
        }

        public static void RotateStepped(BuildTool_Click instance)
        {
            if (VFInput._rotate.onDown)
            {
                instance.yaw += SteppedRotationDegrees;
                instance.yaw = Mathf.Repeat(instance.yaw, 360f);
                instance.yaw = Mathf.Round(instance.yaw / SteppedRotationDegrees) * SteppedRotationDegrees;
            }

            if (VFInput._counterRotate.onDown)
            {
                instance.yaw -= SteppedRotationDegrees;
                instance.yaw = Mathf.Repeat(instance.yaw, 360f);
                instance.yaw = Mathf.Round(instance.yaw / SteppedRotationDegrees) * SteppedRotationDegrees;
            }
        }
        */
    }

    internal class TreatStackingAsSingle : PatchImpl<TreatStackingAsSingle>
    {
        // Harmony transpiler: MonitorComponent_InternalUpdate_Transpiler
        // Target: MonitorComponent.InternalUpdate
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MonitorComponent), nameof(MonitorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> MonitorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(MonitorComponent), nameof(MonitorComponent.GetCargoAtIndexByFilter)))
            );
            matcher.Advance(-3);
            var localVar = matcher.Operand;
            matcher.Advance(4).Insert(
                new CodeInstruction(OpCodes.Ldloca, localVar),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Cargo), nameof(Cargo.stack)))
            );
            return matcher.InstructionEnumeration();
        }
    }

    internal class DragBuildPowerPoles : PatchImpl<DragBuildPowerPoles>
    {
        private static readonly List<bool> OldDragBuild = [];
        private static readonly List<Vector2> OldDragBuildDist = [];
        private static readonly int[] PowerPoleIds = [2202, 2212];

        protected override void OnEnable()
        {
            GameLogicProc.OnGameBegin += OnGameBegin;
            GameLogicProc.OnGameEnd += OnGameEnd;
            FixProto();
        }

        protected override void OnDisable()
        {
            UnfixProto();
            GameLogicProc.OnGameEnd -= OnGameEnd;
            GameLogicProc.OnGameBegin -= OnGameBegin;
        }

        public static void AlternatelyChanged()
        {
            UnfixProto();
            FixProto();
        }

        private static bool IsPowerPole(int id)
        {
            return PowerPoleIds.Contains(id);
        }

        private static void FixProto()
        {
            if (DSPGame.IsMenuDemo) return;
            OldDragBuild.Clear();
            OldDragBuildDist.Clear();
            foreach (var id in PowerPoleIds)
            {
                var prefabDesc = LDB.items.Select(id)?.prefabDesc;
                if (prefabDesc == null) return;
                OldDragBuild.Add(prefabDesc.dragBuild);
                OldDragBuildDist.Add(prefabDesc.dragBuildDist);
                prefabDesc.dragBuild = true;
                var distance = prefabDesc.powerConnectDistance - 0.8f;
                prefabDesc.dragBuildDist = new Vector2(distance, distance);
            }
        }

        private static void UnfixProto()
        {
            if (GetHarmony() == null || OldDragBuild.Count < PowerPoleIds.Length || DSPGame.IsMenuDemo) return;
            var i = 0;
            foreach (var id in PowerPoleIds)
            {
                var powerPole = LDB.items.Select(id);
                if (powerPole?.prefabDesc != null)
                {
                    powerPole.prefabDesc.dragBuild = OldDragBuild[i];
                    powerPole.prefabDesc.dragBuildDist = OldDragBuildDist[i];
                }

                i++;
            }

            OldDragBuild.Clear();
            OldDragBuildDist.Clear();
        }

        private static void OnGameBegin()
        {
            _powerPoleProto ??= LDB.items.Select(2201);
            FixProto();
        }

        private static void OnGameEnd()
        {
            UnfixProto();
            _powerPoleProto = null;
        }

        private static int PlanetGridSnapDotsNonAllocNotAligned(PlanetGrid planetGrid, Vector3 begin, Vector3 end, Vector2 interval, float yaw, float planetRadius, float gap, Vector3[] snaps)
        {
            begin = begin.normalized;
            end = end.normalized;
            var finalCount = 1;
            var ignoreGrid = VFInput._switchGridSnap;
            if (ignoreGrid)
                snaps[0] = begin;
            else
                snaps[0] = planetGrid.SnapTo(begin);
            var dot = Vector3.Dot(begin, end);
            if (dot is > 0.999999f or < -0.999999f)
                return 1;
            var distTotal = Mathf.Acos(dot) * planetRadius;

            var intervalAll = interval.x;
            var maxT = 1f - intervalAll * 0.5f / distTotal;
            if (maxT < 0f)
                return 1;
            var maxCount = snaps.Length;
            while (finalCount < maxCount)
            {
                var t = finalCount * intervalAll / distTotal;
                if (ignoreGrid)
                    snaps[finalCount] = Vector3.Slerp(begin, end, t);
                else
                    snaps[finalCount] = planetGrid.SnapTo(Vector3.Slerp(begin, end, t));
                finalCount++;
                if (t > maxT) break;
            }

            return finalCount;
        }

        private static int PlanetAuxDataSnapDotsNonAllocNotAligned(PlanetAuxData aux, Vector3 begin, Vector3 end, Vector2 interval, float height, float yaw, float gap, Vector3[] snaps)
        {
            var num = 0;
            var magnitude = begin.magnitude;
            if (aux.activeGrid != null)
            {
                num = PlanetGridSnapDotsNonAllocNotAligned(aux.activeGrid, begin, end, interval, yaw, aux.planet.realRadius + height, gap, snaps);
                for (var i = 0; i < num; i++)
                {
                    snaps[i] *= magnitude;
                }
            }
            else
            {
                snaps[num++] = aux.Snap(begin, false);
            }

            return num;
        }
        // Harmony transpiler: BuildTool_Click_DeterminePreviews_Transpiler
        // Target: BuildTool_Click.DeterminePreviews
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.DeterminePreviews))]
        private static IEnumerable<CodeInstruction> BuildTool_Click_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetAuxData), nameof(PlanetAuxData.SnapDotsNonAlloc)))
            );
            matcher.Labels.Add(label1);
            matcher.InstructionAt(1).labels.Add(label2);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.control))),
                new CodeInstruction(OpCodes.Brtrue, label1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.handItem))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DragBuildPowerPoles), nameof(IsPowerPole))),
                new CodeInstruction(OpCodes.Brfalse, label1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DragBuildPowerPoles), nameof(PlanetAuxDataSnapDotsNonAllocNotAligned))),
                new CodeInstruction(OpCodes.Br, label2)
            ).Advance(1).MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.handItem))),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.item))),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Click), nameof(BuildTool_Click.handPrefabDesc))),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.desc)))
            );
            var pos = matcher.Pos;
            matcher.MatchBack(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Add)
            );
            var operand = matcher.Operand;
            matcher.Start().Advance(pos);
            matcher.Advance(2).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, operand)
            ).SetInstructionAndAdvance(Transpilers.EmitDelegate((BuildTool_Click click, int i) =>
            {
                if (click.handItem.ID != 2202 || (i & 1) == 0 || !FactoryPatch.DragBuildPowerPolesAlternatelyEnabled.Value)
                    return click.handItem;
                return _powerPoleProto;
            })).Advance(3).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, operand)
            ).SetInstructionAndAdvance(Transpilers.EmitDelegate((BuildTool_Click click, int i) =>
            {
                if (click.handItem.ID != 2202 || (i & 1) == 0 || !FactoryPatch.DragBuildPowerPolesAlternatelyEnabled.Value)
                    return click.handPrefabDesc;
                return _powerPoleProto.prefabDesc;
            }));
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BuildPreview), nameof(BuildPreview.Clone)))
            ).Advance(2).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, operand)
            ).SetInstructionAndAdvance(Transpilers.EmitDelegate((BuildPreview to, BuildPreview from, int i) =>
            {
                if (from.item.ID != 2202 || (i & 1) == 0 || !FactoryPatch.DragBuildPowerPolesAlternatelyEnabled.Value)
                {
                    to.Clone(from);
                    return;
                }
                to.ResetAll();
                to.item = _powerPoleProto;
                to.desc = _powerPoleProto.prefabDesc;
                to.needModel = _powerPoleProto.prefabDesc.lodCount > 0 && _powerPoleProto.prefabDesc.lodMeshes[0] != null;
            }));
            return matcher.InstructionEnumeration();
        }

        private static ItemProto _powerPoleProto;
    }

    internal class TankFastFillInAndTakeOut : PatchImpl<TankFastFillInAndTakeOut>
    {
        private static readonly CodeInstruction[] MultiplierWithCountCheck = [
            new(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryPatch), nameof(FactoryPatch._tankFastFillInAndTakeOutMultiplierRealValue))),
            new(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), [typeof(int), typeof(int)]))
        ];
        private static readonly CodeInstruction GetRealCount = new(OpCodes.Ldsfld, AccessTools.Field(typeof(FactoryPatch), nameof(FactoryPatch._tankFastFillInAndTakeOutMultiplierRealValue)));
        // Harmony transpiler: PlanetFactory_EntityFastFillIn_Transpiler
        // Target: PlanetFactory.EntityFastFillIn
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastFillIn))]
        private static IEnumerable<CodeInstruction> PlanetFactory_EntityFastFillIn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(ci => ci.IsStloc())
            ).Advance(1).RemoveInstruction().InsertAndAdvance(GetRealCount).MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.Branches(out _)),
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(ci => ci.IsStloc())
            ).RemoveInstructions(5).Insert(MultiplierWithCountCheck);
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: PlanetFactory_EntityFastTakeOut_Transpiler
        // Target: PlanetFactory.EntityFastTakeOut
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastTakeOut))]
        private static IEnumerable<CodeInstruction> PlanetFactory_EntityFastTakeOut_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloca || ci.opcode == OpCodes.Ldloca_S)
            ).Advance(1).RemoveInstruction().InsertAndAdvance(GetRealCount).MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.opcode == OpCodes.Bgt || ci.opcode == OpCodes.Bgt_S),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.opcode == OpCodes.Br || ci.opcode == OpCodes.Br_S),
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(ci => ci.IsLdloc())
            ).RemoveInstructions(5).Insert(MultiplierWithCountCheck);
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: UITankWindow__OnUpdate_Transpiler
        // Target: UITankWindow._OnUpdate
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UITankWindow), nameof(UITankWindow._OnUpdate))]
        private static IEnumerable<CodeInstruction> UITankWindow__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.opcode == OpCodes.Bgt || ci.opcode == OpCodes.Bgt_S),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.opcode == OpCodes.Br || ci.opcode == OpCodes.Br_S),
                new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(ci => ci.IsStloc())
            );
            matcher.Repeat(m => m.RemoveInstructions(5).InsertAndAdvance(MultiplierWithCountCheck));
            return matcher.InstructionEnumeration();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TankComponent), nameof(TankComponent.TickOutput))]
        private static bool TankComponent_TickOutput_Prefix(ref TankComponent __instance, PlanetFactory factory)
        {
            if (!__instance.outputSwitch || __instance.fluidCount <= 0)
                return false;
            var lastTankId = __instance.lastTankId;
            if (lastTankId <= 0)
                return false;
            var factoryStorage = factory.factoryStorage;
            ref var tankComponent = ref factoryStorage.tankPool[lastTankId];
            if (!tankComponent.inputSwitch || (tankComponent.fluidId > 0 && tankComponent.fluidId != __instance.fluidId))
                return false;
            var left = tankComponent.fluidCapacity - tankComponent.fluidCount;
            if (left <= 0)
                return false;
            if (tankComponent.fluidId == 0)
                tankComponent.fluidId = __instance.fluidId;
            var takeOut = Math.Min(left, FactoryPatch._tankFastFillInAndTakeOutMultiplierRealValue);
            if (takeOut >= __instance.fluidCount)
            {
                tankComponent.fluidCount += __instance.fluidCount;
                tankComponent.fluidInc += __instance.fluidInc;
                __instance.fluidId = 0;
                __instance.fluidCount = 0;
                __instance.fluidInc = 0;
            }
            else
            {
                var takeInc = __instance.split_inc(ref __instance.fluidCount, ref __instance.fluidInc, takeOut);
                tankComponent.fluidCount += takeOut;
                tankComponent.fluidInc += takeInc;
            }
            return false;
        }
    }

    internal class PressShiftToTakeWholeBeltItems : PatchImpl<PressShiftToTakeWholeBeltItems>
    {
        private static long nextTimei = 0;

        protected override void OnEnable()
        {
            GameLogicProc.OnGameBegin += OnGameBegin;
        }

        protected override void OnDisable()
        {
            GameLogicProc.OnGameBegin -= OnGameBegin;
        }

        private static void OnGameBegin()
        {
            nextTimei = 0;
        }
        // Harmony transpiler: VFInput_fastTransferWithEntityDown_Transpiler
        // Target: VFInput._fastTransferWithEntityDown (getter), VFInput._fastTransferWithEntityPress (getter)
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput._fastTransferWithEntityDown), MethodType.Getter)]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput._fastTransferWithEntityPress), MethodType.Getter)]
        private static IEnumerable<CodeInstruction> VFInput_fastTransferWithEntityDown_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))),
                new CodeMatch(ci => ci.opcode == OpCodes.Brtrue || ci.opcode == OpCodes.Brtrue_S)
            );
            var lables = matcher.Labels;
            matcher.RemoveInstructions(2);
            matcher.Labels.AddRange(lables);
            return matcher.InstructionEnumeration();
        }
        // Harmony transpiler: PlayerAction_Inspect_GameTick_Transpiler
        // Target: PlayerAction_Inspect.GameTick
        // Fallback: None — patch will fail loudly if the target method body changes.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GameTick))]
        private static IEnumerable<CodeInstruction> PlayerAction_Inspect_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.fastFillIn)))
            );
            matcher.SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))).Insert(
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ceq)
            );

            var label0 = generator.DefineLabel();
            var label1 = generator.DefineLabel();
            matcher.Start().MatchForward(false,
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastTakeOut)))
            ).Advance(8).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))),
                new CodeInstruction(OpCodes.Brfalse_S, label0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PressShiftToTakeWholeBeltItems), nameof(EntityFastTakeOutAlt))),
                new CodeInstruction(OpCodes.Br, label1)
            ).Labels.Add(label0);
            matcher.Advance(1).Labels.Add(label1);
            return matcher.InstructionEnumeration();
        }

        private static void EntityFastTakeOutAlt(PlanetFactory factory, int entityId, bool toPackage, out ItemBundle itemBundle, out bool full)
        {
            if (factory._tmp_items == null)
            {
                factory._tmp_items = new ItemBundle();
            }
            else
            {
                factory._tmp_items.Clear();
            }
            itemBundle = factory._tmp_items;
            full = false;
            if (entityId == 0 || factory.entityPool[entityId].id != entityId)
            {
                return;
            }
            var main = GameMain.instance;
            if (main.timei < nextTimei) return;
            nextTimei = main.timei + 12;

            ref var entityData = ref factory.entityPool[entityId];
            if (entityData.beltId <= 0) return;
            var cargoTraffic = factory.cargoTraffic;
            ref var belt = ref cargoTraffic.beltPool[entityData.beltId];
            if (belt.id != entityData.beltId) return;

            HashSet<int> pathIds = [belt.segPathId];
            HashSet<int> inserterIds = [];
            var includeBranches = FactoryPatch.PressShiftToTakeWholeBeltItemsIncludeBranches.Value;
            var includeInserters = FactoryPatch.PressShiftToTakeWholeBeltItemsIncludeInserters.Value;
            List<int> pendingPathIds = [belt.segPathId];
            Dictionary<int, long> takeOutItems = [];
            var factorySystem = factory.factorySystem;
            while (pendingPathIds.Count > 0)
            {
                var lastIndex = pendingPathIds.Count - 1;
                var thisPathId = pendingPathIds[lastIndex];
                pendingPathIds.RemoveAt(lastIndex);
                var path = cargoTraffic.GetCargoPath(thisPathId);
                if (path == null) continue;
                if (includeInserters)
                {
                    foreach (var beltId in path.belts)
                    {
                        ref var b = ref cargoTraffic.beltPool[beltId];
                        if (b.id != beltId) return;
                        // From WriteObjectConn: Only slot 4 to 11 is used for belt <-> inserter connections (method argument slot/otherSlot is -1 there)
                        for (int cidx = 4; cidx < 12; cidx++)
                        {
                            factory.ReadObjectConn(b.entityId, cidx, out var isOutput, out var otherObjId, out var otherSlot);
                            if (otherObjId <= 0) continue;
                            var inserterId = factory.entityPool[otherObjId].inserterId;
                            if (inserterId <= 0) continue;
                            ref var inserter = ref factorySystem.inserterPool[inserterId];
                            if (inserter.id != inserterId) continue;
                            inserterIds.Add(inserterId);
                            if (includeBranches)
                            {
                                var pickTargetId = inserter.pickTarget;
                                if (pickTargetId > 0)
                                {
                                    ref var pickTarget = ref factory.entityPool[pickTargetId];
                                    if (pickTarget.id == pickTargetId && pickTarget.beltId > 0)
                                    {
                                        ref var pickTargetBelt = ref cargoTraffic.beltPool[pickTarget.beltId];
                                        if (pickTargetBelt.id == pickTargetBelt.segPathId && !pathIds.Contains(pickTargetBelt.segPathId))
                                        {
                                            pathIds.Add(pickTargetBelt.segPathId);
                                            pendingPathIds.Add(pickTargetBelt.segPathId);
                                        }
                                    }
                                }
                                var insertTargetId = inserter.insertTarget;
                                if (insertTargetId > 0)
                                {
                                    ref var insertTarget = ref factory.entityPool[insertTargetId];
                                    if (insertTarget.id == insertTargetId && insertTarget.beltId > 0)
                                    {
                                        ref var insertTargetBelt = ref cargoTraffic.beltPool[insertTarget.beltId];
                                        if (insertTargetBelt.id == insertTargetBelt.segPathId && !pathIds.Contains(insertTargetBelt.segPathId))
                                        {
                                            pathIds.Add(insertTargetBelt.segPathId);
                                            pendingPathIds.Add(insertTargetBelt.segPathId);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (!includeBranches) continue;
                foreach (var inputPathId in path.inputPaths)
                {
                    if (pathIds.Contains(inputPathId)) continue;
                    pathIds.Add(inputPathId);
                    pendingPathIds.Add(inputPathId);
                }
                if (path.outputPath == null) continue;
                var outputPathId = path.outputPath.id;
                if (pathIds.Contains(outputPathId)) continue;
                pathIds.Add(outputPathId);
                pendingPathIds.Add(outputPathId);
            }

            var mainPlayer = factory.gameData.mainPlayer;
            foreach (var pathId in pathIds)
            {
                var cargoPath = cargoTraffic.GetCargoPath(pathId);
                if (cargoPath == null) continue;
                var end = cargoPath.bufferLength - 5;
                var buffer = cargoPath.buffer;
                for (var i = 0; i <= end;)
                {
                    if (buffer[i] >= 246)
                    {
                        var delta = 250 - buffer[i];
                        if (delta > 0) i += delta;
                        var index = buffer[i + 1] - 1 + (buffer[i + 2] - 1) * 100 + (buffer[i + 3] - 1) * 10000 + (buffer[i + 4] - 1) * 1000000;
                        ref var cargo = ref cargoPath.cargoContainer.cargoPool[index];
                        var item = cargo.item;
                        takeOutItems[item] = (takeOutItems.TryGetValue(item, out var value) ? value : 0)
                            + ((long)cargo.stack | ((long)cargo.inc << 32));
                        Array.Clear(buffer, i - 4, 10);
                        i += 6;
                        if (cargoPath.updateLen < i) cargoPath.updateLen = i;
                        i += 4;
                        cargoPath.cargoContainer.RemoveCargo(index);
                    }
                    else
                    {
                        i += 5;
                        if (i > end && i < end + 5)
                        {
                            i = end;
                        }
                    }
                }
            }
            foreach (var inserterId in inserterIds)
            {
                ref var inserter = ref factorySystem.inserterPool[inserterId];
                if (inserter.itemId > 0 && inserter.stackCount > 0)
                {
                    takeOutItems[inserter.itemId] = (takeOutItems.TryGetValue(inserter.itemId, out var value) ? value : 0)
                            + ((long)inserter.itemCount | ((long)inserter.itemInc << 32));
                    inserter.itemId = 0;
                    inserter.stackCount = 0;
                    inserter.itemCount = 0;
                    inserter.itemInc = 0;
                }
            }
            foreach (var kvp in takeOutItems)
            {
                var added = mainPlayer.TryAddItemToPackage(kvp.Key, (int)(kvp.Value & 0xFFFFFFFF), (int)(kvp.Value >> 32), true, entityId);
                if (added > 0) UIItemup.Up(kvp.Key, added);
            }
        }
    }
}
