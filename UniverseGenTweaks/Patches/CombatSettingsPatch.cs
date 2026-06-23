using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common.ModFeatures;

namespace UniverseGenTweaks;

[ModFeature("UniverseGenCombat", Order = 12)]
public static class CombatSettingsPatch
{
    private static Harmony _patch;

    private delegate Slider SliderGetter(UICombatSettingsDF instance);
    private delegate void CombatValueSetter(UICombatSettingsDF instance, float value);

    private static readonly SliderMapper[] SliderMappers =
    {
        new(
            "OnInitLevelSliderChanged",
            inst => inst.initLevelSlider,
            (inst, v) => inst.combatSettings.initialLevel = v,
            new[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f, 10.5f, 11.5f, 12.5f, 13.5f, 14.5f, 15.5f, 16.5f, 17.5f, 18.5f, 19.5f, 20.5f, 21.5f, 22.5f, 23.5f, 24.5f, 25.5f, 26.5f, 27.5f, 28.5f, 29.5f },
            new[] { 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f, 12f, 13f, 14f, 15f, 16f, 17f, 18f, 19f, 20f, 21f, 22f, 23f, 24f, 25f, 26f, 27f, 28f, 29f, 30f }
        ),
        new(
            "OnInitGrowthSliderChanged",
            inst => inst.initGrowthSlider,
            (inst, v) => inst.combatSettings.initialGrowth = v,
            new[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f },
            new[] { 0f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f }
        ),
        new(
            "OnInitOccupiedSliderChanged",
            inst => inst.initOccupiedSlider,
            (inst, v) => inst.combatSettings.initialColonize = v,
            new[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f },
            new[] { 0.01f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f }
        ),
        new(
            "OnGrowthSpeedSliderChanged",
            inst => inst.growthSpeedSlider,
            (inst, v) => inst.combatSettings.growthSpeedFactor = v,
            new[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f },
            new[] { 0.25f, 0.5f, 1f, 2f, 3f, 4f, 5f, 6f }
        ),
        new(
            "OnPowerThreatSliderChanged",
            inst => inst.powerThreatSlider,
            (inst, v) => inst.combatSettings.powerThreatFactor = v,
            new[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f },
            new[] { 0.01f, 0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 8f, 10f, 15f, 20f }
        ),
        new(
            "OnCombatThreatSliderChanged",
            inst => inst.combatThreatSlider,
            (inst, v) => inst.combatSettings.battleThreatFactor = v,
            new[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f },
            new[] { 0.01f, 0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 8f, 10f, 15f, 20f }
        ),
        new(
            "OnEXPSliderChanged",
            inst => inst.DFExpSlider,
            (inst, v) => inst.combatSettings.battleExpFactor = v,
            new[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f },
            new[] { 0.01f, 0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 8f, 10f, 15f, 20f }
        )
    };

    public static void Init()
    {
        MoreSettings.Enabled.SettingChanged += OnEnabledChanged;
        Enable(MoreSettings.Enabled.Value);
    }

    public static void Uninit()
    {
        MoreSettings.Enabled.SettingChanged -= OnEnabledChanged;
        Enable(false);
    }

    private static void OnEnabledChanged(object sender, EventArgs e)
    {
        Enable(MoreSettings.Enabled.Value);
    }

    internal static void Enable(bool on)
    {
        if (on)
        {
            _patch ??= Harmony.CreateAndPatchAll(typeof(CombatSettingsPatch));
            return;
        }
        _patch?.UnpatchSelf();
        _patch = null;
    }

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
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnInitGrowthSliderChanged))]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnInitOccupiedSliderChanged))]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnGrowthSpeedSliderChanged))]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnPowerThreatSliderChanged))]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnCombatThreatSliderChanged))]
    [HarmonyPatch(typeof(UICombatSettingsDF), nameof(UICombatSettingsDF.OnEXPSliderChanged))]
    private static bool SliderChanged_Prefix(UICombatSettingsDF __instance, MethodInfo __originalMethod)
    {
        foreach (var mapper in SliderMappers)
        {
            if (mapper.MethodName != __originalMethod.Name) continue;
            var slider = mapper.GetSlider(__instance);
            var value = MapSlider(slider.value, mapper.Thresholds, mapper.Values);
            mapper.SetValue(__instance, value);
            __instance.UpdateUIParametersDisplay();
            return false;
        }
        return true;
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
            0 => Localization.AggressivenessSittingDuck.Translate(),
            1 => Localization.AggressivenessPassive.Translate(),
            2 => Localization.AggressivenessNegative.Translate(),
            3 => Localization.AggressivenessNormal.Translate(),
            4 => Localization.AggressivenessAggressive.Translate(),
            5 => Localization.AggressivenessRampage.Translate(),
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
        __instance.difficultyText.text = string.Format(Localization.DifficultyValueFormat.Translate(), text2);
        __instance.difficultTipGroupDF.SetActive((__instance.combatSettings.aggressiveLevel == EAggressiveLevel.Rampage && difficulty > 4.5f) || difficulty > 6f);
        __instance.gameDesc.CopyTo(gameDesc);
        gameDesc.combatSettings = __instance.combatSettings;
        __instance.propertyMultiplierText.text = Localization.PropertyMultiplier.Translate() + " " + gameDesc.propertyMultiplier.ToString("0%");
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CombatSettings), nameof(CombatSettings.difficulty), MethodType.Getter)]
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

    private static float MapSlider(float value, float[] thresholds, float[] values)
    {
        for (var i = 0; i < thresholds.Length; i++)
        {
            if (value < thresholds[i]) return values[i];
        }
        return values[values.Length - 1];
    }

    private readonly struct SliderMapper
    {
        public readonly string MethodName;
        public readonly SliderGetter GetSlider;
        public readonly CombatValueSetter SetValue;
        public readonly float[] Thresholds;
        public readonly float[] Values;

        public SliderMapper(string methodName, SliderGetter getSlider, CombatValueSetter setValue, float[] thresholds, float[] values)
        {
            MethodName = methodName;
            GetSlider = getSlider;
            SetValue = setValue;
            Thresholds = thresholds;
            Values = values;
        }
    }
}
