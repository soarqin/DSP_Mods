using BepInEx;
using HarmonyLib;

namespace DevShortcuts;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class DevShortcuts : BaseUnityPlugin
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _cfgEnabled = true;

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        if (!_cfgEnabled) return;
        Harmony.CreateAndPatchAll(typeof(DevShortcuts));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerController), "Init")]
    static void PlayerControllerInit(ref PlayerAction[] ___actions, Player ___player)
    {
        var cnt = ___actions.Length;
        var newActions = new PlayerAction[cnt + 1];
        for (int i = 0; i < cnt; i++)
        {
            newActions[i] = ___actions[i];
        }
        var test = new PlayerAction_Test();
        test.Init(___player);
        newActions[cnt] = test;
        ___actions = newActions;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAction_Test), "GameTick")]
    static void PlayerAction_TestGameTick(PlayerAction_Test __instance, long timei)
    {
        __instance.Update();
    }
}
