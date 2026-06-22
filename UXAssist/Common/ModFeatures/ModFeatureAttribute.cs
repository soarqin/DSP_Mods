using System;

namespace UXAssist.Common.ModFeatures;

/// <summary>
/// Marks a static class as a mod feature so that <see cref="ModFeatureRegistry"/> can discover it.
/// Lifecycle methods (<c>Init</c>, <c>Start</c>, <c>Uninit</c>, <c>OnInputUpdate</c>, <c>OnUpdate</c>) are optional.
/// </summary>
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
