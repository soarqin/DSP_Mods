using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Random = UnityEngine.Random;

namespace LogisticMiner;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class LogisticMiner : BaseUnityPlugin
{
    private static LogisticMiner _this;
    private const int VeinEnergyRatio = 200000;
    private const int WaterEnergyRatio = 50000;
    private const int WaterSpeed = 100;

    private static float _frame, _nextFrame;
    private static float _miningCostRate;
    private static uint _miningCostBarrier;

    private static uint _seed = (uint)Random.Range(int.MinValue, int.MaxValue);
    private static readonly Dictionary<int, List<int>> Veins = new();

    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        _this = this;
    }

    private void Start()
    {
        Harmony.CreateAndPatchAll(typeof(LogisticMiner));
    }

    private void FixedUpdate()
    {
        _frame++;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetFactory), "Init")]
    [HarmonyPatch(typeof(PlanetFactory), "RecalculateVeinGroup")]
    [HarmonyPatch(typeof(PlanetFactory), "RecalculateAllVeinGroups")]
    private static void NeedRecalcVeins(PlanetFactory __instance)
    {
        VeinData[] veinPool = __instance.veinPool;
        Veins.Clear();
        for (var i = 0; i < veinPool.Length; i++)
        {
            var veinData = veinPool[i];
            if (veinData.amount > 0 && veinData.type > EVeinType.None)
            {
                AddVeinData(__instance.planetId, veinData.productId, i);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FactorySystem), "GameTickLabResearchMode")]
    private static void Miner(FactorySystem __instance)
    {
        if (_nextFrame > _frame)
            return;
        var history = GameMain.history;
        var miningSpeedScale = history.miningSpeedScale;
        if (miningSpeedScale <= 0f)
            return;
        var miningCostRate = history.miningCostRate;
        if (!miningCostRate.Equals(_miningCostRate))
        {
            _miningCostRate = miningCostRate;
            _miningCostBarrier = (uint)(int)Math.Ceiling(2147483646.0 * miningCostRate);
        }

        var miningFrames = 60f / miningSpeedScale;
        var factory = __instance.factory;
        var key0 = factory.planetId << 16;
        var veinPool = factory.veinPool;
        var planetTransport = __instance.planet.factory.transport;
        var factoryProductionStat =
            GameMain.statistics.production.factoryStatPool[__instance.factory.index];
        var productRegister = factoryProductionStat?.productRegister;
        do
        {
            for (var j = 1; j < planetTransport.stationCursor; j++)
            {
                var stationComponent = planetTransport.stationPool[j];
                if (stationComponent == null) continue;
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

                    var isVein = Veins.TryGetValue(key0 | stationStore.itemId, out var val);
                    var isVeinOrWater = isVein || stationStore.itemId == __instance.planet.waterItemId;
                    if (!isVeinOrWater) continue;
                    int amount;
                    int energyRatio;
                    if (isVein)
                    {
                        if (stationComponent.energy < VeinEnergyRatio)
                        {
                            isCollecting = true;
                            k = int.MaxValue - 1;
                            continue;
                        }

                        if (veinPool[val.First()].type == EVeinType.Oil)
                            amount = (int)val
                                .Where(item => veinPool.Length > item && veinPool[item].type > EVeinType.None)
                                .Sum(item => veinPool[item].amount / 6000f);
                        else
                            amount = val.Sum(
                                item2 => GetMine(veinPool, item2, __instance.planet.factory) ? 1 : 0);
                        energyRatio = VeinEnergyRatio;
                    }
                    else
                    {
                        if (stationComponent.energy < WaterEnergyRatio)
                        {
                            isCollecting = true;
                            k = int.MaxValue - 1;
                            continue;
                        }

                        amount = WaterSpeed;
                        energyRatio = WaterEnergyRatio;
                    }

                    if (amount == 0) continue;
                    isCollecting = true;
                    var energyConsume = energyRatio * amount;
                    if (energyConsume > stationComponent.energy)
                    {
                        amount = (int)(stationComponent.energy / energyRatio);
                        energyConsume = energyRatio * amount;
                    }

                    stationStore.count += amount;
                    if (factoryProductionStat != null)
                        productRegister[stationStore.itemId] += amount;
                    stationComponent.energy -= energyConsume;
                }

                if (isCollecting && stationComponent.energyMax > stationComponent.energy * 2)
                {
                    var index = storage.Length - 2;
                    var fuelCount = storage[index].count;
                    if (fuelCount > 0)
                    {
                        var heatValue = LDB.items.Select(storage[index].itemId).HeatValue;
                        if (heatValue > 0)
                        {
                            var count = (int)((stationComponent.energyMax - stationComponent.energy) /
                                              heatValue);
                            if (count > fuelCount)
                                count = fuelCount;
                            storage[index].count -= count;
                            stationComponent.energy += count * heatValue;
                        }
                    }
                }
            }

            _nextFrame += miningFrames;
        } while (_nextFrame <= _frame);

        if (_frame >= 1000000f)
        {
            _frame -= 1000000f;
            _nextFrame -= 1000000f;
        }
    }

    private static void AddVeinData(int planetId, int productId, int index)
    {
        var key = (planetId << 16) + productId;
        if (Veins.TryGetValue(key, out var val))
            val.Add(index);
        else
            Veins.Add(key, new List<int> { index });
    }

    private static bool GetMine(VeinData[] veinDatas, int index, PlanetFactory factory)
    {
        if (veinDatas.Length == 0 || veinDatas[index].type == EVeinType.None)
            return false;

        if (veinDatas[index].amount > 0)
        {
            bool flag = true;
            if (_miningCostBarrier < 2147483646u)
            {
                _seed = (uint)((int)((ulong)((_seed % 2147483646u + 1) * 48271L) % 2147483647uL) - 1);
                flag = _seed < _miningCostBarrier;
            }

            if (flag)
            {
                veinDatas[index].amount--;
                factory.veinGroups[veinDatas[index].groupIndex].amount--;
                if (veinDatas[index].amount <= 0)
                {
                    short groupIndex = veinDatas[index].groupIndex;
                    factory.veinGroups[groupIndex].count--;
                    factory.RemoveVeinWithComponents(index);
                    factory.RecalculateVeinGroup(groupIndex);
                }
            }

            return true;
        }

        short groupIndex2 = veinDatas[index].groupIndex;
        factory.veinGroups[groupIndex2].count--;
        factory.RemoveVeinWithComponents(index);
        factory.RecalculateVeinGroup(groupIndex2);
        return false;
    }
}