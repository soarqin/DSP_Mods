using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UXAssist.Common;
using UXAssist.Common.GameConstants;
using GameLogicProc = UXAssist.Common.GameLogic;

namespace UXAssist.Patches.Factory;

internal static class BeltSignalPatch
{
    public static void Enable(bool enable)
    {
        BeltSignalsForBuyOut.Enable(enable);
    }

    public static void InitPersist()
    {
        BeltSignalsForBuyOut.InitPersist();
    }

    public static void UninitPersist()
    {
        BeltSignalsForBuyOut.UninitPersist();
    }

    public static void Export(BinaryWriter w)
    {
        var storage = BeltSignalsForBuyOut.DarkFogItemsInVoid;
        for (var i = 0; i < 6; i++)
            w.Write(storage[i]);
    }

    public static void Import(BinaryReader r)
    {
        var storage = BeltSignalsForBuyOut.DarkFogItemsInVoid;
        for (var i = 0; i < 6; i++)
            storage[i] = r.ReadInt32();
    }

    internal class BeltSignalsForBuyOut : PatchImpl<BeltSignalsForBuyOut>
    {
        private static bool _initialized;
        private static bool _loaded;
        private static long _clusterSeedKey;
        private static readonly int[] DarkFogItemIds = ItemIds.DarkFogItemIds;
        private static readonly int[] DarkFogItemExchangeRate = [20, 60, 30, 30, 30, 10];
        public static readonly int[] DarkFogItemsInVoid = [0, 0, 0, 0, 0, 0];
        private static Dictionary<int, uint>[] _signalBelts = new Dictionary<int, uint>[64];
        private static readonly HashSet<int> SignalBeltFactoryIndices = [];

        public static void InitPersist()
        {
            Persist.Enable(true);
        }

        public static void UninitPersist()
        {
            Persist.Enable(false);
        }

        private static void AddBeltSignalProtos()
        {
            if (!_initialized || _loaded) return;
            var assembly = Assembly.GetExecutingAssembly();
            var signals = LDB._signals;
            SignalProto[] protos =
            [
                new SignalProto
                {
                    ID = 301,
                    Name = "Memory Unit",
                    GridIndex = 3801,
                    IconPath = "assets/signal/memory.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/memory.png", assembly),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 302,
                    Name = "Energy Fragment",
                    GridIndex = 3802,
                    IconPath = "assets/signal/energy-fragment.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/energy-fragment.png", assembly),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 303,
                    Name = "Silicon Neuron",
                    GridIndex = 3803,
                    IconPath = "assets/signal/silicon-neuron.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/silicon-neuron.png", assembly),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 304,
                    Name = "Negentropy Singularity",
                    GridIndex = 3804,
                    IconPath = "assets/signal/negentropy.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/negentropy.png", assembly),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 305,
                    Name = "Matter Reassembler",
                    GridIndex = 3805,
                    IconPath = "assets/signal/reassembler.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/reassembler.png", assembly),
                    SID = ""
                },
                new SignalProto
                {
                    ID = 306,
                    Name = "Virtual Particle",
                    GridIndex = 3806,
                    IconPath = "assets/signal/virtual-particle.png",
                    _iconSprite = Util.LoadEmbeddedSprite("assets/signal/virtual-particle.png", assembly),
                    SID = ""
                },
            ];
            foreach (var proto in protos)
            {
                proto.name = proto.Name.Translate();
            }

            var index = signals.dataArray.Length;
            signals.dataArray = [.. signals.dataArray, .. protos];
            foreach (var proto in protos)
            {
                signals.dataIndices[proto.ID] = index;
                index++;
            }

            _loaded = true;
        }

        private static void RemoveBeltSignalProtos()
        {
            if (!_initialized || !_loaded) return;
            var signals = LDB._signals;
            if (signals.dataIndices.TryGetValue(301, out var index))
            {
                signals.dataArray = [.. signals.dataArray.Take(index), .. signals.dataArray.Skip(index + 6)];
                for (var id = 301; id <= 306; id++)
                    signals.dataIndices.Remove(id);
                var len = signals.dataArray.Length;
                for (; index < len; index++)
                    signals.dataIndices[signals.dataArray[index].ID] = index;
            }

            _loaded = false;
        }

        private static void InitSignalBelts()
        {
            if (!GameMain.isRunning) return;

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
                    if (signalId is < 301U or > 306U) continue;
                    SetSignalBelt(factory.index, i, signalId - 301U);
                }
            }
        }

        private static void SetSignalBelt(int factory, int beltId, uint signal)
        {
            var signalBelts = GetOrCreateSignalBelts(factory);
            if (signalBelts.Count == 0)
                SignalBeltFactoryIndices.Add(factory);
            signalBelts[beltId] = signal;
        }

        private static Dictionary<int, uint> GetOrCreateSignalBelts(int index)
        {
            Dictionary<int, uint> obj;
            if (index < 0) return null;
            if (index >= _signalBelts.Length)
            {
                Array.Resize(ref _signalBelts, (index + 1) * 2);
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

        private static Dictionary<int, uint> GetSignalBelts(int index)
        {
            return index >= 0 && index < _signalBelts.Length ? _signalBelts[index] : null;
        }

        private static void RemoveSignalBelt(int factory, int beltId)
        {
            var signalBelts = GetSignalBelts(factory);
            if (signalBelts == null) return;
            signalBelts.Remove(beltId);
            if (signalBelts.Count == 0)
                SignalBeltFactoryIndices.Remove(factory);
        }

        private static void RemovePlanetSignalBelts(int factory)
        {
            var signalBelts = GetSignalBelts(factory);
            if (signalBelts == null) return;
            signalBelts.Clear();
            SignalBeltFactoryIndices.Remove(factory);
        }

        private class Persist : PatchImpl<Persist>
        {
            protected override void OnEnable()
            {
                AddBeltSignalProtos();
                GameLogicProc.OnDataLoaded += VFPreload_InvokeOnLoadWorkEnded_Postfix;
                GameLogicProc.OnGameBegin += OnGameBegin;
            }

            protected override void OnDisable()
            {
                GameLogicProc.OnGameBegin -= OnGameBegin;
                GameLogicProc.OnDataLoaded -= VFPreload_InvokeOnLoadWorkEnded_Postfix;
            }

            private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
            {
                if (_initialized) return;
                _initialized = true;
                AddBeltSignalProtos();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DigitalSystem), MethodType.Constructor, typeof(PlanetData))]
            private static void DigitalSystem_Constructor_Postfix(PlanetData _planet)
            {
                var player = GameMain.mainPlayer;
                if (player == null) return;
                var factory = _planet?.factory;
                if (factory == null) return;
                RemovePlanetSignalBelts(factory.index);
            }

            private static void OnGameBegin()
            {
                _clusterSeedKey = GameMain.data.GetClusterSeedKey();
                InitSignalBelts();
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RemoveBeltComponent))]
            public static void CargoTraffic_RemoveBeltComponent_Prefix(int id)
            {
                var planet = GameMain.localPlanet;
                if (planet == null) return;
                RemoveSignalBelt(planet.factoryIndex, id);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SetBeltSignalIcon))]
            public static void CargoTraffic_SetBeltSignalIcon_Postfix(CargoTraffic __instance, int entityId, int signalId)
            {
                var planet = GameMain.localPlanet;
                if (planet == null) return;
                var factory = __instance.factory;
                var factoryIndex = planet.factoryIndex;
                var beltId = factory.entityPool[entityId].beltId;
                if (signalId is < 301 or > 306)
                {
                    RemoveSignalBelt(factoryIndex, beltId);
                }
                else
                {
                    SetSignalBelt(factoryIndex, beltId, (uint)signalId - 301U);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.OnFactoryFrameBegin))]
        public static void GameLogic_OnFactoryFrameBegin_Postfix()
        {
            var factories = GameMain.data?.factories;
            if (factories == null) return;
            var factoriesCount = factories.Length;
            var propertySystem = DSPGame.propertySystem;
            List<int> factoriesToRemove = null;
            foreach (var factoryIndex in SignalBeltFactoryIndices)
            {
                if (factoryIndex >= factoriesCount)
                {
                    if (factoriesToRemove == null)
                        factoriesToRemove = [factoryIndex];
                    else
                        factoriesToRemove.Add(factoryIndex);
                    continue;
                }
                var signalBelts = GetSignalBelts(factoryIndex);
                if (signalBelts == null) continue;
                var factory = factories[factoryIndex];
                if (factory == null) continue;
                var cargoTraffic = factory.cargoTraffic;
                var beltCount = cargoTraffic.beltCursor;
                List<int> beltsToRemove = null;
                foreach (var kvp in signalBelts)
                {
                    if (kvp.Key >= beltCount)
                    {
                        if (beltsToRemove == null)
                            beltsToRemove = [kvp.Key];
                        else
                            beltsToRemove.Add(kvp.Key);
                        continue;
                    }
                    ref var belt = ref cargoTraffic.beltPool[kvp.Key];
                    var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
                    var itemIdx = kvp.Value;
                    if (cargoPath == null) continue;
                    var itemId = DarkFogItemIds[itemIdx];
                    var consume = (byte)Math.Min(DarkFogItemsInVoid[itemIdx], 4);
                    if (consume < 4)
                    {
                        var metaverseLong = propertySystem.GetItemAvaliableProperty(_clusterSeedKey, ItemIds.Metaverse);
                        if (metaverseLong > 0L)
                        {
                            var metaverse = metaverseLong > 10 ? 10 : (int)metaverseLong;
                            propertySystem.AddItemConsumption(_clusterSeedKey, ItemIds.Metaverse, metaverse);
                            var mainPlayer = GameMain.mainPlayer;
                            GameMain.history.AddPropertyItemConsumption(ItemIds.Metaverse, metaverse, true);
                            var count = DarkFogItemExchangeRate[itemIdx] * metaverse;
                            DarkFogItemsInVoid[itemIdx] += count;
                            consume = (byte)Math.Min(DarkFogItemsInVoid[itemIdx], 4);
                            mainPlayer.mecha.AddProductionStat(itemId, count, mainPlayer.nearestFactory);
                        }
                    }

                    if (consume > 0 && cargoPath.TryInsertItem(belt.segIndex + belt.segPivotOffset, itemId, consume, 0))
                        DarkFogItemsInVoid[itemIdx] -= consume;
                }
                if (beltsToRemove == null) continue;
                foreach (var beltId in beltsToRemove)
                    signalBelts.Remove(beltId);
                if (signalBelts.Count > 0) continue;
                if (factoriesToRemove == null)
                    factoriesToRemove = [factoryIndex];
                else
                    factoriesToRemove.Add(factoryIndex);
            }
            if (factoriesToRemove == null) return;
            foreach (var factoryIndex in factoriesToRemove)
            {
                RemovePlanetSignalBelts(factoryIndex);
            }
        }
    }
}
