using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UXAssist.Common;
using UXAssist.Common.ModFeatures;
using UXAssist.Production;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace CheatEnabler.Patches.Factory;

[ModFeature("BeltSignalGenerator")]
internal class BeltSignalGenerator : PatchImpl<BeltSignalGenerator>
{
    private static Dictionary<int, BeltSignal>[] _signalBelts;
    private static Dictionary<long, int> _portalFrom;
    private static Dictionary<int, HashSet<long>> _portalTo;
    private static int _signalBeltsCapacity;
    private static bool _initialized;
    private static ProductionCatalog _sourceCatalog;
    private static ProductionItem _preferredProliferator;
    private static readonly Dictionary<(int itemId, bool proliferated, bool sprayed),
        IReadOnlyList<BeltSignalSourceRate>> SourceCache = [];
    private static readonly HashSet<(int itemId, bool proliferated, bool sprayed)> FailedSources = [];

    public static void Init()
    {
        GameLogicProc.OnGameEnd += ResetState;
        ProductionCatalogService.Changed += OnProductionCatalogChanged;
    }

    public static void Uninit()
    {
        GameLogicProc.OnGameEnd -= ResetState;
        ProductionCatalogService.Changed -= OnProductionCatalogChanged;
    }

    private static void ResetState()
    {
        _initialized = false;
        _signalBelts = null;
        _portalFrom = null;
        _portalTo = null;
        _signalBeltsCapacity = 0;
        ClearSourceCache();
    }

    private static void ClearSourceCache()
    {
        _sourceCatalog = null;
        _preferredProliferator = null;
        SourceCache.Clear();
        FailedSources.Clear();
    }

    private class BeltSignal
    {
        public int SignalId;
        public int SpeedLimit;
        public byte Stack;
        public byte Inc;
        public int Progress;
        public BeltSignalSourceStats SourceStats;
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
        ClearSourceCache();
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
        ClearSourceCache();
        RefreshSourceStats(true);
    }

    private static void OnProductionCatalogChanged()
    {
        ClearSourceCache();
        RefreshSourceStats(false);
    }

    private static void RefreshSourceStats(bool resetGenerationProgress)
    {
        if (_signalBelts == null) return;
        if (ProductionCatalogService.Current == null)
        {
            foreach (var belts in _signalBelts)
            {
                if (belts == null) continue;
                foreach (var belt in belts.Values)
                {
                    belt.SourceStats = null;
                    if (resetGenerationProgress) belt.Progress = 0;
                }
            }

            return;
        }

        var factories = GameMain.data?.factories;
        if (factories == null) return;
        var factoryCount = GameMain.data.factoryCount;
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
                if (resetGenerationProgress) signalBelt.Progress = 0;
                signalBelt.SourceStats = null;
                AddSourcesToBeltSignal(signalBelt);
            }
        }
    }

    private static void InitSignalBelts()
    {
        if (DSPGame.IsMenuDemo) return;
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
            if (oldBeltSignal.SignalId == signalId && oldBeltSignal.SpeedLimit == speedLimit &&
                oldBeltSignal.Stack == stack && oldBeltSignal.Inc == inc)
            {
                if (!ReferenceEquals(_sourceCatalog, ProductionCatalogService.Current))
                    AddSourcesToBeltSignal(oldBeltSignal);
                return;
            }
            oldBeltSignal.SpeedLimit = speedLimit;
            oldBeltSignal.Stack = (byte)stack;
            oldBeltSignal.Inc = (byte)inc;
            oldBeltSignal.Progress = 0;
            oldBeltSignal.SignalId = signalId;
            oldBeltSignal.SourceStats = null;
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
        beltSignal.SourceStats = null;
        var itemId = beltSignal.SignalId;
        if (itemId < 1000 || beltSignal.Stack == 0) return;
        var catalog = ProductionCatalogService.Current;
        if (!ReferenceEquals(_sourceCatalog, catalog))
        {
            ClearSourceCache();
            _sourceCatalog = catalog;
            _preferredProliferator = BeltSignalSourcePreset.SelectProliferator(catalog);
        }

        var proliferated = FactoryPatch.BeltSignalUseProliferatorEnabled.Value;
        var sprayed = proliferated && _preferredProliferator != null &&
                      beltSignal.Inc / beltSignal.Stack >= _preferredProliferator.ProliferationLevel;
        var key = (itemId, proliferated, sprayed);
        if (catalog == null)
        {
            LogSourceFailure(key, "Production data is not loaded.");
            return;
        }

        if (FailedSources.Contains(key)) return;
        if (!SourceCache.TryGetValue(key, out var rates))
        {
            try
            {
                var request = BeltSignalSourcePreset.Create(catalog, itemId, proliferated, sprayed);
                var report = new ProductionPlanner(catalog).Calculate(request);
                if (!report.MaterialComplete)
                {
                    LogSourceFailure(key, string.Join("; ", report.Diagnostics.Select(diagnostic => diagnostic.Message)));
                    return;
                }

                rates = BeltSignalSourceStats.FromReport(report, itemId);
            }
            catch (Exception exception)
            {
                LogSourceFailure(key, exception.ToString());
                return;
            }

            SourceCache[key] = rates;
        }

        if (rates.Count > 0) beltSignal.SourceStats = new BeltSignalSourceStats(rates);
    }

    private static void LogSourceFailure((int itemId, bool proliferated, bool sprayed) key, string reason)
    {
        if (FailedSources.Add(key))
            CheatEnabler.Logger.LogWarning(
                $"Belt signal upstream statistics for item {key.itemId} are suspended: {reason}");
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
                            beltSignal.SourceStats?.Apply(stack, productRegister, consumeRegister);

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

}
