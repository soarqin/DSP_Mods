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
    private new static readonly BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private static int _veinEnergyRatio = 200000;
    private static int _waterEnergyRatio = 50000;
    private static int _waterSpeed = 100;
    private static int _miningScale = 100;

    private static float _frame;
    private static float _miningCostRate;
    private static uint _miningCostBarrier;

    private static uint _seed = (uint)Random.Range(int.MinValue, int.MaxValue);
    private static readonly Dictionary<int, List<int>> Veins = new();
    private static readonly Dictionary<int, float> FrameNext = new();

    private bool _cfgEnabled = true;

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        _veinEnergyRatio = Config.Bind("General", "EnergyConsumptionEachVein", _veinEnergyRatio / 1000, "200 for default. Energy consumption for each vein(in kJ)").Value * 1000;
        _waterEnergyRatio = Config.Bind("General", "EnergyConsumptionEachWater", _waterEnergyRatio / 1000, "50 for default. Energy consumption for each water(in kJ)").Value * 1000;
        _waterSpeed = Config.Bind("General", "WaterMiningSpeed", _waterSpeed, "100 for default. Water mining speed (count per second)").Value;
        _miningScale = Config.Bind("General", "MiningScale", _miningScale, "100 for default. Must not be less than 100. Mining scale(in percents) for slots nearly empty (mining scale will slowly reduce to 1 till reach half of slot limits)").Value;
        if (_miningScale < 100)
        {
            _miningScale = 100;
        }
        if (!_cfgEnabled) return;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        Harmony.CreateAndPatchAll(typeof(LogisticMiner));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DSPGame), "StartGame", typeof(GameDesc))]
    [HarmonyPatch(typeof(DSPGame), "StartGame", typeof(string))]
    private static void OnGameStart()
    {
        Logger.LogInfo("Game Start");
        FrameNext.Clear();
        _frame = 0f;
        Veins.Clear();
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
        foreach(var key in FrameNext.Keys.ToList())
        {
            FrameNext[key] -= 1000000f;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetFactory), "Init")]
    [HarmonyPatch(typeof(PlanetFactory), "RecalculateVeinGroup")]
    [HarmonyPatch(typeof(PlanetFactory), "RecalculateAllVeinGroups")]
    private static void NeedRecalcVeins(PlanetFactory __instance)
    {
        RecalcVeins(__instance);
    }

    private static void RecalcVeins(PlanetFactory factory)
    {
        VeinData[] veinPool = factory.veinPool;
        var planetId = factory.planetId;
        /* remove planet veins from dict */
        Veins.Keys.Where(key => (key >> 16) == planetId).ToList().ForEach(key => Veins.Remove(key));
        /* re-add all veins to dict */
        for (var i = 0; i < veinPool.Length; i++)
        {
            var veinData = veinPool[i];
            if (veinData.amount > 0 && veinData.type > EVeinType.None)
            {
                AddVeinData(planetId, veinData.productId, i);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FactorySystem), "CheckBeforeGameTick")]
    private static void Miner(FactorySystem __instance)
    {
        var history = GameMain.history;
        var miningSpeedScale = history.miningSpeedScale;
        if (miningSpeedScale <= 0f)
            return;
        var factory = __instance.factory;
        var planetId = factory.planetId;
        if (FrameNext.TryGetValue(planetId, out var frameNext))
        {
            if (frameNext > _frame)
                return;
        }
        else
        {
            FrameNext[planetId] = _frame + 60f / miningSpeedScale;
            return;
        }

        var miningFrames = 60f / miningSpeedScale;
        var miningCostRate = history.miningCostRate;
        if (!miningCostRate.Equals(_miningCostRate))
        {
            _miningCostRate = miningCostRate;
            _miningCostBarrier = (uint)(int)Math.Ceiling(2147483646.0 * miningCostRate);
        }
        var key0 = planetId << 16;
        var veinPool = factory.veinPool;
        var planetTransport = __instance.planet.factory.transport;
        var factoryProductionStat =
            GameMain.statistics.production.factoryStatPool[__instance.factory.index];
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

                    var isVein = Veins.TryGetValue(key0 | stationStore.itemId, out var val);
                    var isVeinOrWater = isVein || stationStore.itemId == __instance.planet.waterItemId;
                    if (!isVeinOrWater) continue;
                    int amount;
                    int energyRatio;
                    if (isVein)
                    {
                        if (stationComponent.energy < _veinEnergyRatio)
                        {
                            isCollecting = true;
                            k = int.MaxValue - 1;
                            continue;
                        }

                        if (veinPool[val.First()].type == EVeinType.Oil)
                            amount = (int)val
                                .Where(item => item < veinPool.Length && veinPool[item].type > EVeinType.None)
                                .Sum(item => veinPool[item].amount * VeinData.oilSpeedMultiplier * 2f);
                        else
                            amount = val.Sum(
                                item => GetMine(veinPool, item, __instance.planet.factory) ? 1 : 0);
                        energyRatio = _veinEnergyRatio;
                    }
                    else
                    {
                        if (stationComponent.energy < _waterEnergyRatio)
                        {
                            isCollecting = true;
                            k = int.MaxValue - 1;
                            continue;
                        }

                        amount = _waterSpeed;
                        energyRatio = _waterEnergyRatio;
                    }

                    if (amount == 0) continue;
                    isCollecting = true;
                    var energyConsume = (int)Math.Ceiling(energyRatio * amount / miningSpeedScale);
                    if (energyConsume > stationComponent.energy)
                    {
                        amount = (int)(stationComponent.energy * miningSpeedScale / energyRatio);
                        energyConsume = (int)Math.Ceiling(energyRatio * amount / miningSpeedScale);
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

            frameNext += miningFrames;
        } while (frameNext <= _frame);
        PerformanceMonitor.EndSample(ECpuWorkEntry.Miner);
        FrameNext[planetId] = frameNext;
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

        short groupIndex;
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
                    groupIndex = veinDatas[index].groupIndex;
                    factory.veinGroups[groupIndex].count--;
                    factory.RemoveVeinWithComponents(index);
                    factory.RecalculateVeinGroup(groupIndex);
                }
            }

            return true;
        }

        groupIndex = veinDatas[index].groupIndex;
        factory.veinGroups[groupIndex].count--;
        factory.RemoveVeinWithComponents(index);
        factory.RecalculateVeinGroup(groupIndex);
        return false;
    }
}
