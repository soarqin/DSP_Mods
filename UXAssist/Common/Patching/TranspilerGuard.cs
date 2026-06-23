using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace UXAssist.Common.Patching;

/// <summary>
/// Helper for Harmony transpilers. Provides a standardized way to bail out and return the original
/// instructions when a <see cref="CodeMatcher"/> fails to match, which makes version-fragile patches
/// easier to diagnose at runtime.
/// </summary>
public static class TranspilerGuard
{
    public static IEnumerable<CodeInstruction> Finish(
        this CodeMatcher matcher,
        IEnumerable<CodeInstruction> originalInstructions,
        ManualLogSource logger,
        string transpilerName)
    {
        if (matcher.IsInvalid)
        {
            logger?.LogWarning($"Transpiler '{transpilerName}' failed to match; returning original instructions.");
            return originalInstructions;
        }
        return matcher.InstructionEnumeration();
    }
}
