using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using Object = UnityEngine.Object;

namespace UniverseGenTweaks; 

public class MoreSettings
{
    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<int> MaxStarCount;
    private static double _minDist = 2;
    private static double _minStep = 2;
    private static double _maxStep = 3.2;
    private static double _flatten = 0.18;

    private static Text _minDistTitle;
    private static Text _minStepTitle;
    private static Text _maxStepTitle;
    private static Text _flattenTitle;
    private static Slider _minDistSlider;
    private static Slider _minStepSlider;
    private static Slider _maxStepSlider;
    private static Slider _flattenSlider;
    private static Text _minDistText;
    private static Text _minStepText;
    private static Text _maxStepText;
    private static Text _flattenText;
    private static Harmony _patch, _permanentPatch;

    private static double _gameMinDist = 2;
    private static double _gameMinStep = 2;
    private static double _gameMaxStep = 3.2;
    private static double _gameFlatten = 0.18;

    public static void Init()
    {
        I18N.Add("恒星最小距离", "Star Distance Min", "恒星最小距离");
        I18N.Add("步进最小距离", "Step Distance Min", "步进最小距离");
        I18N.Add("步进最大距离", "Step Distance Max", "步进最大距离");
        I18N.Add("扁平度", "Flatness", "扁平度");
        I18N.Apply();

        _permanentPatch ??= Harmony.CreateAndPatchAll(typeof(PermanentPatch));

        Enabled.SettingChanged += (_, _) => Enable(Enabled.Value);
        Enable(Enabled.Value);
    }

    public static void Uninit()
    {
        Enable(false);

        _permanentPatch?.UnpatchSelf();
        _permanentPatch = null;
    }

    private static void Enable(bool on)
    {
        if (on)
        {
            _patch ??= Harmony.CreateAndPatchAll(typeof(MoreSettings));
            return;
        }
        _patch?.UnpatchSelf();
        _patch = null;
    }

    private static void CreateSliderWithText(Slider orig, out Text title, out Slider slider, out Text text, out Localizer loc)
    {
        var origText = orig.transform.parent.GetComponent<Text>();
        title = Object.Instantiate(origText, origText.transform.parent);
        slider = title.transform.FindChildRecur("Slider").GetComponent<Slider>();
        text = slider.transform.FindChildRecur("Text").GetComponent<Text>();
        loc = title.GetComponent<Localizer>();
    }

    private static void TransformDeltaY(Transform trans, float delta)
    {
        var pos = ((RectTransform)trans).anchoredPosition3D;
        pos.y += delta;
        ((RectTransform)trans).anchoredPosition3D = pos;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect._OnCreate))]
    private static void UIGalaxySelect__OnCreate_Postfix(UIGalaxySelect __instance)
    {
        __instance.starCountSlider.maxValue = MaxStarCount.Value;

        CreateSliderWithText(__instance.starCountSlider, out _minDistTitle, out _minDistSlider, out _minDistText, out var minDistLocalizer);
        CreateSliderWithText(__instance.starCountSlider, out _minStepTitle, out _minStepSlider, out _minStepText, out var minStepLocalizer);
        CreateSliderWithText(__instance.starCountSlider, out _maxStepTitle, out _maxStepSlider, out _maxStepText, out var maxStepLocalizer);
        CreateSliderWithText(__instance.starCountSlider, out _flattenTitle, out _flattenSlider, out _flattenText, out var flattenLocalizer);
        minDistLocalizer.stringKey = "恒星最小距离";
        minStepLocalizer.stringKey = "步进最小距离";
        maxStepLocalizer.stringKey = "步进最大距离";
        flattenLocalizer.stringKey = "扁平度";

        _minDistTitle.name = "min-dist";
        _minDistSlider.minValue = 10f;
        _minDistSlider.maxValue = 50f;
        _minDistSlider.value = (float)(_minDist * 10.0);

        _minStepTitle.name = "min-step";
        _minStepSlider.minValue = 10f;
        _minStepSlider.maxValue = (float)(_maxStep * 10.0 - 1.0);
        _minStepSlider.value = (float)(_minStep * 10.0);

        _maxStepTitle.name = "max-step";
        _maxStepSlider.minValue = (float)(_minStep * 10.0 + 1.0);
        _maxStepSlider.maxValue = 100f;
        _maxStepSlider.value = (float)(_maxStep * 10.0);

        _flattenTitle.name = "flatten";
        _flattenSlider.minValue = 1f;
        _flattenSlider.maxValue = 50f;
        _flattenSlider.value = (float)(_flatten * 50.0);

        TransformDeltaY(_minDistTitle.transform, -36f);
        TransformDeltaY(_minStepTitle.transform, -36f * 2);
        TransformDeltaY(_maxStepTitle.transform, -36f * 3);
        TransformDeltaY(_flattenTitle.transform, -36f * 4);
        TransformDeltaY(__instance.darkFogToggle.transform.parent, -36f * 4);
        TransformDeltaY(__instance.resourceMultiplierSlider.transform.parent, -36f * 4);
        TransformDeltaY(__instance.sandboxToggle.transform.parent, -36f * 4);
        TransformDeltaY(__instance.propertyMultiplierText.transform, -36f * 4);
        TransformDeltaY(__instance.addrText.transform.parent, -36f * 4);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect._OnOpen))]
    private static void UIGalaxySelect__OnOpen_Prefix()
    {
        _minDistText.text = _minDist.ToString();
        _minStepText.text = _minStep.ToString();
        _maxStepText.text = _maxStep.ToString();
        _flattenText.text = _flatten.ToString();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect._OnRegEvent))]
    private static void UIGalaxySelect__OnRegEvent_Postfix(UIGalaxySelect __instance)
    {
        _minDistSlider.onValueChanged.RemoveAllListeners();
        _minDistSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / 10.0;
            if (newVal.Equals(_minDist)) return;
            _minDist = _gameMinDist = newVal;
            _minDistText.text = _minDist.ToString();
            if (_minStep < _minDist)
            {
                _minStep = _gameMinStep = _minDist;
                _minStepSlider.value = (float)(_minStep * 10.0);
                _minStepText.text = _minStep.ToString();
                if (_maxStep < _minStep)
                {
                    _maxStep = _gameMaxStep = _minStep;
                    _maxStepSlider.value = (float)(_maxStep * 10.0);
                    _maxStepText.text = _maxStep.ToString();
                }
            }
            __instance.SetStarmapGalaxy();
        });
        _minStepSlider.onValueChanged.RemoveAllListeners();
        _minStepSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / 10.0;
            if (newVal.Equals(_minStep)) return;
            _minStep = _gameMinStep = newVal;
            _maxStepSlider.minValue = (float)(newVal * 10.0);
            _minStepText.text = _minStep.ToString();
            __instance.SetStarmapGalaxy();
        });
        _maxStepSlider.onValueChanged.RemoveAllListeners();
        _maxStepSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / 10.0;
            if (newVal.Equals(_maxStep)) return;
            _maxStep = _gameMaxStep = newVal;
            _minStepSlider.maxValue = (float)(newVal * 10.0);
            _maxStepText.text = _maxStep.ToString();
            __instance.SetStarmapGalaxy();
        });
        _flattenSlider.onValueChanged.RemoveAllListeners();
        _flattenSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / 50.0;
            if (newVal.Equals(_flatten)) return;
            _flatten = _gameFlatten = newVal;
            _flattenText.text = _flatten.ToString();
            __instance.SetStarmapGalaxy();
        });
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect._OnUnregEvent))]
    private static void UIGalaxySelect__OnUnregEvent_Postfix()
    {
        _minDistSlider.onValueChanged.RemoveAllListeners();
        _minStepSlider.onValueChanged.RemoveAllListeners();
        _maxStepSlider.onValueChanged.RemoveAllListeners();
        _flattenSlider.onValueChanged.RemoveAllListeners();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect.OnStarCountSliderValueChange))]
    private static IEnumerable<CodeInstruction> UIGalaxySelect_OnStarCountSliderValueChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        // Increase hard-coded maxium star count from 80 to MaxStarCount.Value
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && ci.OperandIs(80))
        ).SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(MoreSettings.MaxStarCount))).Insert(
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<int>), nameof(ConfigEntry<int>.Value)))
        );
        return matcher.InstructionEnumeration();
    }

    private static class PermanentPatch
    {
        private static void ResetSettings()
        {
            _gameMinDist = 2;
            _gameMinStep = 2;
            _gameMaxStep = 3.2;
            _gameFlatten = 0.18;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameData), nameof(GameData.Import))]
        private static void GameData_Import_Prefix(GameData __instance)
        {
            ResetSettings();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameData), nameof(GameData.SetForNewGame))]
        private static void GameData_SetForNewGame_Prefix(GameData __instance)
        {
            if (Enabled.Value) return;
            ResetSettings();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GalaxyData), MethodType.Constructor)]
        private static IEnumerable<CodeInstruction> GalaxyData_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // 25700 -> 102500
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(25700))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 102500));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SectorModel), nameof(SectorModel.CreateGalaxyAstroBuffer))]
        [HarmonyPatch(typeof(SpaceColliderLogic), nameof(SpaceColliderLogic.UpdateCollidersPose))]
        private static IEnumerable<CodeInstruction> SectorModel_CreateGalaxyAstroBuffer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // 25600 -> 102500
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(25600))
            );
            matcher.Repeat(m => m.SetAndAdvance(OpCodes.Ldc_I4, 102500));
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UniverseGen), nameof(UniverseGen.CreateGalaxy))]
        private static IEnumerable<CodeInstruction> UniverseGen_CreateGalaxy_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UniverseGen), nameof(UniverseGen.GenerateTempPoses)))
            ).Advance(-4).RemoveInstructions(4).Insert(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(_gameMinDist))),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(_gameMinStep))),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(_gameMaxStep))),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(_gameFlatten)))
            );
            return matcher.InstructionEnumeration();
        }

        /* Patch `rand() * (maxStepLen - minStepLen) + minDist` to `rand() * (maxStepLen - minStepLen) + minStepLen`,
           this should be a bugged line in original game code. */
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UniverseGen), nameof(UniverseGen.RandomPoses))]
        static IEnumerable<CodeInstruction> UniverseGen_RandomPoses_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(OpCodes.Ldarg_2)
            );
            matcher.Repeat(m => m.Advance(1).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldarg_3)));
            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect.EnterGame))]
    private static void UIGalaxySelect_EnterGame_Prefix()
    {
        _gameMinDist = _minDist;
        _gameMinStep = _minStep;
        _gameMaxStep = _maxStep;
        _gameFlatten = _flatten;
    }

    #region CombatSettings

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF._OnCreate))]
    private static void UICombatSettingsDF__OnCreate_Postfix(UICombatSettingsDF __instance)
    {
        __instance.initLevelSlider.maxValue = 30f;
        __instance.initGrowthSlider.maxValue = 10f;
        __instance.initOccupiedSlider.maxValue = 10f;
        __instance.growthSpeedSlider.maxValue = 7f;
        __instance.powerThreatSlider.maxValue = 10f;
        __instance.combatThreatSlider.maxValue = 10f;
        __instance.DFExpSlider.maxValue = 10f;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnInitLevelSliderChanged))]
    private static bool UICombatSettingsDF_OnInitLevelSliderChanged_Prefix(UICombatSettingsDF __instance)
    {
        __instance.combatSettings.initialLevel = __instance.initLevelSlider.value switch
        {
            < 0.5f => 0f,
            < 1.5f => 1f,
            < 2.5f => 2f,
            < 3.5f => 3f,
            < 4.5f => 4f,
            < 5.5f => 5f,
            < 6.5f => 6f,
            < 7.5f => 7f,
            < 8.5f => 8f,
            < 9.5f => 9f,
            < 10.5f => 10f,
            < 11.5f => 11f,
            < 12.5f => 12f,
            < 13.5f => 13f,
            < 14.5f => 14f,
            < 15.5f => 15f,
            < 16.5f => 16f,
            < 17.5f => 17f,
            < 18.5f => 18f,
            < 19.5f => 19f,
            < 20.5f => 20f,
            < 21.5f => 21f,
            < 22.5f => 22f,
            < 23.5f => 23f,
            < 24.5f => 24f,
            < 25.5f => 25f,
            < 26.5f => 26f,
            < 27.5f => 27f,
            < 28.5f => 28f,
            < 29.5f => 29f,
            _ => 30f
        };
        __instance.UpdateUIParametersDisplay();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnInitGrowthSliderChanged))]
    private static bool UICombatSettingsDF_OnInitGrowthSliderChanged_Prefix(UICombatSettingsDF __instance)
    {
        __instance.combatSettings.initialGrowth = __instance.initGrowthSlider.value switch
        {
            < 0.5f => 0f,
            < 1.5f => 0.25f,
            < 2.5f => 0.5f,
            < 3.5f => 0.75f,
            < 4.5f => 1f,
            < 5.5f => 1.5f,
            < 6.5f => 2f,
            < 7.5f => 2.5f,
            < 8.5f => 3f,
            < 9.5f => 3.5f,
            _ => 4f
        };
        __instance.UpdateUIParametersDisplay();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnInitOccupiedSliderChanged))]
    private static bool UICombatSettingsDF_OnInitOccupiedSliderChanged_Prefix(UICombatSettingsDF __instance)
    {
        __instance.combatSettings.initialColonize = __instance.initOccupiedSlider.value switch
        {
            < 0.5f => 0.01f,
            < 1.5f => 0.25f,
            < 2.5f => 0.5f,
            < 3.5f => 0.75f,
            < 4.5f => 1f,
            < 5.5f => 1.5f,
            < 6.5f => 2f,
            < 7.5f => 2.5f,
            < 8.5f => 3f,
            < 9.5f => 3.5f,
            _ => 4f
        };
        __instance.UpdateUIParametersDisplay();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnGrowthSpeedSliderChanged))]
    private static bool UICombatSettingsDF_OnGrowthSpeedSliderChanged_Prefix(UICombatSettingsDF __instance)
    {
        __instance.combatSettings.growthSpeedFactor = __instance.growthSpeedSlider.value switch
        {
            < 0.5f => 0.25f,
            < 1.5f => 0.5f,
            < 2.5f => 1f,
            < 3.5f => 2f,
            < 4.5f => 3f,
            < 5.5f => 4f,
            < 6.5f => 5f,
            _ => 6f
        };
        __instance.UpdateUIParametersDisplay();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnPowerThreatSliderChanged))]
    private static bool UICombatSettingsDF_OnPowerThreatSliderChanged_Prefix(UICombatSettingsDF __instance)
    {
        __instance.combatSettings.powerThreatFactor = __instance.powerThreatSlider.value switch
        {
            < 0.5f => 0.01f,
            < 1.5f => 0.1f,
            < 2.5f => 0.2f,
            < 3.5f => 0.5f,
            < 4.5f => 1f,
            < 5.5f => 2f,
            < 6.5f => 5f,
            < 7.5f => 8f,
            < 8.5f => 10f,
            < 9.5f => 15f,
            _ => 20f
        };
        __instance.UpdateUIParametersDisplay();
        return false;
    }

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnCombatThreatSliderChanged))]
	private static bool UICombatSettingsDF_OnCombatThreatSliderChanged_Prefix(UICombatSettingsDF __instance)
	{
        __instance.combatSettings.battleThreatFactor = __instance.combatThreatSlider.value switch
        {
            < 0.5f => 0.01f,
            < 1.5f => 0.1f,
            < 2.5f => 0.2f,
            < 3.5f => 0.5f,
            < 4.5f => 1f,
            < 5.5f => 2f,
            < 6.5f => 5f,
            < 7.5f => 8f,
            < 8.5f => 10f,
            < 9.5f => 15f,
            _ => 20f
        };
        __instance.UpdateUIParametersDisplay();
		return false;
	}

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnEXPSliderChanged))]
    private static bool UICombatSettingsDF_OnEXPSliderChanged_Prefix(UICombatSettingsDF __instance)
    {
        __instance.combatSettings.battleExpFactor = __instance.DFExpSlider.value switch
        {
            < 0.5f => 0.01f,
            < 1.5f => 0.1f,
            < 2.5f => 0.2f,
            < 3.5f => 0.5f,
            < 4.5f => 1f,
            < 5.5f => 2f,
            < 6.5f => 5f,
            < 7.5f => 8f,
            < 8.5f => 10f,
            < 9.5f => 15f,
            _ => 20f
        };
        __instance.UpdateUIParametersDisplay();
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.UpdateUIParametersDisplay))]
	private static bool UICombatSettingsDF_UpdateUIParametersDisplay_Prefix(UICombatSettingsDF __instance)
	{
		var text = "";
        __instance.aggresiveSlider.value = __instance.combatSettings.aggressiveness switch
        {
            < -0.99f => 0f,
            < 0.01f => 1f,
            < 0.51f => 2f,
            < 1.01f => 3f,
            < 2.01f => 4f,
            _ => 5f
        };
        text = (int)(__instance.aggresiveSlider.value + 0.5f) switch
        {
            0 => "活靶子".Translate(),
            1 => "被动".Translate(),
            2 => "消极".Translate(),
            3 => "正常".Translate(),
            4 => "积极".Translate(),
            5 => "狂暴".Translate(),
            _ => text
        };
        __instance.aggresiveText.text = text;
		var num = __instance.combatSettings.initialLevel;
        __instance.initLevelSlider.value = num switch
        {
            < 0.01f => 0f,
            < 1.01f => 1f,
            < 2.01f => 2f,
            < 3.01f => 3f,
            < 4.01f => 4f,
            < 5.01f => 5f,
            < 6.01f => 6f,
            < 7.01f => 7f,
            < 8.01f => 8f,
            < 9.01f => 9f,
            < 10.01f => 10f,
            < 11.01f => 11f,
            < 12.01f => 12f,
            < 13.01f => 13f,
            < 14.01f => 14f,
            < 15.01f => 15f,
            < 16.01f => 16f,
            < 17.01f => 17f,
            < 18.01f => 18f,
            < 19.01f => 19f,
            < 20.01f => 20f,
            < 21.01f => 21f,
            < 22.01f => 22f,
            < 23.01f => 23f,
            < 24.01f => 24f,
            < 25.01f => 25f,
            < 26.01f => 26f,
            < 27.01f => 27f,
            < 28.01f => 28f,
            < 29.01f => 29f,
            _ => 30f
        };
        __instance.initLevelText.text = num.ToString();
		num = __instance.combatSettings.initialGrowth;
        __instance.initGrowthSlider.value = num switch
        {
            < 0.01f => 0f,
            < 0.26f => 1f,
            < 0.51f => 2f,
            < 0.76f => 3f,
            < 1.01f => 4f,
            < 1.51f => 5f,
            < 2.01f => 6f,
            < 2.51f => 7f,
            < 3.01f => 8f,
            < 3.51f => 9f,
            _ => 10f
        };
        text = num * 100f + "%";
        __instance.initGrowthText.text = text;
		num = __instance.combatSettings.initialColonize;
        __instance.initOccupiedSlider.value = num switch
        {
            < 0.02f => 0f,
            < 0.26f => 1f,
            < 0.51f => 2f,
            < 0.76f => 3f,
            < 1.01f => 4f,
            < 1.51f => 5f,
            < 2.01f => 6f,
            < 2.51f => 7f,
            < 3.01f => 8f,
            < 3.51f => 9f,
            _ => 10f
        };
        text = num * 100f + "%";
        __instance.initOccupiedText.text = text;
		num = __instance.combatSettings.maxDensity;
        __instance.maxDensitySlider.value = num switch
        {
            < 1.01f => 0f,
            < 1.51f => 1f,
            < 2.01f => 2f,
            < 2.51f => 3f,
            _ => 4f
        };
        text = num + "x";
        __instance.maxDensityText.text = text;
		num = __instance.combatSettings.growthSpeedFactor;
        __instance.growthSpeedSlider.value = num switch
        {
            < 0.26f => 0f,
            < 0.51f => 1f,
            < 1.01f => 2f,
            < 2.01f => 3f,
            < 3.01f => 4f,
            < 4.01f => 5f,
            < 5.01f => 6f,
            _ => 7f
        };
        text = num * 100f + "%";
        __instance.growthSpeedText.text = text;
		num = __instance.combatSettings.powerThreatFactor;
        __instance.powerThreatSlider.value = num switch
        {
            < 0.02f => 0f,
            < 0.11f => 1f,
            < 0.21000001f => 2f,
            < 0.51f => 3f,
            < 1.01f => 4f,
            < 2.01f => 5f,
            < 5.01f => 6f,
            < 8.01f => 7f,
            < 10.01f => 8f,
            < 15.01f => 9f,
            _ => 10f
        };
        text = num * 100f + "%";
        __instance.powerThreatText.text = text;
		num = __instance.combatSettings.battleThreatFactor;
        __instance.combatThreatSlider.value = num switch
        {
            < 0.02f => 0f,
            < 0.11f => 1f,
            < 0.21000001f => 2f,
            < 0.51f => 3f,
            < 1.01f => 4f,
            < 2.01f => 5f,
            < 5.01f => 6f,
            < 8.01f => 7f,
            < 10.01f => 8f,
            < 15.01f => 9f,
            _ => 10f
        };
        text = num * 100f + "%";
        __instance.combatThreatText.text = text;
		num = __instance.combatSettings.battleExpFactor;
        __instance.DFExpSlider.value = num switch
        {
            < 0.02f => 0f,
            < 0.11f => 1f,
            < 0.21000001f => 2f,
            < 0.51f => 3f,
            < 1.01f => 4f,
            < 2.01f => 5f,
            < 5.01f => 6f,
            < 8.01f => 7f,
            < 10.01f => 8f,
            < 15.01f => 9f,
            _ => 10f
        };
        text = num * 100f + "%";
        __instance.DFExpText.text = text;
		var gameDesc = new GameDesc();
		var difficulty = __instance.combatSettings.difficulty;
		var text2 = difficulty >= 9.9999f ? difficulty.ToString("0.00") : difficulty.ToString("0.000");
        __instance.difficultyText.text = string.Format("难度系数值".Translate(), text2);
        __instance.difficultTipGroupDF.SetActive((__instance.combatSettings.aggressiveLevel == EAggressiveLevel.Rampage && difficulty > 4.5f) || difficulty > 6f);
        __instance.gameDesc.CopyTo(gameDesc);
		gameDesc.combatSettings = __instance.combatSettings;
        __instance.propertyMultiplierText.text = "元数据生成倍率".Translate() + " " + gameDesc.propertyMultiplier.ToString("0%");
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CombatSettings),nameof(CombatSettings.difficulty), MethodType.Getter)]
    private static bool CombatSettings_difficulty_Getter_Prefix(CombatSettings __instance, ref float __result)
    {
        var aggressivenessScore = __instance.aggressiveness switch
        {
            < -0.1f => -0.2f,
            < 0.25f => 0f,
            < 0.75f => 0.5f,
            < 1.5f => 0.75f,
            < 2.5f => 0.875f,
            _ => 1.125f
        };
        var initialLevelScore = __instance.initialLevel * 0.8f;
        var initialGrowthScore = __instance.initialGrowth switch
        {
            < 0.15f => 0f,
            < 0.3f => 0.25f,
            < 0.65f => 0.5f,
            < 0.8f => 0.75f,
            < 1.15f => 1f,
            < 1.65f => 1.25f,
            < 2.15f => 1.5f,
            < 2.65f => 1.75f,
            < 3.15f => 2f,
            < 3.65f => 2.25f,
            _ => 2.5f
        };
        var initialColonizeScore = __instance.initialColonize switch
        {
            < 0.15f => 0f,
            < 0.3f => 0.25f,
            < 0.65f => 0.5f,
            < 0.8f => 0.75f,
            < 1.15f => 1f,
            < 1.65f => 1.25f,
            < 2.15f => 1.5f,
            < 2.65f => 1.75f,
            < 3.15f => 2f,
            < 3.65f => 2.25f,
            _ => 2.5f
        };
        var maxDensityScore = __instance.maxDensity - 1f;
        var growthSpeedFactorScore = __instance.growthSpeedFactor switch
        {
            < 0.35f => 0.3f,
            < 0.75f => 0.7f,
            < 1.5f => 1f,
            < 2.5f => 1.2f,
            < 3.5f => 1.5f,
            < 4.5f => 1.6f,
            < 5.5f => 1.8f,
            _ => 2f
        };
        var powerThreatFactorScore = __instance.powerThreatFactor switch
        {
            < 0.05f => 0.125f,
            < 0.15f => 0.3f,
            < 0.25f => 0.6f,
            < 0.55f => 0.8f,
            < 1.15f => 1f,
            < 2.15f => 1.2f,
            < 5.15f => 1.5f,
            < 8.15f => 1.8f,
            < 10.15f => 2f,
            < 15.15f => 2.5f,
            _ => 3f
        };
        var battleThreatFactorScore = __instance.battleThreatFactor switch
        {
            < 0.05f => 0.125f,
            < 0.15f => 0.3f,
            < 0.25f => 0.6f,
            < 0.55f => 0.8f,
            < 1.15f => 1f,
            < 2.15f => 1.2f,
            < 5.15f => 1.5f,
            < 8.15f => 1.8f,
            < 10.15f => 2f,
            < 15.15f => 2.5f,
            _ => 3f
        };
        var battleExpFactorScore = __instance.battleExpFactor switch
        {
            < 0.05f => 0f,
            < 0.15f => 1f,
            < 0.25f => 3f,
            < 0.55f => 6f,
            < 1.15f => 10f,
            < 2.15f => 12f,
            < 5.15f => 14f,
            < 8.15f => 16f,
            < 10.15f => 18f,
            < 15.15f => 19f,
            _ => 20f
        };
        var score1 = aggressivenessScore < 0f ? 0f : 0.25f + aggressivenessScore * (powerThreatFactorScore * 0.5f + battleThreatFactorScore * 0.5f);
		var score2 = 0.375f + 0.625f * ((initialLevelScore + battleExpFactorScore) / 10f);
		var score3 = 0.375f + 0.625f * ((initialColonizeScore * 0.6f + initialGrowthScore * 0.4f * (initialColonizeScore * 0.75f + 0.25f)) * 0.6f + growthSpeedFactorScore * 0.4f * (initialColonizeScore * 0.8f + 0.2f) + maxDensityScore * 0.29f * (initialColonizeScore * 0.5f + 0.5f));
		__result = (int)(score1 * score2 * score3 * 10000f + 0.5f) / 10000f;
        
        return false;
    }
    #endregion

    #region ModSave
    public static void Export(BinaryWriter w)
    {
        w.Write(_gameMinDist);
        w.Write(_gameMinStep);
        w.Write(_gameMaxStep);
        w.Write(_gameFlatten);
    }

    public static void Import(BinaryReader r)
    {
        _gameMinDist = r.ReadDouble();
        _gameMinStep = r.ReadDouble();
        _gameMaxStep = r.ReadDouble();
        _gameFlatten = r.ReadDouble();
    }
    #endregion
}