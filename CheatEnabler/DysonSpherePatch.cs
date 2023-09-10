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
    public static ConfigEntry<bool> QuickAbsortEnabled;
    public static ConfigEntry<bool> EjectAnywayEnabled;
    private static Harmony _skipBulletPatch;
    private static Harmony _skipAbsorbPatch;
    private static Harmony _quickAbsortPatch;
    private static Harmony _ejectAnywayPatch;
    
    public static void Init()
    {
        SkipBulletEnabled.SettingChanged += (_, _) => SkipBulletValueChanged();
        SkipAbsorbEnabled.SettingChanged += (_, _) => SkipAbsorbValueChanged();
        QuickAbsortEnabled.SettingChanged += (_, _) => QuickAbsortValueChanged();
        EjectAnywayEnabled.SettingChanged += (_, _) => EjectAnywayValueChanged();
        SkipBulletValueChanged();
        SkipAbsorbValueChanged();
        QuickAbsortValueChanged();
        EjectAnywayValueChanged();
    }
    
    public static void Uninit()
    {
        if (_skipBulletPatch != null)
        {
            _skipBulletPatch.UnpatchSelf();
            _skipBulletPatch = null;
        }
        if (_skipAbsorbPatch != null)
        {
            _skipAbsorbPatch.UnpatchSelf();
            _skipAbsorbPatch = null;
        }
        if (_quickAbsortPatch != null)
        {
            _quickAbsortPatch.UnpatchSelf();
            _quickAbsortPatch = null;
        }
        if (_ejectAnywayPatch != null)
        {
            _ejectAnywayPatch.UnpatchSelf();
            _ejectAnywayPatch = null;
        }
    }
    
    private static void SkipBulletValueChanged()
    {
        if (SkipBulletEnabled.Value)
        {
            if (_skipBulletPatch != null)
            {
                _skipBulletPatch.UnpatchSelf();
                _skipBulletPatch = null;
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
        if (SkipAbsorbEnabled.Value)
        {
            if (_skipAbsorbPatch != null)
            {
                _skipAbsorbPatch.UnpatchSelf();
                _skipAbsorbPatch = null;
            }
            _skipAbsorbPatch = Harmony.CreateAndPatchAll(typeof(SkipAbsorbPatch));
        }
        else if (_skipAbsorbPatch != null)
        {
            _skipAbsorbPatch.UnpatchSelf();
            _skipAbsorbPatch = null;
        }
    }
    
    private static void QuickAbsortValueChanged()
    {
        if (QuickAbsortEnabled.Value)
        {
            if (_quickAbsortPatch != null)
            {
                _quickAbsortPatch.UnpatchSelf();
                _quickAbsortPatch = null;
            }
            _quickAbsortPatch = Harmony.CreateAndPatchAll(typeof(QuickAbsortPatch));
        }
        else if (_quickAbsortPatch != null)
        {
            _quickAbsortPatch.UnpatchSelf();
            _quickAbsortPatch = null;
        }
    }
    
    private static void EjectAnywayValueChanged()
    {
        if (EjectAnywayEnabled.Value)
        {
            if (_ejectAnywayPatch != null)
            {
                _ejectAnywayPatch.UnpatchSelf();
                _ejectAnywayPatch = null;
            }
            _ejectAnywayPatch = Harmony.CreateAndPatchAll(typeof(EjectAnywayPatch));
        }
        else if (_ejectAnywayPatch != null)
        {
            _ejectAnywayPatch.UnpatchSelf();
            _ejectAnywayPatch = null;
        }
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
        [HarmonyPatch(typeof(GameData), nameof(GameData.NewGame))]
        [HarmonyPatch(typeof(GameData), nameof(GameData.Import))]
        private static void GameData_NewGame_Postfix()
        {
            UpdateSailsCacheForThisGame();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.SetForNewGame))]
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.Import))]
        private static void GameHistoryData_SetForNewGame_Postfix()
        {
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
                ref var sailCache = ref cache[len];
                ref var ss = ref sailCache.Sail;
                ss.px = (float)delta1.x;
                ss.py = (float)delta1.y;
                ss.pz = (float)delta1.z;
                ss.vx = (float)delta2.x;
                ss.vy = (float)delta2.y;
                ss.vz = (float)delta2.z;
                ss.gs = 1f;
                sailCache.OrbitId = orbitId;
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
            for (var i = len - 1; i >= 0; i--)
            {
                __instance.AddSolarSail(cache[i].Sail, cache[i].OrbitId, deadline);
            }
        }
    }
    
    private static class SkipAbsorbPatch
    {
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
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ret)
            );
            return matcher.InstructionEnumeration();
        }
    }
    
    private static class QuickAbsortPatch
    {
        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(DysonSphereLayer), "GameTick")]
        // public static void DysonSphereLayerGameTick(ref DysonSphereLayer __instance, long gameTick)
        // {
        //     DysonSwarm swarm = __instance.dysonSphere.swarm;
        //     for (int i = __instance.nodeCursor - 1; i > 0; i--)
        //     {
        //         DysonNode dysonNode = __instance.nodePool[i];
        //         if (dysonNode != null && dysonNode.id == i && dysonNode.sp == dysonNode.spMax)
        //         {
        //             dysonNode.OrderConstructCp(gameTick, swarm);
        //         }
        //     }
        // }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonSphereLayer), nameof(DysonSphereLayer.GameTick))]
        private static IEnumerable<CodeInstruction> DysonSphereLayer_GameTick_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.Start().InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuickAbsortPatch), nameof(QuickAbsortPatch.DoAbsorb)))
            ).MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphereLayer), nameof(DysonSphereLayer.dysonSphere))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphere), nameof(DysonSphere.swarm)))
            ).Insert(new CodeInstruction(OpCodes.Ret));
            return matcher.InstructionEnumeration();
        }
        
        private static void DoAbsorb(DysonSphereLayer layer, long gameTick)
        {
            var swarm = layer.dysonSphere.swarm;
            for (var i = layer.nodeCursor - 1; i > 0; i--)
            {
                var node = layer.nodePool[i];
                if (node != null && node.id == i && node.sp == node.spMax)
                {
                    node.OrderConstructCp(gameTick, swarm);
                }
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
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.localRot)))
            );
            var start = matcher.Pos - 6;
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
}
