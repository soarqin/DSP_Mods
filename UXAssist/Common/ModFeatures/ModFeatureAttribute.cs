using System;

namespace UXAssist.Common.ModFeatures;

/// <summary>
/// Marks a class as a mod feature so that <see cref="ModFeatureRegistry"/> can discover it.
/// Lifecycle methods (<c>Init</c>, <c>Start</c>, <c>Uninit</c>, <c>OnInputUpdate</c>, <c>OnUpdate</c>)
/// are optional and are skipped if missing.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Timing contract</strong> (same as <see cref="IModFeature"/>): <c>Init</c> runs eagerly at
/// discovery time, synchronously inside <see cref="ModFeatureRegistry.Discover"/>, during the
/// registering mod's BepInEx <c>Awake</c> phase — before the game scene loads, before any plugin's
/// <c>Start</c>. This is the phase where early setup that must precede game initialization (e.g.
/// keybind registration) must run. <c>Start</c> runs once during the host mod's (UXAssist) <c>Start</c>,
/// after all mods' <c>Awake</c> have completed. The per-frame methods are called by UXAssist with at
/// most one invocation per frame.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ModFeatureAttribute : Attribute
{
    /// <summary>
    /// Optional display name of the feature.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Execution order among discovered static features. Lower values run first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModFeatureAttribute"/> class.
    /// </summary>
    /// <param name="name">Optional display name of the feature.</param>
    public ModFeatureAttribute(string name = null)
    {
        Name = name;
    }
}
