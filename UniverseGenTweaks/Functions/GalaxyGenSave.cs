using System.IO;

namespace UniverseGenTweaks;

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
