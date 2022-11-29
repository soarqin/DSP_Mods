﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace OverclockEverything;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Patch : BaseUnityPlugin
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _cfgEnabled = true;
    public static uint[] beltSpeed = {
        2, 5, 10
    };
    private static int inserterSpeedMultiplier = 2;
    private static int assembleSpeedMultiplier = 2;
    private static int powerConsumptionMultiplier = 2;
    private static long powerGenerationMultiplier = 4;
    private static long powerFuelConsumptionMultiplier = 1;

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        if (!_cfgEnabled) return;
        beltSpeed[0] = Config.Bind("Belt", "MkI_Speed", beltSpeed[0],
            new ConfigDescription("Speed for Belt Mk.I.\n  1: 6/s\n  2: 12/s\n  3: 15/s(Displayed as 18/s)\n  4: 20/s(Displayed as 24/s)\n  5+: 6*n/s", new AcceptableValueRange<uint>(1, 10))).Value;
        beltSpeed[1] = Config.Bind("Belt", "MkII_Speed", beltSpeed[1],
            new ConfigDescription("Speed for Belt Mk.II", new AcceptableValueRange<uint>(1, 10))).Value;
        beltSpeed[2] = Config.Bind("Belt", "MkIII_Speed", beltSpeed[2],
            new ConfigDescription("Speed for Belt Mk.III", new AcceptableValueRange<uint>(1, 10))).Value;
        inserterSpeedMultiplier = Config.Bind("Inserter", "SpeedMultiplier", inserterSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Inserters", new AcceptableValueRange<int>(1, 5))).Value;
        assembleSpeedMultiplier = Config.Bind("Assemble", "SpeedMultiplier", assembleSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 10))).Value;
        powerConsumptionMultiplier = Config.Bind("Assemble", "PowerConsumptionMultiplier", powerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 100))).Value;
        powerGenerationMultiplier = Config.Bind("Power", "GenerationMultiplier", powerGenerationMultiplier,
            new ConfigDescription("Power generation multiplier for all power providers", new AcceptableValueRange<long>(1, 10))).Value;
        powerFuelConsumptionMultiplier = Config.Bind("Power", "FuelConsumptionMultiplier", powerFuelConsumptionMultiplier,
            new ConfigDescription("Fuel consumption multiplier for all fuel-consuming power providers", new AcceptableValueRange<long>(1, 10))).Value;
        Harmony.CreateAndPatchAll(typeof(Patch));
        Harmony.CreateAndPatchAll(typeof(BeltFix));
    }

    private static void BoostAssembler(int id)
    {
        var prefabDesc = LDB.items.Select(id).prefabDesc;
        prefabDesc.assemblerSpeed *= assembleSpeedMultiplier;
        prefabDesc.idleEnergyPerTick *= powerConsumptionMultiplier;
        prefabDesc.workEnergyPerTick *= powerConsumptionMultiplier;
    }

    private static void BoostPower(int id)
    {
        var prefabDesc = LDB.items.Select(id).prefabDesc;
        prefabDesc.genEnergyPerTick *= powerGenerationMultiplier;
        prefabDesc.useFuelPerTick *= powerFuelConsumptionMultiplier;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
    private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        // Belts
        LDB.items.Select(2001).prefabDesc.beltSpeed = (int)beltSpeed[0];
        LDB.items.Select(2002).prefabDesc.beltSpeed = (int)beltSpeed[1];
        LDB.items.Select(2003).prefabDesc.beltSpeed = (int)beltSpeed[2];

        // Inserters
        LDB.items.Select(2011).prefabDesc.inserterSTT /= inserterSpeedMultiplier;
        LDB.items.Select(2012).prefabDesc.inserterSTT /= inserterSpeedMultiplier;
        LDB.items.Select(2013).prefabDesc.inserterSTT /= inserterSpeedMultiplier;

        // Smelters
        BoostAssembler(2302);
        BoostAssembler(2315);
        // Assemblers
        BoostAssembler(2303);
        BoostAssembler(2304);
        BoostAssembler(2305);
        // Chemical Plants
        BoostAssembler(2309);
        BoostAssembler(2317);
        // Refiner
        BoostAssembler(2308);
        // Collider
        BoostAssembler(2310);

        // Thermal
        BoostPower(2204);
        // Fusion
        BoostPower(2211);
        // Artificial Star
        BoostPower(2210);
        // Wind Turbine
        BoostPower(2203);
        // Solar Panel
        BoostPower(2205);
        // Geothermal
        BoostPower(2213);
    }
}