using BepInEx;
using BepInEx.Configuration;

namespace UniverseGenTweaks;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class UniverseGenTweaks : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _moreSettings = true;
    public static int MaxStarCount = 128;
    private bool _epicDifficulty = true;
    public static float OilMultiplier = 0.5f;

    private void Awake()
    {
        _moreSettings = Config.Bind("MoreSettings", "Enabled", _moreSettings, "Enable more settings on Universe Generation").Value;
        MaxStarCount = Config.Bind("MoreSettings", "MaxStarCount", MaxStarCount,
                new ConfigDescription("(32 ~ 1024)\nMaximum star count for Universe Generation, enable MoreSettings.Enabled to take effect",
                    new AcceptableValueRange<int>(32, 1024), new {}))
            .Value;
        _epicDifficulty = Config.Bind("EpicDifficulty", "Enabled", _epicDifficulty, "Enable Epic difficulty").Value;
        OilMultiplier = Config.Bind("EpicDifficulty", "OilMultiplier", OilMultiplier,
                new ConfigDescription("Multiplier relative to the Very-Hard difficulty multiplier",
                    new AcceptableValueRange<float>(0.1f, 1f), new {}))
            .Value;

        I18N.Init();
        if (_moreSettings)
        {
            MoreSettings.Init();
        }

        if (_epicDifficulty)
        {
            EpicDifficulty.Init();
        }
    }
}