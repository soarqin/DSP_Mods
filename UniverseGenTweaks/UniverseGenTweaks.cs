using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace UniverseGenTweaks;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class UniverseGenTweaks : BaseUnityPlugin
{
    private new static readonly BepInEx.Logging.ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

    private bool _cfgEnabled = true;
    private static int _maxStarCount = 128;
    private static float _minDist = 2f;
    private static float _minStep = 2f;
    private static float _maxStep = 3.2f;
    private static float _flatten = 0.18f;

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

    private void Awake()
    {
        _cfgEnabled = Config.Bind("General", "Enabled", _cfgEnabled, "enable/disable this plugin").Value;
        _maxStarCount = Config.Bind("General", "MaxStarCount", _maxStarCount,
                new ConfigDescription("Maximum star count for galaxy creation",
                    new AcceptableValueRange<int>(32, 1024), new {}))
            .Value;
        Harmony.CreateAndPatchAll(typeof(UniverseGenTweaks));
    }

    private static void createSliderWithText(Slider orig, out Text title, out Slider slider, out Text text)
    {
        var origText = orig.transform.parent.GetComponent<Text>();
        title = Object.Instantiate(origText, origText.transform.parent);
        slider = title.transform.FindChildRecur("Slider").GetComponent<Slider>();
        text = slider.transform.FindChildRecur("Text").GetComponent<Text>();
    }

    private static void transformDeltaY(Transform trans, float delta)
    {
        var pos = trans.position;
        pos.y += delta;
        trans.position = pos;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIGalaxySelect), "_OnInit")]
    private static void PatchGalaxyUI_OnInit(UIGalaxySelect __instance)
    {
        __instance.starCountSlider.maxValue = _maxStarCount;

        createSliderWithText(__instance.starCountSlider, out _minDistTitle, out _minDistSlider, out _minDistText);
        createSliderWithText(__instance.starCountSlider, out _minStepTitle, out _minStepSlider, out _minStepText);
        createSliderWithText(__instance.starCountSlider, out _maxStepTitle, out _maxStepSlider, out _maxStepText);
        createSliderWithText(__instance.starCountSlider, out _flattenTitle, out _flattenSlider, out _flattenText);

        _minDistTitle.name = "min-dist";
        _minDistSlider.minValue = 10f;
        _minDistSlider.maxValue = 50f;
        _minDistSlider.value = _minDist * 10f;

        _minStepTitle.name = "min-step";
        _minStepSlider.minValue = 10f;
        _minStepSlider.maxValue = _maxStep * 10f - 1f;
        _minStepSlider.value = _minStep * 10f;

        _maxStepTitle.name = "max-step";
        _maxStepSlider.minValue = _minStep * 10f + 1f;
        _maxStepSlider.maxValue = 100f;
        _maxStepSlider.value = _maxStep * 10f;

        _flattenTitle.name = "flatten";
        _flattenSlider.minValue = 1f;
        _flattenSlider.maxValue = 50f;
        _flattenSlider.value = _flatten * 50f;

        transformDeltaY(_minDistTitle.transform, -0.3573f);
        transformDeltaY(_minStepTitle.transform, -0.3573f * 2);
        transformDeltaY(_maxStepTitle.transform, -0.3573f * 3);
        transformDeltaY(_flattenTitle.transform, -0.3573f * 4);
        transformDeltaY(__instance.resourceMultiplierSlider.transform.parent, -0.3573f * 4);
        transformDeltaY(__instance.sandboxToggle.transform.parent, -0.3573f * 4);
        transformDeltaY(__instance.propertyMultiplierText.transform, -0.3573f * 4);
        transformDeltaY(__instance.addrText.transform.parent, -0.3573f * 4);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIGalaxySelect), "_OnOpen")]
    private static void PatchGalaxyUI_OnOpen(UIGalaxySelect __instance)
    {
        if (Localization.language == Language.zhCN)
        {
            _minDistTitle.text = "恒星最小距离";
            _minStepTitle.text = "步进最小距离";
            _maxStepTitle.text = "步进最大距离";
            _flattenTitle.text = "扁平度";
        }
        else
        {
            _minDistTitle.text = "Star Distance Min";
            _minStepTitle.text = "Step Distance Min";
            _maxStepTitle.text = "Step Distance Max";
            _flattenTitle.text = "Flatten";
        }
        _minDistText.text = _minDist.ToString();
        _minStepText.text = _minStep.ToString();
        _maxStepText.text = _maxStep.ToString();
        _flattenText.text = _flatten.ToString();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIGalaxySelect), "_OnRegEvent")]
    private static void PatchGalaxyUI_OnRegEvent(UIGalaxySelect __instance)
    {
        _minDistSlider.onValueChanged.RemoveAllListeners();
        _minDistSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / 10f;
            if (newVal.Equals(_minDist)) return;
            _minDist = newVal;
            _minDistText.text = _minDist.ToString();
            __instance.SetStarmapGalaxy();
        });
        _minStepSlider.onValueChanged.RemoveAllListeners();
        _minStepSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / 10f;
            if (newVal.Equals(_minStep)) return;
            _minStep = newVal;
            _maxStepSlider.minValue = newVal * 10f;
            _minStepText.text = _minStep.ToString();
            __instance.SetStarmapGalaxy();
        });
        _maxStepSlider.onValueChanged.RemoveAllListeners();
        _maxStepSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / 10f;
            if (newVal.Equals(_maxStep)) return;
            _maxStep = newVal;
            _minStepSlider.maxValue = newVal * 10f;
            _maxStepText.text = _maxStep.ToString();
            __instance.SetStarmapGalaxy();
        });
        _flattenSlider.onValueChanged.RemoveAllListeners();
        _flattenSlider.onValueChanged.AddListener(val =>
        {
            var newVal = Mathf.Round(val) / 50f;
            if (newVal.Equals(_flatten)) return;
            _flatten = newVal;
            _flattenText.text = _flatten.ToString();
            __instance.SetStarmapGalaxy();
        });
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIGalaxySelect), "OnStarCountSliderValueChange")]
    static IEnumerable<CodeInstruction> PatchStarCountOnValueChange(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.OperandIs(80))
            {
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UniverseGenTweaks), "_maxStarCount"));
            }
            else
            {
                yield return instruction;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GalaxyData), MethodType.Constructor)]
    static bool PatchGalaxyData(GalaxyData __instance)
    {
        __instance.astrosData = new AstroData[(_maxStarCount + 1) * 100];
        return false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UniverseGen), "CreateGalaxy")]
    static IEnumerable<CodeInstruction> PatchCreateGalaxy(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Call &&
                instruction.OperandIs(AccessTools.Method(typeof(UniverseGen), "GenerateTempPoses")))
            {
                var pop = new CodeInstruction(OpCodes.Pop);
                yield return pop;
                yield return pop;
                yield return pop;
                yield return pop;
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UniverseGenTweaks), "_minDist"));
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UniverseGenTweaks), "_minStep"));
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UniverseGenTweaks), "_maxStep"));
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UniverseGenTweaks), "_flatten"));
            }
            yield return instruction;
        }
    }

    /* Patch `rand() * (maxStepLen - minStepLen) + minDist` to `rand() * (maxStepLen - minStepLen) + minStepLen`,
       this should be a bugged line in original game code. */
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UniverseGen), "RandomPoses")]
    static IEnumerable<CodeInstruction> PatchUniverGenRandomPoses(IEnumerable<CodeInstruction> instructions)
    {
        var lastIsMul = false;
        foreach (var instruction in instructions)
        {
            if (lastIsMul && instruction.opcode == OpCodes.Ldarg_2)
            {
                lastIsMul = false;
                yield return new CodeInstruction(OpCodes.Ldarg_3);
                continue;
            }
            lastIsMul = instruction.opcode == OpCodes.Mul;
            yield return instruction;
        }
    }
}