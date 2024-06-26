﻿using BepInEx;

namespace CheatEnabler;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(UXAssist.PluginInfo.PLUGIN_GUID)]
public class CheatEnabler : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private void Awake()
    {
        DevShortcuts.Enabled = Config.Bind("General", "DevShortcuts", false, "Enable DevMode shortcuts");
        AbnormalDisabler.Enabled = Config.Bind("General", "DisableAbnormalChecks", false,
            "disable all abnormal checks");
        TechPatch.Enabled = Config.Bind("General", "UnlockTech", false,
            "Unlock clicked tech by holding key-modifilers(Shift/Alt/Ctrl)");
        FactoryPatch.ImmediateEnabled = Config.Bind("Build", "ImmediateBuild", false,
            "Build immediately");
        FactoryPatch.ArchitectModeEnabled = Config.Bind("Build", "Architect", false,
            "Architect Mode");
        FactoryPatch.NoConditionEnabled = Config.Bind("Build", "BuildWithoutCondition", false,
            "Build without condition");
        FactoryPatch.NoCollisionEnabled = Config.Bind("Build", "NoCollision", false,
            "No collision");
        FactoryPatch.BeltSignalGeneratorEnabled = Config.Bind("Build", "BeltSignalGenerator", false,
            "Belt signal generator");
        FactoryPatch.BeltSignalNumberAltFormat = Config.Bind("Build", "BeltSignalNumberFormat", false,
            "Belt signal number format alternative format (AAAA=generation speed in minutes, B=proliferate points, C=stack count):\n  AAAABC by default\n  BCAAAA as alternative");
        FactoryPatch.BeltSignalCountGenEnabled = Config.Bind("Build", "BeltSignalCountGenerations", true,
            "Belt signal count generations as production in statistics");
        FactoryPatch.BeltSignalCountRemEnabled = Config.Bind("Build", "BeltSignalCountRemovals", true,
            "Belt signal count removals as comsumption in statistics");
        FactoryPatch.BeltSignalCountRecipeEnabled = Config.Bind("Build", "BeltSignalCountRecipe", false,
            "Belt signal count all raws and intermediates in statistics");
        FactoryPatch.RemovePowerSpaceLimitEnabled = Config.Bind("Build", "RemovePowerDistanceLimit", false,
            "Remove distance limit for wind turbines and geothermals");
        FactoryPatch.BoostWindPowerEnabled = Config.Bind("Build", "BoostWindPower", false,
            "Boost wind power");
        FactoryPatch.BoostSolarPowerEnabled = Config.Bind("Build", "BoostSolarPower", false,
            "Boost solar power");
        FactoryPatch.BoostFuelPowerEnabled = Config.Bind("Build", "BoostFuelPower", false,
            "Boost fuel power");
        FactoryPatch.BoostGeothermalPowerEnabled = Config.Bind("Build", "BoostGeothermalPower", false,
            "Boost geothermal power");
        FactoryPatch.GreaterPowerUsageInLogisticsEnabled = Config.Bind("Build", "GreaterPowerUsageInLogistics", false,
            "Increase maximum power usage in Logistic Stations and Advanced Mining Machines");
        ResourcePatch.InfiniteResourceEnabled = Config.Bind("Planet", "AlwaysInfiniteResource", false,
            "always infinite natural resource");
        ResourcePatch.FastMiningEnabled = Config.Bind("Planet", "FastMining", false,
            "super-fast mining speed");
        PlanetPatch.WaterPumpAnywhereEnabled = Config.Bind("Planet", "WaterPumpAnywhere", false,
            "Can pump water anywhere (while water type is not None)");
        PlanetPatch.TerraformAnywayEnabled = Config.Bind("Planet", "TerraformAnyway", false,
            "Can do terraform without enough soil piless");
        PlayerPatch.InstantTeleportEnabled = Config.Bind("Player", "InstantTeleport", false,
            "Enable instant teleport (like that in Sandbox mode)");
        DysonSpherePatch.SkipBulletEnabled = Config.Bind("DysonSphere", "SkipBullet", false,
            "Skip bullet");
        DysonSpherePatch.SkipAbsorbEnabled = Config.Bind("DysonSphere", "SkipAbsorb", false,
            "Skip absorption");
        DysonSpherePatch.QuickAbsorbEnabled = Config.Bind("DysonSphere", "QuickAbsorb", false,
            "Quick absorb");
        DysonSpherePatch.EjectAnywayEnabled = Config.Bind("DysonSphere", "EjectAnyway", false,
            "Eject anyway");
        DysonSpherePatch.OverclockEjectorEnabled = Config.Bind("DysonSphere", "OverclockEjector", false,
            "Overclock ejector");
        DysonSpherePatch.OverclockSiloEnabled = Config.Bind("DysonSphere", "OverclockSilo", false,
            "Overclock silo");
        CombatPatch.MechaInvincibleEnabled = Config.Bind("Battle", "MechaInvincible", false,
            "Mecha and Drones/Fleets invincible");
        CombatPatch.BuildingsInvincibleEnabled = Config.Bind("Battle", "BuildingsInvincible", false,
            "Buildings invincible");

        UIConfigWindow.Init();

        DevShortcuts.Init();
        AbnormalDisabler.Init();
        TechPatch.Init();
        FactoryPatch.Init();
        ResourcePatch.Init();
        PlanetPatch.Init();
        PlayerPatch.Init();
        DysonSpherePatch.Init();
        CombatPatch.Init();
    }

    private void OnDestroy()
    {
        CombatPatch.Uninit();
        DysonSpherePatch.Uninit();
        PlayerPatch.Uninit();
        PlanetPatch.Uninit();
        ResourcePatch.Uninit();
        FactoryPatch.Uninit();
        TechPatch.Uninit();
        AbnormalDisabler.Uninit();
        DevShortcuts.Uninit();
    }

    private void Update()
    {
        if (VFInput.inputing) return;
        FactoryPatch.OnUpdate();
    }

}
