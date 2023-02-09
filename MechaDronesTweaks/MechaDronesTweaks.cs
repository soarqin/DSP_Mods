using System;
using BepInEx;
using HarmonyLib;

namespace MechaDronesTweaks;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(FastDronesRemover.FastDronesGuid, BepInDependency.DependencyFlags.SoftDependency)]
public class MechaDronesTweaksPlugin : BaseUnityPlugin
{
    public MechaDronesTweaksPlugin()
    {
        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        /* Remove FastDrones MOD if loaded */
        try {
            if (FastDronesRemover.Run(harmony))
            {
                Logger.LogInfo("Unpatch FastDrones - OK");
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning($"Failed to unpatch FastDrones: {e}");
        }
    }
}
