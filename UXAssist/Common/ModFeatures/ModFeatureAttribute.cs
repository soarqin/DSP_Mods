using System;

namespace UXAssist.Common.ModFeatures;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ModFeatureAttribute : Attribute
{
    public string Name { get; }
    public int Order { get; set; }

    public ModFeatureAttribute(string name = null)
    {
        Name = name;
    }
}
