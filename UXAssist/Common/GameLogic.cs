using System;
using HarmonyLib;

namespace UXAssist.Common;

/// <summary>
/// Provides game lifecycle events for UXAssist and dependent mods.
/// </summary>
public class GameLogic : PatchImpl<GameLogic>
{
    /// <summary>
    /// Raised after game data has finished loading.
    /// </summary>
    public static Action OnDataLoaded;

    /// <summary>
    /// Raised when a game session begins.
    /// </summary>
    public static Action OnGameBegin;

    /// <summary>
    /// Raised when a game session ends.
    /// </summary>
    public static Action OnGameEnd;

    /// <summary>
    /// Harmony postfix for <see cref="VFPreload.InvokeOnLoadWorkEnded"/>.
    /// Raises <see cref="OnDataLoaded"/>.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
    public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
    {
        OnDataLoaded.InvokeSafe(UXAssist.Logger, nameof(OnDataLoaded));
    }

    /// <summary>
    /// Harmony postfix for <see cref="GameMain.Begin"/>.
    /// Raises <see cref="OnGameBegin"/>.
    /// </summary>
    [HarmonyPostfix, HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
    public static void GameMain_Begin_Postfix()
    {
        OnGameBegin.InvokeSafe(UXAssist.Logger, nameof(OnGameBegin));
    }

    /// <summary>
    /// Harmony postfix for <see cref="GameMain.End"/>.
    /// Raises <see cref="OnGameEnd"/>.
    /// </summary>
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
    public static void GameMain_End_Postfix()
    {
        OnGameEnd.InvokeSafe(UXAssist.Logger, nameof(OnGameEnd));
    }
}
