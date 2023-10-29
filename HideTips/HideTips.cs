using System;
using System.Collections.Generic;
using System.Reflection.Emit;
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
    private static bool _noAchievementCardPopups;
    private static bool _noMilestoneCardPopups = true;
    private static bool _noResearchCompletionPopups = true;
    private static bool _noResearchCompletionTips;
    private static bool _skipPrologue = true;
    private static bool _hideMenuDemo;

    private static Harmony _patch;

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        _noRandomReminderTips = Config.Bind("General", "NoRandomReminderTips", _noRandomReminderTips, "Disable Random Reminder Tips").Value;
        _noTutorialTips = Config.Bind("General", "NoTutorialTips", _noTutorialTips, "Disable Tutorial Tips").Value;
        _noAchievementCardPopups = Config.Bind("General", "NoAchievementCardPopups", _noAchievementCardPopups, "Disable Achievement Card Popups").Value;
        _noMilestoneCardPopups = Config.Bind("General", "NoMilestoneCardPopups", _noMilestoneCardPopups, "Disable Milestone Card Popups").Value;
        _noResearchCompletionPopups = Config.Bind("General", "NoResearchCompletionPopups", _noResearchCompletionPopups, "Disable Research Completion Popup Windows").Value;
        _noResearchCompletionTips = Config.Bind("General", "NoResearchCompletionTips", _noResearchCompletionTips, "Disable Research Completion Tips").Value;
        _skipPrologue = Config.Bind("General", "SkipPrologue", _skipPrologue, "Skip prologue for new game").Value;
        _hideMenuDemo = Config.Bind("General", "HideMenuDemo", _hideMenuDemo, "Disable title screen demo scene loading").Value;
        if (!_cfgEnabled) return;
        Harmony.CreateAndPatchAll(typeof(HideTips));
        if (_hideMenuDemo)
        {
            _patch = Harmony.CreateAndPatchAll(typeof(HideMenuDemo));
        }
    }

    private void OnDestroy()
    {
        _patch?.UnpatchSelf();
        _patch = null;
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
    private static void UIGameMenu__OnCreate_Postfix(UIGameMenu __instance)
    {
        __instance.randTipButton0.pop = __instance.randTipButton0.popCount;
        __instance.randTipButton1.pop = __instance.randTipButton1.popCount;
        __instance.randTipButton2.pop = __instance.randTipButton2.popCount;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UITutorialTip), "PopupTutorialTip")]
    private static bool UITutorialTip_PopupTutorialTip_Prefix()
    {
        return !_noTutorialTips;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIVariousPopupGroup), "CreateAchievementPopupCard")]
    private static bool UIVariousPopupGroup_CreateAchievementPopupCard_Prefix()
    {
        return !_noAchievementCardPopups;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIVariousPopupGroup), "CreateMilestonePopupCard")]
    private static bool UIVariousPopupGroup_CreateMilestonePopupCard_Prefix()
    {
        return !_noMilestoneCardPopups;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIResearchResultWindow), "SetTechId")]
    private static bool UIResearchResultWindow_SetTechId_Prefix()
    {
        return !_noResearchCompletionPopups;
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIGeneralTips), "OnTechUnlocked")]
    private static IEnumerable<CodeInstruction> UIGeneralTips_OnTechUnlocked_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(UIGeneralTips), "researchCompleteTip")),
            new CodeMatch(OpCodes.Callvirt)
        );
        var labels = matcher.Labels;
        var label1 = generator.DefineLabel();
        matcher.Labels = new List<Label>();
        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(HideTips), nameof(_noResearchCompletionTips))).WithLabels(labels),
            new CodeInstruction(OpCodes.Brtrue, label1)
        ).MatchForward(false,
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Animation), nameof(Animation.Play))),
            new CodeMatch(OpCodes.Pop)
        ).Advance(2).Labels.Add(label1);
        return matcher.InstructionEnumeration();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DSPGame), "StartGame", typeof(GameDesc))]
    private static bool DSPGame_StartGame_Prefix(GameDesc _gameDesc)
    {
        if (!_skipPrologue) return true;
        DSPGame.StartGameSkipPrologue(_gameDesc);
        return false;
    }
}

[HarmonyPatch]
class HideMenuDemo
{
    [HarmonyPriority(Priority.First), HarmonyPrefix]
    [HarmonyPatch(typeof(DSPGame), "StartDemoGame", typeof(int))]
    private static bool DSPGame_StartDemoGame_Prefix()
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
    
    [HarmonyPriority(Priority.First), HarmonyPrefix]
    [HarmonyPatch(typeof(VFPreload), "IsMenuDemoLoaded")]
    private static bool VFPreload_IsMenuDemoLoaded_Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
    
    [HarmonyPriority(Priority.First), HarmonyPrefix]
    [HarmonyPatch(typeof(DSPGame), "LateUpdate")]
    [HarmonyPatch(typeof(GameMain), "LateUpdate")]
    [HarmonyPatch(typeof(GameMain), "FixedUpdate")]
    [HarmonyPatch(typeof(GameMain), "Update")]
    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    private static bool DSPGame_LateUpdate_Prefix()
    {
        return !DSPGame.IsMenuDemo;
    }

    [HarmonyPriority(Priority.First), HarmonyPrefix]
    [HarmonyPatch(typeof(GameMain), "Begin")]
    private static bool GameMain_Begin_Prefix()
    {
        if (!DSPGame.IsMenuDemo) return true;
        DSPGame.Game._loading = false;
        DSPGame.Game._running = true;
        return false;
    }
}