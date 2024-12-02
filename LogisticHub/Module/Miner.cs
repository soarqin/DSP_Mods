using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;
using Random = UnityEngine.Random;

namespace LogisticHub.Module;

public class Miner: PatchImpl<Miner>
{
    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<long> OreEnergyConsume;
    public static ConfigEntry<long> OilEnergyConsume;
    public static ConfigEntry<long> WaterEnergyConsume;
    public static ConfigEntry<int> WaterSpeed;
    public static ConfigEntry<int> MiningScale;
    public static ConfigEntry<int> FuelIlsSlot;
    public static ConfigEntry<int> FuelPlsSlot;

    private static float _frame;
    private static float _miningCostRateByTech;
    private static float _miningSpeedScaleByTech;
    private static float _miningFrames;
    private static long _miningSpeedScaleLong;
    private static bool _advancedMiningMachineUnlocked;
    private static uint _miningCostBarrier;
    private static uint _miningCostBarrierOil;

    private static uint _seed = (uint)Random.Range(int.MinValue, int.MaxValue);
    private static readonly Dictionary<int, VeinCacheData> PlanetVeinCacheData = new();
    private static readonly Dictionary<int, (long, bool)> Fuels = new();

    public static void Init()
    {
        Enable(Enabled.Value);
        Enabled.SettingChanged += (_, _) => Enable(Enabled.Value);
    }

    public static void Uninit()
    {
        Enable(false);
    }

    private static int SplitIncLevel(ref int n, ref int m, int p)
    {
        var level = m / n;
        var left = m - level * n;
        n -= p;
        left -= n;
        m -= left > 0 ? level * p + left : level * p;
        return level;
    }

    private static void CheckRecipes()
    {
        _advancedMiningMachineUnlocked = GameMain.history.recipeUnlocked.Contains(119);
    }

    private static void UpdateMiningCostRate()
    {
        _miningCostRateByTech = GameMain.history.miningCostRate;
        _miningCostBarrier = (uint)(int)Math.Ceiling(2147483646.0 * _miningCostRateByTech);
        _miningCostBarrierOil =
            (uint)(int)Math.Ceiling(2147483646.0 * _miningCostRateByTech * 0.401116669f /
                                    DSPGame.GameDesc.resourceMultiplier);
    }

    private static void UpdateSpeedScale()
    {
        _miningSpeedScaleByTech = GameMain.history.miningSpeedScale;
        _miningSpeedScaleLong = (long)(_miningSpeedScaleByTech * 100);
        lock (PlanetVeinCacheData)
        {
            _miningFrames = 120f / _miningSpeedScaleByTech;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DSPGame), "StartGame", typeof(GameDesc))]
    [HarmonyPatch(typeof(DSPGame), "StartGame", typeof(string))]
    private static void OnGameStart()
    {
        LogisticHub.Logger.LogInfo("Game Start");
        PlanetVeinCacheData.Clear();
        Fuels.Clear();
        foreach (var data in LDB.items.dataArray)
        {
            if (data.HeatValue > 0)
            {
                Fuels.Add(data.ID, (data.HeatValue, data.Productive));
            }
        }
        /* Thinking: storage max may affect mining scale?
        _localStationMax = LDB.items.Select(2103).prefabDesc.stationMaxItemCount;
        _remoteStationMax = LDB.items.Select(2104).prefabDesc.stationMaxItemCount;
        */
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameMain), "Start")]
    private static void OnGameLoaded()
    {
        _frame = 0f;

        UpdateMiningCostRate();
        UpdateSpeedScale();
        CheckRecipes();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameHistoryData), "UnlockTechFunction")]
    private static void OnUnlockTech(int func)
    {
        switch (func)
        {
            case 20:
                UpdateMiningCostRate();
                break;
            case 21:
                UpdateSpeedScale();
                break;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameData), "GameTick")]
    private static void FrameTick()
    {
        var main = GameMain.instance;
        if (main.isMenuDemo)
        {
            return;
        }

        _frame++;
        if (_frame <= 1000000f) return;
        /* keep precision of floats by limiting them <= 1000000f */
        _frame -= 1000000f;
        foreach (var pair in PlanetVeinCacheData)
        {
            pair.Value.FrameNext -= 1000000f;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameHistoryData), "UnlockRecipe")]
    private static void OnUnlockRecipe(int recipeId)
    {
        if (recipeId == 119)
        {
            CheckRecipes();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Init))]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RecalculateVeinGroup))]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RecalculateAllVeinGroups))]
    private static void NeedRecalcVeins(PlanetFactory instance)
    {
        RecalcVeins(instance);
    }

    private static void RecalcVeins(PlanetFactory factory)
    {
        var planetId = factory.planetId;
        lock (PlanetVeinCacheData)
        {
            /* remove planet veins from dict */
            if (PlanetVeinCacheData.TryGetValue(planetId, out var vcd))
            {
                vcd.GenVeins(factory);
            }
            else
            {
                vcd = new VeinCacheData();
                vcd.GenVeins(factory);
                vcd.FrameNext = _frame + _miningFrames;
                PlanetVeinCacheData.Add(planetId, vcd);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FactorySystem), "CheckBeforeGameTick")]
    private static void FactorySystemLogisticMiner(FactorySystem instance)
    {
        if (_miningSpeedScaleLong <= 0)
            return;
        var factory = instance.factory;
        var planetId = factory.planetId;
        lock (PlanetVeinCacheData)
        {
            if (PlanetVeinCacheData.TryGetValue(planetId, out var vcd))
            {
                if (vcd.FrameNext > _frame)
                    return;
            }
            else
            {
                PlanetVeinCacheData[planetId] = new VeinCacheData
                {
                    FrameNext = _frame + _miningFrames
                };
                return;
            }

            var planetTransport = instance.planet.factory.transport;
            var factoryProductionStat =
                GameMain.statistics.production.factoryStatPool[instance.factory.index];
            var productRegister = factoryProductionStat?.productRegister;
            PerformanceMonitor.BeginSample(ECpuWorkEntry.Miner);
            do
            {
                for (var j = 1; j < planetTransport.stationCursor; j++)
                {
                    var stationComponent = planetTransport.stationPool[j];
                    if (stationComponent == null) continue;
                    /* skip Orbital Collectors and Advanced Mining Machines */
                    if (stationComponent.isCollector || stationComponent.isVeinCollector) continue;
                    var storage = stationComponent.storage;
                    if (storage == null) continue;
                    var isCollecting = false;
                    for (var k = 0; k < stationComponent.storage.Length; k++)
                    {
                        ref var stationStore = ref storage[k];
                        if (stationStore.localLogic != ELogisticStorage.Demand ||
                            stationStore.max <= stationStore.count)
                            continue;

                        var isVein = vcd.HasVein(stationStore.itemId);
                        var isVeinOrWater = isVein || stationStore.itemId == instance.planet.waterItemId;
                        if (!isVeinOrWater) continue;
                        int amount;
                        long energyConsume;
                        isCollecting = true;
                        var miningScale = MiningScale.Value;
                        if (miningScale == 0)
                        {
                            miningScale = _advancedMiningMachineUnlocked ? 300 : 100;
                        }

                        if (miningScale > 100 && stationStore.count * 2 > stationStore.max)
                        {
                            miningScale = 100 +
                                          ((miningScale - 100) * (stationStore.max - stationStore.count) * 2 +
                                              stationStore.max - 1) / stationStore.max;
                        }

                        if (isVein)
                        {
                            (amount, energyConsume) = vcd.Mine(factory, stationStore.itemId, miningScale,
                                _miningSpeedScaleLong,
                                stationComponent.energy);
                            if (amount < 0)
                            {
                                k = int.MaxValue - 1;
                                continue;
                            }
                        }
                        else
                        {
                            energyConsume = (WaterEnergyConsume.Value * miningScale * miningScale + 9999L) / 100L /
                                            _miningSpeedScaleLong;
                            if (stationComponent.energy < energyConsume)
                            {
                                k = int.MaxValue - 1;
                                continue;
                            }

                            amount = WaterSpeed.Value * miningScale / 100;
                        }

                        if (amount <= 0) continue;
                        stationStore.count += amount;
                        if (factoryProductionStat != null)
                            productRegister[stationStore.itemId] += amount;
                        stationComponent.energy -= energyConsume;
                    }

                    if (!isCollecting || stationComponent.energy * 2 >= stationComponent.energyMax) continue;
                    var index = (stationComponent.isStellar ? FuelIlsSlot.Value : FuelPlsSlot.Value) - 1;
                    if (index < 0 || index >= storage.Length)
                        continue;
                    var fuelCount = storage[index].count;
                    if (fuelCount == 0) continue;
                    if (!Fuels.TryGetValue(storage[index].itemId, out var val) || val.Item1 <= 0)
                        continue;
                    /* Sprayed fuels */
                    int pretendIncLevel;
                    if (val.Item2 && (pretendIncLevel = storage[index].inc / storage[index].count) > 0)
                    {
                        var count = (int)((stationComponent.energyMax - stationComponent.energy) * 1000L /
                                          Cargo.incTable[pretendIncLevel] / 7L);
                        if (count > fuelCount)
                            count = fuelCount;
                        var incLevel = SplitIncLevel(ref storage[index].count, ref storage[index].inc, count);
                        if (incLevel > 10)
                            incLevel = 10;
                        stationComponent.energy += val.Item1 * count * (1000L + Cargo.incTable[incLevel]) / 1000L;
                    }
                    else
                    {
                        var count = (int)((stationComponent.energyMax - stationComponent.energy) / val.Item1);
                        if (count > fuelCount)
                            count = fuelCount;
                        SplitIncLevel(ref storage[index].count, ref storage[index].inc, count);
                        stationComponent.energy += val.Item1 * count;
                    }
                }

                vcd.FrameNext += _miningFrames;
            } while (vcd.FrameNext <= _frame);

            PerformanceMonitor.EndSample(ECpuWorkEntry.Miner);
        }
    }

    private class VeinCacheData
    {
        public float FrameNext;

        /* [0] indicates cout of veinGroups, [1..Last] store list of indices to VeinData*/
        private readonly Dictionary<int, List<int>> _veins = new();
        private int _mineIndex = -1;

        public bool HasVein(int productId)
        {
            return _veins.ContainsKey(productId);
        }

        public void GenVeins(PlanetFactory factory)
        {
            _veins.Clear();
            var veinPool = factory.veinPool;
            var vg = new Dictionary<int, HashSet<int>>();
            for (var i = 0; i < veinPool.Length; i++)
            {
                if (veinPool[i].amount <= 0 || veinPool[i].type == EVeinType.None) continue;
                var productId = veinPool[i].productId;
                if (_veins.TryGetValue(productId, out var l))
                {
                    l.Add(i);
                }
                else
                {
                    _veins.Add(productId, [0, i]);
                }

                if (vg.TryGetValue(productId, out var hs))
                {
                    hs.Add(veinPool[i].groupIndex);
                }
                else
                {
                    vg.Add(productId, [veinPool[i].groupIndex]);
                }
            }

            foreach (var pair in vg)
            {
                _veins[pair.Key][0] = pair.Value.Count;
            }
        }

        public (int, long) Mine(PlanetFactory factory, int productId, int percent, long miningSpeedScale, long energyMax)
        {
            if (!_veins.TryGetValue(productId, out var veins))
            {
                return (-1, -1L);
            }

            uint barrier;
            int limit;
            int count;
            long energy;
            var length = veins.Count - 1;
            /* if is Oil */
            if (productId == 1007)
            {
                energy = (OilEnergyConsume.Value * length * percent * percent + 9999L) / 100L / miningSpeedScale;
                if (energy > energyMax)
                    return (-1, -1L);
                var countf = 0f;
                var veinsPool = factory.veinPool;
                for (var i = length; i > 0; i--)
                {
                    countf += veinsPool[veins[i]].amount * 4 * VeinData.oilSpeedMultiplier;
                }

                count = ((int)countf * percent + 99) / 100;
                if (count == 0)
                    return (-1, -1L);
                barrier = _miningCostBarrierOil;
                limit = 2500;
            }
            else
            {
                count = (length * percent + 99) / 100;
                if (count == 0)
                    return (-1, -1L);
                energy = (OreEnergyConsume.Value * veins[0] * percent * percent + 9999L) / 100L / miningSpeedScale;
                if (energy > energyMax)
                    return (-1, -1L);
                barrier = _miningCostBarrier;
                limit = 0;
            }

            var veinsData = factory.veinPool;
            var total = 0;
            for (; count > 0; count--)
            {
                _mineIndex = _mineIndex % length + 1;
                var index = veins[_mineIndex];
                ref var vd = ref veinsData[index];
                int groupIndex;
                if (vd.amount > 0)
                {
                    total++;
                    if (vd.amount > limit)
                    {
                        var consume = true;
                        if (barrier < 2147483646u)
                        {
                            _seed = (uint)((int)((ulong)((_seed % 2147483646u + 1) * 48271L) % 2147483647uL) - 1);
                            consume = _seed < barrier;
                        }

                        if (consume)
                        {
                            vd.amount--;
                            groupIndex = vd.groupIndex;
                            factory.veinGroups[groupIndex].amount--;
                            if (vd.amount <= 0)
                            {
                                factory.veinGroups[groupIndex].count--;
                                factory.RemoveVeinWithComponents(index);
                                factory.RecalculateVeinGroup(groupIndex);
                                if (!_veins.TryGetValue(productId, out veins))
                                    break;
                                length = veins.Count - 1;
                            }
                        }
                    }

                    continue;
                }

                groupIndex = vd.groupIndex;
                factory.veinGroups[groupIndex].count--;
                factory.RemoveVeinWithComponents(index);
                factory.RecalculateVeinGroup(groupIndex);
                if (!_veins.TryGetValue(productId, out veins))
                    break;
                length = veins.Count - 1;
            }

            return (total, energy);
        }
    }
}
