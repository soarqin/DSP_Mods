using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;

namespace CheatEnabler;
public static class AbnormalDisabler
{
    public static ConfigEntry<bool> Enabled;
    private static Dictionary<int, AbnormalityDeterminator> _savedDeterminators;
    private static Harmony _patch;

    public static void Init()
    {
        if (_patch != null) return;
        _patch = Harmony.CreateAndPatchAll(typeof(AbnormalDisabler));
    }

    public static void Uninit()
    {
        if (_patch == null) return;
        _patch.UnpatchSelf();
        _patch = null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbnormalityLogic), "NotifyBeforeGameSave")]
    [HarmonyPatch(typeof(AbnormalityLogic), "NotifyOnAssemblerRecipePick")]
    [HarmonyPatch(typeof(AbnormalityLogic), "NotifyOnGameBegin")]
    [HarmonyPatch(typeof(AbnormalityLogic), "NotifyOnMechaForgeTaskComplete")]
    [HarmonyPatch(typeof(AbnormalityLogic), "NotifyOnUnlockTech")]
    [HarmonyPatch(typeof(AbnormalityLogic), "NotifyOnUseConsole")]
    private static bool DisableAbnormalLogic()
    {
        return !Enabled.Value;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AbnormalityLogic), "InitDeterminators")]
    private static void DisableAbnormalDeterminators(AbnormalityLogic __instance)
    {
        _savedDeterminators = __instance.determinators;
        Enabled.SettingChanged += (_, _) =>
        {
            if (Enabled.Value)
            {
                _savedDeterminators = __instance.determinators;
                __instance.determinators = new Dictionary<int, AbnormalityDeterminator>();
                foreach (var p in _savedDeterminators)
                {
                    p.Value.OnUnregEvent();
                }
            }
            else
            {
                __instance.determinators = _savedDeterminators;
                foreach (var p in _savedDeterminators)
                {
                    p.Value.OnRegEvent();
                }
            }
        };

        _savedDeterminators = __instance.determinators;
        if (!Enabled.Value) return;
        __instance.determinators = new Dictionary<int, AbnormalityDeterminator>();
        foreach (var p in _savedDeterminators)
        {
            p.Value.OnUnregEvent();
        }
    }
}
