using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CheatEnabler;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class CheatEnabler : BaseUnityPlugin
{
    public new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    public static ConfigEntry<KeyboardShortcut> Hotkey;
    private static bool _configWinInitialized = false;
    private static UIConfigWindow _configWin;
    private static string _unlockTechToMaximumLevel = "";
    private static readonly List<int> TechToUnlock = new();
    
    private static Harmony _windowPatch;
    private static Harmony _patch;

    private static bool _initialized;

    private void Awake()
    {
        Hotkey = Config.Bind("General", "Shortcut", KeyboardShortcut.Deserialize("BackQuote + LeftAlt"), "Shortcut to open config window");
        DevShortcuts.Enabled = Config.Bind("General", "DevShortcuts", true, "Enable DevMode shortcuts");
        AbnormalDisabler.Enabled = Config.Bind("General", "DisableAbnormalChecks", false,
            "disable all abnormal checks");
        BuildPatch.ImmediateEnabled = Config.Bind("Build", "ImmediateBuild", false,
            "Build immediately");
        BuildPatch.NoCostEnabled = Config.Bind("Build", "InfiniteBuildings", false,
            "Infinite buildings");
        BuildPatch.NoConditionEnabled = Config.Bind("Build", "BuildWithoutCondition", false,
            "Build without condition");
        BuildPatch.NoCollisionEnabled = Config.Bind("Build", "NoCollision", false,
            "No collision");
        ResourcePatch.InfiniteEnabled = Config.Bind("Planet", "AlwaysInfiniteResource", false,
            "always infinite natural resource");
        ResourcePatch.FastEnabled = Config.Bind("Planet", "FastMining", false,
            "super-fast mining speed");
        WaterPumperPatch.Enabled = Config.Bind("Planet", "WaterPumpAnywhere", false,
            "Can pump water anywhere (while water type is not None)");
        TerraformPatch.Enabled = Config.Bind("Planet", "TerraformAnyway", false,
            "Can do terraform without enough sands");
        DysonSpherePatch.SkipBulletEnabled = Config.Bind("DysonSphere", "SkipBullet", false,
            "Skip bullet");
        DysonSpherePatch.SkipAbsorbEnabled = Config.Bind("DysonSphere", "SkipAbsorb", false,
            "Skip absorption");
        DysonSpherePatch.QuickAbsortEnabled = Config.Bind("DysonSphere", "QuickAbsorb", false,
            "Quick absorb");
        DysonSpherePatch.EjectAnywayEnabled = Config.Bind("DysonSphere", "EjectAnyway", false,
            "Eject anyway");
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
        // _unlockTechToMaximumLevel = Config.Bind("General", "UnlockTechToMaxLevel", _unlockTechToMaximumLevel,
        //     "Unlock listed tech to MaxLevel").Value;

        I18N.Init();
        I18N.Add("CheatEnabler Config", "CheatEnabler Config", "CheatEnabler设置");
        I18N.Apply();

        // UI Patch
        _windowPatch = Harmony.CreateAndPatchAll(typeof(UI.MyWindowManager.Patch));
        _patch = Harmony.CreateAndPatchAll(typeof(CheatEnabler));

        DevShortcuts.Init();
        AbnormalDisabler.Init();
        BuildPatch.Init();
        ResourcePatch.Init();
        WaterPumperPatch.Init();
        TerraformPatch.Init();
        DysonSpherePatch.Init();
        BirthPlanetPatch.Init();
        // foreach (var idstr in _unlockTechToMaximumLevel.Split(','))
        // {
        //     if (int.TryParse(idstr, out var id))
        //     {
        //         TechToUnlock.Add(id);
        //     }
        // }
        // if (TechToUnlock.Count > 0)
        // {
        //     Harmony.CreateAndPatchAll(typeof(UnlockTechOnGameStart));
        // }
    }

    public void OnDestroy()
    {
        BirthPlanetPatch.Uninit();
        DysonSpherePatch.Uninit();
        TerraformPatch.Uninit();
        WaterPumperPatch.Uninit();
        ResourcePatch.Uninit();
        BuildPatch.Uninit();
        AbnormalDisabler.Uninit();
        DevShortcuts.Uninit();
        _patch?.UnpatchSelf();
        _windowPatch?.UnpatchSelf();
    }

    private void Update()
    {
        if (VFInput.inputing)
        {
            return;
        }
        
        if (Hotkey.Value.IsDown())
            ToggleConfigWindow();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), nameof(UIRoot.OpenMainMenuUI))]
    public static void UIRoot_OpenMainMenuUI_Postfix()
    {
        if (_initialized) return;
        {
            var mainMenu = UIRoot.instance.uiMainMenu;
            var src = mainMenu.newGameButton;
            var parent = src.transform.parent;
            var btn = Instantiate(src, parent);
            btn.name = "button-cheatenabler-config";
            var l = btn.text.GetComponent<Localizer>();
            if (l != null)
            {
                l.stringKey = "CheatEnabler Config";
                l.translation = "CheatEnabler Config".Translate();
            }
            btn.text.text = "CheatEnabler Config".Translate();
            btn.text.fontSize = btn.text.fontSize * 7 / 8;
            I18N.OnInitialized += () => { btn.text.text = "CheatEnabler Config".Translate(); };
            var vec = ((RectTransform)mainMenu.exitButton.transform).anchoredPosition3D;
            var vec2 = ((RectTransform)mainMenu.creditsButton.transform).anchoredPosition3D;
            var transform1 = (RectTransform)btn.transform;
            transform1.anchoredPosition3D = new Vector3(vec.x, vec.y + (vec.y - vec2.y) * 2, vec.z);
            btn.button.onClick.RemoveAllListeners();
            btn.button.onClick.AddListener(ToggleConfigWindow);
        }
        {
            var panel = UIRoot.instance.uiGame.planetGlobe;
            var src = panel.button2;
            var sandboxMenu = UIRoot.instance.uiGame.sandboxMenu;
            var icon = sandboxMenu.categoryButtons[6].transform.Find("icon")?.GetComponent<Image>()?.sprite;
            var b = Instantiate(src, src.transform.parent);
            var panelButtonGo = b.gameObject;
            var rect = (RectTransform)panelButtonGo.transform;
            var btn = panelButtonGo.GetComponent<UIButton>();
            var img = panelButtonGo.transform.Find("button-2/icon")?.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = icon;
            }
            if (panelButtonGo != null && btn != null)
            {
                panelButtonGo.name = "open-cheatenabler-config";
                rect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                rect.anchoredPosition3D = new Vector3(128f, -105f, 0f);
                b.onClick.RemoveAllListeners();
                btn.onClick += (_) => { ToggleConfigWindow(); };
                btn.tips.tipTitle = "CheatEnabler Config";
                I18N.OnInitialized += () => { btn.tips.tipTitle = "CheatEnabler Config".Translate(); };
                btn.tips.tipText = null;
                btn.tips.corner = 9;
                btn.tips.offset = new Vector2(-20f, -20f);
                panelButtonGo.SetActive(true);
            }
        }
        _initialized = true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIBuildMenu), nameof(UIBuildMenu._OnUpdate))]
    private static IEnumerable<CodeInstruction> UIBuildMenu__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.inScreen)))
        );
        matcher.Repeat(codeMatcher =>
        {
            var jumpPos = codeMatcher.Advance(1).Operand;
            codeMatcher.Advance(-1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.noModifier))),
                new CodeInstruction(OpCodes.Brfalse_S, jumpPos)
            ).Advance(2);
        });
        return matcher.InstructionEnumeration();
    }

    private static void ToggleConfigWindow()
    {
        if (!_configWinInitialized)
        {
            _configWinInitialized = true;
            _configWin = UIConfigWindow.CreateInstance();
        }

        if (_configWin.active)
        {
            _configWin._Close();
        }
        else
        {
            UIRoot.instance.uiGame.ShutPlayerInventory();
            _configWin.Open();
        }
    }

    /*
    private class UnlockTechOnGameStart
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameScenarioLogic), "NotifyOnGameBegin")]
        private static void UnlockTechPatch()
        {
            var history = GameMain.history;
            if (GameMain.mainPlayer == null || GameMain.mainPlayer.mecha == null)
            {
                return;
            }

            foreach (var currentTech in TechToUnlock)
            {
                UnlockTechRecursive(history, currentTech, currentTech == 3606 ? 7000 : 10000);
            }

            var techQueue = history.techQueue;
            if (techQueue == null || techQueue.Length == 0)
            {
                return;
            }

            history.VarifyTechQueue();
            if (history.currentTech > 0 && history.currentTech != techQueue[0])
            {
                history.AlterCurrentTech(techQueue[0]);
            }
        }

        private static void UnlockTechRecursive(GameHistoryData history, int currentTech, int maxLevel = 10000)
        {
            var techStates = history.techStates;
            if (techStates == null || !techStates.ContainsKey(currentTech))
            {
                return;
            }

            var techProto = LDB.techs.Select(currentTech);
            if (techProto == null)
            {
                return;
            }

            var value = techStates[currentTech];
            var maxLvl = Math.Min(maxLevel, value.maxLevel);
            if (value.unlocked)
            {
                return;
            }

            foreach (var preid in techProto.PreTechs)
            {
                UnlockTechRecursive(history, preid, maxLevel);
            }

            var techQueue = history.techQueue;
            if (techQueue != null)
            {
                for (var i = 0; i < techQueue.Length; i++)
                {
                    if (techQueue[i] == currentTech)
                    {
                        techQueue[i] = 0;
                    }
                }
            }

            if (value.curLevel < techProto.Level) value.curLevel = techProto.Level;
            while (value.curLevel <= maxLvl)
            {
                for (var j = 0; j < techProto.UnlockFunctions.Length; j++)
                {
                    history.UnlockTechFunction(techProto.UnlockFunctions[j], techProto.UnlockValues[j], value.curLevel);
                }

                value.curLevel++;
            }

            value.unlocked = maxLvl >= value.maxLevel;
            value.curLevel = value.unlocked ? maxLvl : maxLvl + 1;
            value.hashNeeded = techProto.GetHashNeeded(value.curLevel);
            value.hashUploaded = value.unlocked ? value.hashNeeded : 0;
            techStates[currentTech] = value;
        }
    }
    */

}