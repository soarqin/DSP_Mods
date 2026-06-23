using System.Reflection;
using HarmonyLib;

namespace UXAssist.Common.Utils;

public static class DysonSphereReflection
{
    private static readonly FieldInfo TotalNodeSpField = AccessTools.Field(typeof(DysonSphereLayer), "totalNodeSP");
    private static readonly FieldInfo TotalFrameSpField = AccessTools.Field(typeof(DysonSphereLayer), "totalFrameSP");
    private static readonly FieldInfo TotalCpField = AccessTools.Field(typeof(DysonSphereLayer), "totalCP");

    public static bool IsAvailable => TotalNodeSpField != null && TotalFrameSpField != null && TotalCpField != null;

    public static bool HasTotalNodeSP => TotalNodeSpField != null;

    public static bool HasTotalFrameSP => TotalFrameSpField != null;

    public static bool HasTotalCP => TotalCpField != null;

    public static long? GetTotalNodeSP(DysonSphereLayer layer)
        => layer != null && TotalNodeSpField != null ? (long?)TotalNodeSpField.GetValue(layer) : null;

    public static long? GetTotalFrameSP(DysonSphereLayer layer)
        => layer != null && TotalFrameSpField != null ? (long?)TotalFrameSpField.GetValue(layer) : null;

    public static long? GetTotalCP(DysonSphereLayer layer)
        => layer != null && TotalCpField != null ? (long?)TotalCpField.GetValue(layer) : null;

    public static void SetTotalNodeSP(DysonSphereLayer layer, long value)
        => TotalNodeSpField?.SetValue(layer, value);

    public static void SetTotalFrameSP(DysonSphereLayer layer, long value)
        => TotalFrameSpField?.SetValue(layer, value);

    public static void SetTotalCP(DysonSphereLayer layer, long value)
        => TotalCpField?.SetValue(layer, value);
}
