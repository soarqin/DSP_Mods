using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using UXAssist.Common;

namespace UXAssist.Patches;

public class PersistPatch : PatchImpl<PersistPatch>
{
    public static void Start()
    {
        Enable(true);
    }

    public static void Uninit()
    {
        Enable(false);
    }

    // Check for noModifier while pressing hotkeys on build bar
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIBuildMenu), nameof(UIBuildMenu._OnUpdate))]
    private static IEnumerable<CodeInstruction> UIBuildMenu__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.inScreen)))
        );
        matcher.Repeat(codeMatcher =>
        {
            var jumpPos = codeMatcher.Advance(1).Operand;
            codeMatcher.Advance(-1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.noModifier))),
                new CodeInstruction(OpCodes.Brfalse_S, jumpPos)
            ).Advance(2);
        });
        return matcher.InstructionEnumeration();
    }

    // Bring popup tip window to top layer
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIButton), nameof(UIButton.LateUpdate))]
    private static IEnumerable<CodeInstruction> UIButton_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldloc || ci.opcode == OpCodes.Ldloc_S),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject))),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.activeSelf)))
        );
        var ldLocOpr = matcher.Operand;
        var labels = matcher.Labels;
        matcher.Labels = null;
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldloc_S, ldLocOpr).WithLabels(labels),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.parent))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.parent))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), nameof(Transform.SetAsLastSibling)))
        );
        return matcher.InstructionEnumeration();
    }

    // Sort blueprint data when pasting
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildTool_BlueprintCopy), nameof(BuildTool_BlueprintCopy.UseToPasteNow))]
    private static IEnumerable<CodeInstruction> BuildTool_BlueprintCopy_UseToPasteNow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool_BlueprintCopy), nameof(BuildTool_BlueprintCopy.RefreshBlueprintData)))
        ).Advance(2).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_BlueprintCopy), nameof(BuildTool_BlueprintCopy.blueprint))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Functions.FactoryFunctions), nameof(Functions.FactoryFunctions.SortBlueprintData)))
        );
        return matcher.InstructionEnumeration();
    }

    // Sort blueprint data when saving
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlueprintData), nameof(BlueprintData.SaveBlueprintData))]
    private static void BlueprintData_SaveBlueprintData_Prefix(BlueprintData __instance)
    {
        if (!__instance.isValid) return;
        Functions.FactoryFunctions.SortBlueprintData(__instance);
    }

    // Increase maximum value of property realizing, 2000 -> 20000
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.UpdateUIElements))]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.OnRealizeButtonClick))]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.OnInputValueEnd))]
    private static IEnumerable<CodeInstruction> UIProductEntry_UpdateUIElements_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldc_I4, 2000)
        );
        matcher.Repeat(m => { m.SetAndAdvance(OpCodes.Ldc_I4, 20000); });
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIPropertyEntry), nameof(UIPropertyEntry.OnInputValueEnd))]
    private static IEnumerable<CodeInstruction> UIProductEntry_OnInputValueEnd_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_R4 && ci.OperandIs(2000f))
        );
        matcher.Repeat(m => { m.SetAndAdvance(OpCodes.Ldc_R4, 20000f); });
        return matcher.InstructionEnumeration();
    }

    // Increase capacity of player order queue, 16 -> 128
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlayerOrder), MethodType.Constructor, typeof(Player))]
    private static IEnumerable<CodeInstruction> PlayerOrder_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => (ci.opcode == OpCodes.Ldc_I4_S || ci.opcode == OpCodes.Ldc_I4) && ci.OperandIs(16))
        );
        matcher.Repeat(m => { m.SetAndAdvance(OpCodes.Ldc_I4, 128); });
        return matcher.InstructionEnumeration();
    }

    // Increase Player Command Queue from 16 to 128
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlayerOrder), nameof(PlayerOrder._trimEnd))]
    [HarmonyPatch(typeof(PlayerOrder), nameof(PlayerOrder.Enqueue))]
    private static IEnumerable<CodeInstruction> PlayerOrder_ExtendCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => (ci.opcode == OpCodes.Ldc_I4_S || ci.opcode == OpCodes.Ldc_I4) && ci.OperandIs(16))
        );
        matcher.Repeat(m => { m.SetAndAdvance(OpCodes.Ldc_I4, 128); });
        return matcher.InstructionEnumeration();
    }

    // Allow F11 in star map
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnLateUpdate))]
    private static IEnumerable<CodeInstruction> UIGame__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.inFullscreenGUI))),
            new CodeMatch(ci => ci.opcode == OpCodes.Brfalse || ci.opcode == OpCodes.Brfalse_S)
        );
        var jumpPos = matcher.Advance(1).Operand;
        matcher.Advance(-1).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIGame), nameof(UIGame.starmap))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ManualBehaviour), nameof(ManualBehaviour.active))),
            new CodeInstruction(OpCodes.Brtrue_S, jumpPos)
        );
        return matcher.InstructionEnumeration();
    }

    // Ignore UIDFCommunicatorWindow.Determine()
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIDFCommunicatorWindow), nameof(UIDFCommunicatorWindow.Determine))]
    private static bool UIDFCommunicatorWindow_Determine_Prefix()
    {
        return false;
    }

    #region Cluster Upload Result

    const int ClusterUploadResultKeepCount = 100;
    private static readonly ClusterUploadResult[] _clusterUploadResults = new ClusterUploadResult[ClusterUploadResultKeepCount];
    private static readonly object _clusterUploadResultsLock = new();
    private static int _clusterUploadResultsHead = 0;
    private static int _clusterUploadResultsCount = 0;
    private static ClusterPlayerData[] _topTenPlayerData = null;

    private struct ClusterUploadResult
    {
        public DateTime UploadTime;
        public int Result;
        public float RequestTime;
    }

    private static void AddClusterUploadResult(int result, float requestTime)
    {
        lock (_clusterUploadResultsLock)
        {
            if (_clusterUploadResultsCount >= ClusterUploadResultKeepCount)
            {
                _clusterUploadResults[_clusterUploadResultsHead] = new ClusterUploadResult { UploadTime = DateTime.Now, Result = result, RequestTime = requestTime };
                _clusterUploadResultsHead = (_clusterUploadResultsHead + 1) % ClusterUploadResultKeepCount;
            }
            else
            {
                _clusterUploadResults[(_clusterUploadResultsHead + _clusterUploadResultsCount) % ClusterUploadResultKeepCount] = new ClusterUploadResult { UploadTime = DateTime.Now, Result = result, RequestTime = requestTime };
                _clusterUploadResultsCount++;
            }
        }
    }

    public static void ExportClusterUploadResults(BinaryWriter w)
    {
        lock (_clusterUploadResultsLock)
        {
            w.Write(_clusterUploadResultsCount);
            w.Write(_clusterUploadResultsHead);
            for (var i = 0; i < _clusterUploadResultsCount; i++)
            {
                ref var result = ref _clusterUploadResults[(i + _clusterUploadResultsHead) % ClusterUploadResultKeepCount];
                w.Write(result.UploadTime.ToBinary());
                w.Write(result.Result);
                w.Write(result.RequestTime);
            }
        }
    }

    public static void ImportClusterUploadResults(BinaryReader r)
    {
        lock (_clusterUploadResultsLock)
        {
            _clusterUploadResultsCount = r.ReadInt32();
            _clusterUploadResultsHead = r.ReadInt32();
            for (var i = 0; i < _clusterUploadResultsCount; i++)
            {
                ref var result = ref _clusterUploadResults[(i + _clusterUploadResultsHead) % ClusterUploadResultKeepCount];
                result.UploadTime = DateTime.FromBinary(r.ReadInt64());
                result.Result = r.ReadInt32();
                result.RequestTime = r.ReadSingle();
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MilkyWayWebClient), nameof(MilkyWayWebClient.OnUploadLoginErrored))]
    private static void MilkyWayWebClient_OnUploadLoginErrored_Postfix(MilkyWayWebClient __instance, DSPWeb.HTTP_ERROR_TYPE errorType, string errorInfo, int errorCode)
    {
        switch (errorType)
        {
            case DSPWeb.HTTP_ERROR_TYPE.NETWORK_ERROR:
                AddClusterUploadResult(-10001, (float)__instance.uploadRequest.reqTime);
                break;
            case DSPWeb.HTTP_ERROR_TYPE.HTTP_ERROR:
                AddClusterUploadResult(-10010 - errorCode, (float)__instance.uploadRequest.reqTime);
                break;
            case DSPWeb.HTTP_ERROR_TYPE.USER_ABORT:
                AddClusterUploadResult(-10003, (float)__instance.uploadRequest.reqTime);
                break;
            case DSPWeb.HTTP_ERROR_TYPE.UNEXPECTED_ERROR:
                AddClusterUploadResult(-10004, (float)__instance.uploadRequest.reqTime);
                break;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MilkyWayWebClient), nameof(MilkyWayWebClient.OnUploadErrored))]
    private static void MilkyWayWebClient_OnUploadErrored_Postfix(MilkyWayWebClient __instance, DSPWeb.HTTP_ERROR_TYPE errorType, string errorInfo, int errorCode)
    {
        switch (errorType)
        {
            case DSPWeb.HTTP_ERROR_TYPE.NETWORK_ERROR:
                AddClusterUploadResult(-101, (float)__instance.uploadRequest.reqTime);
                break;
            case DSPWeb.HTTP_ERROR_TYPE.HTTP_ERROR:
                AddClusterUploadResult(-110 - errorCode, (float)__instance.uploadRequest.reqTime);
                break;
            case DSPWeb.HTTP_ERROR_TYPE.USER_ABORT:
                AddClusterUploadResult(-103, (float)__instance.uploadRequest.reqTime);
                break;
            case DSPWeb.HTTP_ERROR_TYPE.UNEXPECTED_ERROR:
                AddClusterUploadResult(-104, (float)__instance.uploadRequest.reqTime);
                break;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MilkyWayWebClient), nameof(MilkyWayWebClient.OnUploadSucceed))]
    private static void MilkyWayWebClient_OnUploadSucceed_Postfix(MilkyWayWebClient __instance, DownloadHandler handler)
    {
        if (!int.TryParse(handler.text, out var rcode))
            rcode = -1;
        AddClusterUploadResult(rcode, (float)__instance.uploadRequest.reqTime);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(MilkyWayCache), nameof(MilkyWayCache.LoadTopTenPlayerData))]
    private static IEnumerable<CodeInstruction> MilkyWayCache_LoadTopTenPlayerData_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_1),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(BinaryReader), nameof(BinaryReader.ReadInt32)))
        ).Advance(2).InsertAndAdvance(
            new CodeInstruction(OpCodes.Dup),
            Transpilers.EmitDelegate((int count) => _topTenPlayerData = new ClusterPlayerData[count])
        );

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldloca_S),
            new CodeMatch(OpCodes.Ldarg_1),
            new CodeMatch(OpCodes.Callvirt),
            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(ClusterPlayerData), nameof(ClusterPlayerData.isAnon)))
        );
        var objloc = matcher.Operand;
        matcher.Advance(4);
        var iinstr = matcher.Instruction.Clone();
        matcher.InsertAndAdvance(
            iinstr,
            new CodeInstruction(OpCodes.Ldloca_S, objloc),
            Transpilers.EmitDelegate((int index, ref ClusterPlayerData playerData) =>
            {
                if (index < _topTenPlayerData.Length) _topTenPlayerData[index] = playerData;
            })
        );

        return matcher.InstructionEnumeration();
    }

    #endregion
}
