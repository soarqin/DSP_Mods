using BepInEx;
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
    private static int sorterSpeedMultiplier = 2;
    private static int sorterPowerConsumptionMultiplier = 2;
    private static int assembleSpeedMultiplier = 2;
    private static int assemblePowerConsumptionMultiplier = 2;
    private static int minerSpeedMultiplier = 2;
    private static int minerPowerConsumptionMultiplier = 2;
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
        sorterSpeedMultiplier = Config.Bind("Sorter", "SpeedMultiplier", sorterSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Sorters", new AcceptableValueRange<int>(1, 5))).Value;
        sorterPowerConsumptionMultiplier = Config.Bind("Sorter", "PowerConsumptionMultiplier", sorterPowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 100))).Value;
        assembleSpeedMultiplier = Config.Bind("Assemble", "SpeedMultiplier", assembleSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 10))).Value;
        assemblePowerConsumptionMultiplier = Config.Bind("Assemble", "PowerConsumptionMultiplier", assemblePowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 100))).Value;
        minerSpeedMultiplier = Config.Bind("Miner", "SpeedMultiplier", minerSpeedMultiplier,
            new ConfigDescription("Speed multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 10))).Value;
        minerPowerConsumptionMultiplier = Config.Bind("Miner", "PowerConsumptionMultiplier", minerPowerConsumptionMultiplier,
            new ConfigDescription("Power consumption multiplier for Smelters and Assembling Machines", new AcceptableValueRange<int>(1, 100))).Value;
        powerGenerationMultiplier = Config.Bind("Power", "GenerationMultiplier", powerGenerationMultiplier,
            new ConfigDescription("Power generation multiplier for all power providers", new AcceptableValueRange<long>(1, 10))).Value;
        powerFuelConsumptionMultiplier = Config.Bind("Power", "FuelConsumptionMultiplier", powerFuelConsumptionMultiplier,
            new ConfigDescription("Fuel consumption multiplier for all fuel-consuming power providers", new AcceptableValueRange<long>(1, 10))).Value;
        Harmony.CreateAndPatchAll(typeof(Patch));
        Harmony.CreateAndPatchAll(typeof(BeltFix));
    }

    private static void BoostSorter(int id)
    {
        var prefabDesc = LDB.items.Select(id).prefabDesc;
        prefabDesc.inserterSTT *= sorterSpeedMultiplier;
        prefabDesc.idleEnergyPerTick *= sorterPowerConsumptionMultiplier;
        prefabDesc.workEnergyPerTick *= sorterPowerConsumptionMultiplier;
    }

    private static void BoostAssembler(int id)
    {
        var prefabDesc = LDB.items.Select(id).prefabDesc;
        prefabDesc.assemblerSpeed *= assembleSpeedMultiplier;
        prefabDesc.idleEnergyPerTick *= assemblePowerConsumptionMultiplier;
        prefabDesc.workEnergyPerTick *= assemblePowerConsumptionMultiplier;
    }

    private static void BoostMiner(int id)
    {
        var prefabDesc = LDB.items.Select(id).prefabDesc;
        prefabDesc.minerPeriod /= minerSpeedMultiplier;
        prefabDesc.idleEnergyPerTick *= minerPowerConsumptionMultiplier;
        prefabDesc.workEnergyPerTick *= minerPowerConsumptionMultiplier;
    }

    private static void BoostPower(int id)
    {
        var prefabDesc = LDB.items.Select(id).prefabDesc;
        prefabDesc.genEnergyPerTick *= powerGenerationMultiplier;
        prefabDesc.useFuelPerTick *= powerFuelConsumptionMultiplier;
        if (prefabDesc.isPowerExchanger) prefabDesc.exchangeEnergyPerTick *= powerFuelConsumptionMultiplier;
        if (prefabDesc.isAccumulator)
        {
            prefabDesc.maxAcuEnergy *= powerGenerationMultiplier;
            prefabDesc.inputEnergyPerTick *= powerGenerationMultiplier;
            prefabDesc.outputEnergyPerTick *= powerGenerationMultiplier;
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
    private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        // Belts
        LDB.items.Select(2001).prefabDesc.beltSpeed = (int)beltSpeed[0];
        LDB.items.Select(2002).prefabDesc.beltSpeed = (int)beltSpeed[1];
        LDB.items.Select(2003).prefabDesc.beltSpeed = (int)beltSpeed[2];

        // Sorters
        BoostSorter(2011);
        BoostSorter(2012);
        BoostSorter(2013);

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
        
        // Mining Machine
        BoostMiner(2301);
        // Advanced Mining Machine
        BoostMiner(2316);
        // Water Pump
        BoostMiner(2306);
        // Oil Extractor
        BoostMiner(2307);

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
        // Energy Exchanger
        BoostPower(2209);
        // Ray Receiver
        BoostPower(2208);
    }
}
