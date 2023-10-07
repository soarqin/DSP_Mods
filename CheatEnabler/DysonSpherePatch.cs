using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;

public static class DysonSpherePatch
{
    public static ConfigEntry<bool> StopEjectOnNodeCompleteEnabled;
    public static ConfigEntry<bool> OnlyConstructNodesEnabled;
    public static ConfigEntry<bool> SkipBulletEnabled;
    public static ConfigEntry<bool> SkipAbsorbEnabled;
    public static ConfigEntry<bool> QuickAbsorbEnabled;
    public static ConfigEntry<bool> EjectAnywayEnabled;
    public static ConfigEntry<bool> OverclockEjectorEnabled;
    public static ConfigEntry<bool> OverclockSiloEnabled;
    private static Harmony _dysonSpherePatch;
    private static bool _instantAbsorb;
   
    public static void Init()
    {
        _dysonSpherePatch ??= Harmony.CreateAndPatchAll(typeof(DysonSpherePatch));
        StopEjectOnNodeCompleteEnabled.SettingChanged += (_, _) => StopEjectOnNodeComplete.Enable(StopEjectOnNodeCompleteEnabled.Value);
        OnlyConstructNodesEnabled.SettingChanged += (_, _) => OnlyConstructNodes.Enable(OnlyConstructNodesEnabled.Value);
        SkipBulletEnabled.SettingChanged += (_, _) => SkipBulletPatch.Enable(SkipBulletEnabled.Value);
        SkipAbsorbEnabled.SettingChanged += (_, _) => SkipAbsorbPatch.Enable(SkipBulletEnabled.Value);
        QuickAbsorbEnabled.SettingChanged += (_, _) => QuickAbsorbPatch.Enable(QuickAbsorbEnabled.Value);
        EjectAnywayEnabled.SettingChanged += (_, _) => EjectAnywayPatch.Enable(EjectAnywayEnabled.Value);
        OverclockEjectorEnabled.SettingChanged += (_, _) => OverclockEjector.Enable(OverclockEjectorEnabled.Value);
        OverclockSiloEnabled.SettingChanged += (_, _) => OverclockSilo.Enable(OverclockSiloEnabled.Value);
        StopEjectOnNodeComplete.Enable(StopEjectOnNodeCompleteEnabled.Value);
        OnlyConstructNodes.Enable(OnlyConstructNodesEnabled.Value);
        SkipBulletPatch.Enable(SkipBulletEnabled.Value);
        SkipAbsorbPatch.Enable(SkipBulletEnabled.Value);
        QuickAbsorbPatch.Enable(QuickAbsorbEnabled.Value);
        EjectAnywayPatch.Enable(EjectAnywayEnabled.Value);
        OverclockEjector.Enable(OverclockEjectorEnabled.Value);
        OverclockSilo.Enable(OverclockSiloEnabled.Value);
    }
    
    public static void Uninit()
    {
        StopEjectOnNodeComplete.Enable(false);
        OnlyConstructNodes.Enable(false);
        SkipBulletPatch.Enable(false);
        SkipAbsorbPatch.Enable(false);
        QuickAbsorbPatch.Enable(false);
        EjectAnywayPatch.Enable(false);
        OverclockEjector.Enable(false);
        OverclockSilo.Enable(false);
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
        matcher.Labels = new List<Label>();
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
                _initialized = false;
                _patch?.UnpatchSelf();
                _patch = null;
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
            _nodeForAbsorb = new HashSet<int>[galaxy.starCount];
            var spheres = data.dysonSpheres;
            if (spheres == null) return;
            foreach (var sphere in spheres)
            {
                if (sphere?.layersSorted == null) continue;
                var starIndex = sphere.starData.index;
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
            comp ??= new HashSet<int>();
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
            var starIndex = __instance.starData.index;
            if (_nodeForAbsorb[starIndex] == null) return;
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
            matcher.Labels = new List<Label>();
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

    private static class SkipBulletPatch
    {
        private static long _sailLifeTime;
        private static DysonSailCache[][] _sailsCache;
        private static int[] _sailsCacheLen, _sailsCacheCapacity;
        private static Harmony _patch;
        
        private struct DysonSailCache
        {
            public DysonSail Sail;
            public int OrbitId;

            public void FromData(in VectorLF3 delta1, in VectorLF3 delta2, int orbitId)
            {
                Sail.px = (float)delta1.x;
                Sail.py = (float)delta1.y;
                Sail.pz = (float)delta1.z;
                Sail.vx = (float)delta2.x;
                Sail.vy = (float)delta2.y;
                Sail.vz = (float)delta2.z;
                Sail.gs = 1f;
                OrbitId = orbitId;
            }
        }

        public static void Enable(bool on)
        {
            if (on)
            {
                UpdateSailLifeTime();
                UpdateSailsCacheForThisGame();
                _patch ??= Harmony.CreateAndPatchAll(typeof(SkipBulletPatch));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

        private static void UpdateSailLifeTime()
        {
            if (GameMain.history == null) return;
            _sailLifeTime = (long)(GameMain.history.solarSailLife * 60f + 0.1f);
        }

        private static void UpdateSailsCacheForThisGame()
        {
            var galaxy = GameMain.data?.galaxy;
            if (galaxy == null) return;
            var starCount = GameMain.data.galaxy.starCount;
            _sailsCache = new DysonSailCache[starCount][];
            _sailsCacheLen = new int[starCount];
            _sailsCacheCapacity = new int[starCount];
            Array.Clear(_sailsCacheLen, 0, starCount);
            Array.Clear(_sailsCacheCapacity, 0, starCount);
        }
        
        private static void SetSailsCacheCapacity(int index, int capacity)
        {
            var newCache = new DysonSailCache[capacity];
            var len = _sailsCacheLen[index];
            if (len > 0)
            {
                Array.Copy(_sailsCache[index], newCache, len);
            }
            _sailsCache[index] = newCache;
            _sailsCacheCapacity[index] = capacity;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void GameMain_Begin_Postfix()
        {
            UpdateSailsCacheForThisGame();
            UpdateSailLifeTime();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.UnlockTechFunction))]
        private static void GameHistoryData_SetForNewGame_Postfix(int func)
        {
            if (func == 12)
            {
                UpdateSailLifeTime();
            }
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldc_R4, 10f)
            ).Advance(2);
            var start = matcher.Pos;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Pop)
            ).Advance(1);
            var end = matcher.Pos;
            matcher.Start().Advance(start).RemoveInstructions(end - start).Insert(
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.orbitId))),
                new CodeInstruction(OpCodes.Ldloc_S, 8),
                new CodeInstruction(OpCodes.Ldloc_S, 10),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkipBulletPatch), nameof(SkipBulletPatch.AddDysonSail)))
            );
            return matcher.InstructionEnumeration();
        }

        private static void AddDysonSail(DysonSwarm swarm, int orbitId, VectorLF3 uPos, VectorLF3 endVec)
        {
            var index = swarm.starData.index;
            var delta1 = endVec - swarm.starData.uPosition;
            var delta2 = VectorLF3.Cross(endVec - uPos, swarm.orbits[orbitId].up).normalized * Math.Sqrt(swarm.dysonSphere.gravity / swarm.orbits[orbitId].radius)
                         + RandomTable.SphericNormal(ref swarm.randSeed, 0.5);
            lock(swarm)
            {
                var cache = _sailsCache[index];
                var len = _sailsCacheLen[index];
                if (cache == null)
                {
                    SetSailsCacheCapacity(index, 256);
                    cache = _sailsCache[index];
                }
                else
                {
                    var capacity = _sailsCacheCapacity[index];
                    if (len >= capacity)
                    {
                        SetSailsCacheCapacity(index, capacity * 2);
                        cache = _sailsCache[index];
                    }
                }
                _sailsCacheLen[index] = len + 1;
                cache[len].FromData(delta1, delta2, orbitId);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DysonSwarm), "GameTick")]
        public static void DysonSwarm_GameTick_Prefix(DysonSwarm __instance, long time)
        {
            var index = __instance.starData.index;
            var len = _sailsCacheLen[index];
            if (len == 0) return;
            _sailsCacheLen[index] = 0;
            var cache = _sailsCache[index];
            var deadline = time + _sailLifeTime;
            var idx = len - 1;
            if (_instantAbsorb)
            {
                var sphere = __instance.dysonSphere;
                var layers = sphere.layersSorted;
                var llen = sphere.layerCount;
                if (llen > 0)
                {
                    var lidx = time / 16 % llen;
                    for (var i = llen - 1; i >= 0; i--)
                    {
                        var layer = layers[(lidx + i) % llen];
                        var nodes = layer.nodePool;
                        var nlen = layer.nodeCursor;
                        var nidx = time % nlen;
                        for (var j = nlen - 1; j >= 0; j--)
                        {
                            var nodeIdx = (nidx + j) % nlen;
                            var node = nodes[nodeIdx];
                            if (node == null || node.id != nodeIdx || node.sp < node.spMax) continue;
                            while (node.cpReqOrder > 0)
                            {
                                node.cpOrdered++;
                                if (node.ConstructCp() == null) break;
                                if (idx == 0)
                                {
                                    sphere.productRegister[11901] += len;
                                    sphere.consumeRegister[11901] += len;
                                    sphere.productRegister[11903] += len;
                                    return;
                                }
                                idx--;
                            }
                        }
                    }
                }
                var absorbCnt = len - 1 - idx;
                sphere.productRegister[11901] += absorbCnt;
                sphere.consumeRegister[11901] += absorbCnt;
                sphere.productRegister[11903] += absorbCnt;
            }
            for (; idx >= 0; idx--)
            {
                __instance.AddSolarSail(cache[idx].Sail, cache[idx].OrbitId, deadline);
            }
        }
    }
    
    private static class SkipAbsorbPatch
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            _instantAbsorb = SkipAbsorbEnabled.Value && QuickAbsorbEnabled.Value;
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(SkipAbsorbPatch));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonNode), nameof(DysonNode.OrderConstructCp))]
        private static IEnumerable<CodeInstruction> DysonNode_OrderConstructCp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(DysonSwarm), nameof(DysonSwarm.AbsorbSail)))
            ).Advance(1).SetInstructionAndAdvance(
                new CodeInstruction(OpCodes.Pop)
            ).Insert(
                new CodeInstruction(OpCodes.Ret)
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.AbsorbSail))]
        private static IEnumerable<CodeInstruction> DysonSwarm_AbsorbSail_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(ExpiryOrder), nameof(ExpiryOrder.index)))
            ).Advance(1).RemoveInstructions(matcher.Length - matcher.Pos).Insert(
                // node.cpOrdered = node.cpOrdered + 1;
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode.cpOrdered))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode.cpOrdered))),
                
                // if (node.ConstructCp() != null)
                // {
                //     this.dysonSphere.productRegister[11903]++;
                // }
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(DysonNode), nameof(DysonNode.ConstructCp))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSwarm), nameof(DysonSwarm.dysonSphere))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphere), nameof(DysonSphere.productRegister))),
                new CodeInstruction(OpCodes.Ldc_I4, 11903),
                new CodeInstruction(OpCodes.Ldelema, typeof(int)),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldind_I4),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stind_I4),
                
                // this.RemoveSolarSail(index);
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(label1),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DysonSwarm), nameof(DysonSwarm.RemoveSolarSail))),
                
                // return false;
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ret)
            );
            return matcher.InstructionEnumeration();
        }
    }
    
    private static class QuickAbsorbPatch
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            _instantAbsorb = SkipAbsorbEnabled.Value && QuickAbsorbEnabled.Value;
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(QuickAbsorbPatch));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonSphereLayer), nameof(DysonSphereLayer.GameTick))]
        private static IEnumerable<CodeInstruction> DysonSphereLayer_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* Insert absorption functions on beginning */
            matcher.Start().InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuickAbsorbPatch), nameof(QuickAbsorbPatch.DoAbsorb)))
            ).MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphereLayer), nameof(DysonSphereLayer.dysonSphere))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphere), nameof(DysonSphere.swarm)))
            ).Insert(new CodeInstruction(OpCodes.Ret));
            /* Insert a RETURN before old absorption functions */
            return matcher.InstructionEnumeration();
        }
        
        private static void DoAbsorb(DysonSphereLayer layer, long gameTick)
        {
            var swarm = layer.dysonSphere.swarm;
            if (SkipAbsorbEnabled.Value)
            {
                for (var i = layer.nodeCursor - 1; i > 0; i--)
                {
                    var node = layer.nodePool[i];
                    if (node == null || node.id != i || node.sp < node.spMax) continue;
                    if (node._cpReq <= node.cpOrdered) continue;
                    while (swarm.AbsorbSail(node, gameTick)) {}
                }
                return;
            }
            for (var i = layer.nodeCursor - 1; i > 0; i--)
            {
                var node = layer.nodePool[i];
                if (node == null || node.id != i || node.sp < node.spMax) continue;
                var req = node._cpReq;
                var ordered = node.cpOrdered;
                if (req <= ordered) continue;
                if (!swarm.AbsorbSail(node, gameTick)) continue;
                ordered++;
                while (req > ordered)
                {
                    if (!swarm.AbsorbSail(node, gameTick)) break;
                    ordered++;
                } 
                node.cpOrdered = ordered;
            }
        }
    }
    
    private static class EjectAnywayPatch
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(EjectAnywayPatch));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Ldc_R8 && Math.Abs((double)instr.operand - 0.08715574) < 0.00000001)
            );
            var start = matcher.Pos - 3;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.And)
            ).Advance(1).MatchForward(false,
                new CodeMatch(OpCodes.And)
            );
            var end = matcher.Pos - 2;
            /* Remove angle checking codes, then add:
             *   V_13 = this.bulletCount > 0;
             */
            matcher.Start().Advance(start).RemoveInstructions(end - start).Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.bulletCount))),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.Stloc_S, 13)
            );
            return matcher.InstructionEnumeration();
        }
    }

    private static class OverclockEjector
    {
        private static Harmony _patch;

        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(OverclockEjector));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectAndSiloComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* Add a multiply to ejector speed */
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_1)
            ).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I4_S, 10),
                new CodeInstruction(OpCodes.Mul)
            ).Advance(1);

            /* remove boost part of Sandbox Mode for better performance */
            var pos = matcher.Pos;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_1)
            ).Advance(1);
            var end = matcher.Pos;
            matcher.Start().Advance(pos).RemoveInstructions(end - pos);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIEjectorWindow), nameof(UIEjectorWindow._OnUpdate))]
        private static IEnumerable<CodeInstruction> UIEjectAndSiloWindow__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* Add a multiply to ejector speed */
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(Cargo), nameof(Cargo.accTableMilli)))
            ).Advance(-1);
            var operand = matcher.Operand;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_S, operand)
            ).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_R4, 10f),
                new CodeInstruction(OpCodes.Mul)
            ).Advance(1);

            /* remove boost part of Sandbox Mode for better performance */
            var pos = matcher.Pos;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_S, operand)
            ).Advance(1);
            var end = matcher.Pos;
            matcher.Start().Advance(pos).RemoveInstructions(end - pos);
            return matcher.InstructionEnumeration();
        }
    }

    private static class OverclockSilo
    {
        private static Harmony _patch;
        public static void Enable(bool on)
        {
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(OverclockSilo));
            }
            else
            {
                _patch?.UnpatchSelf();
                _patch = null;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectAndSiloComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* Add a multiply to ejector speed */
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_1)
            ).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I4_S, 10),
                new CodeInstruction(OpCodes.Mul)
            ).Advance(1);

            /* remove boost part of Sandbox Mode for better performance */
            var pos = matcher.Pos;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_1)
            ).Advance(1);
            var end = matcher.Pos;
            matcher.Start().Advance(pos).RemoveInstructions(end - pos);
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UISiloWindow), nameof(UISiloWindow._OnUpdate))]
        private static IEnumerable<CodeInstruction> UIEjectAndSiloWindow__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            /* Add a multiply to ejector speed */
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(Cargo), nameof(Cargo.accTableMilli)))
            ).Advance(-1);
            var operand = matcher.Operand;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_S, operand)
            ).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_R4, 10f),
                new CodeInstruction(OpCodes.Mul)
            ).Advance(1);

            /* remove boost part of Sandbox Mode for better performance */
            var pos = matcher.Pos;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stloc_S, operand)
            ).Advance(1);
            var end = matcher.Pos;
            matcher.Start().Advance(pos).RemoveInstructions(end - pos);
            return matcher.InstructionEnumeration();
        }
    }
}
