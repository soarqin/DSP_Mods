using System;
using HarmonyLib;

namespace UXAssist.Common;

public class GameLogic: PatchImpl<GameLogic>
{
    public static Action OnDataLoaded;
    public static Action OnGameBegin;
    public static Action OnGameEnd;
    
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
