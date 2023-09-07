using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;
public static class DevShortcuts
{
    public static ConfigEntry<bool> Enabled;
    private static Harmony _patch;

    public static void Init()
    {
        _patch = Harmony.CreateAndPatchAll(typeof(DevShortcuts));
    }

    public static void Uninit()
    {
        if (_patch == null) return;
        _patch.UnpatchSelf();
        _patch = null;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerController), "Init")]
    private static void PlayerController_Init_Postfix(PlayerController __instance)
    {
        var cnt = __instance.actions.Length;
        var newActions = new PlayerAction[cnt + 1];
        for (var i = 0; i < cnt; i++)
        {
            newActions[i] = __instance.actions[i];
        }

        var test = new PlayerAction_Test();
        test.Init(__instance.player);
        newActions[cnt] = test;
        __instance.actions = newActions;

        Enabled.SettingChanged += (_, _) =>
        {
            if (!Enabled.Value)
            {
                test.active = false;
            }
        };
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAction_Test), "GameTick")]
    private static void PlayerAction_Test_GameTick_Postfix(PlayerAction_Test __instance)
    {
        if (!Enabled.Value) return;
        var lastActive = __instance.active;
        __instance.Update();
        if (lastActive != __instance.active)
        {
            UIRealtimeTip.PopupAhead(
                (lastActive ? "Developer Mode Shortcuts Disabled" : "Developer Mode Shortcuts Enabled").Translate(),
                false);
        }
    }
}