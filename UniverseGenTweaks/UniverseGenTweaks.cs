using System.IO;
using BepInEx;
using BepInEx.Configuration;
using crecheng.DSPModSave;

namespace UniverseGenTweaks;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(UXAssist.PluginInfo.PLUGIN_GUID)]
[BepInDependency(DSPModSavePlugin.MODGUID)]
[ModSaveSettings(LoadOrder = LoadOrder.Preload)]
public class UniverseGenTweaks : BaseUnityPlugin, IModCanSave
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private void Awake()
    {
        MoreSettings.Enabled = Config.Bind("MoreSettings", "Enabled", true, "Enable more settings on Universe Generation");
        MoreSettings.MaxStarCount = Config.Bind("MoreSettings", "MaxStarCount", 128,
                new ConfigDescription("(32 ~ 1024)\nMaximum star count for Universe Generation, enable MoreSettings.Enabled to take effect",
                    new AcceptableValueRange<int>(32, 1024), new {}));

        EpicDifficulty.Enabled = Config.Bind("EpicDifficulty", "Enabled", true, "Enable Epic difficulty");
        EpicDifficulty.ResourceMultiplier = Config.Bind("EpicDifficulty", "ResourceMultiplier", 0.01f,
            new ConfigDescription("Resource multiplier for Epic difficulty",
                new AcceptableValueRange<float>(0.0001f, 0.05f), new {}));
        EpicDifficulty.OilMultiplier = Config.Bind("EpicDifficulty", "OilMultiplier", 0.5f,
                new ConfigDescription("Oil multiplier for Epic difficulty relative to the Very-Hard difficulty",
                    new AcceptableValueRange<float>(0.1f, 1f), new {}));

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

        UIConfigWindow.Init();

        MoreSettings.Init();
        EpicDifficulty.Init();
        BirthPlanetPatch.Init();
    }

    private void OnDestroy()
    {
        BirthPlanetPatch.Uninit();
        EpicDifficulty.Uninit();
        MoreSettings.Uninit();
    }

    #region IModCanSave
    private const ushort ModSaveVersion = 1;

    public void Export(BinaryWriter w)
    {
        w.Write(ModSaveVersion);
        MoreSettings.Export(w);
    }

    public void Import(BinaryReader r)
    {
        var version = r.ReadUInt16();
        if (version <= 0) return;
        MoreSettings.Import(r);
    }

    public void IntoOtherSave()
    {
    }
    #endregion
}