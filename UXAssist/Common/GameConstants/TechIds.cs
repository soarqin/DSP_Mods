using System.Collections.Generic;

namespace UXAssist.Common.GameConstants;

/// <summary>
/// Hard-coded technology IDs used across tech patches.
/// </summary>
public static class TechIds
{
    /// <summary>
    /// Base game tech for "Sorter Cargo Stacking" upgrades.
    /// </summary>
    public const int SorterCargoStacking = 3608;

    /* Custom sorter cargo stacking techs used by UXAssist (repositioned clones). */
    public const int SorterCargoStackingCustomStart = 3301;
    public const int SorterCargoStackingCustomEnd = 3306;

    /* Combat-related tech ranges disabled in peace mode. */
    public static readonly HashSet<int> CombatTechs = CreateCombatTechs();

    private static HashSet<int> CreateCombatTechs()
    {
        (int Start, int End)[] ranges =
        [
            // Explosive Unit, Crystal Explosive Unit
            (1803, 1804),
            // Implosion Cannon
            (1807, 1807),
            // Signal Tower, Planetary Defense System, Jammer Tower, Plasma Turret,
            // ammo boxes, shell/missile sets, capsules, drones, corvettes, destroyers, suppressing/EM capsules
            (1809, 1825),
            // Auto Reconstruction Marking
            (2951, 2956),
            // Energy Shield
            (2801, 2807),
            // Kinetic Weapon Damage
            (5001, 5006),
            // Energy Weapon Damage
            (5101, 5106),
            // Explosive Weapon Damage
            (5201, 5206),
            // Combat Drone Damage
            (5301, 5305),
            // Combat Drone Attack Speed
            (5401, 5405),
            // Combat Drone Engine
            (5601, 5605),
            // Combat Drone Durability
            (5701, 5705),
            // Ground Squadron Expansion
            (5801, 5807),
            // Space Fleet Expansion
            (5901, 5907),
            // Enhanced Structure
            (6001, 6006),
            // Planetary Shield
            (6101, 6106)
        ];

        var set = new HashSet<int>();
        foreach (var (start, end) in ranges)
        {
            for (var id = start; id <= end; id++)
            {
                set.Add(id);
            }
        }

        return set;
    }
}
