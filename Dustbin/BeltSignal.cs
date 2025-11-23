using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Dustbin;

public static class BeltSignal
{
    private static HashSet<int>[] _signalBelts;
    private static int _signalBeltsCapacity;
    private static bool _initialized;
    private static Harmony _patch;

    public static void Enable(bool on)
    {
        if (on)
        {
            _patch ??= Harmony.CreateAndPatchAll(typeof(BeltSignal));
            InitSignalBelts();
        }
        else
        {
            _patch?.UnpatchSelf();
            _patch = null;
            _signalBelts = null;
            _signalBeltsCapacity = 0;
        }
    }

    private static byte[] LoadEmbeddedResource(string path, Assembly assembly = null)
    {
        if (assembly == null)
        {
            assembly = Assembly.GetCallingAssembly();
        }
        var info = assembly.GetName();
        var name = info.Name;
        using var stream = assembly.GetManifestResourceStream($"{name}.{path.Replace('/', '.')}")!;
        var buffer = new byte[stream.Length];
        _ = stream.Read(buffer, 0, buffer.Length);
        return buffer;
    }

    private static Texture2D LoadEmbeddedTexture(string path, Assembly assembly = null)
    {
        var fileData = LoadEmbeddedResource(path, assembly);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(fileData);
        return tex;
    }

    private static Sprite LoadEmbeddedSprite(string path, Assembly assembly = null)
    {
        var tex = LoadEmbeddedTexture(path, assembly);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
    private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        if (_initialized) return;
        var assembly = Assembly.GetExecutingAssembly();
        var signals = LDB._signals;
        var index = signals.dataArray.Length;
        var p = new SignalProto
        {
            ID = 410,
            Name = "DUSTBIN",
            GridIndex = 3110,
            IconPath = "Assets/signal-410.png",
            _iconSprite = LoadEmbeddedSprite("assets/icon/signal-410.png", assembly),
            SID = ""
        };
        p.name = p.Name.Translate();
        signals.dataArray = [.. signals.dataArray.AddItem(p)];
        signals.dataIndices[p.ID] = index;
        _initialized = true;
    }

    private static void InitSignalBelts()
    {
        if (!GameMain.isRunning) return;
        _signalBelts = new HashSet<int>[64];
        _signalBeltsCapacity = 64;

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
                if (signalId != 410U) continue;
                SetSignalBelt(factory.index, i);
            }
        }
    }

    private static void SetSignalBelt(int factory, int beltId)
    {
        var signalBelts = GetOrCreateSignalBelts(factory);
        signalBelts.Add(beltId);
    }

    private static HashSet<int> GetOrCreateSignalBelts(int index)
    {
        HashSet<int> obj;
        if (index < 0) return null;
        if (index >= _signalBeltsCapacity)
        {
            var newCapacity = _signalBeltsCapacity * 2;
            var newSignalBelts = new HashSet<int>[newCapacity];
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

    private static HashSet<int> GetSignalBelts(int index)
    {
        return index >= 0 && index < _signalBeltsCapacity ? _signalBelts[index] : null;
    }

    private static void RemoveSignalBelt(int factory, int beltId)
    {
        GetSignalBelts(factory)?.Remove(beltId);
    }

    private static void RemovePlanetSignalBelts(int factory)
    {
        GetSignalBelts(factory)?.Clear();
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
    private static void GameMain_Begin_Postfix()
    {
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
    public static void CargoTraffic_SetBeltSignalIcon_Postfix(CargoTraffic __instance, int signalId, int entityId)
    {
        var planet = GameMain.localPlanet;
        if (planet == null) return;
        var factory = __instance.factory;
        var factoryIndex = planet.factoryIndex;
        var beltId = factory.entityPool[entityId].beltId;
        if (signalId == 410)
        {
            SetSignalBelt(factoryIndex, beltId);
        }
        else
        {
            RemoveSignalBelt(factoryIndex, beltId);
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
    public static IEnumerable<CodeInstruction> GameData_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), nameof(PerformanceMonitor.EndSample)))
        ).Advance(1).Insert(
            Transpilers.EmitDelegate(() =>
            {
                var factories = GameMain.data?.factories;
                if (factories == null) return;
                foreach (var factory in factories)
                {
                    if (factory == null) continue;
                    var index = factory.index;
                    var belts = GetSignalBelts(index);
                    if (belts == null || belts.Count == 0) continue;
                    var consumeRegister = GameMain.statistics.production.factoryStatPool[index].consumeRegister;
                    var cargoTraffic = factory.cargoTraffic;
                    foreach (var beltId in belts)
                    {
                        ref var belt = ref cargoTraffic.beltPool[beltId];
                        var cargoPath = cargoTraffic.GetCargoPath(belt.segPathId);
                        if (cargoPath == null) continue;
                        int itemId;
                        if ((itemId = cargoPath.TryPickItem(belt.segIndex + belt.segPivotOffset - 5, 12, out var stack, out _)) <= 0) continue;
                        consumeRegister[itemId] += stack;
                        Dustbin.CalcGetSands(itemId, stack, 0);
                    }
                }
            })
        );
        return matcher.InstructionEnumeration();
    }
}
