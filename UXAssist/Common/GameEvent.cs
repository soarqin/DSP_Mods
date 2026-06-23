using System;
using BepInEx.Logging;

namespace UXAssist.Common;

/// <summary>
/// Provides safe invocation helpers for game events.
/// </summary>
public static class GameEvent
{
    /// <summary>
    /// Invokes each handler in the action's invocation list individually,
    /// logging a warning if any handler throws an exception.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    /// <param name="logger">The logger to use for warnings.</param>
    /// <param name="name">The name of the event for diagnostic messages.</param>
    public static void InvokeSafe(this Action action, ManualLogSource logger, string name)
    {
        if (action == null) return;
        foreach (var d in action.GetInvocationList())
        {
            try { d.DynamicInvoke(); }
            catch (Exception ex) { logger?.LogWarning($"GameEvent '{name}' handler failed: {ex}"); }
        }
    }
}
