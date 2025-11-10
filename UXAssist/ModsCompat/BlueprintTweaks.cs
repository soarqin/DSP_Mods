using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UXAssist.Functions;

namespace UXAssist.ModsCompat;

class BlueprintTweaks
{
    public const string BlueprintTweaksGuid = "org.kremnev8.plugin.BlueprintTweaks";
    private static FieldInfo selectObjIdsField;

    public static bool Run(Harmony harmony)
    {
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(BlueprintTweaksGuid, out var pluginInfo)) return false;
        var assembly = pluginInfo.Instance.GetType().Assembly;
        var classType = assembly.GetType("BlueprintTweaks.DragRemoveBuildTool");
        selectObjIdsField = AccessTools.Field(classType, "selectObjIds");
        harmony.Patch(AccessTools.Method(classType, "DeterminePreviews"),
            new HarmonyMethod(AccessTools.Method(typeof(BlueprintTweaks), nameof(PatchDeterminePreviews))));
        return true;
    }

    public static void PatchDeterminePreviews(object __instance)
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
