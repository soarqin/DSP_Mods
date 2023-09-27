﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;

public static class DysonSpherePatch
{
    public static ConfigEntry<bool> SkipBulletEnabled;
    public static ConfigEntry<bool> SkipAbsorbEnabled;
    public static ConfigEntry<bool> QuickAbsorbEnabled;
    public static ConfigEntry<bool> EjectAnywayEnabled;
    public static ConfigEntry<bool> OverclockEjectorEnabled;
    public static ConfigEntry<bool> OverclockSiloEnabled;
    private static Harmony _skipBulletPatch;
    private static Harmony _skipAbsorbPatch;
    private static Harmony _quickAbsorbPatch;
    private static Harmony _ejectAnywayPatch;
    private static Harmony _overclockEjector;
    private static Harmony _overclockSilo;
    private static Harmony _patch;
    private static bool _instantAbsorb;
   
    public static void Init()
    {
        _patch ??= Harmony.CreateAndPatchAll(typeof(DysonSpherePatch));
        SkipBulletEnabled.SettingChanged += (_, _) => SkipBulletValueChanged();
        SkipAbsorbEnabled.SettingChanged += (_, _) => SkipAbsorbValueChanged();
        QuickAbsorbEnabled.SettingChanged += (_, _) => QuickAbsorbValueChanged();
        EjectAnywayEnabled.SettingChanged += (_, _) => EjectAnywayValueChanged();
        OverclockEjectorEnabled.SettingChanged += (_, _) => OverclockEjectorValueChanged();
        OverclockSiloEnabled.SettingChanged += (_, _) => OverclockSiloValueChanged();
        SkipBulletValueChanged();
        SkipAbsorbValueChanged();
        QuickAbsorbValueChanged();
        EjectAnywayValueChanged();
        OverclockEjectorValueChanged();
        OverclockSiloValueChanged();
    }
    
    public static void Uninit()
    {
        _skipBulletPatch?.UnpatchSelf();
        _skipBulletPatch = null;
        _skipAbsorbPatch?.UnpatchSelf();
        _skipAbsorbPatch = null;
        _quickAbsorbPatch?.UnpatchSelf();
        _quickAbsorbPatch = null;
        _ejectAnywayPatch?.UnpatchSelf();
        _ejectAnywayPatch = null;
        _overclockEjector?.UnpatchSelf();
        _overclockEjector = null;
        _overclockSilo?.UnpatchSelf();
        _overclockSilo = null;
        _patch?.UnpatchSelf();
        _patch = null;
    }
    
    private static void SkipBulletValueChanged()
    {
        if (SkipBulletEnabled.Value)
        {
            if (_skipBulletPatch != null)
            {
                return;
            }
            SkipBulletPatch.UpdateSailLifeTime();
            SkipBulletPatch.UpdateSailsCacheForThisGame();
            _skipBulletPatch = Harmony.CreateAndPatchAll(typeof(SkipBulletPatch));
        }
        else if (_skipBulletPatch != null)
        {
            _skipBulletPatch.UnpatchSelf();
            _skipBulletPatch = null;
        }
    }
    
    private static void SkipAbsorbValueChanged()
    {
        _instantAbsorb = SkipAbsorbEnabled.Value && QuickAbsorbEnabled.Value;
        if (SkipAbsorbEnabled.Value)
        {
            if (_skipAbsorbPatch != null)
            {
                return;
            }
            _skipAbsorbPatch = Harmony.CreateAndPatchAll(typeof(SkipAbsorbPatch));
        }
        else if (_skipAbsorbPatch != null)
        {
            _skipAbsorbPatch.UnpatchSelf();
            _skipAbsorbPatch = null;
        }
    }
    
    private static void QuickAbsorbValueChanged()
    {
        _instantAbsorb = SkipAbsorbEnabled.Value && QuickAbsorbEnabled.Value;
        if (QuickAbsorbEnabled.Value)
        {
            if (_quickAbsorbPatch != null)
            {
                return;
            }
            _quickAbsorbPatch = Harmony.CreateAndPatchAll(typeof(QuickAbsorbPatch));
        }
        else if (_quickAbsorbPatch != null)
        {
            _quickAbsorbPatch.UnpatchSelf();
            _quickAbsorbPatch = null;
        }
    }
    
    private static void EjectAnywayValueChanged()
    {
        if (EjectAnywayEnabled.Value)
        {
            if (_ejectAnywayPatch != null)
            {
                return;
            }
            _ejectAnywayPatch = Harmony.CreateAndPatchAll(typeof(EjectAnywayPatch));
        }
        else if (_ejectAnywayPatch != null)
        {
            _ejectAnywayPatch.UnpatchSelf();
            _ejectAnywayPatch = null;
        }
    }
    
    private static void OverclockEjectorValueChanged()
    {
        if (OverclockEjectorEnabled.Value)
        {
            if (_overclockEjector != null)
            {
                return;
            }
            _overclockEjector = Harmony.CreateAndPatchAll(typeof(OverclockEjector));
        }
        else if (_overclockEjector != null)
        {
            _overclockEjector.UnpatchSelf();
            _overclockEjector = null;
        }
    }
    
    private static void OverclockSiloValueChanged()
    {
        if (OverclockSiloEnabled.Value)
        {
            if (_overclockSilo != null)
            {
                return;
            }
            _overclockSilo = Harmony.CreateAndPatchAll(typeof(OverclockSilo));
        }
        else if (_overclockSilo != null)
        {
            _overclockSilo.UnpatchSelf();
            _overclockSilo = null;
        }
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
    private static IEnumerable<CodeInstruction> DysonNode_ConstructCp_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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

    private static class SkipBulletPatch
    {
        private static long _sailLifeTime;
        private static DysonSailCache[][] _sailsCache;
        private static int[] _sailsCacheLen, _sailsCacheCapacity;

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

        public static void UpdateSailLifeTime()
        {
            if (GameMain.history == null) return;
            _sailLifeTime = (long)(GameMain.history.solarSailLife * 60f + 0.1f);
        }

        public static void UpdateSailsCacheForThisGame()
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
        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                        for (var j = nlen - 1; j > 0; j--)
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
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonNode), nameof(DysonNode.OrderConstructCp))]
        private static IEnumerable<CodeInstruction> DysonNode_OrderConstructCp_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
        private static IEnumerable<CodeInstruction> DysonSwarm_AbsorbSail_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonSphereLayer), nameof(DysonSphereLayer.GameTick))]
        private static IEnumerable<CodeInstruction> DysonSphereLayer_GameTick_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    if (node == null || node.id != i || node.sp != node.spMax) continue;
                    if (node._cpReq <= node.cpOrdered) continue;
                    while (swarm.AbsorbSail(node, gameTick)) {}
                }
                return;
            }
            for (var i = layer.nodeCursor - 1; i > 0; i--)
            {
                var node = layer.nodePool[i];
                if (node == null || node.id != i || node.sp != node.spMax) continue;
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
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectAndSiloComponent_InternalUpdate_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
        private static IEnumerable<CodeInstruction> UIEjectAndSiloWindow__OnUpdate_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectAndSiloComponent_InternalUpdate_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
        private static IEnumerable<CodeInstruction> UIEjectAndSiloWindow__OnUpdate_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
