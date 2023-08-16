using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;

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
    private static bool _hideMenuDemo = false;

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        _noRandomReminderTips = Config.Bind("General", "NoRandomReminderTips", _noRandomReminderTips, "Disable Random Reminder Tips").Value;
        _noTutorialTips = Config.Bind("General", "NoTutorialTips", _noTutorialTips, "Disable Tutorial Tips").Value;
        _noAchievementCardPopups = Config.Bind("General", "NoAchievementCardPopups", _noAchievementCardPopups, "Disable Achievement Card Popups").Value;
        _noMilestoneCardPopups = Config.Bind("General", "NoMilestoneCardPopups", _noMilestoneCardPopups, "Disable Milestone Card Popups").Value;
        _skipPrologue = Config.Bind("General", "SkipPrologue", _skipPrologue, "Skip prologue for new game").Value;
        _hideMenuDemo = Config.Bind("General", "HideMenuDemo", _hideMenuDemo, "Disable title screen demo scene loading").Value;
        if (!_cfgEnabled) return;
        Harmony.CreateAndPatchAll(typeof(HideTips));
        if (_hideMenuDemo)
        {
            Harmony.CreateAndPatchAll(typeof(HideMenuDemo));
        }
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
    private static bool DSPGame_OnStartGame_Prefix(GameDesc _gameDesc)
    {
        if (!_skipPrologue) return true;
        DSPGame.StartGameSkipPrologue(_gameDesc);
        return false;
    }
}

[HarmonyPatch]
class HideMenuDemo
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DSPGame), "StartDemoGame", typeof(int))]
    private static bool DSPGame_OnStartDemoGame_Prefix()
    {
        if (DSPGame.Game != null)
        {
            DSPGame.EndGame();
        }

        DSPGame.IsMenuDemo = true;
        DSPGame.CreateGameMainObject();
        DSPGame.Game.isMenuDemo = true;
        DSPGame.Game.CreateIconSet();
        GameMain.data = new GameData();
        GameMain.data.mainPlayer = Player.Create(GameMain.data, 1);
        GameMain.data.galaxy = new GalaxyData
        {
            starCount = 0
        };

        if (GameMain.universeSimulator != null)
        {
            UnityEngine.Object.Destroy(GameMain.universeSimulator.gameObject);
        }
        GameMain.universeSimulator = UnityEngine.Object.Instantiate(Configs.builtin.universeSimulatorPrefab);
        GameMain.universeSimulator.spaceAudio = new GameObject("Space Audio")
        {
            transform = 
            {
                parent = GameMain.universeSimulator.transform
            }
        }.AddComponent<SpaceAudio>();
        GameMain.Begin();
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(VFPreload), "IsMenuDemoLoaded")]
    private static bool VFPreload_IsMenuDemoLoaded_Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DSPGame), "LateUpdate")]
    [HarmonyPatch(typeof(GameMain), "LateUpdate")]
    [HarmonyPatch(typeof(GameMain), "FixedUpdate")]
    [HarmonyPatch(typeof(GameMain), "Update")]
    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    private static bool DSPGame_LateUpdate_Prefix()
    {
        return !DSPGame.IsMenuDemo;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(GameMain), "Begin")]
    private static bool GameMain_Begin_Prefix()
    {
        if (!DSPGame.IsMenuDemo) return true;
        DSPGame.Game._loading = false;
        DSPGame.Game._running = true;
        return false;
    }
}