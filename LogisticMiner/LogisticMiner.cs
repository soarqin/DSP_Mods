using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using Random = UnityEngine.Random;

namespace LogisticMiner;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class LogisticMiner : BaseUnityPlugin
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private static long _oreEnergyConsume = 2000000;
    private static long _oilEnergyConsume = 3600000;
    private static long _waterEnergyConsume = 20000000;
    private static int _waterSpeed = 100;
    private static int _miningScale = 100;

    private static float _frame;
    private static float _miningCostRate;
    private static uint _miningCostBarrier;
    private static uint _miningCostBarrierOil;

    private static uint _seed = (uint)Random.Range(int.MinValue, int.MaxValue);
    private static readonly Dictionary<int, VeinCacheData> PlanetVeinCacheData = new();

    private bool _cfgEnabled = true;

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        _oreEnergyConsume = Config.Bind("General", "EnergyConsumptionForOre", _oreEnergyConsume / 2000,
            "Energy consumption for each ore vein group(in kW)").Value * 2000;
        _oilEnergyConsume = Config.Bind("General", "EnergyConsumptionForOil", _oilEnergyConsume / 2000,
            "Energy consumption for each oil seep(in kW)").Value * 2000;
        _waterEnergyConsume = Config.Bind("General", "EnergyConsumptionForWater", _waterEnergyConsume / 2000,
            "Energy consumption for water slot(in kW)").Value * 2000;
        _waterSpeed = Config.Bind("General", "WaterMiningSpeed", _waterSpeed,
            "Water mining speed (count per second)").Value;
        _miningScale = Config.Bind("General", "MiningScale", _miningScale,
                "Must not be less than 100. Mining scale(in percents) for slots nearly empty, and the scale reduces to 1 smoothly till reach half of slot limits. Please note that the power consumption increases by the square of the scale which is the same as Advanced Mining Machine")
            .Value;
        if (_miningScale < 100)
        {
            _miningScale = 100;
        }

        if (!_cfgEnabled) return;
        Harmony.CreateAndPatchAll(typeof(LogisticMiner));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DSPGame), "StartGame", typeof(GameDesc))]
    [HarmonyPatch(typeof(DSPGame), "StartGame", typeof(string))]
    private static void OnGameStart()
    {
        Logger.LogInfo("Game Start");
        PlanetVeinCacheData.Clear();
        _frame = 0f;
        /* codes reserved for future use: storage max may affect mining scale
        _localStationMax = LDB.items.Select(2103).prefabDesc.stationMaxItemCount;
        _remoteStationMax = LDB.items.Select(2104).prefabDesc.stationMaxItemCount;
        */
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
    [HarmonyPatch(typeof(PlanetFactory), "Init")]
    [HarmonyPatch(typeof(PlanetFactory), "RecalculateVeinGroup")]
    [HarmonyPatch(typeof(PlanetFactory), "RecalculateAllVeinGroups")]
    private static void NeedRecalcVeins(PlanetFactory __instance)
    {
        RecalcVeins(__instance);
    }

    private static void RecalcVeins(PlanetFactory factory)
    {
        var planetId = factory.planetId;
        /* remove planet veins from dict */
        if (PlanetVeinCacheData.TryGetValue(planetId, out var vcd))
        {
            vcd.GenVeins(factory);
        }
        else
        {
            vcd = new VeinCacheData();
            vcd.GenVeins(factory);
            vcd.FrameNext = _frame + 120f / GameMain.history.miningSpeedScale;
            PlanetVeinCacheData.Add(planetId, vcd);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FactorySystem), "CheckBeforeGameTick")]
    private static void Miner(FactorySystem __instance)
    {
        var history = GameMain.history;
        var miningSpeedScalef = history.miningSpeedScale;
        var miningSpeedScale = (long)(miningSpeedScalef * 100);
        if (miningSpeedScale <= 0)
            return;
        var factory = __instance.factory;
        var planetId = factory.planetId;
        if (PlanetVeinCacheData.TryGetValue(planetId, out var vcd))
        {
            if (vcd.FrameNext > _frame)
                return;
        }
        else
        {
            PlanetVeinCacheData[planetId] = new VeinCacheData
            {
                FrameNext = _frame + 120f / miningSpeedScalef
            };
            return;
        }

        var miningFrames = 120f / miningSpeedScalef;
        var miningCostRate = history.miningCostRate;
        if (!miningCostRate.Equals(_miningCostRate))
        {
            _miningCostRate = miningCostRate;
            _miningCostBarrier = (uint)(int)Math.Ceiling(2147483646.0 * miningCostRate);
            _miningCostBarrierOil =
                (uint)(int)Math.Ceiling(2147483646.0 * miningCostRate * 0.401116669f /
                                        factory.gameData.gameDesc.resourceMultiplier);
        }

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

                    var isVein = vcd.HasVein(stationStore.itemId);
                    var isVeinOrWater = isVein || stationStore.itemId == __instance.planet.waterItemId;
                    if (!isVeinOrWater) continue;
                    int amount;
                    long energyConsume;
                    isCollecting = true;
                    var miningScale = _miningScale;
                    if (miningScale > 100)
                    {
                        if (stationStore.count * 2 < stationStore.max)
                            miningScale = 100 +
                                          ((miningScale - 100) * (stationStore.max - stationStore.count * 2) +
                                              stationStore.max - 1) / stationStore.max;
                        else
                            miningScale = 100;
                    }

                    if (isVein)
                    {
                        (amount, energyConsume) = vcd.Mine(factory, stationStore.itemId, miningScale, miningSpeedScale,
                            stationComponent.energy);
                        if (amount < 0)
                        {
                            k = int.MaxValue - 1;
                            continue;
                        }
                    }
                    else
                    {
                        energyConsume = (_waterEnergyConsume * miningScale * miningScale + 9999L) / 100L /
                                        miningSpeedScale;
                        if (stationComponent.energy < energyConsume)
                        {
                            k = int.MaxValue - 1;
                            continue;
                        }

                        amount = _waterSpeed * miningScale / 100;
                    }

                    if (amount <= 0) continue;
                    stationStore.count += amount;
                    if (factoryProductionStat != null)
                        productRegister[stationStore.itemId] += amount;
                    stationComponent.energy -= energyConsume;
                }

                if (!isCollecting || stationComponent.energy * 2 >= stationComponent.energyMax) continue;
                var index = stationComponent.isStellar ? storage.Length - 2 : storage.Length - 1;
                var fuelCount = storage[index].count;
                if (fuelCount == 0) continue;
                var heatValue = LDB.items.Select(storage[index].itemId).HeatValue;
                if (heatValue <= 0) continue;
                var count = (int)((stationComponent.energyMax - stationComponent.energy) /
                                  heatValue);
                if (count > fuelCount)
                    count = fuelCount;
                storage[index].count -= count;
                stationComponent.energy += count * heatValue;
            }

            vcd.FrameNext += miningFrames;
        } while (vcd.FrameNext <= _frame);

        PerformanceMonitor.EndSample(ECpuWorkEntry.Miner);
    }

    private class VeinCacheData
    {
        public float FrameNext;
        /* stores list of indices to veinData, with an extra INT which indicates cout of veinGroups at last */
        private Dictionary<int, List<int>> _veins = new();
        private int _mineIndex = -1;

        public bool HasVein(int productId)
        {
            return _veins.ContainsKey(productId);
        }

        public void GenVeins(PlanetFactory factory)
        {
            _veins = new Dictionary<int, List<int>>();
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
                    _veins.Add(productId, new List<int> { i });
                }

                if (vg.TryGetValue(productId, out var hs))
                {
                    hs.Add(veinPool[i].groupIndex);
                }
                else
                {
                    vg.Add(productId, new HashSet<int> { veinPool[i].groupIndex });
                }
            }

            foreach (var pair in vg)
            {
                _veins[pair.Key].Add(pair.Value.Count);
            }
        }

        public (int, long) Mine(PlanetFactory factory, int productId, int percent, long miningSpeedScale,
            long energyMax)
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
                energy = (_oilEnergyConsume * length * percent * percent + 9999L) / 100L / miningSpeedScale;
                if (energy > energyMax)
                    return (-1, -1L);
                float countf = 0f;
                var veinsPool = factory.veinPool;
                for (var i = 0; i < length; i++)
                {
                    ref var vd = ref veinsPool[veins[i]];
                    countf += vd.amount * 4 * VeinData.oilSpeedMultiplier;
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
                energy = (_oreEnergyConsume * veins[length] * percent * percent + 9999L) / 100L / miningSpeedScale;
                if (energy > energyMax)
                    return (-1, -1L);
                barrier = _miningCostBarrier;
                limit = 0;
            }

            var veinsData = factory.veinPool;
            var total = 0;
            for (; count > 0; count--)
            {
                _mineIndex = (_mineIndex + 1) % length;
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
