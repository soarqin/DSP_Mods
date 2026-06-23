using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.Common.GameConstants;
using UXAssist.Common.ModFeatures;
using Object = UnityEngine.Object;

namespace UniverseGenTweaks;

[ModFeature("UniverseGenGalaxySelectUI", Order = 10)]
public static class GalaxySelectUIPatch
{
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
    private static Harmony _patch;

    public static void Init()
    {
        I18N.Add("恒星最小距离", "Star Distance Min", "恒星最小距离");
        I18N.Add("步进最小距离", "Step Distance Min", "步进最小距离");
        I18N.Add("步进最大距离", "Step Distance Max", "步进最大距离");
        I18N.Add("扁平度", "Flatness", "扁平度");
        I18N.Apply();
        MoreSettings.Enabled.SettingChanged += OnEnabledChanged;
        Enable(MoreSettings.Enabled.Value);
    }

    public static void Uninit()
    {
        MoreSettings.Enabled.SettingChanged -= OnEnabledChanged;
        Enable(false);
    }

    private static void OnEnabledChanged(object sender, System.EventArgs e)
    {
        Enable(MoreSettings.Enabled.Value);
    }

    internal static void Enable(bool on)
    {
        if (on)
        {
            _patch ??= Harmony.CreateAndPatchAll(typeof(Patch));
            return;
        }
        _patch?.UnpatchSelf();
        _patch = null;
    }

    private static class Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect._OnCreate))]
        private static void UIGalaxySelect__OnCreate_Postfix(UIGalaxySelect __instance)
        {
            __instance.starCountSlider.maxValue = MoreSettings.MaxStarCount.Value;

            CreateSliderWithText(__instance.starCountSlider, out _minDistTitle, out _minDistSlider, out _minDistText, out var minDistLocalizer);
            CreateSliderWithText(__instance.starCountSlider, out _minStepTitle, out _minStepSlider, out _minStepText, out var minStepLocalizer);
            CreateSliderWithText(__instance.starCountSlider, out _maxStepTitle, out _maxStepSlider, out _maxStepText, out var maxStepLocalizer);
            CreateSliderWithText(__instance.starCountSlider, out _flattenTitle, out _flattenSlider, out _flattenText, out var flattenLocalizer);
            minDistLocalizer.stringKey = "恒星最小距离";
            minStepLocalizer.stringKey = "步进最小距离";
            maxStepLocalizer.stringKey = "步进最大距离";
            flattenLocalizer.stringKey = "扁平度";

            _minDistTitle.name = "min-dist";
            _minStepTitle.name = "min-step";
            _maxStepTitle.name = "max-step";
            _flattenTitle.name = "flatten";

            TransformDeltaY(_minDistTitle.transform, UniverseGenConstants.SliderRowSpacing);
            TransformDeltaY(_minStepTitle.transform, UniverseGenConstants.SliderRowSpacing * 2);
            TransformDeltaY(_maxStepTitle.transform, UniverseGenConstants.SliderRowSpacing * 3);
            TransformDeltaY(_flattenTitle.transform, UniverseGenConstants.SliderRowSpacing * 4);
            TransformDeltaY(__instance.darkFogToggle.transform.parent, UniverseGenConstants.SliderRowSpacing * 4);
            TransformDeltaY(__instance.resourceMultiplierSlider.transform.parent, UniverseGenConstants.SliderRowSpacing * 4);
            TransformDeltaY(__instance.sandboxToggle.transform.parent, UniverseGenConstants.SliderRowSpacing * 4);
            TransformDeltaY(__instance.propertyMultiplierText.transform, UniverseGenConstants.SliderRowSpacing * 4);
            TransformDeltaY(__instance.addrText.transform.parent, UniverseGenConstants.SliderRowSpacing * 4);

            RemoveAllListeners();
            UpdateSliderControls();
            AddListeners(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect._OnOpen))]
        private static void UIGalaxySelect__OnOpen_Prefix(UIGalaxySelect __instance)
        {
            GalaxyGenSettingsPatch.MinDist = UniverseGenConstants.DefaultMinDist;
            GalaxyGenSettingsPatch.MinStep = UniverseGenConstants.DefaultMinStep;
            GalaxyGenSettingsPatch.MaxStep = UniverseGenConstants.DefaultMaxStep;
            GalaxyGenSettingsPatch.Flatten = UniverseGenConstants.DefaultFlatten;

            RemoveAllListeners();
            UpdateSliderControls();
            AddListeners(__instance);
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

    private static void UpdateSliderControls()
    {
        _minDistSlider.minValue = UniverseGenConstants.MinDistSliderMin;
        _minDistSlider.maxValue = UniverseGenConstants.MinDistSliderMax;
        _minDistSlider.value = (float)(GalaxyGenSettingsPatch.MinDist * UniverseGenConstants.StepSliderScale);

        _minStepSlider.minValue = (float)(GalaxyGenSettingsPatch.MinDist * UniverseGenConstants.StepSliderScale);
        _minStepSlider.maxValue = (float)(GalaxyGenSettingsPatch.MaxStep * UniverseGenConstants.StepSliderScale);
        _minStepSlider.value = (float)(GalaxyGenSettingsPatch.MinStep * UniverseGenConstants.StepSliderScale);

        _maxStepSlider.minValue = (float)(GalaxyGenSettingsPatch.MinStep * UniverseGenConstants.StepSliderScale);
        _maxStepSlider.maxValue = UniverseGenConstants.MaxStepSliderMax;
        _maxStepSlider.value = (float)(GalaxyGenSettingsPatch.MaxStep * UniverseGenConstants.StepSliderScale);

        _flattenSlider.minValue = UniverseGenConstants.FlattenSliderMin;
        _flattenSlider.maxValue = UniverseGenConstants.FlattenSliderMax;
        _flattenSlider.value = (float)(GalaxyGenSettingsPatch.Flatten * UniverseGenConstants.FlattenSliderScale);

        _minDistText.text = GalaxyGenSettingsPatch.MinDist.ToString();
        _minStepText.text = GalaxyGenSettingsPatch.MinStep.ToString();
        _maxStepText.text = GalaxyGenSettingsPatch.MaxStep.ToString();
        _flattenText.text = GalaxyGenSettingsPatch.Flatten.ToString();

        UniverseGenTweaks.Logger.LogDebug($"Updated slider controls: {_minStepSlider.minValue}, {_minStepSlider.maxValue}, {_maxStepSlider.minValue}, {_maxStepSlider.maxValue}");
    }

    private static void RemoveAllListeners()
    {
        _minDistSlider.onValueChanged.RemoveAllListeners();
        _minStepSlider.onValueChanged.RemoveAllListeners();
        _maxStepSlider.onValueChanged.RemoveAllListeners();
        _flattenSlider.onValueChanged.RemoveAllListeners();
    }

    private static void AddListeners(UIGalaxySelect uiGalaxySelect)
    {
        _minDistSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / UniverseGenConstants.StepSliderScale;
            if (newVal.Equals(GalaxyGenSettingsPatch.MinDist)) return;
            GalaxyGenSettingsPatch.MinDist = newVal;
            _minDistText.text = GalaxyGenSettingsPatch.MinDist.ToString();
            if (GalaxyGenSettingsPatch.MinStep < GalaxyGenSettingsPatch.MinDist)
            {
                GalaxyGenSettingsPatch.MinStep = GalaxyGenSettingsPatch.MinDist;
                _minStepSlider.value = (float)(GalaxyGenSettingsPatch.MinStep * 10.0);
                _minStepText.text = GalaxyGenSettingsPatch.MinStep.ToString();
                if (GalaxyGenSettingsPatch.MaxStep < GalaxyGenSettingsPatch.MinStep)
                {
                    GalaxyGenSettingsPatch.MaxStep = GalaxyGenSettingsPatch.MinStep;
                    _maxStepSlider.value = (float)(GalaxyGenSettingsPatch.MaxStep * 10.0);
                    _maxStepText.text = GalaxyGenSettingsPatch.MaxStep.ToString();
                }
            }
            _minStepSlider.minValue = (float)(GalaxyGenSettingsPatch.MinDist * 10.0);
            uiGalaxySelect.SetStarmapGalaxy();
        });
        _minStepSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / UniverseGenConstants.StepSliderScale;
            if (newVal.Equals(GalaxyGenSettingsPatch.MinStep)) return;
            GalaxyGenSettingsPatch.MinStep = newVal;
            _maxStepSlider.minValue = (float)(newVal * 10.0);
            _minStepText.text = GalaxyGenSettingsPatch.MinStep.ToString();
            uiGalaxySelect.SetStarmapGalaxy();
        });
        _maxStepSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / UniverseGenConstants.StepSliderScale;
            if (newVal.Equals(GalaxyGenSettingsPatch.MaxStep)) return;
            GalaxyGenSettingsPatch.MaxStep = newVal;
            _minStepSlider.maxValue = (float)(newVal * 10.0);
            _maxStepText.text = GalaxyGenSettingsPatch.MaxStep.ToString();
            uiGalaxySelect.SetStarmapGalaxy();
        });
        _flattenSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / UniverseGenConstants.FlattenSliderScale;
            if (newVal.Equals(GalaxyGenSettingsPatch.Flatten)) return;
            GalaxyGenSettingsPatch.Flatten = newVal;
            _flattenText.text = GalaxyGenSettingsPatch.Flatten.ToString();
            uiGalaxySelect.SetStarmapGalaxy();
        });
    }
}
