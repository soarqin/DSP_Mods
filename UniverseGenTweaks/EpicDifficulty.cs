using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine.UI;

namespace UniverseGenTweaks;

public class EpicDifficulty
{
    public static void Init()
    {
        I18N.Add("究极少", "Micro", "究极少");
        I18N.Add("史诗难度", "Epic Difficulty !!", "史诗难度 !!");
        Harmony.CreateAndPatchAll(typeof(EpicDifficulty));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect._OnInit))]
    private static void PatchGalaxyUI_OnInit(UIGalaxySelect __instance)
    {
        __instance.resourceMultiplierSlider.maxValue = 10f;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect.OnResourceMultiplierValueChange))]
    private static bool UIGalaxySelect_OnResourceMultiplierValueChange_Prefix(UIGalaxySelect __instance, float val)
    {
        float value = __instance.resourceMultiplierSlider.value;
        if (value < 0.5f)
        {
            __instance.gameDesc.resourceMultiplier = 0.01f;
        }
        else if (value < 1.5f)
        {
            __instance.gameDesc.resourceMultiplier = 0.1f;
        }
        else if (value < 2.5f)
        {
            __instance.gameDesc.resourceMultiplier = 0.5f;
        }
        else if (value < 3.5f)
        {
            __instance.gameDesc.resourceMultiplier = 0.8f;
        }
        else if (value < 4.5f)
        {
            __instance.gameDesc.resourceMultiplier = 1f;
        }
        else if (value < 5.5f)
        {
            __instance.gameDesc.resourceMultiplier = 1.5f;
        }
        else if (value < 6.5f)
        {
            __instance.gameDesc.resourceMultiplier = 2f;
        }
        else if (value < 7.5f)
        {
            __instance.gameDesc.resourceMultiplier = 3f;
        }
        else if (value < 8.5f)
        {
            __instance.gameDesc.resourceMultiplier = 5f;
        }
        else if (value < 9.5f)
        {
            __instance.gameDesc.resourceMultiplier = 8f;
        }
        else
        {
            __instance.gameDesc.resourceMultiplier = 100f;
        }
        __instance.UpdateParametersUIDisplay();
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIGalaxySelect), nameof(UIGalaxySelect.UpdateParametersUIDisplay))]
    private static bool UIGalaxySelect_UpdateParametersUIDisplay_Prefix(UIGalaxySelect __instance)
    {
        float resourceMultiplier = __instance.gameDesc.resourceMultiplier;
        string text = "";
        if (resourceMultiplier < 0.04f)
        {
            __instance.resourceMultiplierSlider.value = 0f;
        }
        else if (resourceMultiplier < 0.11f)
        {
            __instance.resourceMultiplierSlider.value = 1f;
        }
        else if (resourceMultiplier < 0.51f)
        {
            __instance.resourceMultiplierSlider.value = 2f;
        }
        else if (resourceMultiplier < 0.81f)
        {
            __instance.resourceMultiplierSlider.value = 3f;
        }
        else if (resourceMultiplier < 1.01f)
        {
            __instance.resourceMultiplierSlider.value = 4f;
        }
        else if (resourceMultiplier < 1.51f)
        {
            __instance.resourceMultiplierSlider.value = 5f;
        }
        else if (resourceMultiplier < 2.01f)
        {
            __instance.resourceMultiplierSlider.value = 6f;
        }
        else if (resourceMultiplier < 3.01f)
        {
            __instance.resourceMultiplierSlider.value = 7f;
        }
        else if (resourceMultiplier < 5.01f)
        {
            __instance.resourceMultiplierSlider.value = 8f;
        }
        else if (resourceMultiplier < 8.01f)
        {
            __instance.resourceMultiplierSlider.value = 9f;
        }
        else
        {
            __instance.resourceMultiplierSlider.value = 10f;
        }
        if (resourceMultiplier < 100f && resourceMultiplier > 0.1f)
        {
            text = resourceMultiplier.ToString() + "x";
        }
        else if (resourceMultiplier >= 100f)
        {
            text = "无限".Translate();
        }
        else if (resourceMultiplier < 0.04f)
        {
            text = "究极少".Translate();
        }
        else if (resourceMultiplier < 0.11f)
        {
            text = "极少".Translate();
        }
        __instance.resourceMultiplierText.text = text;
        __instance.propertyMultiplierText.text = "元数据生成倍率".Translate() + " " + __instance.gameDesc.propertyMultiplier.ToString("P0");
        __instance.addrText.text = __instance.gameDesc.clusterString;
        var showDifficultTip = resourceMultiplier < 0.11f && !__instance.gameDesc.isSandboxMode;
        __instance.difficultTipGroup.SetActive(showDifficultTip);
        if (showDifficultTip)
        {
            if (resourceMultiplier < 0.04f)
            {
                __instance.difficultTipGroup.transform.Find("difficult-tip-text").GetComponent<Text>().text = "史诗难度".Translate();
            }
            else
            {
                __instance.difficultTipGroup.transform.Find("difficult-tip-text").GetComponent<Text>().text = "非常困难".Translate();
            }
        }

        return false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameDesc), "get_oilAmountMultiplier")]
    private static IEnumerable<CodeInstruction> GameDesc_get_oilAmountMultiplier_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        if (Math.Abs(UniverseGenTweaks.OilMultiplier - 1f) > 0.00001f)
        {
            var label1 = generator.DefineLabel();
            matcher.Start().InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GameDesc), nameof(GameDesc.resourceMultiplier))),
                new CodeInstruction(OpCodes.Ldc_R4, 0.05f),
                new CodeInstruction(OpCodes.Ble_S, label1)
            ).End().Advance(1).Insert(
                new CodeInstruction(OpCodes.Ldc_R4, 0.5f * UniverseGenTweaks.OilMultiplier).WithLabels(label1),
                new CodeInstruction(OpCodes.Ret)
            );
        }
        return matcher.InstructionEnumeration();
    }

}
