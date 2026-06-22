using System.IO;
using UXAssist.Common.ModFeatures;

namespace UniverseGenTweaks;

[ModFeature("UniverseGenSave", Order = 13)]
public static class GalaxyGenSave
{
    public static void Export(BinaryWriter w)
    {
        w.Write(GalaxyGenSettingsPatch.GameMinDist);
        w.Write(GalaxyGenSettingsPatch.GameMinStep);
        w.Write(GalaxyGenSettingsPatch.GameMaxStep);
        w.Write(GalaxyGenSettingsPatch.GameFlatten);
    }

    public static void Import(BinaryReader r)
    {
        GalaxyGenSettingsPatch.GameMinDist = r.ReadDouble();
        GalaxyGenSettingsPatch.GameMinStep = r.ReadDouble();
        GalaxyGenSettingsPatch.GameMaxStep = r.ReadDouble();
        GalaxyGenSettingsPatch.GameFlatten = r.ReadDouble();
    }
}
