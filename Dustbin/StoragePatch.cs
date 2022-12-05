using System.IO;
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

    public static void Export(BinaryWriter w)
    {
        var tempStream = new MemoryStream();
        var tempWriter = new BinaryWriter(tempStream);
        int count = 0;
        storageIsDustbin.ForEachIsDustbin(i =>
        {
            tempWriter.Write(i);
            count++;
        });
        w.Write(count);
        tempStream.Position = 0;
        /* FixMe: May BinaryWriter not sync with its BaseStream while subclass overrides Write()? */
        tempStream.CopyTo(w.BaseStream);
        tempWriter.Dispose();
        tempStream.Dispose();
    }

    public static void Import(BinaryReader r)
    {
        for (var count = r.ReadInt32(); count > 0; count--)
        {
            storageIsDustbin[r.ReadInt32()] = true;
        }
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
    [HarmonyPatch(typeof(FactoryStorage), "RemoveStorageComponent")]
    private static void FactoryStorage_RemoveStorageComponent_Prefix(FactoryStorage __instance, int id)
    {
        var storage = __instance.storagePool[id];
        if (storage != null && storage.id != 0)
        {
            storageIsDustbin[id] = false;
        }
    }

    /* We keep this to make MOD compatible with older version */
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StorageComponent), "Import")]
    private static void StorageComponent_Import_Postfix(StorageComponent __instance)
    {
        if (__instance.bans >= 0)
            return;
        storageIsDustbin[__instance.id] = true;
        __instance.bans = -__instance.bans - 1;
    }
}
