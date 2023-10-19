using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using crecheng.DSPModSave;
using HarmonyLib;
using NebulaAPI;

namespace Dustbin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(DSPModSavePlugin.MODGUID)]
[BepInDependency(NebulaModAPI.API_GUID)]
public class Dustbin : BaseUnityPlugin, IModCanSave, IMultiplayerMod
{
    public string Version => PluginInfo.PLUGIN_VERSION;

    private const ushort ModSaveVersion = 1;

    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _cfgEnabled = true;
    public static readonly int[] SandsFactors = { 0, 1, 5, 10, 100 };
    public static bool[] IsFluid;

    public bool CheckVersion(string hostVersion, string clientVersion)
    {
        return hostVersion.Equals(clientVersion);
    }

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        SandsFactors[1] = Config.Bind("General", "SandsPerItem", SandsFactors[1], "Sands gathered from normal items").Value;
        SandsFactors[0] = Config.Bind("General", "SandsPerFluid", SandsFactors[0], "Sands gathered from fluids").Value;
        SandsFactors[2] = Config.Bind("General", "SandsPerStone", SandsFactors[2], "Sands gathered from stones").Value;
        SandsFactors[3] = Config.Bind("General", "SandsPerSilicon", SandsFactors[3], "Sands gathered from silicon ores").Value;
        SandsFactors[4] = Config.Bind("General", "SandsPerFractal", SandsFactors[4], "Sands gathered from fractal silicon ores").Value;
        Harmony.CreateAndPatchAll(typeof(Dustbin));
        Harmony.CreateAndPatchAll(typeof(StoragePatch));
        Harmony.CreateAndPatchAll(typeof(TankPatch));

        NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
        NebulaModAPI.OnPlanetLoadFinished += RequestPlanetDustbinData;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameMain), "Start")]
    private static void GameMain_Start_Prefix()
    {
        StoragePatch.Reset();
        TankPatch.Reset();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
    private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        var maxId = ItemProto.fluids.Max();
        IsFluid = new bool[maxId + 1];
        foreach (var id in ItemProto.fluids)
            IsFluid[id] = true;
    }

    public void Export(BinaryWriter w)
    {
        w.Write(ModSaveVersion);
        StoragePatch.Export(w);
        TankPatch.Export(w);
    }

    public void Import(BinaryReader r)
    {
        var version = r.ReadUInt16();
        if (version <= 0) return;
        StoragePatch.Import(r);
        TankPatch.Import(r);
    }

    public void IntoOtherSave()
    {
    }

    public static byte[] ExportData(PlanetFactory factory)
    {
        var planetId = factory.planetId;
        var storageIds = new List<int>();
        var tankIds = new List<int>();

        var factoryStorage = factory.factoryStorage;
        var storagePool = factoryStorage.storagePool;
        for (var i = 1; i < factoryStorage.storageCursor; i++)
        {
            var storage = storagePool[i];
            if (storage == null || storage.id != i) continue;
            if (storage.IsDustbin)
                storageIds.Add(i);
        }

        var tankPool = factoryStorage.tankPool;
        for (var i = 1; i < factoryStorage.tankCursor; i++)
        {
            ref var tank = ref tankPool[i];
            if (tank.id != i) continue;
            if (tank.IsDustbin)
                tankIds.Add(i);
        }

        using var p = NebulaModAPI.GetBinaryWriter();
        using var w = p.BinaryWriter;
        w.Write(planetId);
        w.Write(storageIds.Count);
        foreach (var storageId in storageIds)
            w.Write(storageId);
        w.Write(tankIds.Count);
        foreach (var tankId in tankIds)
            w.Write(tankId);
        return p.CloseAndGetBytes();
    }

    public static void ImportData(byte[] bytes)
    {
        using var p = NebulaModAPI.GetBinaryReader(bytes);
        using var r = p.BinaryReader;
        var planetId = r.ReadInt32();
        var factory = GameMain.galaxy.PlanetById(planetId)?.factory;
        if (factory == null) return;

        var factoryStorage = factory.factoryStorage;
        var count = r.ReadInt32();
        var storagePool = factoryStorage.storagePool;
        var cursor = factoryStorage.storageCursor;
        for (var i = 1; i < cursor; i++)
        {
            var storage = storagePool[i];
            if (storage == null || storage.id != i) continue;
            storage.IsDustbin = false;
        }

        for (var i = 0; i < count; i++)
        {
            var id = r.ReadInt32();
            storagePool[id].IsDustbin = true;
        }

        count = r.ReadInt32();
        var tankPool = factoryStorage.tankPool;
        cursor = factoryStorage.tankCursor;
        for (var i = 1; i < cursor; i++)
        {
            ref var tank = ref tankPool[i];
            if (tank.id != i) continue;
            tank.IsDustbin = false;
        }

        for (var i = 0; i < count; i++)
        {
            var id = r.ReadInt32();
            if (id >= cursor) continue;
            ref var tank = ref tankPool[id];
            if (tank.id != id) continue;
            tank.IsDustbin = true;
        }
    }

    public void RequestPlanetDustbinData(int planetId)
    {
        if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new NebulaSupport.Packet.ToggleEvent(planetId, 0, false));
    }
}
