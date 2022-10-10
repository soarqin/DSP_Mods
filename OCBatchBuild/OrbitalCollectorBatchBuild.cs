using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace OCBatchBuild;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class OrbitalCollectorBatchBuild : BaseUnityPlugin
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _cfgEnabled = true;
    private static int _maxBuildCount;

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        _maxBuildCount = Config.Bind("General", "MaxBuildCount", _maxBuildCount,
                new ConfigDescription("Maximum Orbital Collectors to build once, set to 0 to build as many as possible",
                    new AcceptableValueRange<int>(0, 40), new {}))
            .Value;
        Harmony.CreateAndPatchAll(typeof(OrbitalCollectorBatchBuild));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildTool_Click), "CreatePrebuilds")]
    private static void CreatePrebuilds(BuildTool_Click __instance)
    {
        /* Check Gas Planet */
        if (__instance.planet.type != EPlanetType.Gas) return;
        if (__instance.buildPreviews.Count == 0) return;

        var buildPreview = __instance.buildPreviews[0];
        /* Check if is collector station */
        if (!buildPreview.desc.isCollectStation) return;
       
        var factory = __instance.factory;
        var stationPool = factory.transport.stationPool;
        var entityPool = factory.entityPool;
        var stationCursor = factory.transport.stationCursor;
        var pos = buildPreview.lpos;
        var pos2 = buildPreview.lpos2;
        var itemId = buildPreview.item.ID;
        var countToBuild = _maxBuildCount - 1;
        for (var i = 0; i < 39 && countToBuild != 0; i++)
        {
            /* rotate for 1/40 on sphere */
            pos = Maths.RotateLF(0.0, 1.0, 0.0, Math.PI / 40.0, pos);
            pos2 = Maths.RotateLF(0.0, 1.0, 0.0, Math.PI / 40.0, pos2);

            /* Check for collision */
            var collide = false;
            for (var j = 1; j < stationCursor; j++)
            {
                if (stationPool[j] == null || stationPool[j].id != j) continue;
                if ((entityPool[stationPool[j].entityId].pos - pos).sqrMagnitude >= 14297f) continue;
                collide = true;
                break;
            }
            if (collide) continue;

            var player = __instance.player;
            if (player.inhandItemId == itemId && player.inhandItemCount > 0)
            {
                player.UseHandItems(1, out var _);
            }
            else
            {
                var count = 1;
                player.package.TakeTailItems(ref itemId, ref count, out var _);
                if (count == 0) break;
            }

            var rot = Maths.SphericalRotation(pos, 0f);
            var rot2 = Maths.SphericalRotation(pos2, 0f);
            var prebuild = default(PrebuildData);
            prebuild.protoId = (short)buildPreview.item.ID;
            prebuild.modelIndex = (short)buildPreview.desc.modelIndex;
            prebuild.pos = pos;
            prebuild.pos2 = pos2;
            prebuild.rot = rot;
            prebuild.rot2 = rot2;
            prebuild.pickOffset = (short)buildPreview.inputOffset;
            prebuild.insertOffset = (short)buildPreview.outputOffset;
            prebuild.recipeId = buildPreview.recipeId;
            prebuild.filterId = buildPreview.filterId;
            prebuild.InitParametersArray(buildPreview.paramCount);
            for (var j = 0; j < buildPreview.paramCount; j++)
            {
                prebuild.parameters[j] = buildPreview.parameters[j];
            }
            factory.AddPrebuildDataWithComponents(prebuild);
            countToBuild--;
        }
    }
}