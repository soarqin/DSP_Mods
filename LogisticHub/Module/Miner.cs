using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using Random = UnityEngine.Random;

namespace LogisticHub.Module;

public class Miner : PatchImpl<Miner>
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
    private static int[] _mineIndex;

    private static uint _miningSeed = (uint)Random.Range(0, int.MaxValue);

    private static readonly (int, int)[] VeinList =
    [
        (0, 1000),
        (1, 1001),
        (2, 1002),
        (3, 1003),
        (4, 1004),
        (5, 1005),
        (6, 1006),
        (7, 1007),
        (11, 1011),
        (12, 1012),
        (13, 1013),
        (14, 1014),
        (15, 1015),
        (16, 1016)
    ];

    public static void Init()
    {
        Enabled.SettingChanged += (_, _) => { Enable(Enabled.Value); };

        Enable(Enabled.Value);
    }

    public static void Uninit()
    {
        Enable(false);
    }

    protected override void OnEnable()
    {
        GameLogic.OnGameBegin += OnGameBegin;
    }

    protected override void OnDisable()
    {
        GameLogic.OnGameBegin -= OnGameBegin;
    }

    private static void OnGameBegin()
    {
        VeinManager.Clear();
        _frame = 0f;
        UpdateMiningCostRate();
        UpdateSpeedScale();
        CheckRecipes();
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
        _miningCostBarrierOil = (uint)(int)Math.Ceiling(2147483646.0 * _miningCostRateByTech * 0.401116669f / Math.Max(DSPGame.GameDesc.resourceMultiplier, 0.416666657f));
    }

    private static void UpdateSpeedScale()
    {
        _miningSpeedScaleByTech = GameMain.history.miningSpeedScale;
        _miningSpeedScaleLong = (long)(_miningSpeedScaleByTech * 100);
        _miningFrames = _miningSpeedScaleByTech * 600000f;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.UnlockTechFunction))]
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameData), "GameTick")]
    private static void GameData_GameTick_Prefix()
    {
        var main = GameMain.instance;
        if (main.isMenuDemo) return;
        if (_miningSpeedScaleLong <= 0) return;

        PerformanceMonitor.BeginSample(ECpuWorkEntry.Miner);
        if (main.timei % 60 != 0) return;
        _frame += _miningFrames;
        var frameCounter = Mathf.FloorToInt(_frame / 1200000f);
        if (frameCounter <= 0) return;
        _frame -= frameCounter * 1200000f;
        // LogisticHub.Logger.LogDebug($"FrameCounter: {frameCounter}");

        var data = GameMain.data;
        for (var factoryIndex = data.factoryCount - 1; factoryIndex >= 0; factoryIndex--)
        {
            var factory = data.factories[factoryIndex];
            var veins = VeinManager.GetVeins(factoryIndex);
            if (veins == null) return;
            var stations = StationManager.GetStations(factoryIndex);
            var planetTransport = factory.transport;
            var factoryProductionStat = GameMain.statistics.production.factoryStatPool[factoryIndex];
            var productRegister = factoryProductionStat?.productRegister;
            var demands = stations.StorageIndices[1];
            if (_mineIndex == null || factoryIndex >= _mineIndex.Length)
                Array.Resize(ref _mineIndex, factoryIndex + 1);
            foreach (var (itemIndex, itemId) in VeinList)
            {
                foreach (var storageIndex in demands[itemIndex])
                {
                    var station = planetTransport.stationPool[storageIndex / 100];
                    if (station == null)
                        continue;
                    ref var storage = ref station.storage[storageIndex % 100];
                    int amount;
                    long energyConsume;
                    var miningScale = MiningScale.Value;
                    if (miningScale == 0)
                    {
                        miningScale = _advancedMiningMachineUnlocked ? 300 : 100;
                    }

                    if (miningScale > 100 && storage.count * 2 > storage.max)
                    {
                        miningScale = 100 + ((miningScale - 100) * (storage.max - storage.count) * 2 + storage.max - 1) / storage.max;
                    }

                    if (itemIndex > 0)
                    {
                        (amount, energyConsume) = Mine(factory, veins, itemId, miningScale, frameCounter, station.energy);
                        if (amount < 0) continue;
                    }
                    else
                    {
                        energyConsume = (WaterEnergyConsume.Value * frameCounter * miningScale * miningScale + 9999L) / 10000L;
                        if (station.energy < energyConsume) continue;
                        amount = WaterSpeed.Value * miningScale / 100;
                    }

                    if (amount <= 0) continue;
                    storage.count += amount;
                    if (factoryProductionStat != null)
                        productRegister[itemId] += amount;
                    station.energy -= energyConsume;
                }
            }

            for (var i = planetTransport.stationCursor - 1; i > 0; i--)
            {
                var stationComponent = planetTransport.stationPool[i];
                if (stationComponent.isCollector || stationComponent.isVeinCollector || stationComponent.energy * 2 >= stationComponent.energyMax) continue;
                var index = (stationComponent.isStellar ? FuelIlsSlot.Value : FuelPlsSlot.Value) - 1;
                var storage = stationComponent.storage;
                if (index < 0 || index >= storage.Length)
                    continue;
                var fuelCount = storage[index].count;
                if (fuelCount == 0) continue;
                var (heat, prod) = AuxData.Fuels[storage[index].itemId];
                if (heat <= 0)
                    continue;
                /* Sprayed fuels */
                int pretendIncLevel;
                if (prod && (pretendIncLevel = storage[index].inc / storage[index].count) > 0)
                {
                    var count = (int)((stationComponent.energyMax - stationComponent.energy) * 1000L / Cargo.incTable[pretendIncLevel] / 7L);
                    if (count > fuelCount)
                        count = fuelCount;
                    var incLevel = SplitIncLevel(ref storage[index].count, ref storage[index].inc, count);
                    if (incLevel > 10)
                        incLevel = 10;
                    stationComponent.energy += heat * count * (1000L + Cargo.incTable[incLevel]) / 1000L;
                }
                else
                {
                    var count = (int)((stationComponent.energyMax - stationComponent.energy) / heat);
                    if (count > fuelCount)
                        count = fuelCount;
                    SplitIncLevel(ref storage[index].count, ref storage[index].inc, count);
                    stationComponent.energy += heat * count;
                }
            }
        }

        PerformanceMonitor.EndSample(ECpuWorkEntry.Miner);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.UnlockRecipe))]
    private static void OnUnlockRecipe(int recipeId)
    {
        if (recipeId == 119)
        {
            CheckRecipes();
        }
    }

    private static (int, long) Mine(PlanetFactory factory, ProductVeinData[] allVeins, int productId, int percent, int counter, long energyMax)
    {
        var veins = allVeins[productId - 1000];
        if (veins == null)
            return (-1, -1L);

        var veinIndices = veins.VeinIndices;
        uint barrier;
        int limit;
        int count;
        long energy;
        var length = veinIndices.Count - 1;
        /* if is Oil */
        if (productId == 1007)
        {
            energy = (OilEnergyConsume.Value * length * percent * percent + 9999L) / 10000L;
            if (energy > energyMax)
                return (-1, -1L);
            var countf = 0f;
            var veinsPool = factory.veinPool;
            for (var i = length; i > 0; i--)
            {
                countf += veinsPool[veinIndices[i]].amount * 4 * VeinData.oilSpeedMultiplier;
            }

            count = ((int)countf * counter * percent + 99) / 100;
            if (count == 0)
                return (-1, -1L);
            barrier = _miningCostBarrierOil;
            limit = 2500;
        }
        else
        {
            count = (length * counter * percent + 99) / 100;
            if (count == 0)
                return (-1, -1L);
            energy = (OreEnergyConsume.Value * veins.GroupCount * percent * percent + 9999L) / 10000L;
            if (energy > energyMax)
                return (-1, -1L);
            barrier = _miningCostBarrier;
            limit = 0;
        }

        var veinsData = factory.veinPool;
        var total = 0;
        var factoryIndex = factory.index;
        var mineIndex = _mineIndex[factoryIndex];
        for (; count > 0; count--)
        {
            mineIndex = mineIndex % length + 1;
            var index = veinIndices[mineIndex];
            ref var vd = ref veinsData[index];
            int groupIndex;

            if (vd.amount <= 0)
            {
                groupIndex = vd.groupIndex;
                factory.veinGroups[groupIndex].count--;
                factory.RemoveVeinWithComponents(index);
                factory.RecalculateVeinGroup(groupIndex);
                length = veinIndices.Count - 1;
                if (length <= 0) break;
                continue;
            }

            total++;

            if (vd.amount <= limit) continue;
            var consume = true;
            if (barrier < 2147483646u)
            {
                _miningSeed = (uint)((int)((ulong)((_miningSeed % 2147483646u + 1) * 48271L) % 2147483647uL) - 1);
                consume = _miningSeed < barrier;
            }

            if (!consume) continue;

            vd.amount--;
            groupIndex = vd.groupIndex;
            factory.veinGroups[groupIndex].amount--;

            if (vd.amount > 0) continue;
            factory.veinGroups[groupIndex].count--;
            factory.RemoveVeinWithComponents(index);
            factory.RecalculateVeinGroup(groupIndex);
            length = veinIndices.Count - 1;
            if (length <= 0) break;
        }

        _mineIndex[factoryIndex] = mineIndex;

        return (total, energy);
    }
}