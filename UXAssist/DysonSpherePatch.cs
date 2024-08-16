using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;

namespace UXAssist;

public static class DysonSpherePatch
{
    public static ConfigEntry<bool> StopEjectOnNodeCompleteEnabled;
    public static ConfigEntry<bool> OnlyConstructNodesEnabled;
    private static Harmony _dysonSpherePatch;
   
    public static void Init()
    {
        I18N.Add("[UXAssist] No node to fill", "[UXAssist] No node to fill", "[UXAssist] 无可建造节点");
        _dysonSpherePatch ??= Harmony.CreateAndPatchAll(typeof(DysonSpherePatch));
        StopEjectOnNodeCompleteEnabled.SettingChanged += (_, _) => StopEjectOnNodeComplete.Enable(StopEjectOnNodeCompleteEnabled.Value);
        OnlyConstructNodesEnabled.SettingChanged += (_, _) => OnlyConstructNodes.Enable(OnlyConstructNodesEnabled.Value);
        StopEjectOnNodeComplete.Enable(StopEjectOnNodeCompleteEnabled.Value);
        OnlyConstructNodes.Enable(OnlyConstructNodesEnabled.Value);
    }
    
    public static void Uninit()
    {
        StopEjectOnNodeComplete.Enable(false);
        OnlyConstructNodes.Enable(false);
        _dysonSpherePatch?.UnpatchSelf();
        _dysonSpherePatch = null;
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

    private static class StopEjectOnNodeComplete
    {
        private static Harmony _patch;
        private static HashSet<int>[] _nodeForAbsorb;
        private static bool _initialized;

        public static void Enable(bool on)
        {
            if (on)
            {
                InitNodeForAbsorb();
                _patch ??= Harmony.CreateAndPatchAll(typeof(StopEjectOnNodeComplete));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
                _initialized = false;
                _nodeForAbsorb = null;
            }
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
            return comp != null && comp.Count > 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void GameMain_Begin_Postfix()
        {
            InitNodeForAbsorb();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
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
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StopEjectOnNodeComplete), nameof(StopEjectOnNodeComplete.AnyNodeForAbsorb))),
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
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StopEjectOnNodeComplete), nameof(StopEjectOnNodeComplete.UpdateNodeForAbsorbOnSpChange)))
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
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StopEjectOnNodeComplete), nameof(StopEjectOnNodeComplete.UpdateNodeForAbsorbOnCpChange)))
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
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(UnityEngine.UI.Text), nameof(UnityEngine.UI.Text.text)))
            ).InsertAndAdvance(
                // if (StopEjectOnNodeComplete.AnyNodeForAbsorb(this.starData.index))
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIEjectorWindow), nameof(UIEjectorWindow.factorySystem))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.planet))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetData), nameof(PlanetData.star))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StarData), nameof(StarData.index))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StopEjectOnNodeComplete), nameof(StopEjectOnNodeComplete.AnyNodeForAbsorb))),
                new CodeInstruction(OpCodes.Brfalse, label1)
            ).Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Br, label2),
                new CodeInstruction(OpCodes.Ldstr, "[UXAssist] No node to fill").WithLabels(label1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Localization), nameof(Localization.Translate)))
            ).Labels.Add(label2);
            return matcher.InstructionEnumeration();
        }
    }

    private static class OnlyConstructNodes
    {
        private static Harmony _patch;
        
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(OnlyConstructNodes));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }

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
