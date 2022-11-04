using System.Collections.Generic;
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
    private static int _maxStarCount = 64;
    private static float _minDist = 2f;
    private static float _maxDist = 3.2f;
    private static float _flatten = 0.18f;

    private static Text _minDistTitle;
    private static Text _maxDistTitle;
    private static Text _flattenTitle;
    private static Slider _minDistSlider;
    private static Slider _maxDistSlider;
    private static Slider _flattenSlider;
    private static Text _minDistText;
    private static Text _maxDistText;
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
        createSliderWithText(__instance.starCountSlider, out _maxDistTitle, out _maxDistSlider, out _maxDistText);
        createSliderWithText(__instance.starCountSlider, out _flattenTitle, out _flattenSlider, out _flattenText);

        _minDistTitle.name = "min-dist";
        _minDistSlider.minValue = 10f;
        _minDistSlider.maxValue = _maxDist * 10f;
        _minDistSlider.value = _minDist * 10f;

        _maxDistTitle.name = "max-dist";
        _maxDistSlider.minValue = _minDist * 10f;
        _maxDistSlider.maxValue = 100f;
        _maxDistSlider.value = _maxDist * 10f;

        _flattenTitle.name = "flatten";
        _flattenSlider.minValue = 1f;
        _flattenSlider.maxValue = 50f;
        _flattenSlider.value = _flatten * 50f;

        transformDeltaY(_minDistTitle.transform, -0.3573f);
        transformDeltaY(_maxDistTitle.transform, -0.3573f * 2);
        transformDeltaY(_flattenTitle.transform, -0.3573f * 3);
        transformDeltaY(__instance.resourceMultiplierSlider.transform.parent, -0.3573f * 3);
        transformDeltaY(__instance.sandboxToggle.transform.parent, -0.3573f * 3);
        transformDeltaY(__instance.propertyMultiplierText.transform, -0.3573f * 3);
        transformDeltaY(__instance.addrText.transform.parent, -0.3573f * 3);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIGalaxySelect), "_OnOpen")]
    private static void PatchGalaxyUI_OnOpen(UIGalaxySelect __instance)
    {
        if (Localization.language == Language.zhCN)
        {
            _minDistTitle.text = "恒星/步进距离";
            _maxDistTitle.text = "步进最大距离";
            _flattenTitle.text = "扁平度";
        }
        else
        {
            _minDistTitle.text = "Star/Step Distance";
            _maxDistTitle.text = "Step Distance Max";
            _flattenTitle.text = "Flatten";
        }
        _minDistText.text = _minDist.ToString();
        _maxDistText.text = _maxDist.ToString();
        _flattenText.text = _flatten.ToString();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIGalaxySelect), "_OnRegEvent")]
    private static void PatchGalaxyUI_OnRegEvent(UIGalaxySelect __instance)
    {
        _minDistSlider.onValueChanged.RemoveAllListeners();
        _minDistSlider.onValueChanged.AddListener(val =>
        {
            var newVal = val / 10f;
            if (newVal.Equals(_minDist)) return;
            _minDist = newVal;
            _maxDistSlider.minValue = newVal * 10f;
            _minDistText.text = _minDist.ToString();
            __instance.SetStarmapGalaxy();
        });
        _maxDistSlider.onValueChanged.RemoveAllListeners();
        _maxDistSlider.onValueChanged.AddListener(val =>
        {
            var newVal = val / 10f;
            if (newVal.Equals(_maxDist)) return;
            _maxDist = newVal;
            _minDistSlider.maxValue = newVal * 10f;
            _maxDistText.text = _maxDist.ToString();
            __instance.SetStarmapGalaxy();
        });
        _flattenSlider.onValueChanged.RemoveAllListeners();
        _flattenSlider.onValueChanged.AddListener(val =>
        {
            var newVal = val / 50f;
            if (newVal.Equals(_maxDist)) return;
            _flatten = newVal;
            _flattenText.text = _flatten.ToString();
            __instance.SetStarmapGalaxy();
        });
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
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UniverseGenTweaks), "_minDist"));
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UniverseGenTweaks), "_maxDist"));
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UniverseGenTweaks), "_flatten"));
            }
            yield return instruction;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(UIGalaxySelect), "OnStarCountSliderValueChange")]
    static IEnumerable<CodeInstruction> PatchStarCountOnValueChange(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.OperandIs(80))
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4, _maxStarCount);
            }
            else
            {
                yield return instruction;
            }
        }
    }
}