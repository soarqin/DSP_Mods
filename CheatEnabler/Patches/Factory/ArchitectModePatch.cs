using HarmonyLib;
using UXAssist.Common;

namespace CheatEnabler.Patches.Factory;

internal class ArchitectMode : PatchImpl<ArchitectMode>
{
    private static bool[] _canBuildItems;

    protected override void OnEnable()
    {
        var factory = GameMain.mainPlayer?.factory;
        if (factory?.planet?.data != null)
        {
            FactoryPatch.ArrivePlanet(factory);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.TakeTailItems), [typeof(int), typeof(int), typeof(int), typeof(bool)],
        [ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Out, ArgumentType.Normal])]
    [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.TakeTailItems), [typeof(int), typeof(int), typeof(int[]), typeof(int), typeof(bool)],
        [ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal])]
    public static bool TakeTailItemsPatch(StorageComponent __instance, int itemId)
    {
        if (__instance == null || GameMain.mainPlayer == null || __instance.id != GameMain.mainPlayer.package.id) return true;
        if (itemId <= 0) return true;
        if (_canBuildItems == null)
        {
            DoInit();
        }

        return itemId >= 12000 || !_canBuildItems[itemId];
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.GetItemCount), typeof(int))]
    public static void GetItemCountPatch(StorageComponent __instance, int itemId, ref int __result)
    {
        if (__result > 99) return;
        if (__instance == null || GameMain.mainPlayer == null || __instance.id != GameMain.mainPlayer.package.id) return;
        if (itemId <= 0) return;
        if (_canBuildItems == null)
        {
            DoInit();
        }
        if (itemId < 12000 && _canBuildItems[itemId]) __result = 100;
    }

    private static void DoInit()
    {
        _canBuildItems = new bool[12000];
        foreach (var ip in LDB.items.dataArray)
        {
            if ((ip.Type == EItemType.Logistics || ip.CanBuild) && ip.ID < 12000) _canBuildItems[ip.ID] = true;
        }
    }
}
