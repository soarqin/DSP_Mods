namespace UXAssist.Common.GameConstants;

/// <summary>
/// Constants used by logistics capacity and station UI patches.
/// </summary>
public static class LogisticsConstants
{
    /* Default base storage capacity used by the real-time logistics panel. */
    public const int DefaultLocalStorageMax = 5000;
    public const int DefaultRemoteStorageMax = 10000;

    /* Maximum storage when overflow-in-logistics is enabled. */
    public const int OverflowStorageMax = 90000000;

    /* Station storage slot limits. */
    public const int DefaultStorageSlotCount = 5;

    /* Key-repeat timing for keyboard-based max adjustment (in game ticks). */
    public const long KeyRepeatInitialDelay = 30;
    public const long KeyRepeatInterval = 4;

    /* Keyboard adjustment deltas for storage max values. */
    public const int SmallAdjustment = 10;
    public const int MediumAdjustment = 100;
    public const int LargeAdjustment = 1000;
    public const int HugeAdjustment = 10000;
    public const int MassiveAdjustment = 100000;
    public const int GargantuanAdjustment = 1000000;

    /* Rounding divisors used when scaling storage on tech unlock. */
    public const int LocalStorageRounding = 50;
    public const int RemoteStorageRounding = 100;

    /* Charge power slider scaling factors. */
    public const long ChargePowerSliderScale = 50000L;
    public const long ChargePowerSliderMinScale = 500000L;

    /* Mining speed slider ranges. */
    public const int MinMiningSpeedBase = 10000;
    public const int MaxMiningSpeedBase = 30000;
    public const int MiningSpeedFineStep = 1000;
    public const int MiningSpeedCoarseStep = 10000;
    public const float MiningSpeedSliderMaxDefault = 20f;
    public const float MiningSpeedSliderMaxExtended = 27f;

    /* Dispenser / battle base charge power multiplier. */
    public const double DispenserChargePowerMultiplier = 5000.0;

    /* Real-time logistics panel UI dimensions. */
    public const float StorageSliderWidth = 70f;
    public const float StorageSliderHeight = 5f;
}
