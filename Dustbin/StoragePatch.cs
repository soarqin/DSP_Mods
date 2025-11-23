using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using HarmonyLib;
using NebulaAPI;
using UnityEngine;

namespace Dustbin;

[HarmonyPatch]
public static class StoragePatch
{
    private static UI.MyCheckBox _storageDustbinCheckBox;
    private static int _lastStorageId;
    private static Harmony _patch;

    public static void Enable(bool on)
    {
        if (on)
        {
            _patch ??= Harmony.CreateAndPatchAll(typeof(StoragePatch));
        }
        else
        {
            _patch?.UnpatchSelf();
            _patch = null;
        }
    }

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameMain), nameof(GameMain.Start))]
    private static void GameMain_Start_Prefix()
    {
        Reset();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIStorageWindow), nameof(UIStorageWindow._OnCreate))]
    private static void UIStorageWindow__OnCreate_Postfix(UIStorageWindow __instance)
    {
        _storageDustbinCheckBox = UI.MyCheckBox.CreateCheckBox(false, __instance.transform, 50f, 50f, Localization.CurrentLanguageLCID == Localization.LCID_ZHCN ? "垃圾桶" : "Dustbin");
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
    [HarmonyPatch(typeof(UIStorageWindow), nameof(UIStorageWindow._OnUpdate))]
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
            _storageDustbinCheckBox.rectTrans.anchoredPosition3D = new Vector3(190, 57 - rectTrans.sizeDelta.y, 0);
        }
    }

    /* Adopt fix from starfi5h's NebulaCompatiblilityAssist */
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIStorageGrid), nameof(UIStorageGrid.OnStorageSizeChanged))]
    private static void UIStorageGrid_OnStorageSizeChanged_Postfix()
    {
        // Due to storage window is empty when open in client, the size will change after receiving data from host
        _lastStorageId = 0; // Refresh UI to reposition button on client side
    }

    private static void LogTranspilerInfo(string stage, int itemId, int count, int inc, bool isDustbin, bool enteringDustbinLogic)
    {
        Dustbin.Logger.LogInfo($"Transpiler {stage} - ItemID: {itemId}, Count: {count}, Inc: {inc}, IsDustbin: {isDustbin}, EnteringDustbinLogic: {enteringDustbinLogic}");
    }

    private static void LogReturnValue(int returnValue, string context)
    {
        Dustbin.Logger.LogInfo($"Return Value - Context: {context}, Value: {returnValue}");
    }

    private static void LogInserterState(string operation, int itemId, int count, int inc, int storageId)
    {
        Dustbin.Logger.LogInfo($"分拣器状态 - 操作: {operation}, 物品ID: {itemId}, 数量: {count}, 增产剂: {inc}, 储物仓ID: {storageId}");
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.AddItem), [typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool)],
        [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal])]
    private static IEnumerable<CodeInstruction> StorageComponent_AddItem_HarmonyTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = new List<CodeInstruction>(instructions);
        var matcher = new CodeMatcher(codes, generator);
        
        // 找到方法开始处
        matcher.Start();
        
        // 定义标签用于跳转
        var skipDustbinLogic = generator.DefineLabel();
        
        // 在方法开始处插入安全检查
        matcher.InsertAndAdvance(
            // 安全检查：确保StorageComponent实例不为null
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Brfalse, skipDustbinLogic),
            
            // 检查是否是垃圾桶
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageComponent), nameof(StorageComponent.IsDustbin))),
            new CodeInstruction(OpCodes.Brfalse, skipDustbinLogic),
            
            // 检查count是否大于0
            new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ble, skipDustbinLogic),
            
            // 记录进入垃圾桶处理逻辑
            new CodeInstruction(OpCodes.Ldstr, "Processing Dustbin"),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Ldarg_3),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageComponent), nameof(StorageComponent.IsDustbin))),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoragePatch), nameof(LogTranspilerInfo))),
            
            // 记录分拣器状态
            new CodeInstruction(OpCodes.Ldstr, "Before CalcGetSands"),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Ldarg_3),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageComponent), nameof(StorageComponent.id))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoragePatch), nameof(LogInserterState))),
            
            // 调用CalcGetSands处理物品销毁和沙子生成
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Ldarg_3),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Dustbin), nameof(Dustbin.CalcGetSands))),
            
            // 记录返回值
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Ldstr, "CalcGetSands Return"),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoragePatch), nameof(LogReturnValue))),
            
            // 设置remainInc为0（第5个参数，索引4）
            new CodeInstruction(OpCodes.Ldarg, 4),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Stind_I4),
            
            // 返回count表示成功销毁
            new CodeInstruction(OpCodes.Ret),
            
            // 跳过垃圾桶逻辑的标签
            new CodeInstruction(OpCodes.Nop).WithLabels(skipDustbinLogic)
        );
        
        return matcher.InstructionEnumeration();
    }

    /* We keep this to make MOD compatible with older version */
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.Import))]
    private static void StorageComponent_Import_Postfix(StorageComponent __instance)
    {
        if (__instance.bans >= 0)
            return;
        __instance.bans = -__instance.bans - 1;
        __instance.IsDustbin = true;
    }
}
