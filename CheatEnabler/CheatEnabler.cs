﻿using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;

namespace CheatEnabler;

[BepInDependency("org.soardev.uxassist")]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class CheatEnabler : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    public static ConfigEntry<KeyboardShortcut> Hotkey;

    private void Awake()
    {
        Hotkey = Config.Bind("General", "Shortcut", KeyboardShortcut.Deserialize("BackQuote + LeftAlt"), "Shortcut to open config window");
        DevShortcuts.Enabled = Config.Bind("General", "DevShortcuts", false, "Enable DevMode shortcuts");
        AbnormalDisabler.Enabled = Config.Bind("General", "DisableAbnormalChecks", false,
            "disable all abnormal checks");
        TechPatch.Enabled = Config.Bind("General", "UnlockTech", false,
            "Unlock clicked tech by holding key-modifilers(Shift/Alt/Ctrl)");
        FactoryPatch.ImmediateEnabled = Config.Bind("Build", "ImmediateBuild", false,
            "Build immediately");
        FactoryPatch.ArchitectModeEnabled = Config.Bind("Build", "Architect", false,
            "Architect Mode");
        FactoryPatch.UnlimitInteractiveEnabled = Config.Bind("Build", "UnlimitInteractive", false,
            "Unlimit interactive range");
        FactoryPatch.RemoveSomeConditionEnabled = Config.Bind("Build", "RemoveSomeBuildConditionCheck", false,
            "Remove part of build condition checks that does not affect game logic");
        FactoryPatch.NoConditionEnabled = Config.Bind("Build", "BuildWithoutCondition", false,
            "Build without condition");
        FactoryPatch.NoCollisionEnabled = Config.Bind("Build", "NoCollision", false,
            "No collision");
        FactoryPatch.BeltSignalGeneratorEnabled = Config.Bind("Build", "BeltSignalGenerator", false,
            "Belt signal generator");
        FactoryPatch.BeltSignalNumberAltFormat = Config.Bind("Build", "BeltSignalNumberFormat", false,
            "Belt signal number format alternative format (AAAA=generation speed in minutes, B=proliferate points, C=stack count):\n  AAAABC by default\n  BCAAAA as alternative");
        FactoryPatch.BeltSignalCountRecipeEnabled = Config.Bind("Build", "BeltSignalCountRecipe", false,
            "Belt signal count all raws and intermediates in statistics");
        FactoryPatch.NightLightEnabled = Config.Bind("Build", "NightLight", false,
            "Night light");
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
        PlanetFunctions.PlayerActionsInGlobeViewEnabled = Config.Bind("Planet", "PlayerActionsInGlobeView", false,
            "Enable player actions in globe view");
        ResourcePatch.InfiniteResourceEnabled = Config.Bind("Planet", "AlwaysInfiniteResource", false,
            "always infinite natural resource");
        ResourcePatch.FastMiningEnabled = Config.Bind("Planet", "FastMining", false,
            "super-fast mining speed");
        WaterPumperPatch.Enabled = Config.Bind("Planet", "WaterPumpAnywhere", false,
            "Can pump water anywhere (while water type is not None)");
        TerraformPatch.Enabled = Config.Bind("Planet", "TerraformAnyway", false,
            "Can do terraform without enough sands");
        DysonSpherePatch.StopEjectOnNodeCompleteEnabled = Config.Bind("DysonSphere", "StopEjectOnNodeComplete", false,
            "Stop ejectors when available nodes are all filled up");
        DysonSpherePatch.OnlyConstructNodesEnabled = Config.Bind("DysonSphere", "OnlyConstructNodes", false,
            "Construct only nodes but frames");
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
        BirthPlanetPatch.SitiVeinsOnBirthPlanet = Config.Bind("Birth", "SiTiVeinsOnBirthPlanet", false,
            "Silicon/Titanium on birth planet");
        BirthPlanetPatch.FireIceOnBirthPlanet = Config.Bind("Birth", "FireIceOnBirthPlanet", false,
            "Fire ice on birth planet");
        BirthPlanetPatch.KimberliteOnBirthPlanet = Config.Bind("Birth", "KimberliteOnBirthPlanet", false,
            "Kimberlite on birth planet");
        BirthPlanetPatch.FractalOnBirthPlanet = Config.Bind("Birth", "FractalOnBirthPlanet", false,
            "Fractal silicon on birth planet");
        BirthPlanetPatch.OrganicOnBirthPlanet = Config.Bind("Birth", "OrganicOnBirthPlanet", false,
            "Organic crystal on birth planet");
        BirthPlanetPatch.OpticalOnBirthPlanet = Config.Bind("Birth", "OpticalOnBirthPlanet", false,
            "Optical grating crystal on birth planet");
        BirthPlanetPatch.SpiniformOnBirthPlanet = Config.Bind("Birth", "SpiniformOnBirthPlanet", false,
            "Spiniform stalagmite crystal on birth planet");
        BirthPlanetPatch.UnipolarOnBirthPlanet = Config.Bind("Birth", "UnipolarOnBirthPlanet", false,
            "Unipolar magnet on birth planet");
        BirthPlanetPatch.FlatBirthPlanet = Config.Bind("Birth", "FlatBirthPlanet", false,
            "Birth planet is solid flat (no water at all)");
        BirthPlanetPatch.HighLuminosityBirthStar = Config.Bind("Birth", "HighLuminosityBirthStar", false,
            "Birth star has high luminosity");

        I18N.Init();
        I18N.Add("CheatEnabler Config", "CheatEnabler Config", "CheatEnabler设置");
        I18N.Apply();
        
        UIConfigWindow.Init();

        DevShortcuts.Init();
        AbnormalDisabler.Init();
        TechPatch.Init();
        FactoryPatch.Init();
        PlanetFunctions.Init();
        ResourcePatch.Init();
        WaterPumperPatch.Init();
        TerraformPatch.Init();
        DysonSpherePatch.Init();
        BirthPlanetPatch.Init();
    }

    private void OnDestroy()
    {
        BirthPlanetPatch.Uninit();
        DysonSpherePatch.Uninit();
        TerraformPatch.Uninit();
        WaterPumperPatch.Uninit();
        ResourcePatch.Uninit();
        PlanetFunctions.Uninit();
        FactoryPatch.Uninit();
        TechPatch.Uninit();
        AbnormalDisabler.Uninit();
        DevShortcuts.Uninit();
    }

    private void LateUpdate()
    {
        FactoryPatch.NightLight.LateUpdate();
    }
}
