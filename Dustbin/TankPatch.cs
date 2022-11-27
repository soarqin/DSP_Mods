using HarmonyLib;
using UnityEngine;

namespace Dustbin;

[HarmonyPatch]
public static class TankPatch
{
    private static MyCheckBox _tankDustbinCheckBox;
    private static int lastTankId;
    private static IsDusbinIndexer tankIsDustbin = new();

    public static void Reset()
    {
        tankIsDustbin.Reset();
        lastTankId = 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UITankWindow), "_OnCreate")]
    private static void UITankWindow__OnCreate_Postfix(UITankWindow __instance)
    {
        _tankDustbinCheckBox = MyCheckBox.CreateCheckBox(false, __instance.transform, 120f, 20f, Localization.language == Language.zhCN ? "垃圾桶" : "Dustbin");
        var window = __instance;
        _tankDustbinCheckBox.OnChecked += () =>
        {
            var tankId = window.tankId;
            if (tankId <= 0 || window.storage.tankPool[tankId].id != tankId) return;
            tankIsDustbin[tankId] = _tankDustbinCheckBox.Checked;
        };
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UITankWindow), "_OnUpdate")]
    private static void UITankWindow__OnUpdate_Postfix(UITankWindow __instance)
    {
        var tankId = __instance.tankId;
        if (lastTankId == tankId) return;
        lastTankId = tankId;
        if (tankId > 0 && __instance.storage.tankPool[tankId].id == tankId)
        {
            _tankDustbinCheckBox.Checked = tankIsDustbin[tankId];
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TankComponent), "GameTick")]
    private static void TankComponent_GameTick_Prefix(ref TankComponent __instance, out long __state)
    {
        if (tankIsDustbin[__instance.id])
        {
            __state = ((long)__instance.fluidInc << 36) | ((long)__instance.fluidCount << 16) | (uint)__instance.fluidId;
            __instance.fluidId = __instance.fluidCount = __instance.fluidInc = 0;
        }
        else
        {
            __state = -1;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TankComponent), "GameTick")]
    private static void TankComponent_GameTick_Postfix(ref TankComponent __instance, long __state)
    {
        if (__state < 0) return;
        __instance.fluidId = (int)(__state & 0xFFFFL);
        __instance.fluidCount = (int)((__state >> 16) & 0xFFFFFL);
        __instance.fluidInc = (int)(__state >> 36);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TankComponent), "Export")]
    private static void TankComponent_Export_Prefix(ref TankComponent __instance, out int __state)
    {
        if (tankIsDustbin[__instance.id])
        {
            __state = __instance.fluidInc;
            __instance.fluidInc = -__instance.fluidInc - 1;
        }
        else
        {
            __state = -1;
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TankComponent), "Export")]
    private static void TankComponent_Export_Postfix(ref TankComponent __instance, int __state)
    {
        if (__state < 0) return;
        __instance.fluidInc = __state;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TankComponent), "Import")]
    private static void TankComponent_Import_Postfix(TankComponent __instance)
    {
        if (__instance.fluidInc >= 0)
            tankIsDustbin[__instance.id] = false;
        else
        {
            tankIsDustbin[__instance.id] = true;
            __instance.fluidInc = -__instance.fluidInc - 1;
        }
    }
}
