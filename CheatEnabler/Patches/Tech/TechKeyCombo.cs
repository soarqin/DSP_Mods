namespace CheatEnabler.Patches.Tech;

/// <summary>
/// The key-modifier semantics shared by the upgrade and the downgrade side.
///
/// The mapping mirrors the built-in <see cref="GamePatch.UnlockTech"/> handler so both features behave
/// identically, and the downgrade side is its exact mirror:
///
/// <list type="table">
/// <item><term>Shift</term><description>+1 / -1</description></item>
/// <item><term>Ctrl</term><description>+10 / -10</description></item>
/// <item><term>Ctrl + Shift</term><description>+100 / -100</description></item>
/// <item><term>Alt</term><description>to MAX / back to locked</description></item>
/// <item><term>Shift + Alt, Ctrl + Alt</term><description>ignored</description></item>
/// </list>
/// </summary>
internal static class TechKeyCombo
{
    /// <summary>Sentinel for the "Alt" combination: apply or revert everything this click can.</summary>
    public const int LevelToMax = int.MaxValue;

    /// <summary>
    /// Decodes the currently held modifiers.
    /// Returns false when the combination carries no meaning and the click must be left alone.
    /// </summary>
    public static bool TryDecode(out int steps)
    {
        steps = 0;

        if (VFInput.shift)
        {
            if (VFInput.alt)
            {
                return false;
            }

            steps = VFInput.control ? 100 : 1;
            return true;
        }

        if (VFInput.control)
        {
            if (VFInput.alt)
            {
                return false;
            }

            steps = 10;
            return true;
        }

        if (VFInput.alt)
        {
            steps = LevelToMax;
            return true;
        }

        return false;
    }
}
