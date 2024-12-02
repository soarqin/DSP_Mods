using System;
using System.Collections.Generic;
using HarmonyLib;
using UXAssist.Common;

namespace LogisticHub.Module;

public class ProductVeinData
{
    public readonly List<int> VeinIndices = [];
    public readonly HashSet<int> GroupIndices = [];
    public int GroupCount = 0;
}

public class VeinManager : PatchImpl<VeinManager>
{
    private static ProductVeinData[][] _veins;

    public static void Init()
    {
        Enable(true);
    }

    public static void Uninit()
    {
        Enable(false);
    }

    public static void Clear()
    {
        _veins = null;
    }

    public static ProductVeinData[] GetVeins(int planetIndex)
    {
        if (_veins == null || _veins.Length <= planetIndex)
            return null;
        return _veins[planetIndex];
    }

    private static ProductVeinData[] GetOrCreateVeins(int planetIndex)
    {
        if (_veins == null || _veins.Length <= planetIndex)
            Array.Resize(ref _veins, AuxData.AlignUpToPowerOf2(planetIndex + 1));
        var veins = _veins[planetIndex];
        if (veins != null) return veins;
        veins = new ProductVeinData[20];
        _veins[planetIndex] = veins;
        return veins;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Init))]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RecalculateVeinGroup))]
    [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RecalculateAllVeinGroups))]
    private static void PlanetFactory_RecalculateAllVeinGroups_Postfix(PlanetFactory __instance)
    {
        RecalcVeins(__instance);
    }

    private static void DebugLog()
    {
        foreach (var veins in _veins)
        {
            if (veins == null) continue;
            for (var i = 0; i < veins.Length; i++)
            {
                var pvd = veins[i];
                if (pvd == null) continue;
                LogisticHub.Logger.LogInfo($"Product {i} VeinTypeCount={pvd.VeinIndices.Count} GroupCount={pvd.GroupCount}");
            }
        }
    }

    private static void RecalcVeins(PlanetFactory factory)
    {
        var planetIndex = factory.index;
        var veins = GetOrCreateVeins(planetIndex);
        var veinPool = factory.veinPool;

        foreach (var pvd in veins)
        {
            if (pvd == null) continue;
            pvd.VeinIndices.Clear();
            pvd.GroupIndices.Clear();
            pvd.GroupCount = 0;
        }

        for (var i = factory.veinCursor - 1; i > 0; i--)
        {
            if (veinPool[i].id != i || veinPool[i].amount <= 0 || veinPool[i].type == EVeinType.None) continue;
            var productId = veinPool[i].productId - 1000;
            if (productId is < 0 or >= 20) continue;
            var pvd = veins[productId];
            if (pvd == null)
            {
                pvd = new ProductVeinData();
                veins[productId] = pvd;
            }

            pvd.VeinIndices.Add(i);
            pvd.GroupIndices.Add(veinPool[i].groupIndex);
        }

        foreach (var pvd in veins)
        {
            if (pvd == null) continue;
            pvd.GroupCount = pvd.GroupIndices.Count;
        }

        // DebugLog();
    }
}