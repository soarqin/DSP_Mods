# Static Mutable State Inventory

This document inventories non-readonly static fields in UXAssist, CheatEnabler, and UniverseGenTweaks.

## Classification Legend

- **LifecycleSafe**: Set during mod startup / `Awake` and treated as read-only thereafter (config entries, keybinds, one-time prototypes, reflection caches).
- **NeedsReset**: Per-game or per-session mutable state that must be cleared or re-initialized when `GameLogic.OnGameEnd` fires.
- **CandidateForInstancing**: Mutable state that is logically owned by a single game instance and should be moved to a per-game context class.

Total classified fields: **298**

## CheatEnabler

### Functions/DysonSphereFunctions.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Functions/DysonSphereFunctions.cs` | 11 | `public static ConfigEntry<bool> IllegalDysonShellFunctionsEnabled;` | LifecycleSafe |
| `CheatEnabler/Functions/DysonSphereFunctions.cs` | 12 | `public static ConfigEntry<int> ShellsCountForFunctions;` | LifecycleSafe |

### Patches/CombatPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/CombatPatch.cs` | 14 | `public static ConfigEntry<bool> MechaInvincibleEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/CombatPatch.cs` | 15 | `public static ConfigEntry<bool> BuildingsInvincibleEnabled;` | LifecycleSafe |

### Patches/DysonSpherePatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 15 | `public static ConfigEntry<bool> SkipBulletEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 16 | `public static ConfigEntry<bool> FireAllBulletsEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 17 | `public static ConfigEntry<bool> SkipAbsorbEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 18 | `public static ConfigEntry<bool> QuickAbsorbEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 19 | `public static ConfigEntry<bool> EjectAnywayEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 20 | `public static ConfigEntry<bool> OverclockEjectorEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 21 | `public static ConfigEntry<bool> OverclockSiloEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 22 | `public static ConfigEntry<bool> UnlockMaxOrbitRadiusEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 23 | `public static ConfigEntry<float> UnlockMaxOrbitRadiusValue;` | LifecycleSafe |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 24 | `private static bool _instantAbsorb;` | NeedsReset |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 107 | `private static long _sailLifeTime;` | NeedsReset |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 108 | `private static DysonSailCache[][] _sailsCache;` | CandidateForInstancing |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 109 | `private static int[] _sailsCacheLen, _sailsCacheCapacity;` | CandidateForInstancing |
| `CheatEnabler/Patches/DysonSpherePatch.cs` | 110 | `private static bool _fireAllBullets;` | NeedsReset |

### Patches/Factory/ArchitectModePatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/Factory/ArchitectModePatch.cs` | 8 | `private static bool[] _canBuildItems;` | CandidateForInstancing |

### Patches/Factory/BeltSignalPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/Factory/BeltSignalPatch.cs` | 14 | `private static Dictionary<int, BeltSignal>[] _signalBelts;` | CandidateForInstancing |
| `CheatEnabler/Patches/Factory/BeltSignalPatch.cs` | 15 | `private static Dictionary<long, int> _portalFrom;` | CandidateForInstancing |
| `CheatEnabler/Patches/Factory/BeltSignalPatch.cs` | 16 | `private static Dictionary<int, HashSet<long>> _portalTo;` | CandidateForInstancing |
| `CheatEnabler/Patches/Factory/BeltSignalPatch.cs` | 17 | `private static int _signalBeltsCapacity;` | NeedsReset |
| `CheatEnabler/Patches/Factory/BeltSignalPatch.cs` | 18 | `private static bool _initialized;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/BeltSignalPatch.cs` | 600 | `private static bool _itemSourcesInitialized;` | LifecycleSafe |

### Patches/Factory/FactoryPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 18 | `public static ConfigEntry<bool> ImmediateEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 19 | `public static ConfigEntry<bool> ArchitectModeEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 20 | `public static ConfigEntry<bool> NoConditionEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 21 | `public static ConfigEntry<bool> NoCollisionEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 22 | `public static ConfigEntry<bool> BeltSignalGeneratorEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 23 | `public static ConfigEntry<bool> BeltSignalNumberAltFormat;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 24 | `public static ConfigEntry<bool> BeltSignalCountGenEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 25 | `public static ConfigEntry<bool> BeltSignalCountRemEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 26 | `public static ConfigEntry<bool> BeltSignalCountRecipeEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 27 | `public static ConfigEntry<bool> BeltSignalUseProliferatorEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 28 | `public static ConfigEntry<bool> RemovePowerSpaceLimitEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 29 | `public static ConfigEntry<bool> BoostWindPowerEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 30 | `public static ConfigEntry<bool> BoostSolarPowerEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 31 | `public static ConfigEntry<bool> BoostFuelPowerEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 32 | `public static ConfigEntry<bool> BoostGeothermalPowerEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 33 | `public static ConfigEntry<bool> WindTurbinesPowerGlobalCoverageEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 34 | `public static ConfigEntry<bool> ControlPanelRemoteLogisticsEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 36 | `private static PressKeyBind _noConditionKey;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 37 | `private static PressKeyBind _noCollisionKey;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/FactoryPatch.cs` | 38 | `internal static HashSet<int> BeltIds;` | LifecycleSafe |

### Patches/Factory/ImmediateBuildPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs` | 10 | `private static bool _isBatchBuilding;` | NeedsReset |
| `CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs` | 11 | `private static bool _disableRefreshBatchesBuffers;` | NeedsReset |
| `CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs` | 12 | `private static bool _anyBelt;` | NeedsReset |

### Patches/Factory/PowerBoostPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/Factory/PowerBoostPatch.cs` | 157 | `private static bool _patched;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/PowerBoostPatch.cs` | 158 | `private static PrefabDesc _prefabdesc;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/PowerBoostPatch.cs` | 159 | `private static float _oldCoverRadius;` | LifecycleSafe |
| `CheatEnabler/Patches/Factory/PowerBoostPatch.cs` | 160 | `private static float _oldConnectDistance;` | LifecycleSafe |

### Patches/GamePatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/GamePatch.cs` | 15 | `public static ConfigEntry<bool> DevShortcutsEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/GamePatch.cs` | 16 | `public static ConfigEntry<bool> AbnormalDisablerEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/GamePatch.cs` | 17 | `public static ConfigEntry<bool> UnlockTechEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/GamePatch.cs` | 42 | `private static Dictionary<int, AbnormalityDeterminator> _savedDeterminators;` | NeedsReset |
| `CheatEnabler/Patches/GamePatch.cs` | 97 | `private static PlayerAction_Test _test;` | NeedsReset |

### Patches/PlanetPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/PlanetPatch.cs` | 14 | `public static ConfigEntry<bool> WaterPumpAnywhereEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/PlanetPatch.cs` | 15 | `public static ConfigEntry<bool> TerraformAnywayEnabled;` | LifecycleSafe |

### Patches/PlayerPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/PlayerPatch.cs` | 13 | `public static ConfigEntry<bool> InstantHandCraftEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/PlayerPatch.cs` | 14 | `public static ConfigEntry<bool> InstantTeleportEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/PlayerPatch.cs` | 15 | `public static ConfigEntry<bool> WarpWithoutSpaceWarpersEnabled;` | LifecycleSafe |

### Patches/ResourcePatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `CheatEnabler/Patches/ResourcePatch.cs` | 13 | `public static ConfigEntry<bool> InfiniteResourceEnabled;` | LifecycleSafe |
| `CheatEnabler/Patches/ResourcePatch.cs` | 14 | `public static ConfigEntry<bool> FastMiningEnabled;` | LifecycleSafe |

## UXAssist

### Functions/FactoryFunctions.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Functions/FactoryFunctions.cs` | 161 | `private static HashSet<int> _itemIsBelt = null;` | CandidateForInstancing |
| `UXAssist/Functions/FactoryFunctions.cs` | 162 | `private static Dictionary<int, int> _upgradeTypes = null;` | LifecycleSafe |

### Functions/PlanetFunctions.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Functions/PlanetFunctions.cs` | 11 | `public static ConfigEntry<int> OrbitalCollectorMaxBuildCount;` | LifecycleSafe |
| `UXAssist/Functions/PlanetFunctions.cs` | 12 | `public static ConfigEntry<bool> ReturnBuildingsOnInitializeEnabled;` | LifecycleSafe |
| `UXAssist/Functions/PlanetFunctions.cs` | 13 | `public static ConfigEntry<bool> ReturnLogisticStorageItemsOnInitializeEnabled;` | LifecycleSafe |
| `UXAssist/Functions/PlanetFunctions.cs` | 14 | `public static ConfigEntry<bool> ReturnBeltAFactoryItemsOnInitializeEnabled;` | LifecycleSafe |

### Functions/UI/AutoConstructUI.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Functions/UI/AutoConstructUI.cs` | 11 | `public static MyCheckButton ToggleAutoConstruct;` | LifecycleSafe |
| `UXAssist/Functions/UI/AutoConstructUI.cs` | 12 | `public static GameObject ConstructCountPanel;` | NeedsReset |
| `UXAssist/Functions/UI/AutoConstructUI.cs` | 13 | `public static Text ConstructCountText;` | LifecycleSafe |

### Functions/UI/AutoCruiseUI.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Functions/UI/AutoCruiseUI.cs` | 9 | `public static MyCheckButton ToggleAutoCruise;` | LifecycleSafe |

### Functions/UI/MenuButtonUI.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Functions/UI/MenuButtonUI.cs` | 12 | `private static bool _initialized;` | LifecycleSafe |
| `UXAssist/Functions/UI/MenuButtonUI.cs` | 13 | `private static PressKeyBind _toggleKey;` | LifecycleSafe |
| `UXAssist/Functions/UI/MenuButtonUI.cs` | 14 | `private static bool _configWinInitialized;` | LifecycleSafe |
| `UXAssist/Functions/UI/MenuButtonUI.cs` | 15 | `private static MyConfigWindow _configWin;` | LifecycleSafe |
| `UXAssist/Functions/UI/MenuButtonUI.cs` | 16 | `private static GameObject _buttonOnPlanetGlobe;` | LifecycleSafe |

### Functions/UI/MilkyWayUI.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Functions/UI/MilkyWayUI.cs` | 16 | `private static int _clusterUploadResultsHead = 0;` | NeedsReset |
| `UXAssist/Functions/UI/MilkyWayUI.cs` | 17 | `private static int _clusterUploadResultsCount = 0;` | NeedsReset |
| `UXAssist/Functions/UI/MilkyWayUI.cs` | 26 | `private static ClusterPlayerData[] _topTenPlayerData = null;` | CandidateForInstancing |
| `UXAssist/Functions/UI/MilkyWayUI.cs` | 29 | `public static MyCheckButton MilkyWayTopTenPlayersToggler;` | LifecycleSafe |

### Functions/UI/StarmapFilterUI.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Functions/UI/StarmapFilterUI.cs` | 13 | `private static int _cornerComboBoxIndex;` | NeedsReset |
| `UXAssist/Functions/UI/StarmapFilterUI.cs` | 14 | `private static string[] _starOrderNames;` | LifecycleSafe |
| `UXAssist/Functions/UI/StarmapFilterUI.cs` | 15 | `private static bool _starmapFilterInitialized;` | LifecycleSafe |
| `UXAssist/Functions/UI/StarmapFilterUI.cs` | 16 | `private static ulong[] _starmapStarFilterValues;` | CandidateForInstancing |
| `UXAssist/Functions/UI/StarmapFilterUI.cs` | 17 | `private static bool _starFilterEnabled;` | NeedsReset |
| `UXAssist/Functions/UI/StarmapFilterUI.cs` | 18 | `public static MyCheckButton StarmapFilterToggler;` | LifecycleSafe |
| `UXAssist/Functions/UI/StarmapFilterUI.cs` | 19 | `public static bool[] ShowStarName;` | LifecycleSafe |

### Functions/WindowFunctions.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Functions/WindowFunctions.cs` | 10 | `private static bool _initialized;` | LifecycleSafe |
| `UXAssist/Functions/WindowFunctions.cs` | 14 | `private static string _gameWindowTitle = "Dyson Sphere Program";` | LifecycleSafe |
| `UXAssist/Functions/WindowFunctions.cs` | 16 | `private static IntPtr _gameWindowHandle = IntPtr.Zero;` | LifecycleSafe |
| `UXAssist/Functions/WindowFunctions.cs` | 18 | `public static ConfigEntry<int> ProcessPriority;` | LifecycleSafe |

### Patches/DysonSpherePatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/DysonSpherePatch.cs` | 14 | `public static ConfigEntry<bool> StopEjectOnNodeCompleteEnabled;` | LifecycleSafe |
| `UXAssist/Patches/DysonSpherePatch.cs` | 15 | `public static ConfigEntry<bool> OnlyConstructNodesEnabled;` | LifecycleSafe |
| `UXAssist/Patches/DysonSpherePatch.cs` | 16 | `public static ConfigEntry<int> AutoConstructMultiplier;` | LifecycleSafe |
| `UXAssist/Patches/DysonSpherePatch.cs` | 18 | `private static FieldInfo _totalNodeSpInfo, _totalFrameSpInfo, _totalCpInfo;` | LifecycleSafe |
| `UXAssist/Patches/DysonSpherePatch.cs` | 265 | `private static HashSet<int>[] _nodeForAbsorb;` | CandidateForInstancing |
| `UXAssist/Patches/DysonSpherePatch.cs` | 266 | `private static bool _initialized;` | LifecycleSafe |

### Patches/Factory/BeltSignalPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/Factory/BeltSignalPatch.cs` | 46 | `private static bool _initialized;` | LifecycleSafe |
| `UXAssist/Patches/Factory/BeltSignalPatch.cs` | 47 | `private static bool _loaded;` | LifecycleSafe |
| `UXAssist/Patches/Factory/BeltSignalPatch.cs` | 48 | `private static long _clusterSeedKey;` | NeedsReset |
| `UXAssist/Patches/Factory/BeltSignalPatch.cs` | 52 | `private static Dictionary<int, uint>[] _signalBelts = new Dictionary<int, uint>[64];` | CandidateForInstancing |

### Patches/Factory/BuildToolPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/Factory/BuildToolPatch.cs` | 79 | `private static bool _initialized;` | LifecycleSafe |
| `UXAssist/Patches/Factory/BuildToolPatch.cs` | 108 | `private static PlanetData _lastPlanet;` | NeedsReset |
| `UXAssist/Patches/Factory/BuildToolPatch.cs` | 109 | `private static Vector3 _lastPos;` | NeedsReset |
| `UXAssist/Patches/Factory/BuildToolPatch.cs` | 110 | `private static string _lastOffsetText;` | NeedsReset |
| `UXAssist/Patches/Factory/BuildToolPatch.cs` | 573 | `private static ItemProto _powerPoleProto;` | LifecycleSafe |
| `UXAssist/Patches/Factory/BuildToolPatch.cs` | 681 | `private static long nextTimei = 0;` | NeedsReset |

### Patches/Factory/FactoryPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 15 | `public static ConfigEntry<bool> UnlimitInteractiveEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 16 | `public static ConfigEntry<bool> RemoveSomeConditionEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 17 | `public static ConfigEntry<bool> NightLightEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 18 | `public static ConfigEntry<float> NightLightAngleX;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 19 | `public static ConfigEntry<float> NightLightAngleY;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 20 | `public static ConfigEntry<bool> RemoveBuildRangeLimitEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 21 | `public static ConfigEntry<bool> LargerAreaForUpgradeAndDismantleEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 22 | `public static ConfigEntry<bool> LargerAreaForTerraformEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 23 | `public static ConfigEntry<bool> OffGridBuildingEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 24 | `public static ConfigEntry<bool> TreatStackingAsSingleEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 25 | `public static ConfigEntry<bool> QuickBuildAndDismantleLabsEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 26 | `public static ConfigEntry<bool> ProtectVeinsFromExhaustionEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 27 | `public static ConfigEntry<bool> DoNotRenderEntitiesEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 28 | `public static ConfigEntry<bool> DragBuildPowerPolesEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 29 | `public static ConfigEntry<bool> DragBuildPowerPolesAlternatelyEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 30 | `public static ConfigEntry<bool> AutoConstructButtonEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 31 | `public static ConfigEntry<bool> AutoConstructEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 32 | `public static ConfigEntry<bool> BeltSignalsForBuyOutEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 33 | `public static ConfigEntry<bool> TankFastFillInAndTakeOutEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 34 | `public static ConfigEntry<int> TankFastFillInAndTakeOutMultiplier;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 35 | `public static ConfigEntry<bool> CutConveyorBeltEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 36 | `public static ConfigEntry<bool> TweakBuildingBufferEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 37 | `public static ConfigEntry<int> AssemblerBufferTimeMultiplier;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 38 | `public static ConfigEntry<int> AssemblerBufferMininumMultiplier;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 39 | `public static ConfigEntry<int> LabBufferMaxCountForAssemble;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 40 | `public static ConfigEntry<int> LabBufferExtraCountForAdvancedAssemble;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 41 | `public static ConfigEntry<int> LabBufferMaxCountForResearch;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 42 | `public static ConfigEntry<int> ReceiverBufferCount;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 43 | `public static ConfigEntry<int> EjectorBufferCount;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 44 | `public static ConfigEntry<int> SiloBufferCount;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 45 | `public static ConfigEntry<bool> ShortcutKeysForBlueprintCopyEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 46 | `public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 47 | `public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsIncludeBranches;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 48 | `public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsIncludeInserters;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 50 | `internal static PressKeyBind _doNotRenderEntitiesKey;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 51 | `internal static PressKeyBind _offgridfForPathsKey;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 52 | `internal static PressKeyBind _cutConveyorBeltKey;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 53 | `internal static PressKeyBind _dismantleBlueprintSelectionKey;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 54 | `internal static PressKeyBind _selectAllBuildingsInBlueprintCopyKey;` | LifecycleSafe |
| `UXAssist/Patches/Factory/FactoryPatch.cs` | 56 | `internal static int _tankFastFillInAndTakeOutMultiplierRealValue = 2;` | NeedsReset |

### Patches/Factory/ImmediateBuildPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/Factory/ImmediateBuildPatch.cs` | 20 | `private static int _lastPrebuildCount = -1;` | NeedsReset |

### Patches/Factory/RenderingPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/Factory/RenderingPatch.cs` | 80 | `private static bool _nightlightInitialized;` | LifecycleSafe |
| `UXAssist/Patches/Factory/RenderingPatch.cs` | 81 | `private static bool _mechaOnEarth;` | NeedsReset |
| `UXAssist/Patches/Factory/RenderingPatch.cs` | 82 | `private static AnimationState _sail;` | LifecycleSafe |
| `UXAssist/Patches/Factory/RenderingPatch.cs` | 83 | `private static Light _sunlight;` | LifecycleSafe |

### Patches/Factory/VeinProtectionPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/Factory/VeinProtectionPatch.cs` | 22 | `public static int KeepVeinAmount = 100;` | LifecycleSafe |
| `UXAssist/Patches/Factory/VeinProtectionPatch.cs` | 23 | `public static float KeepOilSpeed = 1f;` | LifecycleSafe |
| `UXAssist/Patches/Factory/VeinProtectionPatch.cs` | 24 | `private static int _keepOilAmount;` | NeedsReset |

### Patches/GamePatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/GamePatch.cs` | 19 | `public static ConfigEntry<bool> EnableWindowResizeEnabled;` | LifecycleSafe |
| `UXAssist/Patches/GamePatch.cs` | 20 | `public static ConfigEntry<bool> LoadLastWindowRectEnabled;` | LifecycleSafe |
| `UXAssist/Patches/GamePatch.cs` | 23 | `public static ConfigEntry<bool> ConvertSavesFromPeaceEnabled;` | LifecycleSafe |
| `UXAssist/Patches/GamePatch.cs` | 24 | `public static ConfigEntry<Vector4> LastWindowRect;` | LifecycleSafe |
| `UXAssist/Patches/GamePatch.cs` | 25 | `public static ConfigEntry<bool> ProfileBasedSaveFolderEnabled;` | LifecycleSafe |
| `UXAssist/Patches/GamePatch.cs` | 26 | `public static ConfigEntry<bool> ProfileBasedOptionEnabled;` | LifecycleSafe |
| `UXAssist/Patches/GamePatch.cs` | 27 | `public static ConfigEntry<string> DefaultProfileName;` | LifecycleSafe |
| `UXAssist/Patches/GamePatch.cs` | 28 | `public static ConfigEntry<double> GameUpsFactor;` | LifecycleSafe |
| `UXAssist/Patches/GamePatch.cs` | 30 | `private static PressKeyBind _speedDownKey;` | LifecycleSafe |
| `UXAssist/Patches/GamePatch.cs` | 31 | `private static PressKeyBind _speedUpKey;` | LifecycleSafe |
| `UXAssist/Patches/GamePatch.cs` | 32 | `private static bool _enableGameUpsFactor = true;` | NeedsReset |
| `UXAssist/Patches/GamePatch.cs` | 272 | `private static bool _enabled;` | NeedsReset |
| `UXAssist/Patches/GamePatch.cs` | 451 | `private static bool _needConvert;` | NeedsReset |

### Patches/Logistics/CapacityPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/Logistics/CapacityPatch.cs` | 16 | `private static KeyCode _lastKey = KeyCode.None;` | NeedsReset |
| `UXAssist/Patches/Logistics/CapacityPatch.cs` | 17 | `private static long _nextKeyTick;` | NeedsReset |
| `UXAssist/Patches/Logistics/CapacityPatch.cs` | 18 | `private static bool _skipNextUIStationStorageEvent;` | NeedsReset |
| `UXAssist/Patches/Logistics/CapacityPatch.cs` | 19 | `private static bool _skipNextUIControlPanelStationStorageEvent;` | NeedsReset |
| `UXAssist/Patches/Logistics/CapacityPatch.cs` | 20 | `private static bool _refreshingUIStationStorage;` | NeedsReset |
| `UXAssist/Patches/Logistics/CapacityPatch.cs` | 21 | `private static bool _refreshingUIControlPanelStationStorage;` | NeedsReset |

### Patches/Logistics/LogisticsPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 16 | `public static ConfigEntry<bool> AutoConfigLogisticsEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 17 | `public static ConfigEntry<bool> AutoConfigLimitAutoReplenishCount;` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 19 | `public static ConfigEntry<int> AutoConfigDispenserChargePower; // 3~30, display as 300000.0 * value` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 20 | `public static ConfigEntry<int> AutoConfigDispenserCourierCount; // 0~10` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 22 | `public static ConfigEntry<int> AutoConfigBattleBaseChargePower; // 4~40, display as 300000.0 * value` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 24 | `public static ConfigEntry<int> AutoConfigPLSChargePower; // 2~20, display as 3000000.0 * value` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 25 | `public static ConfigEntry<int> AutoConfigPLSMaxTripDrone; // 1~180, by degress` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 26 | `public static ConfigEntry<int> AutoConfigPLSDroneMinDeliver; // 0~10; 0 = 1%, 1~10 = 10% *value` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 27 | `public static ConfigEntry<int> AutoConfigPLSMinPilerValue; // 0~4; 0 = Maximum in tech, 1~4 = piler stacking count` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 28 | `public static ConfigEntry<int> AutoConfigPLSDroneCount; // 0~50` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 30 | `public static ConfigEntry<bool> SetDefaultRemoteLogicToStorage;` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 31 | `public static ConfigEntry<int> AutoConfigILSChargePower; // 2~20, display as 15000000.0 * value` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 32 | `public static ConfigEntry<int> AutoConfigILSMaxTripDrone; // 1~180, by degress` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 33 | `public static ConfigEntry<int> AutoConfigILSMaxTripShip; // 1~41; 1~20 = value LY, 21-40 = 2*value-20LY, 41 = Unlimited` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 34 | `public static ConfigEntry<int> AutoConfigILSWarperDistance; // 2~21; 2~7 = value * 0.5 - 0.5AU, 8~16 = value - 4AU, 17~20 = value * 2 - 20AU, 21 = 60AU` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 35 | `public static ConfigEntry<int> AutoConfigILSDroneMinDeliver; // 0~10; 0 = 1%, 1~10 = 10% *value` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 36 | `public static ConfigEntry<int> AutoConfigILSShipMinDeliver; // 0~10; 0 = 1%, 1~10 = 10% *value` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 37 | `public static ConfigEntry<int> AutoConfigILSMinPilerValue; // 0~4; 0 = Maximum in tech, 1~4 = piler stacking count` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 38 | `public static ConfigEntry<bool> AutoConfigILSIncludeOrbitCollector;` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 39 | `public static ConfigEntry<bool> AutoConfigILSWarperNecessary;` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 40 | `public static ConfigEntry<int> AutoConfigILSDroneCount; // 0~100` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 41 | `public static ConfigEntry<int> AutoConfigILSShipCount; // 0~10` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 43 | `public static ConfigEntry<int> AutoConfigVeinCollectorHarvestSpeed; // 0-20, 100% + 10% * value` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 44 | `public static ConfigEntry<int> AutoConfigVeinCollectorMinPilerValue; // 0~4; 0 = Maximum in tech, 1~4 = piler stacking count` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 46 | `public static ConfigEntry<bool> LogisticsCapacityTweaksEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 47 | `public static ConfigEntry<bool> AllowOverflowInLogisticsEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 48 | `public static ConfigEntry<bool> GreaterPowerUsageInLogisticsEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 49 | `public static ConfigEntry<bool> LogisticsConstrolPanelImprovementEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 50 | `public static ConfigEntry<bool> RealtimeLogisticsInfoPanelEnabled;` | LifecycleSafe |
| `UXAssist/Patches/Logistics/LogisticsPatch.cs` | 51 | `public static ConfigEntry<bool> RealtimeLogisticsInfoPanelBarsEnabled;` | LifecycleSafe |

### Patches/Logistics/OverflowPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/Logistics/OverflowPatch.cs` | 11 | `private static bool _blueprintPasting;` | NeedsReset |

### Patches/Logistics/RealtimeInfoPanelPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 172 | `private static StationTip[] _stationTips = new StationTip[16];` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 175 | `private static int _stationTipsRecycleCount;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 176 | `private static GameObject _stationTipsRoot;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 177 | `private static GameObject _tipPrefab;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 183 | `private static int _lastPlanetId;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 185 | `private static int _localStorageMax = LogisticsConstants.DefaultLocalStorageMax;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 186 | `private static int _remoteStorageMax = LogisticsConstants.DefaultRemoteStorageMax;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 187 | `private static int _localStorageExtra;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 188 | `private static int _remoteStorageExtra;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 189 | `private static int _localStorageMaxTotal = _localStorageMax;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 190 | `private static int _remoteStorageMaxTotal = _remoteStorageMax;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 191 | `private static float _localStoragePixelPerItem = LogisticsConstants.StorageSliderWidth / _localStorageMaxTotal;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 192 | `private static float _remoteStoragePixelPerItem = LogisticsConstants.StorageSliderWidth / _remoteStorageMaxTotal;` | NeedsReset |
| `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs` | 194 | `private static int _storageMaxSlotCount = LogisticsConstants.DefaultStorageSlotCount;` | NeedsReset |

### Patches/PlanetPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/PlanetPatch.cs` | 11 | `public static ConfigEntry<bool> PlayerActionsInGlobeViewEnabled;` | LifecycleSafe |

### Patches/PlayerPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/PlayerPatch.cs` | 13 | `public static ConfigEntry<bool> EnhancedMechaForgeCountControlEnabled;` | LifecycleSafe |
| `UXAssist/Patches/PlayerPatch.cs` | 14 | `public static ConfigEntry<bool> HideTipsForSandsChangesEnabled;` | LifecycleSafe |
| `UXAssist/Patches/PlayerPatch.cs` | 15 | `public static ConfigEntry<bool> ShortcutKeysForStarsNameEnabled;` | LifecycleSafe |
| `UXAssist/Patches/PlayerPatch.cs` | 16 | `public static ConfigEntry<bool> AutoNavigationEnabled;` | LifecycleSafe |
| `UXAssist/Patches/PlayerPatch.cs` | 17 | `public static ConfigEntry<bool> AutoCruiseEnabled;` | LifecycleSafe |
| `UXAssist/Patches/PlayerPatch.cs` | 18 | `public static ConfigEntry<bool> AutoBoostEnabled;` | LifecycleSafe |
| `UXAssist/Patches/PlayerPatch.cs` | 19 | `public static ConfigEntry<double> DistanceToWarp;` | LifecycleSafe |
| `UXAssist/Patches/PlayerPatch.cs` | 20 | `private static PressKeyBind _showAllStarsNameKey;` | LifecycleSafe |
| `UXAssist/Patches/PlayerPatch.cs` | 21 | `private static PressKeyBind _toggleAllStarsNameKey;` | LifecycleSafe |
| `UXAssist/Patches/PlayerPatch.cs` | 22 | `private static PressKeyBind _autoDriveKey;` | LifecycleSafe |
| `UXAssist/Patches/PlayerPatch.cs` | 194 | `public static int ShowAllStarsNameStatus;` | NeedsReset |
| `UXAssist/Patches/PlayerPatch.cs` | 195 | `public static bool ForceShowAllStarsName;` | NeedsReset |
| `UXAssist/Patches/PlayerPatch.cs` | 196 | `public static bool ForceShowAllStarsNameExternal;` | NeedsReset |
| `UXAssist/Patches/PlayerPatch.cs` | 328 | `private static bool _canUseWarper;` | NeedsReset |
| `UXAssist/Patches/PlayerPatch.cs` | 329 | `private static int _indicatorAstroId;` | NeedsReset |
| `UXAssist/Patches/PlayerPatch.cs` | 330 | `private static bool _speedUp;` | NeedsReset |
| `UXAssist/Patches/PlayerPatch.cs` | 331 | `private static Vector3 _direction;` | NeedsReset |
| `UXAssist/Patches/PlayerPatch.cs` | 332 | `private static EMovementState _movementState = EMovementState.Walk;` | NeedsReset |

### Patches/TechPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/TechPatch.cs` | 16 | `public static ConfigEntry<bool> SorterCargoStackingEnabled;` | LifecycleSafe |
| `UXAssist/Patches/TechPatch.cs` | 17 | `public static ConfigEntry<bool> DisableBattleRelatedTechsInPeaceModeEnabled;` | LifecycleSafe |
| `UXAssist/Patches/TechPatch.cs` | 18 | `public static ConfigEntry<bool> BatchBuyoutTechEnabled;` | LifecycleSafe |
| `UXAssist/Patches/TechPatch.cs` | 43 | `private static bool _protoPatched;` | LifecycleSafe |
| `UXAssist/Patches/TechPatch.cs` | 151 | `private static bool _protoPatched;` | LifecycleSafe |
| `UXAssist/Patches/TechPatch.cs` | 152 | `private static HashSet<int> _techsToDisableSet;` | LifecycleSafe |
| `UXAssist/Patches/TechPatch.cs` | 153 | `private static Dictionary<int, TechProto[]> _originTechPosts = [];` | LifecycleSafe |

### Patches/UIPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UXAssist/Patches/UIPatch.cs` | 15 | `public static ConfigEntry<bool> PlanetVeinUtilizationEnabled;` | LifecycleSafe |
| `UXAssist/Patches/UIPatch.cs` | 40 | `private static VeinTypeInfo[] planetVeinCount = null;` | CandidateForInstancing |
| `UXAssist/Patches/UIPatch.cs` | 41 | `private static VeinTypeInfo[] starVeinCount = null;` | CandidateForInstancing |

## UniverseGenTweaks

### BirthPlanetPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 12 | `public static ConfigEntry<bool> SitiVeinsOnBirthPlanet;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 13 | `public static ConfigEntry<bool> FireIceOnBirthPlanet;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 14 | `public static ConfigEntry<bool> KimberliteOnBirthPlanet;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 15 | `public static ConfigEntry<bool> FractalOnBirthPlanet;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 16 | `public static ConfigEntry<bool> OrganicOnBirthPlanet;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 17 | `public static ConfigEntry<bool> OpticalOnBirthPlanet;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 18 | `public static ConfigEntry<bool> SpiniformOnBirthPlanet;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 19 | `public static ConfigEntry<bool> UnipolarOnBirthPlanet;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 20 | `public static ConfigEntry<bool> FlatBirthPlanet;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 21 | `public static ConfigEntry<bool> HighLuminosityBirthStar;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 23 | `private static BackupData _backupData;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 24 | `private static bool _initialized;` | LifecycleSafe |
| `UniverseGenTweaks/BirthPlanetPatch.cs` | 25 | `private static Harmony _patch;` | LifecycleSafe |

### EpicDifficulty.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UniverseGenTweaks/EpicDifficulty.cs` | 16 | `public static ConfigEntry<bool> Enabled;` | LifecycleSafe |
| `UniverseGenTweaks/EpicDifficulty.cs` | 17 | `public static ConfigEntry<float> ResourceMultiplier;` | LifecycleSafe |
| `UniverseGenTweaks/EpicDifficulty.cs` | 18 | `public static ConfigEntry<float> OilMultiplier;` | LifecycleSafe |
| `UniverseGenTweaks/EpicDifficulty.cs` | 19 | `private static Harmony _harmony;` | LifecycleSafe |

### MoreSettings.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UniverseGenTweaks/MoreSettings.cs` | 7 | `public static ConfigEntry<bool> Enabled;` | LifecycleSafe |
| `UniverseGenTweaks/MoreSettings.cs` | 8 | `public static ConfigEntry<int> MaxStarCount;` | LifecycleSafe |

### Patches/CombatSettingsPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UniverseGenTweaks/Patches/CombatSettingsPatch.cs` | 13 | `private static Harmony _patch;` | LifecycleSafe |

### Patches/GalaxyGenSettingsPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs` | 14 | `internal static double MinDist = UniverseGenConstants.DefaultMinDist;` | NeedsReset |
| `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs` | 15 | `internal static double MinStep = UniverseGenConstants.DefaultMinStep;` | NeedsReset |
| `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs` | 16 | `internal static double MaxStep = UniverseGenConstants.DefaultMaxStep;` | NeedsReset |
| `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs` | 17 | `internal static double Flatten = UniverseGenConstants.DefaultFlatten;` | NeedsReset |
| `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs` | 19 | `internal static double GameMinDist = UniverseGenConstants.DefaultMinDist;` | NeedsReset |
| `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs` | 20 | `internal static double GameMinStep = UniverseGenConstants.DefaultMinStep;` | NeedsReset |
| `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs` | 21 | `internal static double GameMaxStep = UniverseGenConstants.DefaultMaxStep;` | NeedsReset |
| `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs` | 22 | `internal static double GameFlatten = UniverseGenConstants.DefaultFlatten;` | NeedsReset |
| `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs` | 24 | `private static Harmony _patch;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs` | 25 | `private static Harmony _permanentPatch;` | LifecycleSafe |

### Patches/GalaxySelectUIPatch.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 14 | `private static Text _minDistTitle;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 15 | `private static Text _minStepTitle;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 16 | `private static Text _maxStepTitle;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 17 | `private static Text _flattenTitle;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 18 | `private static Slider _minDistSlider;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 19 | `private static Slider _minStepSlider;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 20 | `private static Slider _maxStepSlider;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 21 | `private static Slider _flattenSlider;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 22 | `private static Text _minDistText;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 23 | `private static Text _minStepText;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 24 | `private static Text _maxStepText;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 25 | `private static Text _flattenText;` | LifecycleSafe |
| `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs` | 26 | `private static Harmony _patch;` | LifecycleSafe |

### UIConfigWindow.cs

| File | Line | Field | Classification |
|------|------|-------|----------------|
| `UniverseGenTweaks/UIConfigWindow.cs` | 10 | `private static RectTransform _windowTrans;` | LifecycleSafe |

## Raw Grep Output

The following is the verbatim output of the two inventory grep commands (includes methods and property accessors for completeness):

```
UXAssist/Patches/DysonSpherePatch.cs:18:    private static FieldInfo _totalNodeSpInfo, _totalFrameSpInfo, _totalCpInfo;
UXAssist/Patches/DysonSpherePatch.cs:45:    private static bool DysonSwarm_AutoConstruct_Prefix(DysonSwarm __instance)
UXAssist/Patches/DysonSpherePatch.cs:52:    private static bool DysonSphere_AutoConstruct_Prefix(DysonSphere __instance)
UXAssist/Patches/DysonSpherePatch.cs:238:    private static IEnumerable<CodeInstruction> DysonSpherePatch_DysonNode_ConstructCp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/DysonSpherePatch.cs:265:        private static HashSet<int>[] _nodeForAbsorb;
UXAssist/Patches/DysonSpherePatch.cs:266:        private static bool _initialized;
UXAssist/Patches/DysonSpherePatch.cs:283:        private static void InitNodeForAbsorb()
UXAssist/Patches/DysonSpherePatch.cs:314:        private static void SetNodeForAbsorb(int index, int layerId, int nodeId, bool canAbsorb)
UXAssist/Patches/DysonSpherePatch.cs:325:        private static void UpdateNodeForAbsorbOnSpChange(DysonNode node)
UXAssist/Patches/DysonSpherePatch.cs:334:        private static void UpdateNodeForAbsorbOnCpChange(DysonNode node)
UXAssist/Patches/DysonSpherePatch.cs:343:        private static bool AnyNodeForAbsorb(int starIndex)
UXAssist/Patches/DysonSpherePatch.cs:349:        private static void OnGameBegin()
UXAssist/Patches/DysonSpherePatch.cs:354:        private static void OnGameEnd()
UXAssist/Patches/DysonSpherePatch.cs:362:        private static void DysonNode_RecalcCpReq_Postfix(DysonNode __instance)
UXAssist/Patches/DysonSpherePatch.cs:369:        private static void DysonSphereLayer_RemoveDysonNode_Prefix(DysonSphereLayer __instance, int nodeId)
UXAssist/Patches/DysonSpherePatch.cs:377:        private static void DysonSphere_ResetNew_Prefix(DysonSphere __instance)
UXAssist/Patches/DysonSpherePatch.cs:388:        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/DysonSpherePatch.cs:427:        private static IEnumerable<CodeInstruction> DysonNode_ConstructSp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/DysonSpherePatch.cs:444:        private static IEnumerable<CodeInstruction> DysonNode_ConstructCp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/DysonSpherePatch.cs:507:        private static void RecheckDysonSphereAutoNodes()
UXAssist/Patches/DysonSpherePatch.cs:525:        private static IEnumerable<CodeInstruction> DysonNode_spReqOrder_Getter_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/ArchitectModePatch.cs:25:        private static IEnumerable<CodeInstruction> PlayerAction_Inspect_GetObjectSelectDistance_Transpiler(IEnumerable<CodeInstruction> instructions)
UXAssist/Patches/Factory/ArchitectModePatch.cs:37:        private static IEnumerable<CodeInstruction> BuildTool_Click_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/ArchitectModePatch.cs:71:        private static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/ArchitectModePatch.cs:146:        private static IEnumerable<CodeInstruction> BuildTool_Click__OnInit_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/ArchitectModePatch.cs:166:        private static IEnumerable<CodeInstruction> BuildAreaLimitRemoval_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/ArchitectModePatch.cs:191:        private static IEnumerable<CodeInstruction> BuildTools_CursorSizePatch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/ArchitectModePatch.cs:205:        private static IEnumerable<CodeInstruction> BuildTool_Reform_ReformAction_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BeltSignalPatch.cs:46:        private static bool _initialized;
UXAssist/Patches/Factory/BeltSignalPatch.cs:47:        private static bool _loaded;
UXAssist/Patches/Factory/BeltSignalPatch.cs:48:        private static long _clusterSeedKey;
UXAssist/Patches/Factory/BeltSignalPatch.cs:52:        private static Dictionary<int, uint>[] _signalBelts = new Dictionary<int, uint>[64];
UXAssist/Patches/Factory/BeltSignalPatch.cs:65:        private static void AddBeltSignalProtos()
UXAssist/Patches/Factory/BeltSignalPatch.cs:143:        private static void RemoveBeltSignalProtos()
UXAssist/Patches/Factory/BeltSignalPatch.cs:160:        private static void InitSignalBelts()
UXAssist/Patches/Factory/BeltSignalPatch.cs:183:        private static void SetSignalBelt(int factory, int beltId, uint signal)
UXAssist/Patches/Factory/BeltSignalPatch.cs:191:        private static Dictionary<int, uint> GetOrCreateSignalBelts(int index)
UXAssist/Patches/Factory/BeltSignalPatch.cs:210:        private static Dictionary<int, uint> GetSignalBelts(int index)
UXAssist/Patches/Factory/BeltSignalPatch.cs:215:        private static void RemoveSignalBelt(int factory, int beltId)
UXAssist/Patches/Factory/BeltSignalPatch.cs:224:        private static void RemovePlanetSignalBelts(int factory)
UXAssist/Patches/Factory/BeltSignalPatch.cs:247:            private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
UXAssist/Patches/Factory/BeltSignalPatch.cs:256:            private static void DigitalSystem_Constructor_Postfix(PlanetData _planet)
UXAssist/Patches/Factory/BeltSignalPatch.cs:265:            private static void OnGameBegin()
UXAssist/Patches/Factory/BuildingBufferPatch.cs:73:        private static IEnumerable<CodeInstruction> PowerGeneratorComponent_GameTick_Gamma_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildingBufferPatch.cs:95:        private static IEnumerable<CodeInstruction> AssemblerComponent_UpdateNeeds_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildingBufferPatch.cs:131:        private static IEnumerable<CodeInstruction> LabComponent_UpdateNeedsAssemble_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildingBufferPatch.cs:169:        private static IEnumerable<CodeInstruction> LabComponent_UpdateNeedsResearch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildingBufferPatch.cs:189:        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildingBufferPatch.cs:203:        private static IEnumerable<CodeInstruction> SiloComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:29:        private static IEnumerable<CodeInstruction> ConnGizmoGraph_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:41:        private static IEnumerable<CodeInstruction> ConnGizmoGraph_SetPointCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:53:        private static IEnumerable<CodeInstruction> BuildTool_Path__OnInit_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:64:        private static IEnumerable<CodeInstruction> BuildTool_Reform_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:79:        private static bool _initialized;
UXAssist/Patches/Factory/BuildToolPatch.cs:81:        private static void SetupRichTextSupport()
UXAssist/Patches/Factory/BuildToolPatch.cs:89:        private static void CalculateGridOffset(PlanetData planet, Vector3 pos, out float x, out float y, out float z)
UXAssist/Patches/Factory/BuildToolPatch.cs:103:        private static string FormatOffsetFloat(float f)
UXAssist/Patches/Factory/BuildToolPatch.cs:108:        private static PlanetData _lastPlanet;
UXAssist/Patches/Factory/BuildToolPatch.cs:109:        private static Vector3 _lastPos;
UXAssist/Patches/Factory/BuildToolPatch.cs:110:        private static string _lastOffsetText;
UXAssist/Patches/Factory/BuildToolPatch.cs:116:        private static void BuildTool_Click_CheckBuildConditions_Postfix(BuildTool __instance)
UXAssist/Patches/Factory/BuildToolPatch.cs:139:        private static IEnumerable<CodeInstruction> UIEntityBriefInfo__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:170:        private static void MatchIgnoreGridAndCheckIfRotatable(CodeMatcher matcher, out Label? ifBlockEntryLabel, out Label? elseBlockEntryLabel)
UXAssist/Patches/Factory/BuildToolPatch.cs:344:        private static IEnumerable<CodeInstruction> MonitorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:387:        private static bool IsPowerPole(int id)
UXAssist/Patches/Factory/BuildToolPatch.cs:392:        private static void FixProto()
UXAssist/Patches/Factory/BuildToolPatch.cs:409:        private static void UnfixProto()
UXAssist/Patches/Factory/BuildToolPatch.cs:429:        private static void OnGameBegin()
UXAssist/Patches/Factory/BuildToolPatch.cs:435:        private static void OnGameEnd()
UXAssist/Patches/Factory/BuildToolPatch.cs:441:        private static int PlanetGridSnapDotsNonAllocNotAligned(PlanetGrid planetGrid, Vector3 begin, Vector3 end, Vector2 interval, float yaw, float planetRadius, float gap, Vector3[] snaps)
UXAssist/Patches/Factory/BuildToolPatch.cs:475:        private static int PlanetAuxDataSnapDotsNonAllocNotAligned(PlanetAuxData aux, Vector3 begin, Vector3 end, Vector2 interval, float height, float yaw, float gap, Vector3[] snaps)
UXAssist/Patches/Factory/BuildToolPatch.cs:497:        private static IEnumerable<CodeInstruction> BuildTool_Click_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:573:        private static ItemProto _powerPoleProto;
UXAssist/Patches/Factory/BuildToolPatch.cs:586:        private static IEnumerable<CodeInstruction> PlanetFactory_EntityFastFillIn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:606:        private static IEnumerable<CodeInstruction> PlanetFactory_EntityFastTakeOut_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:627:        private static IEnumerable<CodeInstruction> UITankWindow__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:644:        private static bool TankComponent_TickOutput_Prefix(ref TankComponent __instance, PlanetFactory factory)
UXAssist/Patches/Factory/BuildToolPatch.cs:681:        private static long nextTimei = 0;
UXAssist/Patches/Factory/BuildToolPatch.cs:693:        private static void OnGameBegin()
UXAssist/Patches/Factory/BuildToolPatch.cs:701:        private static IEnumerable<CodeInstruction> VFInput_fastTransferWithEntityDown_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:716:        private static IEnumerable<CodeInstruction> PlayerAction_Inspect_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:750:        private static void EntityFastTakeOutAlt(PlanetFactory factory, int entityId, bool toPackage, out ItemBundle itemBundle, out bool full)
UXAssist/Patches/Factory/FactoryPatch.cs:186:    private static void UpdateTankFastFillInAndTakeOutMultiplierRealValue()
UXAssist/Patches/Factory/ImmediateBuildPatch.cs:20:        private static int _lastPrebuildCount = -1;
UXAssist/Patches/Factory/ImmediateBuildPatch.cs:35:        private static void PlanetData_NotifyFactoryLoaded_Postfix()
UXAssist/Patches/Factory/ImmediateBuildPatch.cs:43:        private static void PlanetData_UnloadFactory_Postfix()
UXAssist/Patches/Factory/ImmediateBuildPatch.cs:51:        private static void PlayerAction_Rts_GameTick_Postfix(PlayerAction_Rts __instance, long timei)
UXAssist/Patches/Factory/ImmediateBuildPatch.cs:109:        private static bool DetermineMoreLabsForDismantle(BuildTool dismantle, int id)
UXAssist/Patches/Factory/ImmediateBuildPatch.cs:178:        private static void BuildLabsToTop(BuildTool_Click click)
UXAssist/Patches/Factory/ImmediateBuildPatch.cs:218:        private static IEnumerable<CodeInstruction> BuildTool_Dismantle_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/ImmediateBuildPatch.cs:239:        private static IEnumerable<CodeInstruction> BuildTool_Click__OnTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/RenderingPatch.cs:25:        private static bool ObjectRenderer_Render_Prefix()
UXAssist/Patches/Factory/RenderingPatch.cs:32:        private static bool LabRenderer_Render_Prefix()
UXAssist/Patches/Factory/RenderingPatch.cs:39:        private static void FactoryModel_DrawInstancedBatches_Postfix(GPUInstancingManager __instance)
UXAssist/Patches/Factory/RenderingPatch.cs:46:        private static IEnumerable<CodeInstruction> RaycastLogic_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/RenderingPatch.cs:80:        private static bool _nightlightInitialized;
UXAssist/Patches/Factory/RenderingPatch.cs:81:        private static bool _mechaOnEarth;
UXAssist/Patches/Factory/RenderingPatch.cs:82:        private static AnimationState _sail;
UXAssist/Patches/Factory/RenderingPatch.cs:83:        private static Light _sunlight;
UXAssist/Patches/Factory/RenderingPatch.cs:103:        private static void OnGameEnd()
UXAssist/Patches/Factory/RenderingPatch.cs:162:        private static IEnumerable<CodeInstruction> StarSimulator_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/RenderingPatch.cs:188:        private static IEnumerable<CodeInstruction> PlanetSimulator_LateRefresh_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/VeinProtectionPatch.cs:24:        private static int _keepOilAmount;
UXAssist/Patches/Factory/VeinProtectionPatch.cs:33:        private static bool MinerComponent_InternalUpdate_Prefix(PlanetFactory factory, VeinData[] veinPool, float power, float miningRate, float miningSpeed, int[] productRegister,
UXAssist/Patches/GamePatch.cs:30:    private static PressKeyBind _speedDownKey;
UXAssist/Patches/GamePatch.cs:31:    private static PressKeyBind _speedUpKey;
UXAssist/Patches/GamePatch.cs:32:    private static bool _enableGameUpsFactor = true;
UXAssist/Patches/GamePatch.cs:155:    private static void RefreshSavePath()
UXAssist/Patches/GamePatch.cs:165:    private static void GameMain_HandleApplicationQuit_Prefix()
UXAssist/Patches/GamePatch.cs:173:    private static void FixLastWindowRect()
UXAssist/Patches/GamePatch.cs:220:    private static void Screen_SetResolution_Prefix(ref int width, ref int height, FullScreenMode fullscreenMode, ref Vector2Int __state)
UXAssist/Patches/GamePatch.cs:247:    private static void Screen_SetResolution_Postfix(FullScreenMode fullscreenMode, Vector2Int __state)
UXAssist/Patches/GamePatch.cs:272:        private static bool _enabled;
UXAssist/Patches/GamePatch.cs:301:        private static void UIOptionWindow_ApplyOptions_Postfix()
UXAssist/Patches/GamePatch.cs:324:        private static bool GameSave_AutoSave_Prefix(ref bool __result)
UXAssist/Patches/GamePatch.cs:420:        private static IEnumerable<CodeInstruction> UILoadGameWindow_ReplaceSaveName_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/GamePatch.cs:437:        private static IEnumerable<CodeInstruction> GameSave_RemoveValidateOnLoad_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/GamePatch.cs:451:        private static bool _needConvert;
UXAssist/Patches/GamePatch.cs:455:        private static void GameDesc_Import_Postfix(GameDesc __instance)
UXAssist/Patches/GamePatch.cs:465:        private static void GameHistoryData_Import_Postfix(GameHistoryData __instance)
UXAssist/Patches/GamePatch.cs:475:        private static IEnumerable<CodeInstruction> GameData_Import_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/GamePatch.cs:491:        private static void GameData_Import_Postfix()
UXAssist/Patches/GamePatch.cs:501:        private static bool GameOption_LoadGlobal_Prefix(ref GameOption __instance)
UXAssist/Patches/GamePatch.cs:545:        private static bool GameOption_SaveGlobal_Prefix(ref GameOption __instance)
UXAssist/Patches/Logistics/AutoConfigPatch.cs:34:        private static IEnumerable<CodeInstruction> PlanetFactory_StationAutoReplenishIfNeeded_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Logistics/AutoConfigPatch.cs:85:    private static void BuildTool_Addon_SetDefaultParams_Postfix(BuildTool_Addon __instance, int bpIndex)
UXAssist/Patches/Logistics/AutoConfigPatch.cs:95:    private static void PlanetTransport_NewStationComponent_Postfix(PlanetTransport __instance, StationComponent __result)
UXAssist/Patches/Logistics/AutoConfigPatch.cs:102:    private static void DefenseSystem_NewBattleBaseComponent_Postfix(DefenseSystem __instance, int __result)
UXAssist/Patches/Logistics/AutoConfigPatch.cs:110:    private static void PlanetTransport_NewDispenserComponent_Postfix(PlanetTransport __instance, int __result)
UXAssist/Patches/Logistics/AutoConfigPatch.cs:122:    private static IEnumerable<CodeInstruction> UIStationStorage_OnItemPickerReturn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Logistics/CapacityPatch.cs:16:    private static KeyCode _lastKey = KeyCode.None;
UXAssist/Patches/Logistics/CapacityPatch.cs:17:    private static long _nextKeyTick;
UXAssist/Patches/Logistics/CapacityPatch.cs:18:    private static bool _skipNextUIStationStorageEvent;
UXAssist/Patches/Logistics/CapacityPatch.cs:19:    private static bool _skipNextUIControlPanelStationStorageEvent;
UXAssist/Patches/Logistics/CapacityPatch.cs:20:    private static bool _refreshingUIStationStorage;
UXAssist/Patches/Logistics/CapacityPatch.cs:21:    private static bool _refreshingUIControlPanelStationStorage;
UXAssist/Patches/Logistics/CapacityPatch.cs:23:    private static bool UpdateKeyPressed(KeyCode code)
UXAssist/Patches/Logistics/CapacityPatch.cs:164:    private static void UIStationStorage_RefreshValues_Prefix()
UXAssist/Patches/Logistics/CapacityPatch.cs:171:    private static void UIStationStorage_RefreshValues_Postfix()
UXAssist/Patches/Logistics/CapacityPatch.cs:178:    private static void UIControlPanelStationStorage_RefreshValues_Prefix()
UXAssist/Patches/Logistics/CapacityPatch.cs:185:    private static void UIControlPanelStationStorage_RefreshValues_Postfix()
UXAssist/Patches/Logistics/CapacityPatch.cs:192:    private static bool UIStationStorage_OnMaxSliderValueChange_Prefix()
UXAssist/Patches/Logistics/CapacityPatch.cs:201:    private static bool UIControlPanelStationStorage_OnMaxSliderValueChange_Prefix()
UXAssist/Patches/Logistics/CapacityPatch.cs:210:    private static bool PlanetTransport_OnTechFunctionUnlocked_Prefix(PlanetTransport __instance, int _funcId, double _valuelf, int _level)
UXAssist/Patches/Logistics/CapacityPatch.cs:292:    private static IEnumerable<CodeInstruction> UIStationWindow_OnStationIdChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Logistics/CapacityPatch.cs:362:    private static IEnumerable<CodeInstruction> UIStationWindow_OnMaxMiningSpeedChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Logistics/CapacityPatch.cs:392:    private static IEnumerable<CodeInstruction> UIStationWindow_OnMaxChargePowerSliderValueChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Logistics/LogisticsPatch.cs:236:    private static void ForEachStation(StationKind kind, Action<PlanetFactory, StationComponent> action)
UXAssist/Patches/Logistics/LogisticsPatch.cs:259:    private static void ForEachDispenser(Action<PlanetFactory, DispenserComponent> action)
UXAssist/Patches/Logistics/LogisticsPatch.cs:274:    private static void ForEachBattleBase(Action<PlanetFactory, BattleBaseComponent> action)
UXAssist/Patches/Logistics/OverflowPatch.cs:11:    private static bool _blueprintPasting;
UXAssist/Patches/Logistics/OverflowPatch.cs:17:    private static IEnumerable<CodeInstruction> UIStationStorage_OnItemIconMouseDown_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Logistics/OverflowPatch.cs:44:    private static IEnumerable<CodeInstruction> PlanetTransport_SetStationStorage_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Logistics/OverflowPatch.cs:71:    private static void BuildTool_BlueprintPaste_CreatePrebuilds_Prefix()
UXAssist/Patches/Logistics/OverflowPatch.cs:78:    private static void BuildTool_BlueprintPaste_CreatePrebuilds_Postfix()
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:29:    private static int ItemIdHintUnderMouse()
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:82:    private static bool SetFilterItemId(UIControlPanelFilterPanel filterPanel, int itemId)
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:93:    private static IEnumerable<CodeInstruction> UIGame_On_I_Switch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:120:    private static void OnStationEntryItemIconRightClick(UIControlPanelStationEntry stationEntry, int slot)
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:140:    private static void UIControlPanelStationEntry__OnRegEvent_Postfix(UIControlPanelStationEntry __instance)
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:158:    private static void UIControlPanelStationEntry__OnUnregEvent_Postfix(UIControlPanelStationEntry __instance)
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:172:    private static StationTip[] _stationTips = new StationTip[16];
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:175:    private static int _stationTipsRecycleCount;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:176:    private static GameObject _stationTipsRoot;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:177:    private static GameObject _tipPrefab;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:183:    private static int _lastPlanetId;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:185:    private static int _localStorageMax = LogisticsConstants.DefaultLocalStorageMax;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:186:    private static int _remoteStorageMax = LogisticsConstants.DefaultRemoteStorageMax;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:187:    private static int _localStorageExtra;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:188:    private static int _remoteStorageExtra;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:189:    private static int _localStorageMaxTotal = _localStorageMax;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:190:    private static int _remoteStorageMaxTotal = _remoteStorageMax;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:191:    private static float _localStoragePixelPerItem = LogisticsConstants.StorageSliderWidth / _localStorageMaxTotal;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:192:    private static float _remoteStoragePixelPerItem = LogisticsConstants.StorageSliderWidth / _remoteStorageMaxTotal;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:194:    private static int _storageMaxSlotCount = LogisticsConstants.DefaultStorageSlotCount;
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:198:    private static bool UpdateStorageMax()
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:492:    private static void ReleaseStationTip(StationTip stationTip)
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:513:    private static void RecycleStationTips()
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:522:    private static void RecycleStationTip(int index)
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:530:    private static void HideAndRecycleStationTips()
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:537:    private static StationTip AllocateStationTip()
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:693:        private static void GameData_localPlanet_Setter_Prefix(GameData __instance, PlanetData value)
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:861:        private static Sprite GetItemSprite(int itemId)
UXAssist/Patches/PersistPatch.cs:26:    private static IEnumerable<CodeInstruction> UIBuildMenu__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:46:    private static IEnumerable<CodeInstruction> UIButton_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:70:    private static IEnumerable<CodeInstruction> BuildTool_BlueprintCopy_UseToPasteNow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:87:    private static void BlueprintData_SaveBlueprintData_Prefix(BlueprintData __instance)
UXAssist/Patches/PersistPatch.cs:98:    private static IEnumerable<CodeInstruction> UIProductEntry_UpdateUIElements_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:110:    private static IEnumerable<CodeInstruction> UIProductEntry_OnInputValueEnd_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:123:    private static IEnumerable<CodeInstruction> PlayerOrder_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:137:    private static IEnumerable<CodeInstruction> PlayerOrder_ExtendCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:150:    private static IEnumerable<CodeInstruction> UIGame__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:170:    private static bool UIDFCommunicatorWindow_Determine_Prefix()
UXAssist/Patches/PersistPatch.cs:178:    private static IEnumerable<CodeInstruction> NeutronStarHandler_OnEnable_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:200:    private static IEnumerable<CodeInstruction> GameLogic_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:219:    private static void UniverseSimulator_SetPlanetSimulator_Postfix(UniverseSimulator __instance, PlanetSimulator sim)
UXAssist/Patches/PersistPatch.cs:228:    private static void MilkyWayWebClient_OnUploadLoginErrored_Postfix(MilkyWayWebClient __instance, DSPWeb.HTTP_ERROR_TYPE errorType, string errorInfo, int errorCode)
UXAssist/Patches/PersistPatch.cs:249:    private static void MilkyWayWebClient_OnUploadErrored_Postfix(MilkyWayWebClient __instance, DSPWeb.HTTP_ERROR_TYPE errorType, string errorInfo, int errorCode)
UXAssist/Patches/PersistPatch.cs:270:    private static void MilkyWayWebClient_OnUploadSucceed_Postfix(MilkyWayWebClient __instance, DownloadHandler handler)
UXAssist/Patches/PersistPatch.cs:279:    private static IEnumerable<CodeInstruction> MilkyWayCache_LoadTopTenPlayerData_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PersistPatch.cs:315:    private static void MilkyWayCache_LoadTopTenPlayerData_Postfix(MilkyWayCache __instance)
UXAssist/Patches/PlanetPatch.cs:32:        private static IEnumerable<CodeInstruction> VFInput_UpdateGameStates_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PlanetPatch.cs:55:        private static IEnumerable<CodeInstruction> PlayerController_GetInput_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PlanetPatch.cs:68:        private static IEnumerable<CodeInstruction> PlayerAction_Rts_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PlayerPatch.cs:20:    private static PressKeyBind _showAllStarsNameKey;
UXAssist/Patches/PlayerPatch.cs:21:    private static PressKeyBind _toggleAllStarsNameKey;
UXAssist/Patches/PlayerPatch.cs:22:    private static PressKeyBind _autoDriveKey;
UXAssist/Patches/PlayerPatch.cs:87:    private static IEnumerable<CodeInstruction> UIStarmapStar__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PlayerPatch.cs:131:        private static IEnumerable<CodeInstruction> UIReplicatorWindow_OnOkButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PlayerPatch.cs:144:        private static IEnumerable<CodeInstruction> UIReplicatorWindow_OnPlusButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PlayerPatch.cs:182:        private static IEnumerable<CodeInstruction> Player_SetSandCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PlayerPatch.cs:221:        private static void UIStarmap__OnOpen_Prefix()
UXAssist/Patches/PlayerPatch.cs:228:                private static IEnumerable<CodeInstruction> UIStarmapPlanet__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PlayerPatch.cs:276:                private static IEnumerable<CodeInstruction> UIStarmapDFHive__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PlayerPatch.cs:328:        private static bool _canUseWarper;
UXAssist/Patches/PlayerPatch.cs:329:        private static int _indicatorAstroId;
UXAssist/Patches/PlayerPatch.cs:330:        private static bool _speedUp;
UXAssist/Patches/PlayerPatch.cs:331:        private static Vector3 _direction;
UXAssist/Patches/PlayerPatch.cs:332:        private static EMovementState _movementState = EMovementState.Walk;
UXAssist/Patches/PlayerPatch.cs:354:        private static bool UpdateMovementState(PlayerController controller)
UXAssist/Patches/PlayerPatch.cs:366:        private static IEnumerable<CodeInstruction> PlayerController_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/PlayerPatch.cs:565:        private static IEnumerable<CodeInstruction> VFInput_sailSpeedUp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/TechPatch.cs:43:        private static bool _protoPatched;
UXAssist/Patches/TechPatch.cs:59:        private static void TryPatchProto(bool on)
UXAssist/Patches/TechPatch.cs:143:        private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
UXAssist/Patches/TechPatch.cs:149:    private static class DisableBattleRelatedTechsInPeaceMode
UXAssist/Patches/TechPatch.cs:151:        private static bool _protoPatched;
UXAssist/Patches/TechPatch.cs:152:        private static HashSet<int> _techsToDisableSet;
UXAssist/Patches/TechPatch.cs:153:        private static Dictionary<int, TechProto[]> _originTechPosts = [];
UXAssist/Patches/TechPatch.cs:171:        private static void OnGameBegin()
UXAssist/Patches/TechPatch.cs:176:        private static void TryPatchProto(bool on)
UXAssist/Patches/TechPatch.cs:268:        private static IEnumerable<CodeInstruction> UITechNode_UpdateInfoDynamic_Transpiler(IEnumerable<CodeInstruction> instructions)
UXAssist/Patches/TechPatch.cs:284:        private static bool UITechNode_OnBuyoutButtonClick_Prefix(UITechNode __instance)
UXAssist/Patches/UIPatch.cs:40:        private static VeinTypeInfo[] planetVeinCount = null;
UXAssist/Patches/UIPatch.cs:41:        private static VeinTypeInfo[] starVeinCount = null;
UXAssist/Patches/UIPatch.cs:97:        private static Vector2 GetAdjustedSizeDelta(Vector2 origSizeDelta)
UXAssist/Patches/UIPatch.cs:123:        private static void ProcessVeinData(VeinTypeInfo[] veinCount, VeinData[] veinPool)
UXAssist/Patches/UIPatch.cs:156:        private static void FormatResource(int refId, UIResAmountEntry uiresAmountEntry, VeinTypeInfo vt)
UXAssist/Patches/UIPatch.cs:181:        private static void InitializeVeinCountArray(VeinTypeInfo[] veinCountArray)
UXAssist/Patches/UIPatch.cs:341:    private static void PlanetData_NotifyScanEnded_Postfix(PlanetData __instance)
UXAssist/Patches/UIPatch.cs:349:    private static void UIPlanetGlobe_DistributeButtons_Postfix(UIPlanetGlobe __instance)
UXAssist/Patches/UIPatch.cs:356:    private static bool UIStarmapStar_OnStarDisplayNameChange_Prefix()
UXAssist/Patches/UIPatch.cs:363:    private static void UIStarmapStar__OnClose_Postfix(UIStarmapStar __instance)
UXAssist/Patches/UIPatch.cs:370:    private static bool UIMechaLab_DetermineVisible_Prefix(UIMechaLab __instance, ref bool __result)
UXAssist/Patches/UIPatch.cs:383:    private static bool UIGoalPanel_DetermineVisiable_Prefix(UIGoalPanel __instance)
UXAssist/Functions/FactoryFunctions.cs:161:    private static HashSet<int> _itemIsBelt = null;
UXAssist/Functions/FactoryFunctions.cs:162:    private static Dictionary<int, int> _upgradeTypes = null;
UXAssist/Functions/TechFunctions.cs:14:    private static void CheckTechUnlockProperties(GameHistoryData history, TechProto techProto, SortedList<int, int> properties, List<Tuple<TechProto, int, int>> techList, int maxLevel = 10000, bool withPrerequisites = true, HashSet<int> seenTechs = null)
UXAssist/Functions/TechFunctions.cs:77:    private static int UnlockTechImpl(GameHistoryData history, TechProto techProto, int maxLevel = 10000, bool withPrerequisites = true)
UXAssist/Functions/TechFunctions.cs:141:    private static bool UnlockTechImmediately(TechProto techProto, int maxLevel = 10000, bool withPrerequisites = true)
UXAssist/Functions/UI/MenuButtonUI.cs:12:    private static bool _initialized;
UXAssist/Functions/UI/MenuButtonUI.cs:13:    private static PressKeyBind _toggleKey;
UXAssist/Functions/UI/MenuButtonUI.cs:14:    private static bool _configWinInitialized;
UXAssist/Functions/UI/MenuButtonUI.cs:15:    private static MyConfigWindow _configWin;
UXAssist/Functions/UI/MenuButtonUI.cs:16:    private static GameObject _buttonOnPlanetGlobe;
UXAssist/Functions/UI/MilkyWayUI.cs:16:    private static int _clusterUploadResultsHead = 0;
UXAssist/Functions/UI/MilkyWayUI.cs:17:    private static int _clusterUploadResultsCount = 0;
UXAssist/Functions/UI/MilkyWayUI.cs:26:    private static ClusterPlayerData[] _topTenPlayerData = null;
UXAssist/Functions/UI/StarmapFilterUI.cs:13:    private static int _cornerComboBoxIndex;
UXAssist/Functions/UI/StarmapFilterUI.cs:14:    private static string[] _starOrderNames;
UXAssist/Functions/UI/StarmapFilterUI.cs:15:    private static bool _starmapFilterInitialized;
UXAssist/Functions/UI/StarmapFilterUI.cs:16:    private static ulong[] _starmapStarFilterValues;
UXAssist/Functions/UI/StarmapFilterUI.cs:17:    private static bool _starFilterEnabled;
UXAssist/Functions/UI/StarmapFilterUI.cs:398:    private static void StarmapUpdateFilterValues()
UXAssist/Functions/UI/StarmapFilterUI.cs:499:    private static void SetStarFilterEnabled(bool enabled)
UXAssist/Functions/UI/StarmapFilterUI.cs:508:    private static void UpdateStarmapStarNames()
UXAssist/Functions/WindowFunctions.cs:10:    private static bool _initialized;
UXAssist/Functions/WindowFunctions.cs:14:    private static string _gameWindowTitle = "Dyson Sphere Program";
UXAssist/Functions/WindowFunctions.cs:16:    private static IntPtr _gameWindowHandle = IntPtr.Zero;
UXAssist/Functions/WindowFunctions.cs:42:    private static void ApplyProcessPriority()
UXAssist/Functions/WindowFunctions.cs:49:    private static string GetPriorityName(int priority)
CheatEnabler/Patches/CombatPatch.cs:39:        private static IEnumerable<CodeInstruction> Player_get_invincible_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/CombatPatch.cs:53:        private static IEnumerable<CodeInstruction> SkillSystem_DamageObject_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
CheatEnabler/Patches/CombatPatch.cs:75:        private static IEnumerable<CodeInstruction> SkillSystem_DamageObject_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:24:    private static bool _instantAbsorb;
CheatEnabler/Patches/DysonSpherePatch.cs:66:    // private static void DysonShell_ImportFromBlueprint_Postfix(DysonShell __instance)
CheatEnabler/Patches/DysonSpherePatch.cs:73:    private static IEnumerable<CodeInstruction> DysonNode_OrderConstructCp_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:88:    private static IEnumerable<CodeInstruction> DysonSwarm_AbsorbSail_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:107:        private static long _sailLifeTime;
CheatEnabler/Patches/DysonSpherePatch.cs:108:        private static DysonSailCache[][] _sailsCache;
CheatEnabler/Patches/DysonSpherePatch.cs:109:        private static int[] _sailsCacheLen, _sailsCacheCapacity;
CheatEnabler/Patches/DysonSpherePatch.cs:110:        private static bool _fireAllBullets;
CheatEnabler/Patches/DysonSpherePatch.cs:147:        private static void UpdateSailLifeTime()
CheatEnabler/Patches/DysonSpherePatch.cs:153:        private static void UpdateSailsCacheForThisGame()
CheatEnabler/Patches/DysonSpherePatch.cs:165:        private static void SetSailsCacheCapacity(int index, int capacity)
CheatEnabler/Patches/DysonSpherePatch.cs:177:        private static void OnGameBegin()
CheatEnabler/Patches/DysonSpherePatch.cs:185:        private static void GameHistoryData_SetForNewGame_Postfix(int func)
CheatEnabler/Patches/DysonSpherePatch.cs:195:        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:218:        private static void AddDysonSail(ref EjectorComponent ejector, DysonSwarm swarm, VectorLF3 uPos, VectorLF3 endVec, int[] consumeRegister)
CheatEnabler/Patches/DysonSpherePatch.cs:380:        private static IEnumerable<CodeInstruction> DysonSwarm_AbsorbSail_Transpiler2(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:422:        private static IEnumerable<CodeInstruction> DysonSphereLayer_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:439:        private static void DoAbsorb(DysonSphereLayer layer, long gameTick)
CheatEnabler/Patches/DysonSpherePatch.cs:463:        private static IEnumerable<CodeInstruction> EjectorComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:493:        private static IEnumerable<CodeInstruction> EjectComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:516:        private static IEnumerable<CodeInstruction> UIEjectAndSiloWindow__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:546:        private static IEnumerable<CodeInstruction> SiloComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:569:        private static IEnumerable<CodeInstruction> UIEjectAndSiloWindow__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/DysonSpherePatch.cs:626:        private static IEnumerable<CodeInstruction> MaxOrbitRadiusPatch_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/Factory/ArchitectModePatch.cs:8:    private static bool[] _canBuildItems;
CheatEnabler/Patches/Factory/ArchitectModePatch.cs:50:    private static void DoInit()
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:14:    private static Dictionary<int, BeltSignal>[] _signalBelts;
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:15:    private static Dictionary<long, int> _portalFrom;
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:16:    private static Dictionary<int, HashSet<long>> _portalTo;
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:17:    private static int _signalBeltsCapacity;
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:18:    private static bool _initialized;
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:108:    private static void InitSignalBelts()
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:153:    private static Dictionary<int, BeltSignal> GetOrCreateSignalBelts(int index)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:176:    private static Dictionary<int, BeltSignal> GetSignalBelts(int index)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:181:    private static void SetSignalBelt(int factory, int beltId, int signalId, int number)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:236:    private static void AddSourcesToBeltSignal(BeltSignal beltSignal)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:295:    private static void SetSignalBeltPortalTo(int factory, int beltId, int number)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:308:    private static void RemoveSignalBelt(int factory, int beltId)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:313:    private static void RemovePlanetSignalBelts(int factory)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:318:    private static void RemoveSignalBeltPortalEnd(int factory, int beltId)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:327:    private static void OnGameBegin()
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:335:    private static void DigitalSystem_Constructor_Postfix(PlanetData _planet)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:450:    private static void ProcessBeltSignals()
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:600:    private static bool _itemSourcesInitialized;
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:609:    private static void InitItemSources()
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:703:    private static void CalculateAllProductions(IDictionary<int, float> result, IDictionary<int, float> extra, ref float sprayedCount, int itemId, float count = 1f)
CheatEnabler/Patches/Factory/FactoryPatch.cs:36:    private static PressKeyBind _noConditionKey;
CheatEnabler/Patches/Factory/FactoryPatch.cs:37:    private static PressKeyBind _noCollisionKey;
CheatEnabler/Patches/Factory/FactoryPatch.cs:113:    private static void OnDataLoaded()
CheatEnabler/Patches/Factory/FactoryPatch.cs:186:    private static void PlanetData_NotifyFactoryLoaded_Postfix(PlanetData __instance)
CheatEnabler/Patches/Factory/FactoryPatch.cs:195:    private static void OnGameBegin_For_ImmBuild()
CheatEnabler/Patches/Factory/FactoryPatch.cs:208:    private static IEnumerable<CodeInstruction> WarningSystem_hasCriticalWarning_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/Factory/FactoryPatch.cs:234:    private static IEnumerable<CodeInstruction> WarningSystem_UpdateCriticalWarningText_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:10:    private static bool _isBatchBuilding;
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:11:    private static bool _disableRefreshBatchesBuffers;
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:12:    private static bool _anyBelt;
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:80:    private static bool CargoTraffic_AlterBeltRenderer_Prefix(int beltId)
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:90:    private static bool CargoTraffic_AlterPathRenderer_Prefix(int pathId)
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:100:    private static bool CargoTraffic_RefreshPathUV_Prefix(int pathId)
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:111:    private static bool CargoTraffic_RefreshBeltBatchesBuffers_Prefix()
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:134:    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:165:    private static void GameLogic_FactoryConstructionSystemGameTick_Prefix(GameLogic __instance)
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:251:    private static void UXAssist_PlanetFunctions_BuildOrbitalCollectors_Postfix()
CheatEnabler/Patches/Factory/LogisticsControlPatch.cs:14:    private static IEnumerable<CodeInstruction> UIControlPanelDispenserInspector_OnItemIconMouseDown_Transpiler(IEnumerable<CodeInstruction> instructions)
CheatEnabler/Patches/Factory/LogisticsControlPatch.cs:43:    private static IEnumerable<CodeInstruction> UIControlPanelStationInspector_OnShipIconClick_Transpiler(IEnumerable<CodeInstruction> instructions)
CheatEnabler/Patches/Factory/LogisticsControlPatch.cs:70:    private static IEnumerable<CodeInstruction> UIControlPanelStationStorage_OnItemIconMouseDown_Transpiler(IEnumerable<CodeInstruction> instructions)
CheatEnabler/Patches/Factory/LogisticsControlPatch.cs:97:    private static IEnumerable<CodeInstruction> UIControlPanelStationStorage_OnTakeBackButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions)
CheatEnabler/Patches/Factory/LogisticsControlPatch.cs:116:    private static IEnumerable<CodeInstruction> UIControlPanelVeinCollectorPanel_OnProductIconClick_Transpiler(IEnumerable<CodeInstruction> instructions)
CheatEnabler/Patches/Factory/NoConditionBuildPatch.cs:23:    private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
CheatEnabler/Patches/Factory/NoConditionBuildPatch.cs:31:    private static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/Factory/NoConditionBuildPatch.cs:67:    private static IEnumerable<CodeInstruction> BuildTool_Click_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/Factory/PowerBoostPatch.cs:15:    private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
CheatEnabler/Patches/Factory/PowerBoostPatch.cs:44:    private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_Wind_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/Factory/PowerBoostPatch.cs:69:    private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_PV_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/Factory/PowerBoostPatch.cs:93:    private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_Fuel_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/Factory/PowerBoostPatch.cs:134:    private static IEnumerable<CodeInstruction> PowerGeneratorComponent_EnergyCap_GTH_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/Factory/PowerBoostPatch.cs:157:    private static bool _patched;
CheatEnabler/Patches/Factory/PowerBoostPatch.cs:158:    private static PrefabDesc _prefabdesc;
CheatEnabler/Patches/Factory/PowerBoostPatch.cs:159:    private static float _oldCoverRadius;
CheatEnabler/Patches/Factory/PowerBoostPatch.cs:160:    private static float _oldConnectDistance;
CheatEnabler/Patches/GamePatch.cs:42:        private static Dictionary<int, AbnormalityDeterminator> _savedDeterminators;
CheatEnabler/Patches/GamePatch.cs:76:        private static bool DisableAbnormalLogic()
CheatEnabler/Patches/GamePatch.cs:83:        private static void DisableAbnormalDeterminators(AbnormalityLogic __instance)
CheatEnabler/Patches/GamePatch.cs:97:        private static PlayerAction_Test _test;
CheatEnabler/Patches/GamePatch.cs:111:        private static void PlayerController_Init_Postfix(PlayerController __instance)
CheatEnabler/Patches/GamePatch.cs:129:        private static void PlayerAction_Test_GameTick_Postfix(PlayerAction_Test __instance)
CheatEnabler/Patches/GamePatch.cs:136:        private static IEnumerable<CodeInstruction> PlayerAction_Test_Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/GamePatch.cs:169:        private static IEnumerable<CodeInstruction> GameCamera_Logic_Transpiler(IEnumerable<CodeInstruction> instructions)
CheatEnabler/Patches/GamePatch.cs:195:        private static void UnlockTechRecursive(GameHistoryData history, [NotNull] TechProto techProto, int maxLevel = 10000)
CheatEnabler/Patches/GamePatch.cs:258:        private static void OnClickTech(UITechNode node)
CheatEnabler/Patches/GamePatch.cs:297:        private static IEnumerable<CodeInstruction> UITechNode_OnPointerDown_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/PlanetPatch.cs:40:        private static IEnumerable<CodeInstruction> BuildTool_CheckBuildConditions_Transpiler(
CheatEnabler/Patches/PlanetPatch.cs:61:        private static IEnumerable<CodeInstruction> BuildTool_BlueprintPaste_DetermineReforms_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/PlanetPatch.cs:85:        private static IEnumerable<CodeInstruction> BuildTool_Reform_RemoveBasePit_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/PlanetPatch.cs:111:        private static IEnumerable<CodeInstruction> UIRemoveBasePitButton_OnRemoveButtonClick_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/PlanetPatch.cs:147:        private static IEnumerable<CodeInstruction> BuildTool_Reform_ReformAction_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/PlayerPatch.cs:42:        private static void ForgeTask_Ctor_Postfix(ForgeTask __instance)
CheatEnabler/Patches/PlayerPatch.cs:60:        private static IEnumerable<CodeInstruction> UIGlobemap__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/PlayerPatch.cs:75:        private static bool Mecha_HasWarper_Prefix(ref bool __result)
CheatEnabler/Patches/PlayerPatch.cs:83:        private static void Mecha_UseWarper_Postfix(ref bool __result)
CheatEnabler/Patches/ResourcePatch.cs:48:        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Patches/ResourcePatch.cs:100:        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:54:    private static double RawToDouble(ulong value)
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:62:    private static float RawToFloat(uint value)
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:18:    private static void EnsureDysonShellMaps()
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:25:    private static void ResetLayerPools(DysonSphereLayer layer)
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:44:    private static void FinalizeDysonSphereChanges(global::DysonSphere sphere, DysonSphereLayer layer, bool notify = false, bool resetRenderMasks = false)
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:325:    private static bool CreateIllegalDysonShellWithMaxOutputForLayer(DysonSphereLayer layer)
CheatEnabler/Functions/PlayerFunctions.cs:63:    private static void PurgePropertySystem(PropertySystem propertySystem)
UniverseGenTweaks/BirthPlanetPatch.cs:23:    private static BackupData _backupData;
UniverseGenTweaks/BirthPlanetPatch.cs:24:    private static bool _initialized;
UniverseGenTweaks/BirthPlanetPatch.cs:25:    private static Harmony _patch;
UniverseGenTweaks/BirthPlanetPatch.cs:90:    private static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
UniverseGenTweaks/BirthPlanetPatch.cs:95:    private static void PatchBirthThemeData()
UniverseGenTweaks/EpicDifficulty.cs:19:    private static Harmony _harmony;
UniverseGenTweaks/EpicDifficulty.cs:35:    private static void Enable(bool on)
UniverseGenTweaks/EpicDifficulty.cs:86:    private static void PatchGalaxyUI_OnInit(UIGalaxySelect __instance)
UniverseGenTweaks/EpicDifficulty.cs:93:    private static bool UIGalaxySelect_OnResourceMultiplierValueChange_Prefix(UIGalaxySelect __instance, float val)
UniverseGenTweaks/EpicDifficulty.cs:117:    private static bool UIGalaxySelect_UpdateParametersUIDisplay_Prefix(UIGalaxySelect __instance)
UniverseGenTweaks/EpicDifficulty.cs:157:    private static IEnumerable<CodeInstruction> GameDesc_get_oilAmountMultiplier_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UniverseGenTweaks/Patches/CombatSettingsPatch.cs:13:    private static Harmony _patch;
UniverseGenTweaks/Patches/CombatSettingsPatch.cs:83:    private static void OnEnabledChanged(object sender, EventArgs e)
UniverseGenTweaks/Patches/CombatSettingsPatch.cs:101:    private static void UICombatSettingsDF__OnCreate_Postfix(UICombatSettingsDF __instance)
UniverseGenTweaks/Patches/CombatSettingsPatch.cs:120:    private static bool SliderChanged_Prefix(UICombatSettingsDF __instance, MethodInfo __originalMethod)
UniverseGenTweaks/Patches/CombatSettingsPatch.cs:136:    private static bool UICombatSettingsDF_UpdateUIParametersDisplay_Prefix(UICombatSettingsDF __instance)
UniverseGenTweaks/Patches/CombatSettingsPatch.cs:318:    private static bool CombatSettings_difficulty_Getter_Prefix(CombatSettings __instance, ref float __result)
UniverseGenTweaks/Patches/CombatSettingsPatch.cs:420:    private static float MapSlider(float value, float[] thresholds, float[] values)
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:24:    private static Harmony _patch;
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:25:    private static Harmony _permanentPatch;
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:42:    private static void OnEnabledChanged(object sender, System.EventArgs e)
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:58:    private static class Patch
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:62:        private static IEnumerable<CodeInstruction> UIGalaxySelect_OnStarCountSliderValueChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:75:    private static class PermanentPatch
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:79:        private static void GameMain_Start_Prefix()
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:100:        private static IEnumerable<CodeInstruction> GalaxyData_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:114:        private static IEnumerable<CodeInstruction> SectorModel_CreateGalaxyAstroBuffer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:127:        private static IEnumerable<CodeInstruction> UniverseGen_CreateGalaxy_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:157:        private static IEnumerable<CodeInstruction> UniverseGen_RandomPoses_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:170:        private static IEnumerable<CodeInstruction> UIVirtualStarmap__OnLateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:199:        private static IEnumerable<CodeInstruction> UIGalaxySelect_UpdateUIDisplay_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:14:    private static Text _minDistTitle;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:15:    private static Text _minStepTitle;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:16:    private static Text _maxStepTitle;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:17:    private static Text _flattenTitle;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:18:    private static Slider _minDistSlider;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:19:    private static Slider _minStepSlider;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:20:    private static Slider _maxStepSlider;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:21:    private static Slider _flattenSlider;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:22:    private static Text _minDistText;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:23:    private static Text _minStepText;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:24:    private static Text _maxStepText;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:25:    private static Text _flattenText;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:26:    private static Harmony _patch;
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:40:    private static void OnEnabledChanged(object sender, System.EventArgs e)
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:56:    private static class Patch
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:60:        private static void UIGalaxySelect__OnCreate_Postfix(UIGalaxySelect __instance)
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:95:        private static void UIGalaxySelect__OnOpen_Prefix(UIGalaxySelect __instance)
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:109:        private static void UIGalaxySelect__OnUnregEvent_Postfix()
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:118:    private static void CreateSliderWithText(Slider orig, out Text title, out Slider slider, out Text text, out Localizer loc)
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:127:    private static void TransformDeltaY(Transform trans, float delta)
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:134:    private static void UpdateSliderControls()
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:160:    private static void RemoveAllListeners()
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:168:    private static void AddListeners(UIGalaxySelect uiGalaxySelect)
UniverseGenTweaks/UIConfigWindow.cs:10:    private static RectTransform _windowTrans;
UniverseGenTweaks/UIConfigWindow.cs:17:    private static void CreateUI(MyConfigWindow wnd, RectTransform trans)
UXAssist/Patches/DysonSpherePatch.cs:14:    public static ConfigEntry<bool> StopEjectOnNodeCompleteEnabled;
UXAssist/Patches/DysonSpherePatch.cs:15:    public static ConfigEntry<bool> OnlyConstructNodesEnabled;
UXAssist/Patches/DysonSpherePatch.cs:16:    public static ConfigEntry<int> AutoConstructMultiplier;
UXAssist/Patches/DysonSpherePatch.cs:20:    public static void Init()
UXAssist/Patches/DysonSpherePatch.cs:30:    public static void Start()
UXAssist/Patches/DysonSpherePatch.cs:36:    public static void Uninit()
UXAssist/Patches/Factory/ArchitectModePatch.cs:12:    public static void Enable(bool enable)
UXAssist/Patches/Factory/BeltSignalPatch.cs:15:    public static void Enable(bool enable)
UXAssist/Patches/Factory/BeltSignalPatch.cs:20:    public static void InitPersist()
UXAssist/Patches/Factory/BeltSignalPatch.cs:25:    public static void UninitPersist()
UXAssist/Patches/Factory/BeltSignalPatch.cs:30:    public static void Export(BinaryWriter w)
UXAssist/Patches/Factory/BeltSignalPatch.cs:37:    public static void Import(BinaryReader r)
UXAssist/Patches/Factory/BeltSignalPatch.cs:55:        public static void InitPersist()
UXAssist/Patches/Factory/BeltSignalPatch.cs:60:        public static void UninitPersist()
UXAssist/Patches/Factory/BeltSignalPatch.cs:273:            public static void CargoTraffic_RemoveBeltComponent_Prefix(int id)
UXAssist/Patches/Factory/BeltSignalPatch.cs:282:            public static void CargoTraffic_SetBeltSignalIcon_Postfix(CargoTraffic __instance, int entityId, int signalId)
UXAssist/Patches/Factory/BeltSignalPatch.cs:302:        public static void GameLogic_OnFactoryFrameBegin_Postfix()
UXAssist/Patches/Factory/BuildingBufferPatch.cs:10:    public static void Enable(bool enable)
UXAssist/Patches/Factory/BuildingBufferPatch.cs:17:        public static void RefreshAssemblerBufferMultipliers()
UXAssist/Patches/Factory/BuildingBufferPatch.cs:26:        public static void RefreshLabBufferMaxCountForAssemble()
UXAssist/Patches/Factory/BuildingBufferPatch.cs:35:        public static void RefreshLabBufferMaxCountForResearch()
UXAssist/Patches/Factory/BuildingBufferPatch.cs:44:        public static void RefreshReceiverBufferCount()
UXAssist/Patches/Factory/BuildingBufferPatch.cs:53:        public static void RefreshEjectorBufferCount()
UXAssist/Patches/Factory/BuildingBufferPatch.cs:62:        public static void RefreshSiloBufferCount()
UXAssist/Patches/Factory/BuildToolPatch.cs:15:    public static void Enable(bool enable)
UXAssist/Patches/Factory/BuildToolPatch.cs:195:        public static IEnumerable<CodeInstruction> AllowOffGridConstruction(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:212:        public static IEnumerable<CodeInstruction> PreventDraggingWhenOffGrid(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:239:        public static IEnumerable<CodeInstruction> AllowOffGridConstructionForPath(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:283:        public static IEnumerable<CodeInstruction> PatchToPerformSteppedRotate(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
UXAssist/Patches/Factory/BuildToolPatch.cs:321:        public static void RotateStepped(BuildTool_Click instance)
UXAssist/Patches/Factory/BuildToolPatch.cs:381:        public static void AlternatelyChanged()
UXAssist/Patches/Factory/FactoryPatch.cs:13:public static class FactoryPatch
UXAssist/Patches/Factory/FactoryPatch.cs:15:    public static ConfigEntry<bool> UnlimitInteractiveEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:16:    public static ConfigEntry<bool> RemoveSomeConditionEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:17:    public static ConfigEntry<bool> NightLightEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:18:    public static ConfigEntry<float> NightLightAngleX;
UXAssist/Patches/Factory/FactoryPatch.cs:19:    public static ConfigEntry<float> NightLightAngleY;
UXAssist/Patches/Factory/FactoryPatch.cs:20:    public static ConfigEntry<bool> RemoveBuildRangeLimitEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:21:    public static ConfigEntry<bool> LargerAreaForUpgradeAndDismantleEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:22:    public static ConfigEntry<bool> LargerAreaForTerraformEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:23:    public static ConfigEntry<bool> OffGridBuildingEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:24:    public static ConfigEntry<bool> TreatStackingAsSingleEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:25:    public static ConfigEntry<bool> QuickBuildAndDismantleLabsEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:26:    public static ConfigEntry<bool> ProtectVeinsFromExhaustionEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:27:    public static ConfigEntry<bool> DoNotRenderEntitiesEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:28:    public static ConfigEntry<bool> DragBuildPowerPolesEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:29:    public static ConfigEntry<bool> DragBuildPowerPolesAlternatelyEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:30:    public static ConfigEntry<bool> AutoConstructButtonEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:31:    public static ConfigEntry<bool> AutoConstructEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:32:    public static ConfigEntry<bool> BeltSignalsForBuyOutEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:33:    public static ConfigEntry<bool> TankFastFillInAndTakeOutEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:34:    public static ConfigEntry<int> TankFastFillInAndTakeOutMultiplier;
UXAssist/Patches/Factory/FactoryPatch.cs:35:    public static ConfigEntry<bool> CutConveyorBeltEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:36:    public static ConfigEntry<bool> TweakBuildingBufferEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:37:    public static ConfigEntry<int> AssemblerBufferTimeMultiplier;
UXAssist/Patches/Factory/FactoryPatch.cs:38:    public static ConfigEntry<int> AssemblerBufferMininumMultiplier;
UXAssist/Patches/Factory/FactoryPatch.cs:39:    public static ConfigEntry<int> LabBufferMaxCountForAssemble;
UXAssist/Patches/Factory/FactoryPatch.cs:40:    public static ConfigEntry<int> LabBufferExtraCountForAdvancedAssemble;
UXAssist/Patches/Factory/FactoryPatch.cs:41:    public static ConfigEntry<int> LabBufferMaxCountForResearch;
UXAssist/Patches/Factory/FactoryPatch.cs:42:    public static ConfigEntry<int> ReceiverBufferCount;
UXAssist/Patches/Factory/FactoryPatch.cs:43:    public static ConfigEntry<int> EjectorBufferCount;
UXAssist/Patches/Factory/FactoryPatch.cs:44:    public static ConfigEntry<int> SiloBufferCount;
UXAssist/Patches/Factory/FactoryPatch.cs:45:    public static ConfigEntry<bool> ShortcutKeysForBlueprintCopyEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:46:    public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsEnabled;
UXAssist/Patches/Factory/FactoryPatch.cs:47:    public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsIncludeBranches;
UXAssist/Patches/Factory/FactoryPatch.cs:48:    public static ConfigEntry<bool> PressShiftToTakeWholeBeltItemsIncludeInserters;
UXAssist/Patches/Factory/FactoryPatch.cs:58:    public static void Init()
UXAssist/Patches/Factory/FactoryPatch.cs:137:    public static void Start()
UXAssist/Patches/Factory/FactoryPatch.cs:161:    public static void Uninit()
UXAssist/Patches/Factory/FactoryPatch.cs:191:    public static void OnInputUpdate()
UXAssist/Patches/Factory/FactoryPatch.cs:215:    public static void Export(BinaryWriter w)
UXAssist/Patches/Factory/FactoryPatch.cs:220:    public static void Import(BinaryReader r, ushort version)
UXAssist/Patches/Factory/ImmediateBuildPatch.cs:12:    public static void Enable(bool enable)
UXAssist/Patches/Factory/RenderingPatch.cs:14:    public static void Enable(bool enable)
UXAssist/Patches/Factory/RenderingPatch.cs:116:        public static void UpdateSunlightAngle()
UXAssist/Patches/Factory/RenderingPatch.cs:126:        public static void GameMain_LateUpdate_Postfix(GameMain __instance)
UXAssist/Patches/Factory/VeinProtectionPatch.cs:10:    public static void Enable(bool enable)
UXAssist/Patches/Factory/VeinProtectionPatch.cs:15:    public static void InitConfig()
UXAssist/Patches/Factory/VeinProtectionPatch.cs:22:        public static int KeepVeinAmount = 100;
UXAssist/Patches/Factory/VeinProtectionPatch.cs:23:        public static float KeepOilSpeed = 1f;
UXAssist/Patches/Factory/VeinProtectionPatch.cs:26:        public static void InitConfig()
UXAssist/Patches/GamePatch.cs:19:    public static ConfigEntry<bool> EnableWindowResizeEnabled;
UXAssist/Patches/GamePatch.cs:20:    public static ConfigEntry<bool> LoadLastWindowRectEnabled;
UXAssist/Patches/GamePatch.cs:22:    // public static ConfigEntry<bool> AutoSaveOptEnabled;
UXAssist/Patches/GamePatch.cs:23:    public static ConfigEntry<bool> ConvertSavesFromPeaceEnabled;
UXAssist/Patches/GamePatch.cs:24:    public static ConfigEntry<Vector4> LastWindowRect;
UXAssist/Patches/GamePatch.cs:25:    public static ConfigEntry<bool> ProfileBasedSaveFolderEnabled;
UXAssist/Patches/GamePatch.cs:26:    public static ConfigEntry<bool> ProfileBasedOptionEnabled;
UXAssist/Patches/GamePatch.cs:27:    public static ConfigEntry<string> DefaultProfileName;
UXAssist/Patches/GamePatch.cs:28:    public static ConfigEntry<double> GameUpsFactor;
UXAssist/Patches/GamePatch.cs:34:    public static bool EnableGameUpsFactor
UXAssist/Patches/GamePatch.cs:58:    public static void Init()
UXAssist/Patches/GamePatch.cs:103:    public static void Start()
UXAssist/Patches/GamePatch.cs:122:    public static void Uninit()
UXAssist/Patches/GamePatch.cs:130:    public static void OnInputUpdate()
UXAssist/Patches/GamePatch.cs:149:    public static void GameConfig_gameSaveFolder_Postfix(ref string __result)
UXAssist/Patches/GamePatch.cs:211:    public static IEnumerator SetWindowPositionCoroutine(IntPtr wnd, int x, int y)
UXAssist/Patches/GamePatch.cs:342:        public static void UILoadGameWindow_RefreshList_Postfix(UILoadGameWindow __instance)
UXAssist/Patches/GamePatch.cs:404:        public static void UISaveGameWindow_RefreshList_Postfix(UISaveGameWindow __instance)
UXAssist/Patches/Logistics/LogisticsPatch.cs:14:public static class LogisticsPatch
UXAssist/Patches/Logistics/LogisticsPatch.cs:16:    public static ConfigEntry<bool> AutoConfigLogisticsEnabled;
UXAssist/Patches/Logistics/LogisticsPatch.cs:17:    public static ConfigEntry<bool> AutoConfigLimitAutoReplenishCount;
UXAssist/Patches/Logistics/LogisticsPatch.cs:19:    public static ConfigEntry<int> AutoConfigDispenserChargePower; // 3~30, display as 300000.0 * value
UXAssist/Patches/Logistics/LogisticsPatch.cs:20:    public static ConfigEntry<int> AutoConfigDispenserCourierCount; // 0~10
UXAssist/Patches/Logistics/LogisticsPatch.cs:22:    public static ConfigEntry<int> AutoConfigBattleBaseChargePower; // 4~40, display as 300000.0 * value
UXAssist/Patches/Logistics/LogisticsPatch.cs:24:    public static ConfigEntry<int> AutoConfigPLSChargePower; // 2~20, display as 3000000.0 * value
UXAssist/Patches/Logistics/LogisticsPatch.cs:25:    public static ConfigEntry<int> AutoConfigPLSMaxTripDrone; // 1~180, by degress
UXAssist/Patches/Logistics/LogisticsPatch.cs:26:    public static ConfigEntry<int> AutoConfigPLSDroneMinDeliver; // 0~10; 0 = 1%, 1~10 = 10% *value
UXAssist/Patches/Logistics/LogisticsPatch.cs:27:    public static ConfigEntry<int> AutoConfigPLSMinPilerValue; // 0~4; 0 = Maximum in tech, 1~4 = piler stacking count
UXAssist/Patches/Logistics/LogisticsPatch.cs:28:    public static ConfigEntry<int> AutoConfigPLSDroneCount; // 0~50
UXAssist/Patches/Logistics/LogisticsPatch.cs:30:    public static ConfigEntry<bool> SetDefaultRemoteLogicToStorage;
UXAssist/Patches/Logistics/LogisticsPatch.cs:31:    public static ConfigEntry<int> AutoConfigILSChargePower; // 2~20, display as 15000000.0 * value
UXAssist/Patches/Logistics/LogisticsPatch.cs:32:    public static ConfigEntry<int> AutoConfigILSMaxTripDrone; // 1~180, by degress
UXAssist/Patches/Logistics/LogisticsPatch.cs:33:    public static ConfigEntry<int> AutoConfigILSMaxTripShip; // 1~41; 1~20 = value LY, 21-40 = 2*value-20LY, 41 = Unlimited
UXAssist/Patches/Logistics/LogisticsPatch.cs:34:    public static ConfigEntry<int> AutoConfigILSWarperDistance; // 2~21; 2~7 = value * 0.5 - 0.5AU, 8~16 = value - 4AU, 17~20 = value * 2 - 20AU, 21 = 60AU
UXAssist/Patches/Logistics/LogisticsPatch.cs:35:    public static ConfigEntry<int> AutoConfigILSDroneMinDeliver; // 0~10; 0 = 1%, 1~10 = 10% *value
UXAssist/Patches/Logistics/LogisticsPatch.cs:36:    public static ConfigEntry<int> AutoConfigILSShipMinDeliver; // 0~10; 0 = 1%, 1~10 = 10% *value
UXAssist/Patches/Logistics/LogisticsPatch.cs:37:    public static ConfigEntry<int> AutoConfigILSMinPilerValue; // 0~4; 0 = Maximum in tech, 1~4 = piler stacking count
UXAssist/Patches/Logistics/LogisticsPatch.cs:38:    public static ConfigEntry<bool> AutoConfigILSIncludeOrbitCollector;
UXAssist/Patches/Logistics/LogisticsPatch.cs:39:    public static ConfigEntry<bool> AutoConfigILSWarperNecessary;
UXAssist/Patches/Logistics/LogisticsPatch.cs:40:    public static ConfigEntry<int> AutoConfigILSDroneCount; // 0~100
UXAssist/Patches/Logistics/LogisticsPatch.cs:41:    public static ConfigEntry<int> AutoConfigILSShipCount; // 0~10
UXAssist/Patches/Logistics/LogisticsPatch.cs:43:    public static ConfigEntry<int> AutoConfigVeinCollectorHarvestSpeed; // 0-20, 100% + 10% * value
UXAssist/Patches/Logistics/LogisticsPatch.cs:44:    public static ConfigEntry<int> AutoConfigVeinCollectorMinPilerValue; // 0~4; 0 = Maximum in tech, 1~4 = piler stacking count
UXAssist/Patches/Logistics/LogisticsPatch.cs:46:    public static ConfigEntry<bool> LogisticsCapacityTweaksEnabled;
UXAssist/Patches/Logistics/LogisticsPatch.cs:47:    public static ConfigEntry<bool> AllowOverflowInLogisticsEnabled;
UXAssist/Patches/Logistics/LogisticsPatch.cs:48:    public static ConfigEntry<bool> GreaterPowerUsageInLogisticsEnabled;
UXAssist/Patches/Logistics/LogisticsPatch.cs:49:    public static ConfigEntry<bool> LogisticsConstrolPanelImprovementEnabled;
UXAssist/Patches/Logistics/LogisticsPatch.cs:50:    public static ConfigEntry<bool> RealtimeLogisticsInfoPanelEnabled;
UXAssist/Patches/Logistics/LogisticsPatch.cs:51:    public static ConfigEntry<bool> RealtimeLogisticsInfoPanelBarsEnabled;
UXAssist/Patches/Logistics/LogisticsPatch.cs:53:    public static void Init()
UXAssist/Patches/Logistics/LogisticsPatch.cs:66:    public static void Start()
UXAssist/Patches/Logistics/LogisticsPatch.cs:82:    public static void Uninit()
UXAssist/Patches/Logistics/LogisticsPatch.cs:97:    public static void OnUpdate() => RealtimeInfoPanelPatch.OnUpdate();
UXAssist/Patches/Logistics/LogisticsPatch.cs:99:    public static void OnInputUpdate()
UXAssist/Patches/Logistics/LogisticsPatch.cs:306:    public static void ApplyDispenserChargePower() => ForEachDispenser(DispenserSetChargePower);
UXAssist/Patches/Logistics/LogisticsPatch.cs:307:    public static void ApplyDispenserCourierCount() => ForEachDispenser(DispenserFillCouriers);
UXAssist/Patches/Logistics/LogisticsPatch.cs:308:    public static void ApplyAllDispenser() => ForEachDispenser((f, d) => { DispenserSetChargePower(f, d); DispenserFillCouriers(f, d); });
UXAssist/Patches/Logistics/LogisticsPatch.cs:311:    public static void ApplyBattleBaseChargePower() => ForEachBattleBase(BattleBaseSetChargePower);
UXAssist/Patches/Logistics/LogisticsPatch.cs:312:    public static void ApplyAllBattleBase() => ForEachBattleBase(BattleBaseSetChargePower);
UXAssist/Patches/Logistics/LogisticsPatch.cs:315:    public static void ApplyPLSChargePower() => ForEachStation(StationKind.Pls, StationSetChargePower);
UXAssist/Patches/Logistics/LogisticsPatch.cs:316:    public static void ApplyPLSTripRangeDrones() => ForEachStation(StationKind.Pls, StationSetTripRangeDrones);
UXAssist/Patches/Logistics/LogisticsPatch.cs:317:    public static void ApplyPLSDroneMinDeliver() => ForEachStation(StationKind.Pls, StationSetDeliveryDrones);
UXAssist/Patches/Logistics/LogisticsPatch.cs:318:    public static void ApplyPLSMinPilerValue() => ForEachStation(StationKind.Pls, StationSetPilerCount);
UXAssist/Patches/Logistics/LogisticsPatch.cs:319:    public static void ApplyPLSDroneCount() => ForEachStation(StationKind.Pls, StationFillDrones);
UXAssist/Patches/Logistics/LogisticsPatch.cs:320:    public static void ApplyAllPLS() => ForEachStation(StationKind.Pls, DoConfigStation);
UXAssist/Patches/Logistics/LogisticsPatch.cs:323:    public static void ApplyILSChargePower() => ForEachStation(StationKind.Ils, StationSetChargePower);
UXAssist/Patches/Logistics/LogisticsPatch.cs:324:    public static void ApplyILSTripRangeDrones() => ForEachStation(StationKind.Ils, StationSetTripRangeDrones);
UXAssist/Patches/Logistics/LogisticsPatch.cs:325:    public static void ApplyILSTripRangeShips() => ForEachStation(StationKind.Ils, StationSetTripRangeShips);
UXAssist/Patches/Logistics/LogisticsPatch.cs:326:    public static void ApplyILSWarpDistance() => ForEachStation(StationKind.Ils, StationSetWarpDistance);
UXAssist/Patches/Logistics/LogisticsPatch.cs:327:    public static void ApplyILSDroneMinDeliver() => ForEachStation(StationKind.Ils, StationSetDeliveryDrones);
UXAssist/Patches/Logistics/LogisticsPatch.cs:328:    public static void ApplyILSShipMinDeliver() => ForEachStation(StationKind.Ils, StationSetDeliveryShips);
UXAssist/Patches/Logistics/LogisticsPatch.cs:329:    public static void ApplyILSMinPilerValue() => ForEachStation(StationKind.Ils, StationSetPilerCount);
UXAssist/Patches/Logistics/LogisticsPatch.cs:330:    public static void ApplyILSDroneCount() => ForEachStation(StationKind.Ils, StationFillDrones);
UXAssist/Patches/Logistics/LogisticsPatch.cs:331:    public static void ApplyILSShipCount() => ForEachStation(StationKind.Ils, StationFillShips);
UXAssist/Patches/Logistics/LogisticsPatch.cs:332:    public static void ApplyILSIncludeOrbitCollector() => ForEachStation(StationKind.Ils, StationSetIncludeOrbitCollector);
UXAssist/Patches/Logistics/LogisticsPatch.cs:333:    public static void ApplyILSWarperNecessary() => ForEachStation(StationKind.Ils, StationSetWarperNecessary);
UXAssist/Patches/Logistics/LogisticsPatch.cs:334:    public static void ApplyAllILS() => ForEachStation(StationKind.Ils, DoConfigStation);
UXAssist/Patches/Logistics/LogisticsPatch.cs:337:    public static void ApplyVeinCollectorHarvestSpeed() => ForEachStation(StationKind.VeinCollector, (f, s) => VeinCollectorSetHarvestSpeed(f, s));
UXAssist/Patches/Logistics/LogisticsPatch.cs:338:    public static void ApplyVeinCollectorMinPilerValue() => ForEachStation(StationKind.VeinCollector, VeinCollectorSetPilerCount);
UXAssist/Patches/Logistics/LogisticsPatch.cs:339:    public static void ApplyAllVeinCollector() => ForEachStation(StationKind.VeinCollector, DoConfigStation);
UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs:18:    public static void OnUpdate()
UXAssist/Patches/PersistPatch.cs:13:    public static void Start()
UXAssist/Patches/PersistPatch.cs:18:    public static void Uninit()
UXAssist/Patches/PlanetPatch.cs:9:public static class PlanetPatch
UXAssist/Patches/PlanetPatch.cs:11:    public static ConfigEntry<bool> PlayerActionsInGlobeViewEnabled;
UXAssist/Patches/PlanetPatch.cs:13:    public static void Init()
UXAssist/Patches/PlanetPatch.cs:18:    public static void Start()
UXAssist/Patches/PlanetPatch.cs:23:    public static void Uninit()
UXAssist/Patches/PlayerPatch.cs:13:    public static ConfigEntry<bool> EnhancedMechaForgeCountControlEnabled;
UXAssist/Patches/PlayerPatch.cs:14:    public static ConfigEntry<bool> HideTipsForSandsChangesEnabled;
UXAssist/Patches/PlayerPatch.cs:15:    public static ConfigEntry<bool> ShortcutKeysForStarsNameEnabled;
UXAssist/Patches/PlayerPatch.cs:16:    public static ConfigEntry<bool> AutoNavigationEnabled;
UXAssist/Patches/PlayerPatch.cs:17:    public static ConfigEntry<bool> AutoCruiseEnabled;
UXAssist/Patches/PlayerPatch.cs:18:    public static ConfigEntry<bool> AutoBoostEnabled;
UXAssist/Patches/PlayerPatch.cs:19:    public static ConfigEntry<double> DistanceToWarp;
UXAssist/Patches/PlayerPatch.cs:24:    public static void Init()
UXAssist/Patches/PlayerPatch.cs:57:    public static void Start()
UXAssist/Patches/PlayerPatch.cs:66:    public static void OnInputUpdate()
UXAssist/Patches/PlayerPatch.cs:75:    public static void Uninit()
UXAssist/Patches/PlayerPatch.cs:194:        public static int ShowAllStarsNameStatus;
UXAssist/Patches/PlayerPatch.cs:195:        public static bool ForceShowAllStarsName;
UXAssist/Patches/PlayerPatch.cs:196:        public static bool ForceShowAllStarsNameExternal;
UXAssist/Patches/PlayerPatch.cs:198:        public static void ToggleAllStarsName()
UXAssist/Patches/PlayerPatch.cs:203:        public static void OnInputUpdate()
UXAssist/Patches/PlayerPatch.cs:334:        public static int IndicatorAstroId => _indicatorAstroId;
UXAssist/Patches/PlayerPatch.cs:345:        public static void ToggleAutoCruise()
UXAssist/Patches/PlayerPatch.cs:581:        public static void OnOpen_Prefix()
UXAssist/Patches/TechPatch.cs:14:public static class TechPatch
UXAssist/Patches/TechPatch.cs:16:    public static ConfigEntry<bool> SorterCargoStackingEnabled;
UXAssist/Patches/TechPatch.cs:17:    public static ConfigEntry<bool> DisableBattleRelatedTechsInPeaceModeEnabled;
UXAssist/Patches/TechPatch.cs:18:    public static ConfigEntry<bool> BatchBuyoutTechEnabled;
UXAssist/Patches/TechPatch.cs:20:    public static void Init()
UXAssist/Patches/TechPatch.cs:27:    public static void Start()
UXAssist/Patches/TechPatch.cs:34:    public static void Uninit()
UXAssist/Patches/TechPatch.cs:45:        public static void Enable(bool enable)
UXAssist/Patches/TechPatch.cs:155:        public static void Enable(bool enable)
UXAssist/Patches/UIPatch.cs:15:    public static ConfigEntry<bool> PlanetVeinUtilizationEnabled;
UXAssist/Patches/UIPatch.cs:17:    public static void Init()
UXAssist/Patches/UIPatch.cs:22:    public static void Start()
UXAssist/Patches/UIPatch.cs:31:    public static void Uninit()
UXAssist/Patches/UIPatch.cs:44:        public static void OnGameBegin()
UXAssist/Patches/UIPatch.cs:192:        public static void UIPlanetDetail_OnPlanetDataSet_Prefix(UIPlanetDetail __instance)
UXAssist/Patches/UIPatch.cs:201:        public static void UIPlanetDetail_RefreshDynamicProperties_Postfix(UIPlanetDetail __instance)
UXAssist/Patches/UIPatch.cs:255:        public static void UIStaretail_OnStarDataSet_Prefix(UIStarDetail __instance)
UXAssist/Patches/UIPatch.cs:264:        public static void UIStarDetail_RefreshDynamicProperties_Postfix(UIStarDetail __instance)
UXAssist/Patches/UIPatch.cs:334:    public static void UIRoot__OnOpen_Postfix()
UXAssist/Functions/DysonSphereFunctions.cs:3:public static class DysonSphereFunctions
UXAssist/Functions/DysonSphereFunctions.cs:5:    public static StarData CurrentStarForDysonSystem()
UXAssist/Functions/DysonSphereFunctions.cs:16:    public static void InitCurrentDysonLayer(StarData star, int layerId)
UXAssist/Functions/FactoryFunctions.cs:6:public static class FactoryFunctions
UXAssist/Functions/FactoryFunctions.cs:8:    public static void CutConveyorBelt(CargoTraffic cargoTraffic, int beltId)
UXAssist/Functions/FactoryFunctions.cs:27:    public static bool ObjectIsBeltOrInserter(PlanetFactory factory, int objId)
UXAssist/Functions/FactoryFunctions.cs:34:    public static void DismantleBlueprintSelectedBuildings()
UXAssist/Functions/FactoryFunctions.cs:124:    public static void SelectAllBuildingsInBlueprintCopy()
UXAssist/Functions/FactoryFunctions.cs:164:    public static void SortBlueprintData(BlueprintData blueprintData)
UXAssist/Functions/PlanetFunctions.cs:9:public static class PlanetFunctions
UXAssist/Functions/PlanetFunctions.cs:11:    public static ConfigEntry<int> OrbitalCollectorMaxBuildCount;
UXAssist/Functions/PlanetFunctions.cs:12:    public static ConfigEntry<bool> ReturnBuildingsOnInitializeEnabled;
UXAssist/Functions/PlanetFunctions.cs:13:    public static ConfigEntry<bool> ReturnLogisticStorageItemsOnInitializeEnabled;
UXAssist/Functions/PlanetFunctions.cs:14:    public static ConfigEntry<bool> ReturnBeltAFactoryItemsOnInitializeEnabled;
UXAssist/Functions/PlanetFunctions.cs:18:    public static void DismantleAll(bool toBag)
UXAssist/Functions/PlanetFunctions.cs:65:    public static void RecreatePlanet(bool revertReform)
UXAssist/Functions/PlanetFunctions.cs:680:    public static void BuildOrbitalCollectors()
UXAssist/Functions/TechFunctions.cs:8:public static class TechFunctions
UXAssist/Functions/TechFunctions.cs:10:    public static void Init()
UXAssist/Functions/TechFunctions.cs:152:    public static void UnlockAllProtoWithMetadataAndPrompt()
UXAssist/Functions/TechFunctions.cs:166:    public static void UnlockProtoWithMetadataAndPrompt(TechProto[] techProtos, int toLevel, bool withPrerequisites = true)
UXAssist/Functions/TechFunctions.cs:276:    public static void RemoveCargoStackingTechs()
UXAssist/Functions/UI/AutoConstructUI.cs:11:    public static MyCheckButton ToggleAutoConstruct;
UXAssist/Functions/UI/AutoConstructUI.cs:12:    public static GameObject ConstructCountPanel;
UXAssist/Functions/UI/AutoConstructUI.cs:13:    public static Text ConstructCountText;
UXAssist/Functions/UI/AutoConstructUI.cs:15:    public static void Init()
UXAssist/Functions/UI/AutoConstructUI.cs:19:    public static void Start()
UXAssist/Functions/UI/AutoConstructUI.cs:23:    public static void Uninit()
UXAssist/Functions/UI/AutoConstructUI.cs:27:    public static void OnInputUpdate()
UXAssist/Functions/UI/AutoConstructUI.cs:31:    public static void OnUpdate()
UXAssist/Functions/UI/AutoConstructUI.cs:35:    public static void InitToggleAutoConstructCheckButton()
UXAssist/Functions/UI/AutoConstructUI.cs:90:    public static void UpdateToggleAutoConstructCheckButtonVisiblility()
UXAssist/Functions/UI/AutoConstructUI.cs:99:    public static void UpdateConstructCountText(int count)
UXAssist/Functions/UI/AutoCruiseUI.cs:9:    public static MyCheckButton ToggleAutoCruise;
UXAssist/Functions/UI/AutoCruiseUI.cs:11:    public static void Init()
UXAssist/Functions/UI/AutoCruiseUI.cs:15:    public static void Start()
UXAssist/Functions/UI/AutoCruiseUI.cs:19:    public static void Uninit()
UXAssist/Functions/UI/AutoCruiseUI.cs:23:    public static void OnInputUpdate()
UXAssist/Functions/UI/AutoCruiseUI.cs:27:    public static void OnUpdate()
UXAssist/Functions/UI/AutoCruiseUI.cs:31:    public static void InitToggleAutoCruiseCheckButton()
UXAssist/Functions/UI/AutoCruiseUI.cs:58:    public static void UpdateToggleAutoCruiseCheckButtonVisiblility()
UXAssist/Functions/UI/MenuButtonUI.cs:18:    public static void Init()
UXAssist/Functions/UI/MenuButtonUI.cs:30:    public static void Start()
UXAssist/Functions/UI/MenuButtonUI.cs:34:    public static void Uninit()
UXAssist/Functions/UI/MenuButtonUI.cs:38:    public static void OnInputUpdate()
UXAssist/Functions/UI/MenuButtonUI.cs:46:    public static void OnUpdate()
UXAssist/Functions/UI/MenuButtonUI.cs:50:    public static void ToggleConfigWindow()
UXAssist/Functions/UI/MenuButtonUI.cs:69:    public static void InitMenuButtons()
UXAssist/Functions/UI/MenuButtonUI.cs:133:    public static void RecreateConfigWindow()
UXAssist/Functions/UI/MenuButtonUI.cs:143:    public static void UpdateGlobeButtonPosition(UIPlanetGlobe planetGlobe)
UXAssist/Functions/UI/MilkyWayUI.cs:29:    public static MyCheckButton MilkyWayTopTenPlayersToggler;
UXAssist/Functions/UI/MilkyWayUI.cs:30:    public static event Action OnMilkyWayTopTenPlayersUpdated;
UXAssist/Functions/UI/MilkyWayUI.cs:32:    public static void Init()
UXAssist/Functions/UI/MilkyWayUI.cs:36:    public static void Start()
UXAssist/Functions/UI/MilkyWayUI.cs:40:    public static void Uninit()
UXAssist/Functions/UI/MilkyWayUI.cs:44:    public static void OnInputUpdate()
UXAssist/Functions/UI/MilkyWayUI.cs:48:    public static void OnUpdate()
UXAssist/Functions/UI/MilkyWayUI.cs:52:    public static void AddClusterUploadResult(int result, float requestTime)
UXAssist/Functions/UI/MilkyWayUI.cs:69:    public static void Export(BinaryWriter w)
UXAssist/Functions/UI/MilkyWayUI.cs:85:    public static void Import(BinaryReader r)
UXAssist/Functions/UI/MilkyWayUI.cs:101:    public static void ClearClusterUploadResults()
UXAssist/Functions/UI/MilkyWayUI.cs:110:    public static void ShowRecentMilkywayUploadResults()
UXAssist/Functions/UI/MilkyWayUI.cs:131:    public static void SetTopPlayerCount(int count)
UXAssist/Functions/UI/MilkyWayUI.cs:136:    public static void SetTopPlayerData(int index, ref ClusterPlayerData playerData)
UXAssist/Functions/UI/MilkyWayUI.cs:142:    public static void UpdateMilkyWayTopTenPlayers()
UXAssist/Functions/UI/MilkyWayUI.cs:148:    public static void InitMilkyWayTopTenPlayers()
UXAssist/Functions/UI/StarmapFilterUI.cs:18:    public static MyCheckButton StarmapFilterToggler;
UXAssist/Functions/UI/StarmapFilterUI.cs:19:    public static bool[] ShowStarName;
UXAssist/Functions/UI/StarmapFilterUI.cs:61:    public static void Init()
UXAssist/Functions/UI/StarmapFilterUI.cs:65:    public static void Start()
UXAssist/Functions/UI/StarmapFilterUI.cs:69:    public static void Uninit()
UXAssist/Functions/UI/StarmapFilterUI.cs:73:    public static void OnInputUpdate()
UXAssist/Functions/UI/StarmapFilterUI.cs:77:    public static void OnUpdate()
UXAssist/Functions/UI/StarmapFilterUI.cs:81:    public static void InitStarmapButtons()
UXAssist/Functions/UI/StarmapFilterUI.cs:391:    public static void OnPlanetScanEnded()
UXAssist/Functions/UI/StarmapFilterUI.cs:488:    public static int CornerComboBoxIndex
UXAssist/Functions/UIFunctions.cs:8:public static class UIFunctions
UXAssist/Functions/UIFunctions.cs:10:    public static void Init()
UXAssist/Functions/UIFunctions.cs:22:    public static void Start()
UXAssist/Functions/UIFunctions.cs:31:    public static void Uninit()
UXAssist/Functions/UIFunctions.cs:40:    public static void OnInputUpdate()
UXAssist/Functions/UIFunctions.cs:49:    public static void OnUpdate()
UXAssist/Functions/UIFunctions.cs:58:    public static void InitMenuButtons()
UXAssist/Functions/UIFunctions.cs:63:    public static void InitMilkyWayTopTenPlayers()
UXAssist/Functions/UIFunctions.cs:68:    public static void UpdateGlobeButtonPosition(UIPlanetGlobe planetGlobe)
UXAssist/Functions/UIFunctions.cs:73:    public static void UpdateToggleAutoCruiseCheckButtonVisiblility()
UXAssist/Functions/UIFunctions.cs:78:    public static void UpdateToggleAutoConstructCheckButtonVisiblility()
UXAssist/Functions/UIFunctions.cs:83:    public static void UpdateConstructCountText(int count)
UXAssist/Functions/UIFunctions.cs:88:    public static void OnPlanetScanEnded()
UXAssist/Functions/UIFunctions.cs:93:    public static void AddClusterUploadResult(int result, float requestTime)
UXAssist/Functions/UIFunctions.cs:98:    public static void ExportClusterUploadResults(BinaryWriter w)
UXAssist/Functions/UIFunctions.cs:103:    public static void ImportClusterUploadResults(BinaryReader r, ushort version)
UXAssist/Functions/UIFunctions.cs:111:    public static void ClearClusterUploadResults()
UXAssist/Functions/UIFunctions.cs:116:    public static void ShowRecentMilkywayUploadResults()
UXAssist/Functions/UIFunctions.cs:121:    public static void SetTopPlayerCount(int count)
UXAssist/Functions/UIFunctions.cs:126:    public static void SetTopPlayerData(int index, ref ClusterPlayerData playerData)
UXAssist/Functions/UIFunctions.cs:131:    public static void UpdateMilkyWayTopTenPlayers()
UXAssist/Functions/WindowFunctions.cs:8:public static class WindowFunctions
UXAssist/Functions/WindowFunctions.cs:11:    public static string ProfileName { get; private set; }
UXAssist/Functions/WindowFunctions.cs:18:    public static ConfigEntry<int> ProcessPriority;
UXAssist/Functions/WindowFunctions.cs:29:    public static void Init()
UXAssist/Functions/WindowFunctions.cs:36:    public static void Start()
UXAssist/Functions/WindowFunctions.cs:62:    public static void SetWindowTitle()
UXAssist/Functions/WindowFunctions.cs:97:    public static IntPtr FindGameWindow()
CheatEnabler/Patches/CombatPatch.cs:12:public static class CombatPatch
CheatEnabler/Patches/CombatPatch.cs:14:    public static ConfigEntry<bool> MechaInvincibleEnabled;
CheatEnabler/Patches/CombatPatch.cs:15:    public static ConfigEntry<bool> BuildingsInvincibleEnabled;
CheatEnabler/Patches/CombatPatch.cs:17:    public static void Init()
CheatEnabler/Patches/CombatPatch.cs:23:    public static void Start()
CheatEnabler/Patches/CombatPatch.cs:29:    public static void Uninit()
CheatEnabler/Patches/DysonSpherePatch.cs:15:    public static ConfigEntry<bool> SkipBulletEnabled;
CheatEnabler/Patches/DysonSpherePatch.cs:16:    public static ConfigEntry<bool> FireAllBulletsEnabled;
CheatEnabler/Patches/DysonSpherePatch.cs:17:    public static ConfigEntry<bool> SkipAbsorbEnabled;
CheatEnabler/Patches/DysonSpherePatch.cs:18:    public static ConfigEntry<bool> QuickAbsorbEnabled;
CheatEnabler/Patches/DysonSpherePatch.cs:19:    public static ConfigEntry<bool> EjectAnywayEnabled;
CheatEnabler/Patches/DysonSpherePatch.cs:20:    public static ConfigEntry<bool> OverclockEjectorEnabled;
CheatEnabler/Patches/DysonSpherePatch.cs:21:    public static ConfigEntry<bool> OverclockSiloEnabled;
CheatEnabler/Patches/DysonSpherePatch.cs:22:    public static ConfigEntry<bool> UnlockMaxOrbitRadiusEnabled;
CheatEnabler/Patches/DysonSpherePatch.cs:23:    public static ConfigEntry<float> UnlockMaxOrbitRadiusValue;
CheatEnabler/Patches/DysonSpherePatch.cs:26:    public static void Init()
CheatEnabler/Patches/DysonSpherePatch.cs:39:    public static void Start()
CheatEnabler/Patches/DysonSpherePatch.cs:52:    public static void Uninit()
CheatEnabler/Patches/DysonSpherePatch.cs:112:        public static void SetFireAllBullets(bool value)
CheatEnabler/Patches/DysonSpherePatch.cs:302:        public static void DysonSwarm_GameTick_Prefix(DysonSwarm __instance, long time)
CheatEnabler/Patches/DysonSpherePatch.cs:609:        public static void OnViewStarChange(object o, EventArgs e)
CheatEnabler/Patches/Factory/ArchitectModePatch.cs:24:    public static bool TakeTailItemsPatch(StorageComponent __instance, int itemId)
CheatEnabler/Patches/Factory/ArchitectModePatch.cs:38:    public static void GetItemCountPatch(StorageComponent __instance, int itemId, ref int __result)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:347:    public static void CargoTraffic_RemoveBeltComponent_Prefix(int id)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:358:    public static void CargoTraffic_SetBeltSignalIcon_Postfix(CargoTraffic __instance, int signalId, int entityId)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:409:    public static void CargoTraffic_SetBeltSignalNumber_Postfix(CargoTraffic __instance, float number, int entityId)
CheatEnabler/Patches/Factory/BeltSignalPatch.cs:589:    public static void GameLogic_OnFactoryFrameBegin_Postfix()
CheatEnabler/Patches/Factory/FactoryPatch.cs:18:    public static ConfigEntry<bool> ImmediateEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:19:    public static ConfigEntry<bool> ArchitectModeEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:20:    public static ConfigEntry<bool> NoConditionEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:21:    public static ConfigEntry<bool> NoCollisionEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:22:    public static ConfigEntry<bool> BeltSignalGeneratorEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:23:    public static ConfigEntry<bool> BeltSignalNumberAltFormat;
CheatEnabler/Patches/Factory/FactoryPatch.cs:24:    public static ConfigEntry<bool> BeltSignalCountGenEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:25:    public static ConfigEntry<bool> BeltSignalCountRemEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:26:    public static ConfigEntry<bool> BeltSignalCountRecipeEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:27:    public static ConfigEntry<bool> BeltSignalUseProliferatorEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:28:    public static ConfigEntry<bool> RemovePowerSpaceLimitEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:29:    public static ConfigEntry<bool> BoostWindPowerEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:30:    public static ConfigEntry<bool> BoostSolarPowerEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:31:    public static ConfigEntry<bool> BoostFuelPowerEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:32:    public static ConfigEntry<bool> BoostGeothermalPowerEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:33:    public static ConfigEntry<bool> WindTurbinesPowerGlobalCoverageEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:34:    public static ConfigEntry<bool> ControlPanelRemoteLogisticsEnabled;
CheatEnabler/Patches/Factory/FactoryPatch.cs:40:    public static void Init()
CheatEnabler/Patches/Factory/FactoryPatch.cs:74:    public static void Start()
CheatEnabler/Patches/Factory/FactoryPatch.cs:94:    public static void Uninit()
CheatEnabler/Patches/Factory/FactoryPatch.cs:119:    public static void OnInputUpdate()
CheatEnabler/Patches/Factory/FactoryPatch.cs:149:    public static void ArrivePlanet(PlanetFactory factory)
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:17:    public static bool IsBatchBuilding => _isBatchBuilding;
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:19:    public static void StartBatchBuilding(PlanetFactory factory)
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:29:    public static void EndBatchBuilding(PlanetFactory factory)
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:64:    public static void TryEndBatchBuilding(PlanetFactory factory)
CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs:70:    public static void InstantBuild(Player player, PlanetFactory factory, int id)
CheatEnabler/Patches/Factory/NoConditionBuildPatch.cs:82:    public static bool CheckForMiner(BuildTool tool)
CheatEnabler/Patches/Factory/PowerBoostPatch.cs:164:    public static void Enable(bool enable)
CheatEnabler/Patches/GamePatch.cs:13:public static class GamePatch
CheatEnabler/Patches/GamePatch.cs:15:    public static ConfigEntry<bool> DevShortcutsEnabled;
CheatEnabler/Patches/GamePatch.cs:16:    public static ConfigEntry<bool> AbnormalDisablerEnabled;
CheatEnabler/Patches/GamePatch.cs:17:    public static ConfigEntry<bool> UnlockTechEnabled;
CheatEnabler/Patches/GamePatch.cs:19:    public static void Init()
CheatEnabler/Patches/GamePatch.cs:26:    public static void Start()
CheatEnabler/Patches/GamePatch.cs:33:    public static void Uninit()
CheatEnabler/Patches/PlanetPatch.cs:12:public static class PlanetPatch
CheatEnabler/Patches/PlanetPatch.cs:14:    public static ConfigEntry<bool> WaterPumpAnywhereEnabled;
CheatEnabler/Patches/PlanetPatch.cs:15:    public static ConfigEntry<bool> TerraformAnywayEnabled;
CheatEnabler/Patches/PlanetPatch.cs:17:    public static void Init()
CheatEnabler/Patches/PlanetPatch.cs:23:    public static void Start()
CheatEnabler/Patches/PlanetPatch.cs:29:    public static void Uninit()
CheatEnabler/Patches/PlayerPatch.cs:11:public static class PlayerPatch
CheatEnabler/Patches/PlayerPatch.cs:13:    public static ConfigEntry<bool> InstantHandCraftEnabled;
CheatEnabler/Patches/PlayerPatch.cs:14:    public static ConfigEntry<bool> InstantTeleportEnabled;
CheatEnabler/Patches/PlayerPatch.cs:15:    public static ConfigEntry<bool> WarpWithoutSpaceWarpersEnabled;
CheatEnabler/Patches/PlayerPatch.cs:17:    public static void Init()
CheatEnabler/Patches/PlayerPatch.cs:24:    public static void Start()
CheatEnabler/Patches/PlayerPatch.cs:31:    public static void Uninit()
CheatEnabler/Patches/ResourcePatch.cs:11:public static class ResourcePatch
CheatEnabler/Patches/ResourcePatch.cs:13:    public static ConfigEntry<bool> InfiniteResourceEnabled;
CheatEnabler/Patches/ResourcePatch.cs:14:    public static ConfigEntry<bool> FastMiningEnabled;
CheatEnabler/Patches/ResourcePatch.cs:16:    public static void Init()
CheatEnabler/Patches/ResourcePatch.cs:22:    public static void Start()
CheatEnabler/Patches/ResourcePatch.cs:28:    public static void Uninit()
CheatEnabler/Functions/DysonSphere/DysonSphereResolver.cs:10:public static class DysonSphereResolver
CheatEnabler/Functions/DysonSphere/DysonSphereResolver.cs:12:    public static (global::DysonSphere sphere, StarData star)? ResolveCurrent()
CheatEnabler/Functions/DysonSphere/DysonSphereResolver.cs:21:    public static StarData ResolveEditorOrLocalStar()
CheatEnabler/Functions/DysonSphere/DysonSphereResolver.cs:36:    public static (global::DysonSphere sphere, StarData star)? ResolveEditorOrLocalSphere(bool requireLayer = true)
CheatEnabler/Functions/DysonSphere/FrameRemovalFunctions.cs:12:public static class FrameRemovalFunctions
CheatEnabler/Functions/DysonSphere/FrameRemovalFunctions.cs:14:    public static void RemoveAllFrames()
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:11:public static class GeometryHelpers
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:70:    public static DysonNode QuickAddDysonNode(this DysonSphereLayer layer, int protoId, Vector3 pos)
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:111:    public static DysonFrame QuickAddDysonFrame(this DysonSphereLayer layer, int protoId, DysonNode nodeA, DysonNode nodeB, bool euler)
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:157:    public static int CalculateTriangleVertCount(Vector3[] pos)
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:294:    public static bool GenerateCustomShellGeometry(this DysonShell shell)
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:665:    public static int QuickAddDysonShell(this DysonSphereLayer layer, int protoId, DysonNode[] nodes, DysonFrame[] frames, bool limit)
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:743:    public static void QuickRemoveDysonNode(this DysonSphereLayer layer, int nodeId)
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:759:    public static void QuickRemoveDysonFrame(this DysonSphereLayer layer, int frameId)
CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs:770:    public static int AlignUpToPowerOfTwo(int value)
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:16:public static class IllegalShellFunctions
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:60:    public static void DuplicateShellsWithHighestProduction()
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:146:    public static void KeepMaxProductionShells()
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:251:    public static void CreateIllegalDysonShellQuickly(int triangleCount)
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:402:    public static void CreateIllegalDysonShellWithMaxOutput()
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:422:    public static void CreateIllegalDysonShellWithMaxOutputForAllLayers()
CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs:438:    public static void CreateIllegalDysonShellsSpecially()
CheatEnabler/Functions/DysonSphere/ShellCompletionFunctions.cs:12:public static class ShellCompletionFunctions
CheatEnabler/Functions/DysonSphere/ShellCompletionFunctions.cs:14:    public static void CompleteShellsInstantly()
CheatEnabler/Functions/DysonSphereFunctions.cs:9:public static class DysonSphereFunctions
CheatEnabler/Functions/DysonSphereFunctions.cs:11:    public static ConfigEntry<bool> IllegalDysonShellFunctionsEnabled;
CheatEnabler/Functions/DysonSphereFunctions.cs:12:    public static ConfigEntry<int> ShellsCountForFunctions;
CheatEnabler/Functions/DysonSphereFunctions.cs:14:    public static void Init()
CheatEnabler/Functions/DysonSphereFunctions.cs:18:    public static void CompleteShellsInstantly() => ShellCompletionFunctions.CompleteShellsInstantly();
CheatEnabler/Functions/DysonSphereFunctions.cs:19:    public static void RemoveAllFrames() => FrameRemovalFunctions.RemoveAllFrames();
CheatEnabler/Functions/DysonSphereFunctions.cs:20:    public static void DuplicateShellsWithHighestProduction() => IllegalShellFunctions.DuplicateShellsWithHighestProduction();
CheatEnabler/Functions/DysonSphereFunctions.cs:21:    public static void KeepMaxProductionShells() => IllegalShellFunctions.KeepMaxProductionShells();
CheatEnabler/Functions/DysonSphereFunctions.cs:22:    public static void CreateIllegalDysonShellQuickly(int triangleCount) => IllegalShellFunctions.CreateIllegalDysonShellQuickly(triangleCount);
CheatEnabler/Functions/DysonSphereFunctions.cs:23:    public static void CreateIllegalDysonShellWithMaxOutput() => IllegalShellFunctions.CreateIllegalDysonShellWithMaxOutput();
CheatEnabler/Functions/DysonSphereFunctions.cs:24:    public static void CreateIllegalDysonShellWithMaxOutputForAllLayers() => IllegalShellFunctions.CreateIllegalDysonShellWithMaxOutputForAllLayers();
CheatEnabler/Functions/DysonSphereFunctions.cs:25:    public static void CreateIllegalDysonShellsSpecially() => IllegalShellFunctions.CreateIllegalDysonShellsSpecially();
CheatEnabler/Functions/PlanetFunctions.cs:7:public static class PlanetFunctions
CheatEnabler/Functions/PlanetFunctions.cs:9:    public static void BuryAllVeins(bool bury)
CheatEnabler/Functions/PlayerFunctions.cs:10:public static class PlayerFunctions
CheatEnabler/Functions/PlayerFunctions.cs:12:    public static void Init()
CheatEnabler/Functions/PlayerFunctions.cs:16:    public static void TeleportToOuterSpace()
CheatEnabler/Functions/PlayerFunctions.cs:35:    public static void TeleportToSelectedAstronomical()
CheatEnabler/Functions/PlayerFunctions.cs:79:    public static void RemoveAllMetadataConsumptions()
CheatEnabler/Functions/PlayerFunctions.cs:119:    public static void RemoveCurrentMetadataConsumptions()
CheatEnabler/Functions/PlayerFunctions.cs:168:    public static void ClearMetadataBanAchievements()
UniverseGenTweaks/BirthPlanetPatch.cs:10:public static class BirthPlanetPatch
UniverseGenTweaks/BirthPlanetPatch.cs:12:    public static ConfigEntry<bool> SitiVeinsOnBirthPlanet;
UniverseGenTweaks/BirthPlanetPatch.cs:13:    public static ConfigEntry<bool> FireIceOnBirthPlanet;
UniverseGenTweaks/BirthPlanetPatch.cs:14:    public static ConfigEntry<bool> KimberliteOnBirthPlanet;
UniverseGenTweaks/BirthPlanetPatch.cs:15:    public static ConfigEntry<bool> FractalOnBirthPlanet;
UniverseGenTweaks/BirthPlanetPatch.cs:16:    public static ConfigEntry<bool> OrganicOnBirthPlanet;
UniverseGenTweaks/BirthPlanetPatch.cs:17:    public static ConfigEntry<bool> OpticalOnBirthPlanet;
UniverseGenTweaks/BirthPlanetPatch.cs:18:    public static ConfigEntry<bool> SpiniformOnBirthPlanet;
UniverseGenTweaks/BirthPlanetPatch.cs:19:    public static ConfigEntry<bool> UnipolarOnBirthPlanet;
UniverseGenTweaks/BirthPlanetPatch.cs:20:    public static ConfigEntry<bool> FlatBirthPlanet;
UniverseGenTweaks/BirthPlanetPatch.cs:21:    public static ConfigEntry<bool> HighLuminosityBirthStar;
UniverseGenTweaks/BirthPlanetPatch.cs:66:    public static void Init()
UniverseGenTweaks/BirthPlanetPatch.cs:83:    public static void Uninit()
UniverseGenTweaks/EpicDifficulty.cs:14:public static class EpicDifficulty
UniverseGenTweaks/EpicDifficulty.cs:16:    public static ConfigEntry<bool> Enabled;
UniverseGenTweaks/EpicDifficulty.cs:17:    public static ConfigEntry<float> ResourceMultiplier;
UniverseGenTweaks/EpicDifficulty.cs:18:    public static ConfigEntry<float> OilMultiplier;
UniverseGenTweaks/EpicDifficulty.cs:24:    public static void Init()
UniverseGenTweaks/EpicDifficulty.cs:30:    public static void Uninit()
UniverseGenTweaks/EpicDifficulty.cs:46:    public static float IndexToResourceMultiplier(int index)
UniverseGenTweaks/EpicDifficulty.cs:51:    public static int ResourceMultiplierToIndex(float mult)
UniverseGenTweaks/EpicDifficulty.cs:60:    public static int ResourceMultipliersCount()
UniverseGenTweaks/EpicDifficulty.cs:65:    public static float IndexToOilMultiplier(int index)
UniverseGenTweaks/EpicDifficulty.cs:70:    public static int OilMultiplierToIndex(float mult)
UniverseGenTweaks/EpicDifficulty.cs:79:    public static int OilMultipliersCount()
UniverseGenTweaks/Functions/GalaxyGenSave.cs:7:public static class GalaxyGenSave
UniverseGenTweaks/Functions/GalaxyGenSave.cs:9:    public static void Export(BinaryWriter w)
UniverseGenTweaks/Functions/GalaxyGenSave.cs:17:    public static void Import(BinaryReader r)
UniverseGenTweaks/Localization.cs:5:public static class Localization
UniverseGenTweaks/Localization.cs:43:    public static void Register()
UniverseGenTweaks/MoreSettings.cs:5:public static class MoreSettings
UniverseGenTweaks/MoreSettings.cs:7:    public static ConfigEntry<bool> Enabled;
UniverseGenTweaks/MoreSettings.cs:8:    public static ConfigEntry<int> MaxStarCount;
UniverseGenTweaks/obj/Release/net472/PluginInfo.cs:3:    public static class PluginInfo
UniverseGenTweaks/Patches/CombatSettingsPatch.cs:11:public static class CombatSettingsPatch
UniverseGenTweaks/Patches/CombatSettingsPatch.cs:71:    public static void Init()
UniverseGenTweaks/Patches/CombatSettingsPatch.cs:77:    public static void Uninit()
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:12:public static class GalaxyGenSettingsPatch
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:27:    public static void Init()
UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs:34:    public static void Uninit()
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:12:public static class GalaxySelectUIPatch
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:28:    public static void Init()
UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs:34:    public static void Uninit()
UniverseGenTweaks/UIConfigWindow.cs:8:public static class UIConfigWindow
UniverseGenTweaks/UIConfigWindow.cs:12:    public static void Init()
```
