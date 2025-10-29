using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UXAssist.Common;

namespace LogisticHub;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class LogisticHub : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private Type[] _modules;

    private void Awake()
    {
        Module.Miner.Enabled = Config.Bind("Miner", "Enabled", true, "enable/disable this plugin");
        Module.Miner.OreEnergyConsume = Config.Bind("Miner", "EnergyConsumptionForOre", 2000000L,
            "Energy consumption for each ore vein group(in 0.5W)");
        Module.Miner.OilEnergyConsume = Config.Bind("Miner", "EnergyConsumptionForOil", 3600000L,
            "Energy consumption for each oil seep(in 0.5W)");
        Module.Miner.WaterEnergyConsume = Config.Bind("Miner", "EnergyConsumptionForWater", 2000000L,
            "Energy consumption for water slot(in kW)");
        Module.Miner.WaterSpeed = Config.Bind("Miner", "WaterMiningSpeed", 10,
            "Water mining speed (count per second)");
        Module.Miner.MiningScale = Config.Bind("Miner", "MiningScale", 0,
            """
            0 for Auto(which means having researched makes mining scale 300, otherwise 100).
            Mining scale(in percents) for slots below half of slot limits, and the scale reduces to 100% smoothly till reach full.
            Please note that the power consumption increases by the square of the scale which is the same as Advanced Mining Machine.
            """);
        Module.Miner.FuelIlsSlot = Config.Bind("Miner", "ILSFuelSlot", 4,
            new ConfigDescription("Fuel slot for ILS, set 0 to disable.", new AcceptableValueRange<int>(0, 5)));
        Module.Miner.FuelPlsSlot = Config.Bind("Miner", "PLSFuelSlot", 4,
                new ConfigDescription("Fuel slot for PLS, set 0 to disable.", new AcceptableValueRange<int>(0, 4)));

        _modules = Util.GetTypesFiltered(Assembly.GetExecutingAssembly(),
            t => string.Equals(t.Namespace, "LogisticHub.Module", StringComparison.Ordinal));
        _modules?.Do(type => type.GetMethod("Init")?.Invoke(null, null));
        Harmony.CreateAndPatchAll(typeof(LogisticHub));
    }

    private void Start()
    {
        _modules?.Do(type => type.GetMethod("Start")?.Invoke(null, null));
    }

    private void OnDestroy()
    {
        _modules?.Do(type => type.GetMethod("Uninit")?.Invoke(null, null));
    }
}