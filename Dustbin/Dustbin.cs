using System.Collections.Generic;
using System.IO;
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
    public static readonly int[] SandsFactors = new int[12001];

    public bool CheckVersion(string hostVersion, string clientVersion)
    {
        return hostVersion.Equals(clientVersion);
    }

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        var sandsFactorsStr = Config.Bind("General", "SandsFactors", "", "Sands get from different items\nFormat: id1:value1|id2:value2|...").Value;
        foreach (var s in sandsFactorsStr.Split('|'))
        {
            var sp = s.Split(':');
            if (sp.Length < 2) continue;
            if (!int.TryParse(sp[0], out var id) || id > 12000) continue;
            if (!int.TryParse(sp[1], out var factor)) continue;
            SandsFactors[id] = factor;
        }
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
