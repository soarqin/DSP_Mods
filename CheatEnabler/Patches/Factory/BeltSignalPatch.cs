using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace CheatEnabler.Patches.Factory;

internal class BeltSignalGenerator : PatchImpl<BeltSignalGenerator>
{
    private static Dictionary<int, BeltSignal>[] _signalBelts;
    private static Dictionary<long, int> _portalFrom;
    private static Dictionary<int, HashSet<long>> _portalTo;
    private static int _signalBeltsCapacity;
    private static bool _initialized;

    private class BeltSignal
    {
        public int SignalId;
        public int SpeedLimit;
        public byte Stack;
        public byte Inc;
        public int Progress;
        public (int itemId, float itemCount, bool isExtra)[] Sources;
        public float[] SourceProgress;
    }

    protected override void OnEnable()
    {
        InitSignalBelts();
        GameLogicProc.OnGameBegin += OnGameBegin;
    }

    protected override void OnDisable()
    {
        GameLogicProc.OnGameBegin -= OnGameBegin;
        _initialized = false;
        _signalBelts = null;
        _signalBeltsCapacity = 0;
    }

    internal static void OnAltFormatChanged()
    {
        if (_signalBelts == null) return;
        var factories = GameMain.data?.factories;
        if (factories == null) return;
        var factoryCount = GameMain.data.factoryCount;
        var altFormat = FactoryPatch.BeltSignalNumberAltFormat.Value;
        for (var i = Math.Min(_signalBelts.Length, factoryCount) - 1; i >= 0; i--)
        {
            var factory = factories[i];
            var cargoTraffic = factory?.cargoTraffic;
            if (cargoTraffic == null) continue;
            var entitySignPool = factory.entitySignPool;
            if (entitySignPool == null) continue;
            var belts = _signalBelts[i];
            if (belts == null) continue;
            foreach (var pair in belts)
            {
                var beltId = pair.Key;
                ref var belt = ref cargoTraffic.beltPool[beltId];
                if (belt.id != beltId) continue;
                ref var signal = ref entitySignPool[belt.entityId];
                if (signal.iconId0 < 1000) continue;
                var signalBelt = pair.Value;
                var inc = signalBelt.Inc / signalBelt.Stack;
                if (altFormat)
                    signal.count0 = signalBelt.SpeedLimit + signalBelt.Stack * 10000 + inc * 100000;
                else
                    signal.count0 = signalBelt.SpeedLimit * 100 + signalBelt.Stack + inc * 10;
            }
        }
    }

    internal static void OnUseProliferatorChanged()
    {
        if (_signalBelts == null) return;
        var factories = GameMain.data?.factories;
        if (factories == null) return;
        var factoryCount = GameMain.data.factoryCount;
        var altFormat = FactoryPatch.BeltSignalNumberAltFormat.Value;
        for (var i = Math.Min(_signalBelts.Length, factoryCount) - 1; i >= 0; i--)
        {
            var factory = factories[i];
            var cargoTraffic = factory?.cargoTraffic;
            if (cargoTraffic == null) continue;
            var entitySignPool = factory.entitySignPool;
            if (entitySignPool == null) continue;
            var belts = _signalBelts[i];
            if (belts == null) continue;
            foreach (var pair in belts)
            {
                var beltId = pair.Key;
                ref var belt = ref cargoTraffic.beltPool[beltId];
                if (belt.id != beltId) continue;
                var signalBelt = pair.Value;
                signalBelt.Progress = 0;
                signalBelt.Sources = null;
                signalBelt.SourceProgress = null;
                AddSourcesToBeltSignal(signalBelt);
            }
        }
    }

    private static void InitSignalBelts()
    {
        if (DSPGame.IsMenuDemo) return;
        InitItemSources();
        _signalBelts = new Dictionary<int, BeltSignal>[64];
        _signalBeltsCapacity = 64;
        _portalFrom = [];
        _portalTo = [];

        var factories = GameMain.data?.factories;
        if (factories == null) return;
        foreach (var factory in factories)
        {
            var entitySignPool = factory?.entitySignPool;
            if (entitySignPool == null) continue;
            var cargoTraffic = factory.cargoTraffic;
            var beltPool = cargoTraffic.beltPool;
            for (var i = cargoTraffic.beltCursor - 1; i > 0; i--)
            {
                if (beltPool[i].id != i) continue;
                ref var signal = ref entitySignPool[beltPool[i].entityId];
                var signalId = signal.iconId0;
                if (signalId == 0U) continue;
                var number = Mathf.RoundToInt(signal.count0);
                switch (signalId)
                {
                    case 404:
                        SetSignalBelt(factory.index, i, (int)signalId, 0);
                        continue;
                    case 600:
                    case >= 1000 and < 20000:
                        if (number > 0)
                            SetSignalBelt(factory.index, i, (int)signalId, number);
                        continue;
                    case >= 601 and <= 609:
                        if (number > 0)
                            SetSignalBeltPortalTo(factory.index, i, number);
                        continue;
                }
            }
        }

        _initialized = true;
    }

    private static Dictionary<int, BeltSignal> GetOrCreateSignalBelts(int index)
    {
        Dictionary<int, BeltSignal> obj;
        if (index < 0) return null;
        if (index >= _signalBeltsCapacity)
        {
            var newCapacity = _signalBeltsCapacity * 2;
            var newSignalBelts = new Dictionary<int, BeltSignal>[newCapacity];
            Array.Copy(_signalBelts, newSignalBelts, _signalBeltsCapacity);
            _signalBelts = newSignalBelts;
            _signalBeltsCapacity = newCapacity;
        }
        else
        {
            obj = _signalBelts[index];
            if (obj != null) return obj;
        }

        obj = [];
        _signalBelts[index] = obj;
        return obj;
    }

    private static Dictionary<int, BeltSignal> GetSignalBelts(int index)
    {
        return index >= 0 && index < _signalBeltsCapacity ? _signalBelts[index] : null;
    }

    private static void SetSignalBelt(int factory, int beltId, int signalId, int number)
    {
        int stack;
        int inc;
        int speedLimit;
        if (signalId >= 1000)
        {
            if (!FactoryPatch.BeltSignalNumberAltFormat.Value)
            {
                stack = Mathf.Clamp(number % 10, 1, 4);
                inc = number / 10 % 10 * stack;
                speedLimit = number / 100;
            }
            else
            {
                stack = Mathf.Clamp(number / 10000 % 10, 1, 4);
                inc = number / 100000 % 10 * stack;
                speedLimit = number % 10000;
            }
        }
        else
        {
            stack = 0;
            inc = 0;
            speedLimit = number;
        }

        if (speedLimit > 3600) speedLimit = 3600;

        var signalBelts = GetOrCreateSignalBelts(factory);
        if (signalBelts.TryGetValue(beltId, out var oldBeltSignal))
        {
            if (oldBeltSignal.SignalId == signalId && oldBeltSignal.SpeedLimit == speedLimit && oldBeltSignal.Stack == stack && oldBeltSignal.Inc == inc) return;
            oldBeltSignal.SpeedLimit = speedLimit;
            oldBeltSignal.Stack = (byte)stack;
            oldBeltSignal.Inc = (byte)inc;
            oldBeltSignal.Progress = 0;
            oldBeltSignal.SignalId = signalId;
            oldBeltSignal.Sources = null;
            oldBeltSignal.SourceProgress = null;
            AddSourcesToBeltSignal(oldBeltSignal);
            return;
        }

        var beltSignal = new BeltSignal
        {
            SignalId = signalId,
            SpeedLimit = speedLimit,
            Stack = (byte)stack,
            Inc = (byte)inc
        };
        AddSourcesToBeltSignal(beltSignal);
        signalBelts[beltId] = beltSignal;
    }

    private static void AddSourcesToBeltSignal(BeltSignal beltSignal)
    {
        var itemId = beltSignal.SignalId;
        if (itemId < 1000) return;
        var result = new Dictionary<int, float>();
        var extra = new Dictionary<int, float>();
        var sprayedCount = 0f;
        CalculateAllProductions(result, extra, ref sprayedCount, itemId);

        var proliferatorCount = 0f;
        if (result.TryGetValue(1143, out var pv))
        {
            proliferatorCount = pv;
            result.Remove(1143);
        }
        if (FactoryPatch.BeltSignalUseProliferatorEnabled.Value)
        {
            if (beltSignal.Inc / beltSignal.Stack >= 4)
            {
                sprayedCount += 1f;
            }
            if (sprayedCount > 0)
            {
                proliferatorCount += sprayedCount / ProliferatorSpayCount;
            }
        }
        if (proliferatorCount > 0f)
        {
            foreach (var p in ProliferatorSources)
            {
                result[p.Item1] = (result.TryGetValue(p.Item1, out var v) ? v : 0) + p.Item2 * proliferatorCount / ProliferatorDenom;
            }
        }

        result.Remove(itemId);

        var cnt = result.Count + extra.Count;
        if (cnt == 0)
        {
            beltSignal.Sources = null;
            beltSignal.SourceProgress = null;
            return;
        }

        var items = new (int itemId, float itemCount, bool isExtra)[cnt];
        var progress = new float[cnt];
        foreach (var p in extra)
        {
            items[--cnt] = (p.Key, p.Value, true);
        }
        foreach (var p in result)
        {
            items[--cnt] = (p.Key, p.Value, false);
        }

        beltSignal.Sources = items;
        beltSignal.SourceProgress = progress;
    }

    private static void SetSignalBeltPortalTo(int factory, int beltId, int number)
    {
        var v = ((long)factory << 32) | (uint)beltId;
        _portalFrom[v] = number;
        if (!_portalTo.TryGetValue(number, out var set))
        {
            set = [];
            _portalTo[number] = set;
        }

        set.Add(v);
    }

    private static void RemoveSignalBelt(int factory, int beltId)
    {
        GetSignalBelts(factory)?.Remove(beltId);
    }

    private static void RemovePlanetSignalBelts(int factory)
    {
        GetSignalBelts(factory)?.Clear();
    }

    private static void RemoveSignalBeltPortalEnd(int factory, int beltId)
    {
        var v = ((long)factory << 32) | (uint)beltId;
        if (!_portalFrom.TryGetValue(v, out var number)) return;
        _portalFrom.Remove(v);
        if (!_portalTo.TryGetValue(number, out var set)) return;
        set.Remove(v);
    }

    private static void OnGameBegin()
    {
        if (DSPGame.IsMenuDemo) return;
        if (FactoryPatch.BeltSignalGeneratorEnabled.Value) InitSignalBelts();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DigitalSystem), MethodType.Constructor, typeof(PlanetData))]
    private static void DigitalSystem_Constructor_Postfix(PlanetData _planet)
    {
        if (!FactoryPatch.BeltSignalGeneratorEnabled.Value) return;
        var player = GameMain.mainPlayer;
        if (player == null) return;
        var factory = _planet?.factory;
        if (factory == null) return;
        RemovePlanetSignalBelts(factory.index);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RemoveBeltComponent))]
    public static void CargoTraffic_RemoveBeltComponent_Prefix(int id)
    {
        if (!_initialized) return;
        var planet = GameMain.localPlanet;
        if (planet == null) return;
        RemoveSignalBeltPortalEnd(planet.factoryIndex, id);
        RemoveSignalBelt(planet.factoryIndex, id);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SetBeltSignalIcon))]
    public static void CargoTraffic_SetBeltSignalIcon_Postfix(CargoTraffic __instance, int signalId, int entityId)
    {
        if (!_initialized) return;
        var planet = GameMain.localPlanet;
        if (planet == null) return;
        var factory = __instance.factory;
        int number;
        var needAdd = false;
        switch (signalId)
        {
            case 404:
                number = 0;
                needAdd = true;
                break;
            case 600:
            case >= 1000 and < 20000:
                number = Mathf.RoundToInt(factory.entitySignPool[entityId].count0);
                if (number > 0)
                    needAdd = true;
                break;
            case >= 601 and <= 609:
                number = Mathf.RoundToInt(factory.entitySignPool[entityId].count0);
                var factoryIndex = planet.factoryIndex;
                var beltId = factory.entityPool[entityId].beltId;
                if (number > 0)
                    SetSignalBeltPortalTo(factoryIndex, beltId, number);
                RemoveSignalBelt(factoryIndex, beltId);
                return;
            default:
                number = 0;
                break;
        }

        {
            var factoryIndex = planet.factoryIndex;
            var beltId = factory.entityPool[entityId].beltId;
            if (needAdd)
            {
                SetSignalBelt(factoryIndex, beltId, signalId, number);
            }
            else
            {
                RemoveSignalBelt(factoryIndex, beltId);
            }

            RemoveSignalBeltPortalEnd(factoryIndex, beltId);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SetBeltSignalNumber))]
    public static void CargoTraffic_SetBeltSignalNumber_Postfix(CargoTraffic __instance, float number, int entityId)
    {
        if (!_initialized) return;
        var planet = GameMain.localPlanet;
        if (planet == null) return;
        var factory = __instance.factory;
        var entitySignPool = factory.entitySignPool;
        uint signalId;
        if (entitySignPool[entityId].iconType == 0U || (signalId = entitySignPool[entityId].iconId0) == 0U) return;
        switch (signalId)
        {
            case 404:
                return;
            case 600:
            case >= 1000 and < 20000:
                break;
            case >= 601 and <= 609:
                var factoryIndex = planet.factoryIndex;
                var beltId = factory.entityPool[entityId].beltId;
                RemoveSignalBeltPortalEnd(factoryIndex, beltId);
                SetSignalBeltPortalTo(factoryIndex, beltId, Mathf.RoundToInt(number));
                return;
            default:
                return;
        }

        {
            var factoryIndex = planet.factoryIndex;
            var beltId = factory.entityPool[entityId].beltId;
            var n = Mathf.RoundToInt(number);
            if (n == 0)
            {
                RemoveSignalBelt(factoryIndex, beltId);
            }
            else
            {
                SetSignalBelt(factoryIndex, beltId, (int)signalId, n);
            }
        }
    }

    private static void ProcessBeltSignals()
    {
        if (!_initialized) return;
        var data = GameMain.data;
        var factories = data?.factories;
        if (factories == null) return;
        DeepProfiler.BeginSample(DPEntry.Belt);
        for (var index = data.factoryCount - 1; index >= 0; index--)
        {
            var factory = factories[index];
            if (factory == null) continue;
            var belts = GetSignalBelts(index);
            if (belts == null || belts.Count == 0) continue;
            var factoryProductionStat = GameMain.statistics.production.factoryStatPool[index];
            var productRegister = factoryProductionStat.productRegister;
            var consumeRegister = factoryProductionStat.consumeRegister;
            var countRecipe = FactoryPatch.BeltSignalCountRecipeEnabled.Value;
            var cargoTraffic = factory.cargoTraffic;
            var beltCount = cargoTraffic.beltCursor;
            List<int> beltsToRemove = null;
            foreach (var pair in belts)
            {
                if (pair.Key >= beltCount)
                {
                    if (beltsToRemove == null)
                        beltsToRemove = [pair.Key];
                    else
                        beltsToRemove.Add(pair.Key);
                    continue;
                }
                var beltSignal = pair.Value;
                var signalId = beltSignal.SignalId;
                switch (signalId)
                {
                    case 404:
                        {
                            var beltId = pair.Key;
                            ref var belt = ref cargoTraffic.beltPool[beltId];
                            var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
                            if (cargoPath == null) continue;
                            int itemId;
                            if ((itemId = cargoPath.TryPickItem(belt.segIndex + belt.segPivotOffset - 5, 12, out var stack, out _)) > 0)
                            {
                                if (FactoryPatch.BeltSignalCountRemEnabled.Value) consumeRegister[itemId] += stack;
                            }

                            continue;
                        }
                    case 600:
                        {
                            if (!_portalTo.TryGetValue(beltSignal.SpeedLimit, out var set)) continue;
                            var beltId = pair.Key;
                            ref var belt = ref cargoTraffic.beltPool[beltId];
                            var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
                            if (cargoPath == null) continue;
                            var segIndex = belt.segIndex + belt.segPivotOffset;
                            if (!cargoPath.GetCargoAtIndex(segIndex, out var cargo, out var cargoId, out var _)) break;
                            var itemId = cargo.item;
                            var cargoPool = cargoPath.cargoContainer.cargoPool;
                            var inc = cargoPool[cargoId].inc;
                            var stack = cargoPool[cargoId].stack;
                            foreach (var n in set)
                            {
                                var cargoTraffic1 = factories[(int)(n >> 32)].cargoTraffic;
                                ref var belt1 = ref cargoTraffic1.beltPool[(int)(n & 0x7FFFFFFF)];
                                cargoPath = cargoTraffic1.GetCargoPath(belt1.segPathId);
                                if (cargoPath == null) continue;
                                if (!cargoPath.TryInsertItem(belt1.segIndex + belt1.segPivotOffset, itemId, stack, inc)) continue;
                                cargoPath.TryPickItem(segIndex - 5, 12, out var stack1, out var inc1);
                                if (inc1 != inc || stack1 != stack)
                                    cargoPath.TryPickItem(segIndex - 5, 12, out _, out _);
                                break;
                            }

                            continue;
                        }
                    case >= 1000 and < 20000:
                        {
                            var hasSpeedLimit = beltSignal.SpeedLimit > 0;
                            if (hasSpeedLimit)
                            {
                                beltSignal.Progress += beltSignal.SpeedLimit;
                                switch (beltSignal.Progress)
                                {
                                    case < 3600:
                                        continue;
                                    case > 18000:
                                        beltSignal.Progress = 14400;
                                        break;
                                }
                            }

                            var beltId = pair.Key;
                            ref var belt = ref cargoTraffic.beltPool[beltId];
                            var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
                            if (cargoPath == null) continue;
                            var stack = beltSignal.Stack;
                            var inc = beltSignal.Inc;
                            if (!cargoPath.TryInsertItem(belt.segIndex + belt.segPivotOffset, signalId, stack, inc)) continue;
                            if (hasSpeedLimit) beltSignal.Progress -= 3600;
                            if (FactoryPatch.BeltSignalCountGenEnabled.Value) productRegister[signalId] += stack;
                            if (!countRecipe) continue;
                            var sources = beltSignal.Sources;
                            if (sources == null) continue;
                            var progress = beltSignal.SourceProgress;
                            var stackf = (float)stack;
                            for (var i = sources.Length - 1; i >= 0; i--)
                            {
                                var newCnt = progress[i] + sources[i].itemCount * stackf;
                                if (newCnt > 0)
                                {
                                    var itemId = sources[i].itemId;
                                    var cnt = Mathf.CeilToInt(newCnt);
                                    productRegister[itemId] += cnt;
                                    if (!sources[i].isExtra) consumeRegister[itemId] += cnt;
                                    progress[i] = newCnt - cnt;
                                }
                                else
                                {
                                    progress[i] = newCnt;
                                }
                            }

                            continue;
                        }
                }
            }
            if (beltsToRemove == null) continue;
            foreach (var beltId in beltsToRemove)
            {
                belts.Remove(beltId);
            }
        }

        DeepProfiler.EndSample(DPEntry.Belt);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.OnFactoryFrameBegin))]
    public static void GameLogic_OnFactoryFrameBegin_Postfix()
    {
        ProcessBeltSignals();
    }

    /* BEGIN: Item sources calculation */
    private static readonly int[] ExtraOreItemIds = [1000, 1116, 1120, 1121, 1208, 5201, 5202, 5203, 5204, 5205, 5206];
    private static readonly HashSet<int> ExtraProliferationItemIds = [1107, 1111, 1125, 1142, 1143, 1202, 1203, 1204, 1205, 1209, 1210, 1301, 1305, 1401, 1402, 1403, 1405, 1406, 1502, 1503, 1802, 6001, 6003, 6004, 6005, 6006];
    private static readonly HashSet<int> NoProliferationItemIds = [1126, 6002];
    // All source items used to create 25 proliferators mk.III (not self-sprayed)
    private static readonly List<(int, float)> ProliferatorSources = [(1015, 60f), (1124, 20f), (1006, 64f), (1012, 16f), (1112, 32f), (1141, 64f), (1142, 40f), (1143, 25f)];
    private const float ProliferatorDenom = 21f;
    // One sprayed proliferator mk.III can spray 75 items, but one is used for spray itself, so the actual count is 74
    private const float ProliferatorSpayCount = 74f;
    private static readonly Dictionary<int, ItemSource> ItemSources = [];
    private static bool _itemSourcesInitialized;

    private class ItemSource
    {
        public float Count;
        public Dictionary<int, float> From;
        public Dictionary<int, float> Extra;
    }

    private static void InitItemSources()
    {
        if (_itemSourcesInitialized) return;
        foreach (var vein in LDB.veins.dataArray)
        {
            ItemSources[vein.MiningItem] = new ItemSource { Count = 1 };
        }

        foreach (var ip in LDB.items.dataArray)
        {
            if (!string.IsNullOrEmpty(ip.MiningFrom))
            {
                ItemSources[ip.ID] = new ItemSource { Count = 1 };
            }
        }

        // 水、硫酸、氢、重氢、光子
        foreach (var itemId in ExtraOreItemIds)
        {
            ItemSources[itemId] = new ItemSource { Count = 1 };
        }

        var recipes = LDB.recipes.dataArray;
        foreach (var recipe in recipes)
        {
            if (!recipe.Explicit || recipe.ID == 58 || recipe.ID == 121) continue;
            var res = recipe.Results;
            var rescnt = recipe.ResultCounts;
            var len = res.Length;
            for (var i = 0; i < len; i++)
            {
                if (ItemSources.ContainsKey(res[i])) continue;
                var rs = new ItemSource { Count = rescnt[i], From = [] };
                var it = recipe.Items;
                var itcnt = recipe.ItemCounts;
                var len2 = it.Length;
                for (var j = 0; j < len2; j++)
                {
                    rs.From[it[j]] = itcnt[j];
                }

                if (len > 1)
                {
                    rs.Extra = [];
                    for (var k = 0; k < len; k++)
                    {
                        if (i != k)
                        {
                            rs.Extra[res[k]] = rescnt[k];
                        }
                    }
                }

                ItemSources[res[i]] = rs;
            }
        }

        foreach (var recipe in recipes)
        {
            if (recipe.Explicit) continue;
            var res = recipe.Results;
            var rescnt = recipe.ResultCounts;
            var len = res.Length;
            for (var i = 0; i < len; i++)
            {
                if (ItemSources.ContainsKey(res[i])) continue;
                var rs = new ItemSource { Count = rescnt[i], From = [], Extra = null };
                var it = recipe.Items;
                var itcnt = recipe.ItemCounts;
                var len2 = it.Length;
                for (var j = 0; j < len2; j++)
                {
                    rs.From[it[j]] = itcnt[j];
                }

                if (len > 1)
                {
                    rs.Extra = [];
                    for (var k = 0; k < len; k++)
                    {
                        if (i != k)
                        {
                            rs.Extra[res[k]] = rescnt[k];
                        }
                    }
                }

                ItemSources[res[i]] = rs;
            }
        }

        _itemSourcesInitialized = true;
    }

    private static void CalculateAllProductions(IDictionary<int, float> result, IDictionary<int, float> extra, ref float sprayedCount, int itemId, float count = 1f)
    {
        if (!ItemSources.TryGetValue(itemId, out var itemSource))
        {
            return;
        }

        var times = 1f;
        if (Math.Abs(count - itemSource.Count) > 0.000001f)
        {
            times = count / itemSource.Count;
        }

        result[itemId] = (result.TryGetValue(itemId, out var oldCount) ? oldCount : 0) + count;
        if (itemSource.Extra != null)
        {
            foreach (var p in itemSource.Extra)
            {
                extra[p.Key] = (extra.TryGetValue(p.Key, out oldCount) ? oldCount : 0) + times * p.Value;
            }
        }

        if (itemId == 1143 || itemSource.From == null) return;
        var useProliferator = FactoryPatch.BeltSignalUseProliferatorEnabled.Value;
        if (useProliferator && ExtraProliferationItemIds.Contains(itemId))
        {
            times *= 0.8f;
        }
        foreach (var p in itemSource.From)
        {
            var value = p.Value * times;
            if (useProliferator && !NoProliferationItemIds.Contains(p.Key)) sprayedCount += value;
            if (extra.TryGetValue(p.Key, out var rcount))
            {
                if (value <= rcount)
                {
                    if (value == rcount)
                    {
                        extra.Remove(p.Key);
                    }
                    else
                    {
                        extra[p.Key] = rcount - value;
                    }
                    continue;
                }
                extra.Remove(p.Key);
                value -= rcount;
            }
            if (result.TryGetValue(p.Key, out rcount))
            {
                rcount -= value;
                if (rcount <= 0)
                {
                    result.Remove(p.Key);
                }
                else
                {
                    result[p.Key] = rcount;
                }
                continue;
            }
            CalculateAllProductions(result, extra, ref sprayedCount, p.Key, value);
        }
    }
    /* END: Item sources calculation */
}
