using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UXAssist.Functions;

namespace UXAssist.ModsCompat;

class BlueprintTweaks
{
    public const string BlueprintTweaksGuid = "org.kremnev8.plugin.BlueprintTweaks";
    private static FieldInfo selectObjIdsField;
    private static Type classTypeBlueprintTweaksPlugin;
    private static Type classTypeUIBuildingGridPatch2;

    public static bool Run(Harmony harmony)
    {
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(BlueprintTweaksGuid, out var pluginInfo)) return false;
        var assembly = pluginInfo.Instance.GetType().Assembly;
        var classTypeDragRemoveBuildTool = assembly.GetType("BlueprintTweaks.DragRemoveBuildTool");
        if (classTypeDragRemoveBuildTool == null) return false;
        if (AccessTools.Method(classTypeDragRemoveBuildTool, "DetermineMorePreviews") != null) return true;
        classTypeBlueprintTweaksPlugin = assembly.GetType("BlueprintTweaks.BlueprintTweaksPlugin");
        classTypeUIBuildingGridPatch2 = assembly.GetType("BlueprintTweaks.UIBuildingGridPatch2");
        var UIBuildingGrid_Update = AccessTools.Method(typeof(UIBuildingGrid), nameof(UIBuildingGrid.Update));
        harmony.Patch(AccessTools.Method(classTypeUIBuildingGridPatch2, "UpdateGrid"), null, null, new HarmonyMethod(AccessTools.Method(typeof(BlueprintTweaks), nameof(PatchUpdateGrid))));
        selectObjIdsField = AccessTools.Field(classTypeDragRemoveBuildTool, "selectObjIds");
        harmony.Patch(AccessTools.Method(classTypeDragRemoveBuildTool, "DeterminePreviews"),
            new HarmonyMethod(AccessTools.Method(typeof(BlueprintTweaks), nameof(PatchDeterminePreviews))));
        return true;
    }

    private static readonly int zMin = Shader.PropertyToID("_ZMin");
    private static readonly int reformMode = Shader.PropertyToID("_ReformMode");

    private static IEnumerable<CodeInstruction> PatchUpdateGrid(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(classTypeBlueprintTweaksPlugin, "tool")),
            new CodeMatch(OpCodes.Ldfld),
            new CodeMatch(ci => ci.opcode == OpCodes.Brfalse || ci.opcode == OpCodes.Brfalse_S)
        );
        var label1 = generator.DefineLabel();
        matcher.Advance(2).Operand = label1;
        matcher.Advance(1);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIBuildingGrid), nameof(UIBuildingGrid.blueprintMaterial))),
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(classTypeUIBuildingGridPatch2, "tintColor")),
            new CodeMatch(OpCodes.Call),
            new CodeMatch(OpCodes.Callvirt)
        ).RemoveInstructions(5);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIBuildingGrid), nameof(UIBuildingGrid.blueprintMaterial))),
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(classTypeUIBuildingGridPatch2, "cursorGratBox")),
            new CodeMatch(OpCodes.Ldsfld),
            new CodeMatch(OpCodes.Ldfld),
            new CodeMatch(OpCodes.Call),
            new CodeMatch(OpCodes.Callvirt)
        ).Advance(1).Operand = AccessTools.Field(typeof(UIBuildingGrid), nameof(UIBuildingGrid.material));
        matcher.Advance(6);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIBuildingGrid), nameof(UIBuildingGrid.blueprintMaterial))),
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(classTypeUIBuildingGridPatch2, "selectColor")),
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld),
            new CodeMatch(OpCodes.Call),
            new CodeMatch(OpCodes.Callvirt)
        ).Advance(1).Operand = AccessTools.Field(typeof(UIBuildingGrid), nameof(UIBuildingGrid.material));
        matcher.Advance(1).Operand = AccessTools.Field(classTypeUIBuildingGridPatch2, "tintColor");
        matcher.Advance(5);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIBuildingGrid), nameof(UIBuildingGrid.blueprintMaterial))),
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(classTypeUIBuildingGridPatch2, "showDivideLine")),
            new CodeMatch(OpCodes.Ldc_R4, 0f),
            new CodeMatch(OpCodes.Callvirt)
        ).RemoveInstructions(5);
        matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
            Transpilers.EmitDelegate((UIBuildingGrid grid) =>
            {
                grid.material.SetFloat(reformMode, 0f);
                grid.material.SetFloat(zMin, -0.5f);
            })
        );
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIBuildingGrid), nameof(UIBuildingGrid.blueprintGridRnd))),
            new CodeMatch(OpCodes.Ldc_I4_1),
            new CodeMatch(OpCodes.Callvirt),
            new CodeMatch(ci => ci.opcode == OpCodes.Br || ci.opcode == OpCodes.Br_S)
        ).RemoveInstructions(4).Set(OpCodes.Ret, null).Labels.Add(label1);
        return matcher.InstructionEnumeration();
    }

    private static void PatchDeterminePreviews(object __instance)
    {
        var selectObjIds = (HashSet<int>)selectObjIdsField.GetValue(__instance);
        var buildTool = (BuildTool)__instance;
        var factory = buildTool.factory;
        HashSet<int> extraObjIds = [];
        foreach (var objId in selectObjIds)
        {
            var desc = buildTool.GetPrefabDesc(objId);
            var isBelt = desc.isBelt;
            var isInserter = desc.isInserter;
            if (isInserter) continue;
            if (isBelt)
            {
                var needCheck = false;
                for (var j = 0; j < 2; j++)
                {
                    factory.ReadObjectConn(objId, j, out _, out var connObjId, out _);
                    if (connObjId == 0 || FactoryFunctions.ObjectIsBeltOrInserter(factory, connObjId)) continue;
                    needCheck = true;
                    break;
                }
                if (needCheck)
                {
                    for (var k = 0; k < 16; k++)
                    {
                        factory.ReadObjectConn(objId, k, out _, out var connObjId, out _);
                        if (connObjId != 0 && !selectObjIds.Contains(connObjId) && !extraObjIds.Contains(connObjId) && FactoryFunctions.ObjectIsBeltOrInserter(factory, connObjId))
                            extraObjIds.Add(connObjId);
                    }
                }
                for (var m = 0; m < 4; m++)
                {
                    factory.ReadObjectConn(objId, m, out _, out var connObjId, out _);
                    if (connObjId == 0 || !factory.ObjectIsBelt(connObjId) || selectObjIds.Contains(connObjId) || extraObjIds.Contains(connObjId)) continue;
                    for (var j = 0; j < 2; j++)
                    {
                        factory.ReadObjectConn(connObjId, j, out _, out var connObjId2, out _);
                        if (connObjId2 == 0 || selectObjIds.Contains(connObjId2) || extraObjIds.Contains(connObjId2) || FactoryFunctions.ObjectIsBeltOrInserter(factory, connObjId2)) continue;
                        extraObjIds.Add(connObjId);
                        break;
                    }
                }
                continue;
            }
            if (desc.addonType == EAddonType.Belt) continue;
            for (var j = 0; j < 16; j++)
            {
                factory.ReadObjectConn(objId, j, out _, out var connObjId, out _);
                if (connObjId != 0 && !selectObjIds.Contains(connObjId) && !extraObjIds.Contains(connObjId) && FactoryFunctions.ObjectIsBeltOrInserter(factory, connObjId))
                    extraObjIds.Add(connObjId);
            }
        }
        selectObjIds.UnionWith(extraObjIds);
    }
}
