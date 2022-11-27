using HarmonyLib;
using UnityEngine;

namespace Dustbin;

[HarmonyPatch]
public static class StoragePatch
{
    private static MyCheckBox _storageDustbinCheckBox;
    private static int lastStorageId;
    private static IsDusbinIndexer storageIsDustbin = new();

    public static void Reset()
    {
        storageIsDustbin.Reset();
        lastStorageId = 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIStorageWindow), "_OnCreate")]
    private static void UIStorageWindow__OnCreate_Postfix(UIStorageWindow __instance)
    {
        _storageDustbinCheckBox = MyCheckBox.CreateCheckBox(false, __instance.transform, 50f, 50f, Localization.language == Language.zhCN ? "垃圾桶" : "Dustbin");
        var window = __instance;
        _storageDustbinCheckBox.OnChecked += () =>
        {
            var storageId = window.storageId;
            if (storageId <= 0 || window.factoryStorage.storagePool[storageId].id != storageId) return;
            storageIsDustbin[storageId] = _storageDustbinCheckBox.Checked;
        };
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIStorageWindow), "_OnUpdate")]
    private static void UIStorageWindow__OnUpdate_Postfix(UIStorageWindow __instance)
    {
        var storageId = __instance.storageId;
        if (lastStorageId == storageId) return;
        lastStorageId = storageId;
        if (storageId > 0 && __instance.factoryStorage.storagePool[storageId].id == storageId)
        {
            _storageDustbinCheckBox.Checked = storageIsDustbin[storageId];
        }
        if (__instance.transform is RectTransform rectTrans) {
            _storageDustbinCheckBox.rectTrans.anchoredPosition3D = new Vector3(50, 58 - rectTrans.sizeDelta.y, 0);
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StorageComponent), "AddItem",
        new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool) },
        new[]
        {
            ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal
        })]
    private static bool StorageComponent_AddItem_Prefix(ref int __result, StorageComponent __instance, int itemId, int count, int inc,
        ref int remainInc, bool useBan = false)
    {
        if (!storageIsDustbin[__instance.id]) return true;
        remainInc = inc;
        __result = count;
        var fluidArr = Dustbin.IsFluid;
        var sandsPerItem = Dustbin.SandsFactors[itemId < fluidArr.Length && fluidArr[itemId]
            ? 0
            : itemId switch
            {
                1005 => 2,
                1003 => 3,
                1013 => 4,
                _ => 1,
            }];
        if (sandsPerItem <= 0) return false;
        var player = GameMain.mainPlayer;
        var addCount = count * sandsPerItem;
        player.sandCount += addCount;
        GameMain.history.OnSandCountChange(player.sandCount, addCount);
        /* Following line crashes game, seems that it should not be called in this working thread:
         *   UIRoot.instance.uiGame.OnSandCountChanged(player.sandCount, addCount);
         */
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StorageComponent), "Export")]
    private static void StorageComponent_Export_Prefix(StorageComponent __instance, out int __state)
    {
        if (storageIsDustbin[__instance.id])
        {
            __state = __instance.bans;
            __instance.bans = -__instance.bans - 1;
        }
        else
        {
            __state = -1;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StorageComponent), "Export")]
    private static void StorageComponent_Export_Postfix(StorageComponent __instance, int __state)
    {
        if (__state < 0) return;
        __instance.bans = __state;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StorageComponent), "Import")]
    private static void StorageComponent_Import_Postfix(StorageComponent __instance)
    {
        if ((__instance.bans & 0x8000) == 0)
            storageIsDustbin[__instance.id] = false;
        else
        {
            storageIsDustbin[__instance.id] = true;
            __instance.bans ^= 0x8000;
        }
    }
}
