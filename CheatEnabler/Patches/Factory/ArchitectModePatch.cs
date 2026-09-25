using System;
using HarmonyLib;
using UXAssist.Common;
using UXAssist.Common.GameConstants;

namespace CheatEnabler.Patches.Factory;

[PatchSetCallbackFlag(PatchCallbackFlag.CallOnDisableAfterUnpatch)]
internal class ArchitectMode : PatchImpl<ArchitectMode>
{
    private const int DefaultItemCount = 999;
    private static readonly AccessTools.FieldRef<BuildTool, StorageComponent> BuildToolPackage =
        AccessTools.FieldRefAccess<BuildTool, StorageComponent>(nameof(BuildTool.tmpPackage));

    protected override void OnEnable()
    {
        RefreshPackageStatistics();
        var factory = GameMain.mainPlayer?.factory;
        if (factory?.planet?.data != null)
        {
            global::CheatEnabler.Patches.FactoryPatch.ArrivePlanet(factory);
        }
    }

    protected override void OnDisable()
    {
        RefreshPackageStatistics();
    }

    private static void RefreshPackageStatistics()
    {
        var player = GameMain.mainPlayer;
        if (player?.package == null) return;
        player.packageUtility?.statistics?.Count(player.package);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PackageStatistics), nameof(PackageStatistics.Count))]
    private static void CountPackageItemsPatch(PackageStatistics __instance)
    {
        if (__instance != GameMain.mainPlayer?.packageUtility?.statistics) return;
        var items = LDB.items?.dataArray;
        if (items == null) return;
        foreach (var item in items)
        {
            if (item == null || !IsArchitectItem(item.ID)) continue;
            var count = __instance.itemBundle.GetCount(item.ID);
            if (count < DefaultItemCount) __instance.itemBundle.Add(item.ID, DefaultItemCount - count, 0);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.TakeTailItems), [typeof(int), typeof(int), typeof(int), typeof(bool)],
        [ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Out, ArgumentType.Normal])]
    public static bool TakeTailItemsPatch(StorageComponent __instance, int itemId, int count, ref int inc)
    {
        if (count <= 0 || !IsPlayerStorage(__instance) || !IsArchitectItem(itemId)) return true;
        inc = 0;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.TakeTailItems), [typeof(int), typeof(int), typeof(int[]), typeof(int), typeof(bool)],
        [ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal])]
    private static bool TakeTailItemsWithNeedsPatch(StorageComponent __instance, int itemId, int count, int[] needs, ref int inc, ref bool __result)
    {
        if (needs == null || Array.IndexOf(needs, itemId) < 0) return true;
        if (TakeTailItemsPatch(__instance, itemId, count, ref inc)) return true;
        __result = true;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerPackageUtility), nameof(PlayerPackageUtility.TryTakeItemFromAllPackages))]
    private static bool TryTakeItemFromAllPackagesPatch(PlayerPackageUtility __instance, int itemId, int count, ref int inc)
    {
        if (__instance.player == null || __instance.player != GameMain.mainPlayer || count <= 0 || !IsArchitectItem(itemId)) return true;
        inc = 0;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerPackageUtility), nameof(PlayerPackageUtility.TakeItemFromAllPackages))]
    private static bool TakeItemFromAllPackagesPatch(PlayerPackageUtility __instance, int itemId, int count, ref int inc)
    {
        return TryTakeItemFromAllPackagesPatch(__instance, itemId, count, ref inc);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.GetItemCount), typeof(int))]
    public static void GetItemCountPatch(StorageComponent __instance, int itemId, ref int __result)
    {
        if (__result >= DefaultItemCount || !IsPlayerStorage(__instance) || !IsArchitectItem(itemId)) return;
        __result = DefaultItemCount;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.GetItemCount), [typeof(int), typeof(int)],
        [ArgumentType.Normal, ArgumentType.Out])]
    private static void GetItemCountWithIncPatch(StorageComponent __instance, int itemId, ref int __result)
    {
        GetItemCountPatch(__instance, itemId, ref __result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerPackageUtility), nameof(PlayerPackageUtility.GetPackageItemCount))]
    private static void GetPackageItemCountPatch(PlayerPackageUtility __instance, int itemId, ref int __result)
    {
        if (__instance.player == null || __instance.player != GameMain.mainPlayer || __result >= DefaultItemCount || !IsArchitectItem(itemId)) return;
        __result = DefaultItemCount;
    }

    private static bool IsPlayerStorage(StorageComponent storage)
    {
        var player = GameMain.mainPlayer;
        if (storage == null || player == null) return false;
        if (storage == player.package) return true;
        var buildTools = player.controller?.actionBuild?.tools;
        if (buildTools == null) return false;
        foreach (var buildTool in buildTools)
        {
            if (buildTool != null && storage == BuildToolPackage(buildTool)) return true;
        }

        return false;
    }

    private static bool IsArchitectItem(int itemId)
    {
        if (itemId <= 0) return false;
        if (itemId is ItemIds.LogisticsBot or ItemIds.LogisticsDrone or ItemIds.LogisticsVessel) return true;
        var item = LDB.items?.Select(itemId);
        return item != null && (item.Type == EItemType.Logistics || item.CanBuild);
    }
}
