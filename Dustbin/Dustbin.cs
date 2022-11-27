using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Dustbin;

class IsDusbinIndexer
{
    private bool[] store = new bool[256];

    public bool this[int index]
    {
        get
        {
            if (index < 0 || index >= store.Length) return false;
            return store[index];
        }
        set
        {
            if (index >= store.Length)
            {
                var oldLen = store.Length;
                var newLen = oldLen * 2;
                var oldArr = store;
                store = new bool[newLen];
                Array.Copy(oldArr, store, oldLen);
            }
            store[index] = value;
        }
    }

    public void Reset()
    {
        store = new bool[256];
    }
}

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Dustbin : BaseUnityPlugin
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger =
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
}
