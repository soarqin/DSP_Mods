namespace UXAssist.Common.GameConstants;

/// <summary>
/// Geometric and pool constants used by Dyson Sphere helper functions.
/// </summary>
public static class DysonSphereConstants
{
    /* Angle conversion. */
    public const float RadiansToDegrees = 57.2957802f;

    /* Default capacity for newly reset Dyson sphere layer pools. */
    public const int DefaultLayerPoolCapacity = 64;

    /* Maximum allowed vertices for a custom Dyson shell. */
    public const int MaxShellVertices = 32767;

    /* Minimum vertex count used as a quality guard in quick shell creation. */
    public const int MinShellVertexThreshold = 32000;

    /* Maximum orbit radius supported by precalculated shell triangles (m). */
    public const int MaxOrbitRadius = 250000;

    /* Minimum radius considered when searching for a free layer radius. */
    public const int MinOrbitRadiusSearch = 4000;

    /* Layer radius decrement step when looking for a free slot. */
    public const int OrbitRadiusSearchStep = 10;

    /* Base grid size used for shell vertex tessellation. */
    public const float GridSizeBase = 80f;

    /* Base radius used to derive shell grid scale (gridScale ~ (radius / base)^0.75). */
    public const double GridScaleBaseRadius = 4000.0;

    /* Cell point cost per vertex (cpPerVertex = gridScale^2 * 2). */
    public const int CpPerVertexFactor = 2;

    /* Number of candidate node positions around the orbit for max-output shell search. */
    public const int MaxShellSearchNodeCount = 60;

    /* Maximum triangle combinations evaluated in brute-force shell search. */
    public const int MaxShellSearchCombinations = 60 * 59 * 58;

    /* Seed multiplier for shell randomization. */
    public const int ShellRandSeedMultiplier = 10000;

    /* int.MaxValue, used as a total-cp ceiling before forcing new nodes. */
    public const int TotalCpMaxCeiling = int.MaxValue;
}
