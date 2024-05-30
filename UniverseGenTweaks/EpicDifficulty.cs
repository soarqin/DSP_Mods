using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;
using UXAssist.Common;

namespace UniverseGenTweaks;

public static class EpicDifficulty
{
    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<float> ResourceMultiplier;
    public static ConfigEntry<float> OilMultiplier;
    private static Harmony _harmony;

    private static readonly float[] ResourceMultipliers = new[] { 0.0001f, 0.0005f, 0.001f, 0.005f, 0.01f, 0.02f, 0.03f, 0.04f, 0.05f };
    private static readonly float[] OilMultipliers = new[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f };

    public static void Init()
    {
        I18N.Add("究极少", "Micro", "究极少");
        I18N.Add("史诗难度", "Epic Difficulty !!", "史诗难度 !!");
        I18N.Apply();
        Enabled.SettingChanged += (_, _) => Enable(Enabled.Value);
        Enable(Enabled.Value);
    }

    public static void Uninit()
    {
        Enable(false);
    }

    private static void Enable(bool on)
    {
        if (on)
        {
            _harmony ??= Harmony.CreateAndPatchAll(typeof(EpicDifficulty));
            return;
        }
        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    public static float IndexToResourceMultiplier(int index)
    {
        return ResourceMultipliers[index < 0 ? 0 : index >= ResourceMultipliers.Length ? ResourceMultipliers.Length - 1 : index];
    }

    public static int ResourceMultiplierToIndex(float mult)
    {
        for (var i = ResourceMultipliers.Length - 1; i > 0; i--)
        {
            if (ResourceMultipliers[i] <= mult) return i;
        }
        return 0;
    }

    public static int ResourceMultipliersCount()
    {
        return ResourceMultipliers.Length;
    }

    public static float IndexToOilMultiplier(int index)
    {
        return OilMultipliers[index < 0 ? 0 : index >= OilMultipliers.Length ? OilMultipliers.Length - 1 : index];
    }

    public static int OilMultiplierToIndex(float mult)
    {
        for (var i = OilMultipliers.Length - 1; i > 0; i--)
        {
            if (OilMultipliers[i] <= mult) return i;
        }
        return 0;
    }
    
    public static int OilMultipliersCount()
    {
        return OilMultipliers.Length;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect._OnInit))]
    private static void PatchGalaxyUI_OnInit(UIGalaxySelect __instance)
    {
        __instance.resourceMultiplierSlider.maxValue = 11f;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect.OnResourceMultiplierValueChange))]
    private static bool UIGalaxySelect_OnResourceMultiplierValueChange_Prefix(UIGalaxySelect __instance, float val)
    {
        var value = __instance.resourceMultiplierSlider.value;
        __instance.gameDesc.resourceMultiplier = value switch
        {
            < 0.5f => ResourceMultiplier.Value,
            < 1.5f => 0.1f,
            < 2.5f => 0.3f,
            < 3.5f => 0.5f,
            < 4.5f => 0.8f,
            < 5.5f => 1f,
            < 6.5f => 1.5f,
            < 7.5f => 2f,
            < 8.5f => 3f,
            < 9.5f => 5f,
            < 10.5f => 8f,
            _ => 100f
        };
        __instance.UpdateParametersUIDisplay();
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect.UpdateParametersUIDisplay))]
    private static bool UIGalaxySelect_UpdateParametersUIDisplay_Prefix(UIGalaxySelect __instance)
    {
        var resourceMultiplier = __instance.gameDesc.resourceMultiplier;
        var text = "";
        __instance.resourceMultiplierSlider.value = resourceMultiplier switch
        {
            < 0.09f => 0f,
            < 0.11f => 1f,
            < 0.31f => 2f,
            < 0.51f => 3f,
            < 0.81f => 4f,
            < 1.01f => 5f,
            < 1.51f => 6f,
            < 2.01f => 7f,
            < 3.01f => 8f,
            < 5.01f => 9f,
            < 8.01f => 10f,
            _ => 11f
        };
        text = resourceMultiplier switch
        {
            < 100f and > 0.1f => resourceMultiplier + "x",
            >= 100f => "无限".Translate(),
            < 0.09f => "究极少".Translate(),
            < 0.11f => "极少".Translate(),
            _ => text
        };
        __instance.resourceMultiplierText.text = text;
        __instance.propertyMultiplierText.text = "元数据生成倍率".Translate() + " " + __instance.gameDesc.propertyMultiplier.ToString("P0");
        __instance.addrText.text = __instance.gameDesc.clusterString;
        var showDifficultTip = resourceMultiplier < 0.11f && !__instance.gameDesc.isSandboxMode;
        __instance.difficultTipGroup.SetActive(showDifficultTip);
        if (!showDifficultTip) return false;
        __instance.difficultTipGroup.transform.Find("difficult-tip-text").GetComponent<Text>().text = (resourceMultiplier < 0.09f ? "史诗难度" : "非常困难").Translate();

        return false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameDesc), "get_oilAmountMultiplier")]
    private static IEnumerable<CodeInstruction> GameDesc_get_oilAmountMultiplier_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        if (Math.Abs(OilMultiplier.Value - 1f) > 0.00001f)
        {
            var label1 = generator.DefineLabel();
            matcher.Start().InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GameDesc), nameof(GameDesc.resourceMultiplier))),
                new CodeInstruction(OpCodes.Ldc_R4, 0.05f),
                new CodeInstruction(OpCodes.Ble_S, label1)
            ).End().Advance(1).Insert(
                new CodeInstruction(OpCodes.Ldc_R4, 0.5f * OilMultiplier.Value).WithLabels(label1),
                new CodeInstruction(OpCodes.Ret)
            );
        }
        return matcher.InstructionEnumeration();
    }

}
