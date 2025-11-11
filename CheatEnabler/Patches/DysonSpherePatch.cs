using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace CheatEnabler.Patches;

public class DysonSpherePatch : PatchImpl<DysonSpherePatch>
{
    public static ConfigEntry<bool> SkipBulletEnabled;
    public static ConfigEntry<bool> FireAllBulletsEnabled;
    public static ConfigEntry<bool> SkipAbsorbEnabled;
    public static ConfigEntry<bool> QuickAbsorbEnabled;
    public static ConfigEntry<bool> EjectAnywayEnabled;
    public static ConfigEntry<bool> OverclockEjectorEnabled;
    public static ConfigEntry<bool> OverclockSiloEnabled;
    public static ConfigEntry<bool> UnlockMaxOrbitRadiusEnabled;
    public static ConfigEntry<float> UnlockMaxOrbitRadiusValue;
    private static bool _instantAbsorb;

    public static void Init()
    {
        SkipBulletEnabled.SettingChanged += (_, _) => SkipBulletPatch.Enable(SkipBulletEnabled.Value);
        SkipAbsorbEnabled.SettingChanged += (_, _) => SkipAbsorbPatch.Enable(SkipAbsorbEnabled.Value);
        QuickAbsorbEnabled.SettingChanged += (_, _) => QuickAbsorbPatch.Enable(QuickAbsorbEnabled.Value);
        EjectAnywayEnabled.SettingChanged += (_, _) => EjectAnywayPatch.Enable(EjectAnywayEnabled.Value);
        OverclockEjectorEnabled.SettingChanged += (_, _) => OverclockEjector.Enable(OverclockEjectorEnabled.Value);
        OverclockSiloEnabled.SettingChanged += (_, _) => OverclockSilo.Enable(OverclockSiloEnabled.Value);
        UnlockMaxOrbitRadiusEnabled.SettingChanged += (_, _) => UnlockMaxOrbitRadius.Enable(UnlockMaxOrbitRadiusEnabled.Value);

        FireAllBulletsEnabled.SettingChanged += (_, _) => SkipBulletPatch.SetFireAllBullets(FireAllBulletsEnabled.Value);
    }

    public static void Start()
    {
        SkipBulletPatch.Enable(SkipBulletEnabled.Value);
        SkipAbsorbPatch.Enable(SkipAbsorbEnabled.Value);
        QuickAbsorbPatch.Enable(QuickAbsorbEnabled.Value);
        EjectAnywayPatch.Enable(EjectAnywayEnabled.Value);
        OverclockEjector.Enable(OverclockEjectorEnabled.Value);
        OverclockSilo.Enable(OverclockSiloEnabled.Value);
        UnlockMaxOrbitRadius.Enable(UnlockMaxOrbitRadiusEnabled.Value);
        Enable(true);
        SkipBulletPatch.SetFireAllBullets(FireAllBulletsEnabled.Value);
    }

    public static void Uninit()
    {
        Enable(false);
        SkipBulletPatch.Enable(false);
        SkipAbsorbPatch.Enable(false);
        QuickAbsorbPatch.Enable(false);
        EjectAnywayPatch.Enable(false);
        OverclockEjector.Enable(false);
        OverclockSilo.Enable(false);
        UnlockMaxOrbitRadius.Enable(false);
    }

    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(DysonShell), nameof(DysonShell.ImportFromBlueprint))]
    // private static void DysonShell_ImportFromBlueprint_Postfix(DysonShell __instance)
    // {
    //     CheatEnabler.Logger.LogDebug($"[DysonShell.ImportFromBlueprint] vertCount={__instance.vertexCount}, cpPerVertex={__instance.cpPerVertex}, cpMax={__instance.cellPointMax}");
    // }

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
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(ExpiryOrder), nameof(ExpiryOrder.time)))
        ).Advance(1).Insert(
            // node.cpOrdered = node.cpOrdered + 1;
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode.cpOrdered))),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Add),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(DysonNode), nameof(DysonNode.cpOrdered)))
        );
        return matcher.InstructionEnumeration();
    }

    private class SkipBulletPatch : PatchImpl<SkipBulletPatch>
    {
        private static long _sailLifeTime;
        private static DysonSailCache[][] _sailsCache;
        private static int[] _sailsCacheLen, _sailsCacheCapacity;
        private static bool _fireAllBullets;

        public static void SetFireAllBullets(bool value)
        {
            _fireAllBullets = value;
        }

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

        protected override void OnEnable()
        {
            UpdateSailLifeTime();
            UpdateSailsCacheForThisGame();
            GameLogicProc.OnGameBegin += OnGameBegin;
        }

        protected override void OnDisable()
        {
            GameLogicProc.OnGameBegin -= OnGameBegin;
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

        private static void OnGameBegin()
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
                new CodeMatch(OpCodes.Ldc_I4_M1),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.direction)))
            ).Advance(2);
            var end = matcher.Pos;
            matcher.Start().Advance(start).RemoveInstructions(end - start).Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_3),
                new CodeInstruction(OpCodes.Ldloc_S, 9),
                new CodeInstruction(OpCodes.Ldloc_S, 11),
                new CodeInstruction(OpCodes.Ldarg_S, 6),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkipBulletPatch), nameof(SkipBulletPatch.AddDysonSail)))
            );
            return matcher.InstructionEnumeration();
        }

        private static void AddDysonSail(ref EjectorComponent ejector, DysonSwarm swarm, VectorLF3 uPos, VectorLF3 endVec, int[] consumeRegister)
        {
            var index = swarm.starData.index;
            var orbitId = ejector.orbitId;
            var delta1 = endVec - swarm.starData.uPosition;
            var delta2 = VectorLF3.Cross(endVec - uPos, swarm.orbits[orbitId].up).normalized * Math.Sqrt(swarm.dysonSphere.gravity / swarm.orbits[orbitId].radius);
            var bulletCount = ejector.bulletCount;
            lock (swarm)
            {
                var cache = _sailsCache[index];
                var len = _sailsCacheLen[index];
                if (cache == null)
                {
                    SetSailsCacheCapacity(index, 256);
                    cache = _sailsCache[index];
                }
                if (_fireAllBullets)
                {
                    var capacity = _sailsCacheCapacity[index];
                    var leastCapacity = len + bulletCount;
                    if (leastCapacity > capacity)
                    {
                        do
                        {
                            capacity *= 2;
                        } while (leastCapacity > capacity);
                        SetSailsCacheCapacity(index, capacity);
                        cache = _sailsCache[index];
                    }
                    _sailsCacheLen[index] = len + bulletCount;
                    var end = len + bulletCount;
                    for (var i = len; i < end; i++)
                        cache[i].FromData(delta1, delta2 + RandomTable.SphericNormal(ref swarm.randSeed, 0.5), orbitId);
                }
                else
                {
                    var capacity = _sailsCacheCapacity[index];
                    if (len >= capacity)
                    {
                        SetSailsCacheCapacity(index, capacity * 2);
                        cache = _sailsCache[index];
                    }
                    _sailsCacheLen[index] = len + 1;
                    cache[len].FromData(delta1, delta2 + RandomTable.SphericNormal(ref swarm.randSeed, 0.5), orbitId);
                }
            }

            if (_fireAllBullets)
            {
                if (!ejector.incUsed)
                {
                    ejector.incUsed = ejector.bulletInc >= bulletCount;
                }
                ejector.bulletInc = 0;
                ejector.bulletCount = 0;
                lock (consumeRegister)
                {
                    consumeRegister[ejector.bulletId] += bulletCount;
                }
            }
            else
            {
                var inc = ejector.bulletInc / bulletCount;
                if (!ejector.incUsed)
                {
                    ejector.incUsed = inc > 0;
                }
                ejector.bulletInc -= inc;
                ejector.bulletCount = bulletCount - 1;
                if (ejector.bulletCount == 0)
                {
                    ejector.bulletInc = 0;
                }
                lock (consumeRegister)
                {
                    consumeRegister[ejector.bulletId]++;
                }
            }
            ejector.time = ejector.coldSpend;
            ejector.direction = -1;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.GameTick))]
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
                var sphereProductRegister = sphere.productRegister;
                var sphereConsumeRegister = sphere.consumeRegister;
                if (llen > 0)
                {
                    var lidx = ((int)time >> 4) % llen;
                    for (var i = llen - 1; i >= 0; i--)
                    {
                        var layer = layers[(lidx + i) % llen];
                        var nodes = layer.nodePool;
                        var nlen = layer.nodeCursor - 1;
                        if (nlen <= 0) continue;
                        var nidx = (int)time % nlen;
                        for (var j = nlen; j > 0; j--)
                        {
                            var nodeIdx = (nidx + j) % nlen + 1;
                            var node = nodes[nodeIdx];
                            if (node == null || node.id != nodeIdx || node.sp < node.spMax) continue;
                            while (node.cpReqOrder > 0)
                            {
                                node.cpOrdered++;
                                if (node.ConstructCp() == null) break;
                                if (idx == 0)
                                {
                                    sphereProductRegister[ProductionStatistics.SOLAR_SAIL_ID] += len;
                                    sphereConsumeRegister[ProductionStatistics.SOLAR_SAIL_ID] += len;
                                    sphereProductRegister[ProductionStatistics.DYSON_CELL_ID] += len;
                                    return;
                                }
                                idx--;
                            }
                        }
                    }
                }
                var absorbCnt = len - 1 - idx;
                if (absorbCnt > 0)
                {
                    sphereProductRegister[ProductionStatistics.SOLAR_SAIL_ID] += absorbCnt;
                    sphereConsumeRegister[ProductionStatistics.SOLAR_SAIL_ID] += absorbCnt;
                    sphereProductRegister[ProductionStatistics.DYSON_CELL_ID] += absorbCnt;
                }
            }
            for (; idx >= 0; idx--)
            {
                __instance.AddSolarSail(cache[idx].Sail, cache[idx].OrbitId, deadline);
            }
        }
    }

    private class SkipAbsorbPatch : PatchImpl<SkipAbsorbPatch>
    {
        protected override void OnEnable()
        {
            _instantAbsorb = QuickAbsorbEnabled.Value;
        }

        protected override void OnDisable()
        {
            _instantAbsorb = false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.AbsorbSail))]
        private static IEnumerable<CodeInstruction> DysonSwarm_AbsorbSail_Transpiler2(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            var label1 = generator.DefineLabel();
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(ExpiryOrder), nameof(ExpiryOrder.index)))
            ).Advance(1).RemoveInstructions(matcher.Length - matcher.Pos).Insert(
                // if (node.ConstructCp() != null)
                // {
                //     this.dysonSphere.productRegister[ProductionStatistics.DYSON_CELL_ID]++;
                // }
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(DysonNode), nameof(DysonNode.ConstructCp))),
                new CodeInstruction(OpCodes.Brfalse_S, label1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSwarm), nameof(DysonSwarm.dysonSphere))),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(DysonSphere), nameof(DysonSphere.productRegister))),
                new CodeInstruction(OpCodes.Ldc_I4, ProductionStatistics.DYSON_CELL_ID),
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

    private class QuickAbsorbPatch : PatchImpl<QuickAbsorbPatch>
    {
        protected override void OnEnable()
        {
            _instantAbsorb = SkipAbsorbEnabled.Value;
        }

        protected override void OnDisable()
        {
            _instantAbsorb = false;
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
            var nodeCount = layer.nodeCursor - 1;
            if (nodeCount <= 0) return;
            var nodes = layer.nodePool;
            var swarm = layer.dysonSphere.swarm;
            var delta = ((int)gameTick >> 6) % nodeCount;
            for (var i = nodeCount - ((int)gameTick & 0x3F); i > 0; i -= 0x40)
            {
                var idx = (delta + i) % nodeCount + 1;
                var node = nodes[idx];
                if (node == null || node.id != idx || node.sp < node.spMax) continue;
                for (var j = node.cpReqOrder; j > 0; j--)
                {
                    if (!swarm.AbsorbSail(node, gameTick)) return; // No more sails can be absorbed
                }
            }
        }
    }

    private class EjectAnywayPatch : PatchImpl<EjectAnywayPatch>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.End().MatchBack(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Ldc_R8 && Math.Abs((double)instr.operand - 0.08715574) < 0.00000001)
            );
            var start = matcher.Pos - 3;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.bulletCount)))
            );
            var end = matcher.Pos;
            matcher.Start().Advance(start).RemoveInstructions(end - start).MatchForward(false,
                new CodeMatch(ci => ci.IsStloc())
            ).Advance(1);
            start = matcher.Pos;
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.autoOrbit)))
            );
            end = matcher.Pos;
            matcher.Start().Advance(start).RemoveInstructions(end - start);
            return matcher.InstructionEnumeration();
        }
    }

    private class OverclockEjector : PatchImpl<OverclockEjector>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> EjectComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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

    private class OverclockSilo : PatchImpl<OverclockSilo>
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> SiloComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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

    private class UnlockMaxOrbitRadius : PatchImpl<UnlockMaxOrbitRadius>
    {
        protected override void OnEnable()
        {
            OnViewStarChange(null, null);
            UnlockMaxOrbitRadiusValue.SettingChanged += OnViewStarChange;
        }

        protected override void OnDisable()
        {
            OnViewStarChange(null, null);
            UnlockMaxOrbitRadiusValue.SettingChanged -= OnViewStarChange;
        }

        public static void OnViewStarChange(object o, EventArgs e)
        {
            var dysonEditor = UIRoot.instance?.uiGame?.dysonEditor;
            if (dysonEditor == null || !dysonEditor.gameObject.activeSelf) return;
            dysonEditor.selection?.onViewStarChange?.Invoke();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.CheckLayerRadius))]
        [HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.CheckSwarmRadius))]
        [HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.QueryLayerRadius))]
        [HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.QuerySwarmRadius))]
        [HarmonyPatch(typeof(UIDEAddLayerDialogue), nameof(UIDEAddLayerDialogue.OnViewStarChange))]
        [HarmonyPatch(typeof(UIDEAddSwarmDialogue), nameof(UIDEAddSwarmDialogue.OnViewStarChange))]
        [HarmonyPatch(typeof(UIDysonEditor), nameof(UIDysonEditor.OnViewStarChange))]
        [HarmonyPatch(typeof(UIDESwarmOrbitInfo), nameof(UIDESwarmOrbitInfo._OnInit))]
        [HarmonyPatch(typeof(UIDysonOrbitPreview), nameof(UIDysonOrbitPreview.UpdateAscNodeGizmos))]
        private static IEnumerable<CodeInstruction> MaxOrbitRadiusPatch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphere), nameof(DysonSphere.maxOrbitRadius)))
            );
            matcher.Repeat(m =>
            {
                m.Advance(1).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DysonSpherePatch), nameof(UnlockMaxOrbitRadiusValue))),
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<float>), nameof(ConfigEntry<float>.Value)))
                );
            });
            return matcher.InstructionEnumeration();
        }
    }
}
