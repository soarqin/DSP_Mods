using System;
using HarmonyLib;

namespace UXAssist.Common;

public static class GameLogic
{
    private static Harmony _harmony;
    public static Action OnDataLoaded;
    public static Action OnGameBegin;
    public static Action OnGameEnd;
    
    public static void Init()
    {
        _harmony ??= Harmony.CreateAndPatchAll(typeof(GameLogic));
    }

    public static void Uninit()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
    public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        OnDataLoaded?.Invoke();
    }

    [HarmonyPostfix, HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
    public static void GameMain_Begin_Postfix()
    {
        OnGameBegin?.Invoke();
    }

    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
    public static void GameMain_End_Postfix()
    {
        OnGameEnd?.Invoke();
    }
}
