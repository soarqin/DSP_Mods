using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace UniverseGenTweaks; 

public class MoreSettings
{
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

    public static void Init()
    {
        I18N.Add("恒星最小距离", "Star Distance Min", "恒星最小距离");
        I18N.Add("步进最小距离", "Step Distance Min", "步进最小距离");
        I18N.Add("步进最大距离", "Step Distance Max", "步进最大距离");
        I18N.Add("扁平度", "Flatness", "扁平度");
        I18N.Apply();
        Harmony.CreateAndPatchAll(typeof(MoreSettings));
    }

    private static void CreateSliderWithText(Slider orig, out Text title, out Slider slider, out Text text)
    {
        var origText = orig.transform.parent.GetComponent<Text>();
        title = Object.Instantiate(origText, origText.transform.parent);
        slider = title.transform.FindChildRecur("Slider").GetComponent<Slider>();
        text = slider.transform.FindChildRecur("Text").GetComponent<Text>();
    }

    private static void TransformDeltaY(Transform trans, float delta)
    {
        var pos = trans.position;
        pos.y += delta;
        trans.position = pos;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect._OnInit))]
    private static void UIGalaxySelect__OnInit_Postfix(UIGalaxySelect __instance)
    {
        __instance.starCountSlider.maxValue = MaxStarCount.Value;

        CreateSliderWithText(__instance.starCountSlider, out _minDistTitle, out _minDistSlider, out _minDistText);
        CreateSliderWithText(__instance.starCountSlider, out _minStepTitle, out _minStepSlider, out _minStepText);
        CreateSliderWithText(__instance.starCountSlider, out _maxStepTitle, out _maxStepSlider, out _maxStepText);
        CreateSliderWithText(__instance.starCountSlider, out _flattenTitle, out _flattenSlider, out _flattenText);

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

        TransformDeltaY(_minDistTitle.transform, -0.3573f);
        TransformDeltaY(_minStepTitle.transform, -0.3573f * 2);
        TransformDeltaY(_maxStepTitle.transform, -0.3573f * 3);
        TransformDeltaY(_flattenTitle.transform, -0.3573f * 4);
        TransformDeltaY(__instance.resourceMultiplierSlider.transform.parent, -0.3573f * 4);
        TransformDeltaY(__instance.sandboxToggle.transform.parent, -0.3573f * 4);
        TransformDeltaY(__instance.propertyMultiplierText.transform, -0.3573f * 4);
        TransformDeltaY(__instance.addrText.transform.parent, -0.3573f * 4);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect._OnOpen))]
    private static void UIGalaxySelect__OnOpen_Prefix()
    {
        _minDistTitle.text = "恒星最小距离".Translate();
        _minStepTitle.text = "步进最小距离".Translate();
        _maxStepTitle.text = "步进最大距离".Translate();
        _flattenTitle.text = "扁平度".Translate();
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
            _minDist = newVal;
            _minDistText.text = _minDist.ToString();
            if (_minStep < _minDist)
            {
                _minStep = _minDist;
                _minStepSlider.value = (float)(_minStep * 10.0);
                _minStepText.text = _minStep.ToString();
                if (_maxStep < _minStep)
                {
                    _maxStep = _minStep;
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
            _minStep = newVal;
            _maxStepSlider.minValue = (float)(newVal * 10.0);
            _minStepText.text = _minStep.ToString();
            __instance.SetStarmapGalaxy();
        });
        _maxStepSlider.onValueChanged.RemoveAllListeners();
        _maxStepSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / 10.0;
            if (newVal.Equals(_maxStep)) return;
            _maxStep = newVal;
            _minStepSlider.maxValue = (float)(newVal * 10.0);
            _maxStepText.text = _maxStep.ToString();
            __instance.SetStarmapGalaxy();
        });
        _flattenSlider.onValueChanged.RemoveAllListeners();
        _flattenSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / 50.0;
            if (newVal.Equals(_flatten)) return;
            _flatten = newVal;
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
        ).Set(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(MoreSettings.MaxStarCount))).Insert(
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<int>), nameof(ConfigEntry<int>.Value)))
        );
        return matcher.InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GalaxyData), MethodType.Constructor)]
    private static IEnumerable<CodeInstruction> GalaxyData_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        // 25600 -> (MaxStarCount.Value + 1) * 100
        var matcher = new CodeMatcher(instructions, generator);
        matcher.MatchForward(false,
            new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(25600))
        ).Set(OpCodes.Ldfld, AccessTools.Field(typeof(MoreSettings), nameof(MoreSettings.MaxStarCount))).Insert(
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<int>), nameof(ConfigEntry<int>.Value))),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Add),
            new CodeInstruction(OpCodes.Ldc_I4_S, 100),
            new CodeInstruction(OpCodes.Mul)
        );
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
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(_minDist))),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(_minStep))),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(_maxStep))),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreSettings), nameof(_flatten)))
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