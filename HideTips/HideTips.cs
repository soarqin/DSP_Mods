using System;
using BepInEx;
using HarmonyLib;

namespace HideTips;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class HideTips : BaseUnityPlugin
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _cfgEnabled = true;
    private static bool _noRandomReminderTips = true;
    private static bool _noTutorialTips = true;
    private static bool _noAchievementCardPopups = false;
    private static bool _noMilestoneCardPopups = true;
    private static bool _skipPrologue = true;

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        _noRandomReminderTips = Config.Bind("General", "NoRandomReminderTips", _noRandomReminderTips, "Disable Random Reminder Tips").Value;
        _noTutorialTips = Config.Bind("General", "NoTutorialTips", _noTutorialTips, "Disable Tutorial Tips").Value;
        _noAchievementCardPopups = Config.Bind("General", "NoAchievementCardPopups", _noAchievementCardPopups, "Disable Achievement Card Popups").Value;
        _noMilestoneCardPopups = Config.Bind("General", "NoMilestoneCardPopups", _noMilestoneCardPopups, "Disable Milestone Card Popups").Value;
        _skipPrologue = Config.Bind("General", "SkipPrologue", _skipPrologue, "Skip prologue for new game").Value;
        if (!_cfgEnabled) return;
        Harmony.CreateAndPatchAll(typeof(HideTips));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIBuildMenu), "_OnCreate")]
    private static void ClearRandReminderTips(UIBuildMenu __instance)
    {
        if (!_noRandomReminderTips) return;
        foreach (var randTip in __instance.randRemindTips)
        {
            if (randTip != null)
            {
                randTip._Free();
            }
        }
        __instance.randRemindTips = Array.Empty<UIRandomTip>();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIGameMenu), "_OnCreate")]
    private static void ClearGameMenuRandTips(UIGameMenu __instance)
    {
        __instance.randTipButton0.pop = __instance.randTipButton0.popCount;
        __instance.randTipButton1.pop = __instance.randTipButton1.popCount;
        __instance.randTipButton2.pop = __instance.randTipButton2.popCount;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UITutorialTip), "PopupTutorialTip")]
    private static bool SkipTutorialTips()
    {
        return !_noTutorialTips;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIVariousPopupGroup), "CreateAchievementPopupCard")]
    private static bool SkipAchievementCardPopups()
    {
        return !_noAchievementCardPopups;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIVariousPopupGroup), "CreateMilestonePopupCard")]
    private static bool SkipMilestoneCardPopups()
    {
        return !_noMilestoneCardPopups;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DSPGame), "StartGame", typeof(GameDesc))]
    private static bool OnStartGame(GameDesc _gameDesc)
    {
        if (!_skipPrologue) return true;
        DSPGame.StartGameSkipPrologue(_gameDesc);
        return false;
    }
}
