using System;
using System.Collections;
using System.IO;
using System.Linq;
using BepInEx;
using crecheng.DSPModSave;
using HarmonyLib;

namespace Dustbin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(DSPModSavePlugin.MODGUID)]
public class Dustbin : BaseUnityPlugin, IModCanSave
{
    private const ushort ModSaveVersion = 1;
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _cfgEnabled = true;
    public static readonly int[] SandsFactors = { 0, 1, 5, 10, 100 };
    public static bool[] IsFluid;

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
        if (version > 0)
        {
            StoragePatch.Import(r);
            TankPatch.Import(r);
        }
    }

    public void IntoOtherSave()
    {
    }
}
