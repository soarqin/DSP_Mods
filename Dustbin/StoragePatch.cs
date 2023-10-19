using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NebulaAPI;
using UnityEngine;

namespace Dustbin;

[HarmonyPatch]
public static class StoragePatch
{
    private static UI.MyCheckBox _storageDustbinCheckBox;
    private static int _lastStorageId;

    public static void Reset()
    {
        _lastStorageId = 0;
    }

    public static void Export(BinaryWriter w)
    {
        var tempStream = new MemoryStream();
        var tempWriter = new BinaryWriter(tempStream);
        var factories = GameMain.data.factories;
        var factoryCount = GameMain.data.factoryCount;
        for (var i = 0; i < factoryCount; i++)
        {
            var factory = factories[i];
            var storage = factory?.factoryStorage;
            var storagePool = storage?.storagePool;
            if (storagePool == null) continue;
            var cursor = storage.storageCursor;
            var count = 0;
            for (var j = 1; j < cursor; j++)
            {
                if (storagePool[j] == null || storagePool[j].id != j) continue;
                if (!storagePool[j].IsDustbin) continue;
                tempWriter.Write(j);
                count++;
            }
            if (count == 0) continue;

            tempWriter.Flush();
            tempStream.Position = 0;
            w.Write((byte)1);
            w.Write(factory.planetId);
            w.Write(count);
            /* FixMe: May BinaryWriter not sync with its BaseStream while subclass overrides Write()? */
            tempStream.CopyTo(w.BaseStream);
            tempStream.SetLength(0);
        }
        tempWriter.Dispose();
        tempStream.Dispose();
    }

    public static void Import(BinaryReader r)
    {
        while (r.PeekChar() == 1)
        {
            r.ReadByte();
            var planetId = r.ReadInt32();
            var planet = GameMain.data.galaxy.PlanetById(planetId);
            var storagePool = planet?.factory?.factoryStorage?.storagePool;
            if (storagePool == null)
            {
                for (var count = r.ReadInt32(); count > 0; count--)
                {
                    r.ReadInt32();
                }
                continue;
            }
            for (var count = r.ReadInt32(); count > 0; count--)
            {
                var id = r.ReadInt32();
                if (id > 0 && id < storagePool.Length && storagePool[id] != null && storagePool[id].id == id)
                {
                    storagePool[id].IsDustbin = true;
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIStorageWindow), "_OnCreate")]
    private static void UIStorageWindow__OnCreate_Postfix(UIStorageWindow __instance)
    {
        _storageDustbinCheckBox = UI.MyCheckBox.CreateCheckBox(false, __instance.transform, 50f, 50f, Localization.language == Language.zhCN ? "垃圾桶" : "Dustbin");
        var window = __instance;
        _storageDustbinCheckBox.OnChecked += () =>
        {
            var storageId = window.storageId;
            if (storageId <= 0) return;
            var storagePool = window.factoryStorage.storagePool;
            if (storagePool[storageId].id != storageId) return;
            var enabled = _storageDustbinCheckBox.Checked;
            storagePool[storageId].IsDustbin = enabled;
            if (!NebulaModAPI.IsMultiplayerActive) return;
            var planetId = window.factory.planetId;
            NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(new NebulaSupport.Packet.ToggleEvent(planetId, storageId, enabled));
        };
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIStorageWindow), "_OnUpdate")]
    private static void UIStorageWindow__OnUpdate_Postfix(UIStorageWindow __instance)
    {
        var storageId = __instance.storageId;
        if (_lastStorageId == storageId) return;
        _lastStorageId = storageId;
        if (storageId <= 0) return;
        var storagePool = __instance.factoryStorage.storagePool;
        if (storagePool[storageId].id != storageId) return;
        _storageDustbinCheckBox.Checked = storagePool[storageId].IsDustbin;
        if (__instance.transform is RectTransform rectTrans)
        {
            _storageDustbinCheckBox.rectTrans.anchoredPosition3D = new Vector3(50, 58 - rectTrans.sizeDelta.y, 0);
        }
    }
    
    /* Adopt fix from starfi5h's NebulaCompatiblilityAssist */
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIStorageGrid), "OnStorageSizeChanged")]
    private static void UIStorageGrid_OnStorageSizeChanged_Postfix()
    {
        // Due to storage window is empty when open in client, the size will change after receiving data from host
        _lastStorageId = 0; // Refresh UI to reposition button on client side
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DustbinAddItem(int itemId, int count)
    {
        var fluidArr = Dustbin.IsFluid;
        var sandIndex = itemId < fluidArr.Length && fluidArr[itemId]
            ? 0
            : itemId switch
            {
                1005 => 2,
                1003 => 3,
                1013 => 4,
                _ => 1,
            };
        var sandsPerItem = Dustbin.SandsFactors[sandIndex];
        if (sandsPerItem <= 0) return count;
        var player = GameMain.mainPlayer;
        var addCount = count * sandsPerItem;
        player.sandCount += addCount;
        GameMain.history.OnSandCountChange(player.sandCount, addCount);
        return count;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(StorageComponent), "AddItem", new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    private static IEnumerable<CodeInstruction> StorageComponent_AddItem_HarmonyTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        var label1 = generator.DefineLabel();
        matcher.Start().MatchForward(false,
            new CodeMatch(OpCodes.Stind_I4)
        ).Advance(1).InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageComponent), nameof(StorageComponent.IsDustbin))),
            new CodeInstruction(OpCodes.Brfalse, label1),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoragePatch), nameof(StoragePatch.DustbinAddItem))),
            new CodeInstruction(OpCodes.Ret)
        );
        matcher.Labels.Add(label1);
        return matcher.InstructionEnumeration();
    }

    /* We keep this to make MOD compatible with older version */
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StorageComponent), "Import")]
    private static void StorageComponent_Import_Postfix(StorageComponent __instance)
    {
        if (__instance.bans >= 0)
            return;
        __instance.bans = -__instance.bans - 1;
        __instance.IsDustbin = true;
    }
}
