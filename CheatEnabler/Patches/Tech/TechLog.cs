namespace CheatEnabler.Patches.Tech;

/// <summary>
/// Single funnel for the tech unlock / downgrade feature's log output.
///
/// Only real problems are reported: a Harmony target that no longer resolves, a tech function without
/// a known inverse, or a refused package resize. Those are conditions the player or the maintainer
/// needs to know about after a game update.
/// </summary>
internal static class TechLog
{
    public static void Warn(string message)
    {
        CheatEnabler.Logger.LogWarning("[TechTweaks] " + message);
    }
}
