namespace UXAssist.Common.GameConstants;

/// <summary>
/// Galaxy generation defaults, slider limits and UI constants.
/// </summary>
public static class UniverseGenConstants
{
    /* Default algorithm parameters for galaxy generation. */
    public const double DefaultMinDist = 2.0;
    public const double DefaultMinStep = 2.0;
    public const double DefaultMaxStep = 3.2;
    public const double DefaultFlatten = 0.18;

    /* Vanilla game limits replaced by UniverseGenTweaks. */
    public const int VanillaMaxStarCount = 80;
    public const int VanillaGalaxyCapacity = 25700;
    public const int VanillaSectorCapacity = 25600;

    /* Expanded capacities used by UniverseGenTweaks. */
    public const int ExpandedGalaxyCapacity = 102500;
    public const int ExpandedSectorCapacity = 102500;

    /* Maximum star count settings. */
    public const int MinStarCount = 32;
    public const int MaxStarCount = 1024;
    public const int DefaultMaxStarCount = 128;
    public const float StarCountSliderMin = 64f;
    public const float StarCountSliderMax = 1024f;
    public const int StarCountAlignmentMask = 7; // round up to multiple of 8

    /* UIGalaxySelect custom slider limits (slider value = parameter * StepSliderScale). */
    public const float StepSliderScale = 10f;

    public const float MinDistSliderMin = 10f;
    public const float MinDistSliderMax = 50f;

    public const float MaxStepSliderMax = 100f;

    public const float FlattenSliderMin = 1f;
    public const float FlattenSliderMax = 50f;
    public const double FlattenSliderScale = 50.0;

    /* Vertical spacing between added slider rows in the galaxy select UI. */
    public const float SliderRowSpacing = -36f;

    /* Epic difficulty resource multiplier slider. */
    public const float ResourceMultiplierSliderMax = 11f;

    /* Oil multiplier thresholds used by Epic difficulty. */
    public const float OilMultiplierThreshold = 0.05f;
    public const float OilMultiplierBase = 0.5f;
}
