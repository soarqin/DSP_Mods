using System.Collections.Generic;

namespace UXAssist.Common.GameConstants;

/// <summary>
/// Hard-coded item IDs used across factory and logistics patches.
/// </summary>
public static class ItemIds
{
    /* Ores and basic resources */
    public const int Water = 1000;
    public const int Coal = 1006;
    public const int SulfuricAcid = 1116;
    public const int Hydrogen = 1120;
    public const int Deuterium = 1121;
    public const int Photon = 1208;

    /* Materials used by recipes */
    public const int OrganicCrystal = 1015;
    public const int FireIce = 1012;
    public const int CriticalPhoton = 1124;
    public const int CircuitBoard = 1112; // used in proliferator mk.III recipe
    public const int Steel = 1107;
    public const int TitaniumGlass = 1111;
    public const int GravitonLens = 1125;

    /* Proliferators */
    public const int ProliferatorMkI = 1141;
    public const int ProliferatorMkII = 1142;
    public const int ProliferatorMkIII = 1143;

    /* Logistics carriers */
    public const int SpaceWarper = 1126;

    /* Matrices */
    public const int ElectromagneticMatrix = 1202;
    public const int EnergyMatrix = 1203;
    public const int StructureMatrix = 1204;
    public const int InformationMatrix = 1205;
    public const int UniverseMatrix = 1209;
    public const int Antimatter = 1210;

    /* Components */
    public const int PlasmaExciter = 1301;
    public const int ParticleBroadband = 1305;
    public const int Processor = 1401;
    public const int QuantumChip = 1402;
    public const int MicrocrystallineComponent = 1403;
    public const int PlaneFilter = 1405;
    public const int ParticleContainer = 1406;

    /* Dyson sphere materials */
    public const int SolarSail = 1502;
    public const int FrameMaterial = 1503;
    public const int DysonSphereComponent = 1802;

    /* Power generators */
    public const int WindTurbine = 2203;

    /* Dark Fog items (combat expansion) */
    public const int DarkFogMemoryUnit = 5201;
    public const int DarkFogEnergyFragment = 5202;
    public const int DarkFogSiliconNeuron = 5203;
    public const int DarkFogNegentropySingularity = 5204;
    public const int DarkFogMatterReassembler = 5205;
    public const int DarkFogVirtualParticle = 5206;

    /* Fuel and advanced items */
    public const int HydrogenFuelRod = 6001;
    public const int DeuteronFuelRod = 6002;
    public const int AntimatterFuelRod = 6003;
    public const int StrangeMatter = 6004;
    public const int Foundation = 6005;
    public const int Metaverse = 6006; // also called property/meta-data item

    /* Item groups used by belt signal recipe calculations. */
    public static readonly int[] ExtraOreItemIds =
    [
        Water, SulfuricAcid, Hydrogen, Deuterium, Photon,
        DarkFogMemoryUnit, DarkFogEnergyFragment, DarkFogSiliconNeuron,
        DarkFogNegentropySingularity, DarkFogMatterReassembler, DarkFogVirtualParticle
    ];

    public static readonly HashSet<int> ExtraProliferationItemIds =
    [
        Steel, TitaniumGlass, GravitonLens, ProliferatorMkII, ProliferatorMkIII,
        ElectromagneticMatrix, EnergyMatrix, StructureMatrix, InformationMatrix,
        UniverseMatrix, Antimatter, PlasmaExciter, ParticleBroadband,
        Processor, QuantumChip, MicrocrystallineComponent, PlaneFilter, ParticleContainer,
        SolarSail, FrameMaterial, DysonSphereComponent,
        HydrogenFuelRod, AntimatterFuelRod, StrangeMatter, Foundation, Metaverse
    ];

    public static readonly HashSet<int> NoProliferationItemIds =
    [
        SpaceWarper, DeuteronFuelRod
    ];

    /// <summary>
    /// All source items used to create 25 proliferators mk.III (not self-sprayed).
    /// </summary>
    public static readonly List<(int ItemId, float Count)> ProliferatorSources =
    [
        (OrganicCrystal, 60f),
        (CriticalPhoton, 20f),
        (Coal, 64f),
        (FireIce, 16f),
        (CircuitBoard, 32f),
        (ProliferatorMkI, 64f),
        (ProliferatorMkII, 40f),
        (ProliferatorMkIII, 25f)
    ];

    /* Aliases used by UXAssist belt signal buy-out logic. */
    public static readonly int[] DarkFogItemIds =
    [
        DarkFogMemoryUnit, DarkFogVirtualParticle, DarkFogEnergyFragment,
        DarkFogNegentropySingularity, DarkFogSiliconNeuron, DarkFogMatterReassembler
    ];
}
