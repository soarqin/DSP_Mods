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
    public const int CriticalPhoton = 1208;

    /* Materials and components */
    public const int TitaniumAlloy = 1107;
    public const int Prism = 1111;
    public const int FrameMaterial = 1125;
    public const int CasimirCrystal = 1126;
    public const int MagneticCoil = 1202;
    public const int ElectricMotor = 1203;
    public const int ElectromagneticTurbine = 1204;
    public const int SuperMagneticRing = 1205;
    public const int GravitonLens = 1209;
    public const int CircuitBoard = 1301;
    public const int QuantumChip = 1305;
    public const int PlasmaExciter = 1401;
    public const int ParticleBroadband = 1402;
    public const int AnnihilationConstraintSphere = 1403;
    public const int Thruster = 1405;
    public const int ReinforcedThruster = 1406;

    /* Proliferators */
    public const int ProliferatorMkI = 1141;
    public const int ProliferatorMkII = 1142;
    public const int ProliferatorMkIII = 1143;

    /* Logistics */
    public const int SpaceWarper = 1210;
    public const int LogisticsDrone = 5001;
    public const int LogisticsVessel = 5002;
    public const int LogisticsBot = 5003;

    /* Dyson sphere materials */
    public const int DysonSphereComponent = 1502;
    public const int SmallCarrierRocket = 1503;

    /* Fuel rods */
    public const int DeuteronFuelRod = 1802;
    public const int StrangeAnnihilationFuelRod = 1804;

    /* Power nodes */
    public const int TeslaTower = 2201;

    /* Power generators */
    public const int WindTurbine = 2203;

    /* Mining */
    public const int MiningMachine = 2301;

    /* Dark Fog items (combat expansion) */
    public const int DarkFogMatrix = 5201;
    public const int DarkFogSiliconNeuron = 5202;
    public const int DarkFogMatterRecombinator = 5203;
    public const int DarkFogNegentropySingularity = 5204;
    public const int DarkFogCoreElement = 5205;
    public const int DarkFogEnergyShard = 5206;

    /* Matrices. Metadata types use the same IDs; the Universe Matrix's metadata is Metaverse. */
    public const int ElectromagneticMatrix = 6001;
    public const int EnergyMatrix = 6002;
    public const int StructureMatrix = 6003;
    public const int InformationMatrix = 6004;
    public const int GravityMatrix = 6005;
    public const int UniverseMatrix = 6006;

    /* Item groups used by belt signal recipe calculations. */
    public static readonly int[] ExtraOreItemIds =
    [
        Water, SulfuricAcid, Hydrogen, Deuterium, CriticalPhoton,
        DarkFogMatrix, DarkFogSiliconNeuron, DarkFogMatterRecombinator,
        DarkFogNegentropySingularity, DarkFogCoreElement, DarkFogEnergyShard
    ];

    // Listed in the same order as the belt signal option tips.
    public static readonly HashSet<int> ExtraProliferationItemIds =
    [
        TitaniumAlloy, Prism, FrameMaterial, ProliferatorMkII, ProliferatorMkIII,
        MagneticCoil, ElectricMotor, ElectromagneticTurbine, SuperMagneticRing,
        CircuitBoard, Thruster, ReinforcedThruster, PlasmaExciter, ParticleBroadband,
        GravitonLens, QuantumChip, AnnihilationConstraintSphere, DeuteronFuelRod, SpaceWarper,
        DysonSphereComponent, SmallCarrierRocket, ElectromagneticMatrix, StructureMatrix,
        InformationMatrix, GravityMatrix, UniverseMatrix
    ];

    public static readonly HashSet<int> NoProliferationItemIds =
    [
        CasimirCrystal, EnergyMatrix
    ];

    /* Dark Fog materials used by production calculations. */
    public static readonly int[] DarkFogItemIds =
    [
        DarkFogMatrix, DarkFogSiliconNeuron, DarkFogMatterRecombinator,
        DarkFogNegentropySingularity, DarkFogCoreElement, DarkFogEnergyShard
    ];
}
