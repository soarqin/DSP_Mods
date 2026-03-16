using System;
using HarmonyLib;
using UnityEngine;

namespace UXAssist.Common;

public class GameLogic : PatchImpl<GameLogic>
{
    public static Action OnDataLoaded;
    public static Action OnGameBegin;
    public static Action OnGameEnd;

    private static void InvokeSafe(Action action)
    {
        if (action == null) return;
        foreach (var handler in action.GetInvocationList())
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
    public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        InvokeSafe(OnDataLoaded);
    }

    [HarmonyPostfix, HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
    public static void GameMain_Begin_Postfix()
    {
        InvokeSafe(OnGameBegin);
    }

    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
    public static void GameMain_End_Postfix()
    {
        InvokeSafe(OnGameEnd);
    }
}
