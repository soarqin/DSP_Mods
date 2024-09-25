using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;
using UXAssist.Common;

namespace UXAssist.Patches;

public class DysonSpherePatch: PatchImpl<DysonSpherePatch>
{
    public static ConfigEntry<bool> StopEjectOnNodeCompleteEnabled;
    public static ConfigEntry<bool> OnlyConstructNodesEnabled;
    public static ConfigEntry<int> AutoConstructMultiplier;

    private static FieldInfo _totalNodeSpInfo, _totalFrameSpInfo, _totalCpInfo;

    public static void Init()
    {
        I18N.Add("[UXAssist] No node to fill", "[UXAssist] No node to fill", "[UXAssist] 无可建造节点");
        Enable(true);
        StopEjectOnNodeCompleteEnabled.SettingChanged += (_, _) => StopEjectOnNodeComplete.Enable(StopEjectOnNodeCompleteEnabled.Value);
        OnlyConstructNodesEnabled.SettingChanged += (_, _) => OnlyConstructNodes.Enable(OnlyConstructNodesEnabled.Value);
        _totalNodeSpInfo = AccessTools.Field(typeof(DysonSphereLayer), "totalNodeSP");
        _totalFrameSpInfo = AccessTools.Field(typeof(DysonSphereLayer), "totalFrameSP");
        _totalCpInfo = AccessTools.Field(typeof(DysonSphereLayer), "totalCP");
    }

    public static void Start()
    {
        StopEjectOnNodeComplete.Enable(StopEjectOnNodeCompleteEnabled.Value);
        OnlyConstructNodes.Enable(OnlyConstructNodesEnabled.Value);
    }

    public static void Uninit()
    {
        StopEjectOnNodeComplete.Enable(false);
        OnlyConstructNodes.Enable(false);
        Enable(false);
    }

    public static void InitCurrentDysonSphere(int index)
    {
        var star = GameMain.localStar;
        if (star == null) return;
        var dysonSpheres = GameMain.data?.dysonSpheres;
        if (dysonSpheres == null) return;
        if (index < 0)
        {
            if (dysonSpheres[star.index] == null) return;
            var dysonSphere = new DysonSphere();
            dysonSpheres[star.index] = dysonSphere;
            dysonSphere.Init(GameMain.data, star);
            dysonSphere.ResetNew();
            return;
        }

        var ds = dysonSpheres[star.index];
        if (ds?.layersIdBased[index] == null) return;
        var pool = ds.rocketPool;
        for (var id = ds.rocketCursor - 1; id > 0; id--)
        {
            if (pool[id].id != id) continue;
            if (pool[id].nodeLayerId != index) continue;
            ds.RemoveDysonRocket(id);
        }

        ds.RemoveLayer(index);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.AutoConstruct))]
    private static bool DysonSwarm_AutoConstruct_Prefix(DysonSwarm __instance)
    {
        return __instance.dysonSphere.autoNodeCount == 0;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.AutoConstruct))]
    private static bool DysonSphere_AutoConstruct_Prefix(DysonSphere __instance)
    {
        var totalCount = AutoConstructMultiplier.Value * 6;
        foreach (var dysonSphereLayer in __instance.layersIdBased)
        {
            if (dysonSphereLayer == null) continue;
            for (var j = dysonSphereLayer.nodePool.Length - 1; j >= 0; j--)
            {
                var dysonNode = dysonSphereLayer.nodePool[j];
                if (dysonNode == null || dysonNode.id != j) continue;
                var count = dysonNode._spReq - dysonNode.spOrdered;
                int todoCount;
                int[] productRegister;
                if (count > 0)
                {
                    if (count > totalCount)
                    {
                        count = totalCount;
                    }

                    todoCount = count;
                    if (dysonNode.sp < dysonNode.spMax)
                    {
                        int diff;
                        if (dysonNode.sp + count > dysonNode.spMax)
                        {
                            diff = dysonNode.spMax - dysonNode.sp;
                            count -= diff;
                            dysonNode._spReq -= diff;

                            dysonNode.sp = dysonNode.spMax;
                        }
                        else
                        {
                            diff = count;
                            dysonNode._spReq -= diff;

                            dysonNode.sp += diff;
                            count = 0;
                        }

                        // Make compatible with DSPOptimizations
                        if (_totalNodeSpInfo != null)
                            _totalNodeSpInfo.SetValue(dysonSphereLayer, (long)_totalNodeSpInfo.GetValue(dysonSphereLayer) + diff - 1);
                        __instance.UpdateProgress(dysonNode);
                    }

                    if (count > 0)
                    {
                        var frameCount = dysonNode.frames.Count;
                        var frameIndex = dysonNode.frameTurn % frameCount;
                        for (var i = frameCount; i > 0 && count > 0; i--)
                        {
                            var dysonFrame = dysonNode.frames[frameIndex];
                            var spMax = dysonFrame.spMax >> 1;
                            if (dysonFrame.nodeA == dysonNode && dysonFrame.spA < spMax)
                            {
                                int diff;
                                if (dysonFrame.spA + count > spMax)
                                {
                                    diff = spMax - dysonFrame.spA;
                                    count -= diff;
                                    dysonNode._spReq -= diff;

                                    dysonFrame.spA = spMax;
                                }
                                else
                                {
                                    diff = count;
                                    dysonNode._spReq -= diff;

                                    dysonFrame.spA += diff;
                                    count = 0;
                                }

                                // Make compatible with DSPOptimizations
                                if (_totalFrameSpInfo != null)
                                    _totalFrameSpInfo.SetValue(dysonSphereLayer, (long)_totalFrameSpInfo.GetValue(dysonSphereLayer) + diff - 1);
                                __instance.UpdateProgress(dysonFrame);
                            }

                            if (count > 0 && dysonFrame.nodeB == dysonNode && dysonFrame.spB < spMax)
                            {
                                int diff;
                                if (dysonFrame.spB + count > spMax)
                                {
                                    diff = spMax - dysonFrame.spB;
                                    count -= diff;
                                    dysonNode._spReq -= diff;

                                    dysonFrame.spB = spMax;
                                }
                                else
                                {
                                    diff = count;
                                    dysonNode._spReq -= diff;

                                    dysonFrame.spB += diff;
                                    count = 0;
                                }

                                // Make compatible with DSPOptimizations
                                if (_totalFrameSpInfo != null)
                                    _totalFrameSpInfo.SetValue(dysonSphereLayer, (long)_totalFrameSpInfo.GetValue(dysonSphereLayer) + diff - 1);
                                __instance.UpdateProgress(dysonFrame);
                            }

                            frameIndex = (frameIndex + 1) % frameCount;
                        }

                        dysonNode.frameTurn = frameIndex;
                    }

                    if (dysonNode.spOrdered >= dysonNode._spReq)
                    {
                        __instance.RemoveAutoNode(dysonNode);
                        __instance.PickAutoNode();
                    }

                    productRegister = __instance.productRegister;
                    if (productRegister != null)
                    {
                        lock (productRegister)
                        {
                            productRegister[11902] += todoCount - count;
                        }
                    }
                }

                count = dysonNode._cpReq - dysonNode.cpOrdered;
                if (count > 0)
                {
                    if (count > totalCount) count = totalCount;
                    todoCount = count;
                    var shellCount = dysonNode.shells.Count;
                    var shellIndex = dysonNode.shellTurn % shellCount;
                    for (var i = shellCount; i > 0 && count > 0; i--)
                    {
                        var dysonShell = dysonNode.shells[shellIndex];
                        var nodeIndex = dysonShell.nodeIndexMap[dysonNode.id];
                        var diff = (dysonShell.vertsqOffset[nodeIndex + 1] - dysonShell.vertsqOffset[nodeIndex]) * dysonShell.cpPerVertex - dysonShell.nodecps[nodeIndex];
                        if (diff > count)
                            diff = count;
                        count -= diff;
                        dysonNode._cpReq -= diff;
                        dysonShell.nodecps[nodeIndex] += diff;
                        dysonShell.nodecps[dysonShell.nodecps.Length - 1] += diff;
                        // Make compatible with DSPOptimizations
                        if (_totalCpInfo != null)
                        {
                            _totalCpInfo.SetValue(dysonSphereLayer, (long)_totalCpInfo.GetValue(dysonSphereLayer) + diff);
                            dysonShell.SetMaterialDynamicVars();
                        }
                        shellIndex = (shellIndex + 1) % shellCount;
                    }

                    dysonNode.shellTurn = shellIndex;

                    var solarSailCount = todoCount - count;
                    productRegister = __instance.productRegister;
                    if (productRegister != null)
                    {
                        lock (productRegister)
                        {
                            productRegister[11901] += solarSailCount;
                            productRegister[11903] += solarSailCount;
                        }
                    }
                    var consumeRegister = __instance.consumeRegister;
                    if (consumeRegister != null)
                    {
                        lock (consumeRegister)
                        {
                            consumeRegister[11901] += solarSailCount;
                        }
                    }
                }
            }
        }

        return false;
    }

    [HarmonyTranspiler]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(DysonNode), nameof(DysonNode.ConstructCp))]
    private static IEnumerable<CodeInstruction> DysonSpherePatch_DysonNode_ConstructCp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchBack(false,
            new CodeMatch(OpCodes.Ldc_I4_0),
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(DysonShell), nameof(DysonShell.Construct)))
        ).Advance(3).InsertAndAdvance(
            // node._cpReq = node._cpReq - 1;
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode._cpReq))),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Sub),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode._cpReq)))
        );
        // Remove use of RecalcCpReq()
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(DysonNode), nameof(DysonNode.RecalcCpReq)))
        );
        var labels = matcher.Labels;
        matcher.RemoveInstructions(2).Labels.AddRange(labels);
        return matcher.InstructionEnumeration();
    }

    private class StopEjectOnNodeComplete: PatchImpl<StopEjectOnNodeComplete>
    {
        private static HashSet<int>[] _nodeForAbsorb;
        private static bool _initialized;

        protected override void OnEnable()
        {
            InitNodeForAbsorb();
            GameLogic.OnGameBegin += GameMain_Begin_Postfix;
            GameLogic.OnGameEnd += GameMain_End_Postfix;
        }

        protected override void OnDisable()
        {
            GameLogic.OnGameEnd -= GameMain_End_Postfix;
            GameLogic.OnGameBegin -= GameMain_Begin_Postfix;
            _initialized = false;
            _nodeForAbsorb = null;
        }

        private static void InitNodeForAbsorb()
        {
            _initialized = false;
            _nodeForAbsorb = null;
            var data = GameMain.data;
            var galaxy = data?.galaxy;
            if (galaxy == null) return;
            var galaxyStarCount = galaxy.starCount;
            _nodeForAbsorb = new HashSet<int>[galaxyStarCount];
            var spheres = data.dysonSpheres;
            if (spheres == null) return;
            foreach (var sphere in spheres)
            {
                if (sphere?.layersSorted == null) continue;
                var starIndex = sphere.starData.index;
                if (starIndex >= galaxyStarCount) continue;
                foreach (var layer in sphere.layersSorted)
                {
                    if (layer == null) continue;
                    for (var i = layer.nodeCursor - 1; i > 0; i--)
                    {
                        var node = layer.nodePool[i];
                        if (node == null || node.id != i || node.sp < node.spMax || node.cpReqOrder == 0) continue;
                        SetNodeForAbsorb(starIndex, layer.id, node.id, true);
                    }
                }
            }

            _initialized = true;
        }

        private static void SetNodeForAbsorb(int index, int layerId, int nodeId, bool canAbsorb)
        {
            ref var comp = ref _nodeForAbsorb[index];
            comp ??= [];
            var idx = nodeId * 10 + layerId;
            if (canAbsorb)
                comp.Add(idx);
            else
                comp.Remove(idx);
        }

        private static void UpdateNodeForAbsorbOnSpChange(DysonNode node)
        {
            if (!_initialized) return;
            if (node.sp < node.spMax || node.cpReqOrder <= 0) return;
            var shells = node.shells;
            if (shells.Count == 0) return;
            SetNodeForAbsorb(shells[0].dysonSphere.starData.index, node.layerId, node.id, true);
        }

        private static void UpdateNodeForAbsorbOnCpChange(DysonNode node)
        {
            if (!_initialized) return;
            if (node.sp < node.spMax || node.cpReqOrder > 0) return;
            var shells = node.shells;
            if (shells.Count == 0) return;
            SetNodeForAbsorb(shells[0].dysonSphere.starData.index, node.layerId, node.id, false);
        }

        private static bool AnyNodeForAbsorb(int starIndex)
        {
            var comp = _nodeForAbsorb[starIndex];
            return comp is { Count: > 0 };
        }

        private static void GameMain_Begin_Postfix()
        {
            InitNodeForAbsorb();
        }

        private static void GameMain_End_Postfix()
        {
            _initialized = false;
            _nodeForAbsorb = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DysonNode), nameof(DysonNode.RecalcCpReq))]
        private static void DysonNode_RecalcCpReq_Postfix(DysonNode __instance)
        {
            UpdateNodeForAbsorbOnCpChange(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DysonSphereLayer), nameof(DysonSphereLayer.RemoveDysonNode))]
        private static void DysonSphereLayer_RemoveDysonNode_Prefix(DysonSphereLayer __instance, int nodeId)
        {
            if (_initialized)
                SetNodeForAbsorb(__instance.starData.index, __instance.id, nodeId, false);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.ResetNew))]
        private static void DysonSphere_ResetNew_Prefix(DysonSphere __instance)
        {
            if (_nodeForAbsorb == null) return;
            var starIndex = __instance.starData.index;
            if (starIndex >= _nodeForAbsorb.Length || _nodeForAbsorb[starIndex] == null) return;
            _nodeForAbsorb[starIndex].Clear();
            _nodeForAbsorb[starIndex] = null;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                // if (this.orbitId == 0
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.orbitId))),
                new CodeMatch(OpCodes.Brtrue)
            ).Advance(2).Insert(
                // || !StopEjectOnNodeComplete.AnyNodeForAbsorb(this.starData.index))
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSwarm), nameof(DysonSwarm.starData))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StarData), nameof(StarData.index))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StopEjectOnNodeComplete), nameof(AnyNodeForAbsorb))),
                new CodeInstruction(OpCodes.And)
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonNode), nameof(DysonNode.ConstructSp))]
        private static IEnumerable<CodeInstruction> DysonNode_ConstructSp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().MatchForward(false,
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode.sp)))
            ).Advance(1);
            var labels = matcher.Labels;
            matcher.Labels = [];
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StopEjectOnNodeComplete), nameof(UpdateNodeForAbsorbOnSpChange)))
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonNode), nameof(DysonNode.ConstructCp))]
        private static IEnumerable<CodeInstruction> DysonNode_ConstructCp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchBack(false,
                // Search for previous patch:
                //   node._cpReq = node._cpReq - 1;
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode._cpReq))),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Sub),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode._cpReq)))
            ).Advance(6).Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StopEjectOnNodeComplete), nameof(UpdateNodeForAbsorbOnCpChange)))
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIEjectorWindow), nameof(UIEjectorWindow._OnUpdate))]
        static IEnumerable<CodeInstruction> UIEjectorWindow__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            matcher.MatchForward(false,
                // this.stateText.text = "轨道未设置".Translate();
                new CodeMatch(OpCodes.Ldstr, "待机"),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Localization), nameof(Localization.Translate))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Text), nameof(Text.text)))
            ).InsertAndAdvance(
                // if (StopEjectOnNodeComplete.AnyNodeForAbsorb(this.starData.index))
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIEjectorWindow), nameof(UIEjectorWindow.factorySystem))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.planet))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetData), nameof(PlanetData.star))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StarData), nameof(StarData.index))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StopEjectOnNodeComplete), nameof(AnyNodeForAbsorb))),
                new CodeInstruction(OpCodes.Brfalse, label1)
            ).Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Br, label2),
                new CodeInstruction(OpCodes.Ldstr, "[UXAssist] No node to fill").WithLabels(label1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Localization), nameof(Localization.Translate)))
            ).Labels.Add(label2);
            return matcher.InstructionEnumeration();
        }
    }

    private class OnlyConstructNodes: PatchImpl<OnlyConstructNodes>
    {
        protected override void OnEnable()
        {
            var spheres = GameMain.data?.dysonSpheres;
            if (spheres == null) return;
            foreach (var sphere in spheres)
            {
                if (sphere == null) continue;
                sphere.CheckAutoNodes();
                if (sphere.autoNodeCount > 0) continue;
                sphere.PickAutoNode();
                sphere.PickAutoNode();
                sphere.PickAutoNode();
                sphere.PickAutoNode();
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonNode), nameof(DysonNode.spReqOrder), MethodType.Getter)]
        private static IEnumerable<CodeInstruction> DysonNode_spReqOrder_Getter_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode._spReq)))
            ).Advance(1).SetInstructionAndAdvance(
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode.spMax)))
            ).Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode.sp))),
                new CodeInstruction(OpCodes.Sub)
            );
            return matcher.InstructionEnumeration();
        }
    }
}