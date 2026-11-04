# UXAssist feature and shared-library audit

Baseline: `64a23e9772e7878f216cc2e281da2bc3f87d3140` (`origin/master`).

## Coverage

The owned inventory contains 82 C# files and `UXAssist.csproj`: Common (26), Functions (15), Patches (22), UI (13), ModsCompat (4), and root entry/configuration (2 C# plus the project).
Every file was included in the structural/member/reference audit. Candidate implementations and their connected lifecycle/configuration/UI/native-patch paths were read and traced. Localization registration/constant tables were treated as data, not a source of deletions based on text-reference counts. The seven large hot-path files have a separate supplementary audit in [ux-hot-paths.md](ux-hot-paths.md). Production and its check tool are covered separately in [production.md](production.md).

Checks included single-owner files and small types; public compatibility documentation; regular and string/reflection references; feature discovery; enabled/disabled/session lifetime; cached state; duplicate transformations and implementations; and abandoned commented experiments. This is an overdesign audit, not an assertion that every game interaction has been executed.

## Applied cleanup and evidence

| Change | Evidence and behavior preserved |
| --- | --- |
| Remove 11 empty methods from the internal MenuButtonUI, MilkyWayUI and StarmapFilterUI classes, plus 11 forwarding calls from UIFunctions | Empty bodies perform no work. These types are in `UXAssist.Functions.UI`, lack ModFeature attributes, and are not independently reflection-discovered. Keep public `UIFunctions.Start()` and all remaining lifecycle calls in the same order. |
| Remove unused bulk Enable facades from ArchitectModePatch, BeltSignalPatch, BuildingBufferPatch and VeinProtectionPatch | Whole-tree source/string searches find no callers. Config handlers and startup already invoke each nested owning patch directly. Their internal outer classes are neither discovered through the entry-point's exact namespace search nor through an Enable lifecycle convention. No nested patch or target identity changes. |
| Remove `WindowFunctions.GetPriorityName`, `WinApi.ERROR_INSUFFICIENT_BUFFER`, `UIPatch.PlanetVeinUtilization.GetAdjustedSizeDelta`, and `BeltSignalsForBuyOut.RemoveBeltSignalProtos` | Private members have no source/string/attribute references. The signal proto removal helper is not invoked on disable; deleting it preserves the existing persistence behavior and stock order. The only LINQ use in BeltSignalPatch was inside this dead helper, so remove its unused import too. |
| Remove unused internal `I18N.Translate` | No calls, string lookups or Harmony attributes refer to it. It is not an extension method. Existing localization registration and all native `.Translate()` call sites remain unchanged. |
| Remove MyKeyBinder's never-true `_nextNotOn` and its unreachable branch | Private field has no SerializeField attribute and is only assigned false; it cannot enter the branch through any first-party caller or Unity serialized field. Public click/reset/none-key APIs remain intact. |
| Delete fully commented AutoSaveOpt experiment and its commented config/UI wiring | The class, field, activation and configuration were all comments at baseline. Keep public localization keys, current save behavior, current options, and data formats unchanged. |
| Delete three commented abandoned window methods | The SetRect/UIGame create/free snippets were block comments, not live Harmony patches. No hook is removed. |
| Reuse ItemIds constants from the public KnownItemId enum | Enum identity, names, underlying type and compiled values (5001, 5002, 5003, 1210) remain the same; four independent literal definitions are eliminated. |
| Collapse four identical charge-power text formatting bodies into private `UIConfigWindow.FormatPower` | Four real call sites share the same owning UI class. Each retains its range mapper, multiplier, nine-space StringBuilder, WriteKMG arguments, W suffix and Trim. No new source file, public abstraction, or cross-feature utility. |
| Replace `storageArray.Select(x => x.itemId).All(x => x == 0)` with `storageArray.All(storage => storage.itemId == 0)` | Same empty-array result, short-circuit order, and itemId comparison; avoids an unnecessary projection iterator in the periodic overlay update. |

No version, config key/default, user data format, public type/member identity, live Harmony target or IL pattern is changed by this section. No new C# file or type was introduced.

## Boundaries deliberately retained

| Candidate | Concrete reason to retain |
| --- | --- |
| Common/Config providers, Common.Util, PathUtil, KnownItemId, GameEvent, ReflectionUtil, ResourceUtil, LayoutHelper, ConfigTabGroup, UI window/control wrappers | Public contracts are explicitly recorded in `docs/PublicApiSurface.md`. Removing a public forwarding API because only this repository has one caller would break dependent mods. Config providers retain one actual ConfigEntry owner. Helpers own distinct operations or shared UI primitives, rather than duplicating state. |
| GameEvent vs GameLogic | Safe invocation is a documented shared event helper API. GameLogic raises three events, and each handler must remain isolated from exceptions. Moving methods to GameLogic would still need the compatibility facade. |
| PlayerInputUtil | Both auto-construct and auto-cruise consume the same six-axis override rule. It prevents behavioral drift between two automation features. |
| ModFeatureRegistry, ModFeatureAttribute and IModFeature | Discovery crosses UXAssist, CheatEnabler and UniverseGenTweaks. Cached delegates avoid per-frame reflection; frame and started guards protect real lifecycle boundaries. Instance support is a public extension contract, even if current first-party features are static. |
| PatchImpl and TranspilerGuard | Real consumers across multiple mods. Rollback/fail-soft behavior and mismatch diagnostics cannot be flattened into callers without duplication and exception-chain changes. |
| ModCompatHelper and four ModsCompat files | Shared external plugin/type/member lookup plus independent plugin-specific compatibility rules. Existing public spelling/identity and optional-dependency boundaries remain. |
| AutoConstructUI and AutoCruiseUI | Each owns actual controls, state synchronization and label sizing for a distinct feature. They are not isolated one-line calculation helpers. Periodic reconciliation remains because callbacks can be missed. |
| MenuButtonUI, MilkyWayUI and StarmapFilterUI | Substantial feature-specific UI/state, not trivial extracted helpers. Public UIFunctions forwarding entry points are retained for compatibility. Only proven no-op internals were removed. |
| MyCheckBox/MyCheckButton, MyComboBox/MyCornerComboBox, MySlider/MySideSlider | Different native clone sources, layouts, visuals and event behavior. Introducing a generic component base would add an abstraction and risk inherited Unity behavior merely to reduce text similarity. |
| Value/range/array mapper types and feature mapper subclasses | Multiple real slider callers need discrete choices, ranges, inverses and localized formatting. Reuse their shared formatting operation locally; keep explicit range/semantic types. |
| Real-time logistics cached component arrays, stock state, pixel scales and recycling pool | Reused every frame to avoid repeated hierarchy lookup, allocations, text updates and orphan UI objects. Invalidate/recycle at actual transport replacement boundaries. These are performance caches with specific ownership, not duplicate authoritative game state. |
| Blueprint description, miner geometry/solver/debug and automation recovery | Blueprint behavior belongs to the separate in-flight UI task; the baseline popup helper example is absent here. Miner geometry/solver have independent pure test/replay boundaries, and hot-path caches/recovery have bounded work contracts. See supplementary audit. |
| VeinProtectionPatch/VeinProtectionUIPatch partial split | Simulation replacement and UI/statistics presentation contain substantial different native integration logic. Shared nested identity ensures Harmony activation includes both. Not a tiny single-use helper file. |
| Per-target PlayerOrder transpilers in PersistPatch | Identical matcher bodies were considered, but constructor vs normal-method Harmony descriptors carry different method-kind semantics. Keep their separate descriptors rather than assume multiple attributes form an independent target list. No experimental merge remains. |
| Config/session/enable reset paths | Some resets look similar, but native lifecycle and callback order matter. Keep separate real boundaries unless event-order equivalence is established; do not infer redundancy from matching assignments alone. |

## Validation

- Whole-tree source/string and public API checks were performed before deleting internal/private members.
- `git diff --check -- UXAssist` passed after edits.
- Parent executor owns the normal UXAssist/CheatEnabler/UniverseGenTweaks Release builds and final regression checks, avoiding concurrent UpdateGameDlls writes.
- No in-game rendering, mouse interaction, game-session event execution or third-party compatibility runtime was exercised by this audit worker. The patch/IL bodies themselves remain unchanged.

## Inventory

### Common

- `UXAssist/Common/Config/FactoryConfigProvider.cs`
- `UXAssist/Common/Config/LogisticsConfigProvider.cs`
- `UXAssist/Common/GameConstants/DysonSphereConstants.cs`
- `UXAssist/Common/GameConstants/ItemIds.cs`
- `UXAssist/Common/GameConstants/LogisticsConstants.cs`
- `UXAssist/Common/GameConstants/TechIds.cs`
- `UXAssist/Common/GameConstants/UniverseGenConstants.cs`
- `UXAssist/Common/GameEvent.cs`
- `UXAssist/Common/GameLogic.cs`
- `UXAssist/Common/I18N.cs`
- `UXAssist/Common/I18NKeys.cs`
- `UXAssist/Common/KeyBindings.cs`
- `UXAssist/Common/KnownItemId.cs`
- `UXAssist/Common/ModCompat/ModCompatHelper.cs`
- `UXAssist/Common/ModFeatures/IModFeature.cs`
- `UXAssist/Common/ModFeatures/ModFeatureAttribute.cs`
- `UXAssist/Common/ModFeatures/ModFeatureRegistry.cs`
- `UXAssist/Common/PatchImpl.cs`
- `UXAssist/Common/Patching/TranspilerGuard.cs`
- `UXAssist/Common/Util.cs`
- `UXAssist/Common/Utils/DysonSphereReflection.cs`
- `UXAssist/Common/Utils/PathUtil.cs`
- `UXAssist/Common/Utils/PlayerInputUtil.cs`
- `UXAssist/Common/Utils/ReflectionUtil.cs`
- `UXAssist/Common/Utils/ResourceUtil.cs`
- `UXAssist/Common/WinApi.cs`

### Functions

- `UXAssist/Functions/DysonSphereFunctions.cs`
- `UXAssist/Functions/Factory/MinerPlacementDebug.cs`
- `UXAssist/Functions/Factory/MinerPlacementGeometry.cs`
- `UXAssist/Functions/Factory/MinerPlacementSolver.cs`
- `UXAssist/Functions/Factory/QuickMinerBuild.cs`
- `UXAssist/Functions/FactoryFunctions.cs`
- `UXAssist/Functions/PlanetFunctions.cs`
- `UXAssist/Functions/TechFunctions.cs`
- `UXAssist/Functions/UI/AutoConstructUI.cs`
- `UXAssist/Functions/UI/AutoCruiseUI.cs`
- `UXAssist/Functions/UI/MenuButtonUI.cs`
- `UXAssist/Functions/UI/MilkyWayUI.cs`
- `UXAssist/Functions/UI/StarmapFilterUI.cs`
- `UXAssist/Functions/UIFunctions.cs`
- `UXAssist/Functions/WindowFunctions.cs`

### Patches

- `UXAssist/Patches/BlueprintDescriptionPatch.cs`
- `UXAssist/Patches/DysonSpherePatch.cs`
- `UXAssist/Patches/Factory/ArchitectModePatch.cs`
- `UXAssist/Patches/Factory/BeltSignalPatch.cs`
- `UXAssist/Patches/Factory/BuildToolPatch.cs`
- `UXAssist/Patches/Factory/BuildingBufferPatch.cs`
- `UXAssist/Patches/Factory/FactoryBuildPatches.cs`
- `UXAssist/Patches/Factory/FactoryPatch.cs`
- `UXAssist/Patches/Factory/RenderingPatch.cs`
- `UXAssist/Patches/Factory/VeinProtectionPatch.cs`
- `UXAssist/Patches/Factory/VeinProtectionUIPatch.cs`
- `UXAssist/Patches/GamePatch.cs`
- `UXAssist/Patches/Logistics/AutoConfigPatch.cs`
- `UXAssist/Patches/Logistics/CapacityPatch.cs`
- `UXAssist/Patches/Logistics/LogisticsPatch.cs`
- `UXAssist/Patches/Logistics/OverflowPatch.cs`
- `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs`
- `UXAssist/Patches/PersistPatch.cs`
- `UXAssist/Patches/PlanetPatch.cs`
- `UXAssist/Patches/PlayerPatch.cs`
- `UXAssist/Patches/TechPatch.cs`
- `UXAssist/Patches/UIPatch.cs`

### UI

- `UXAssist/UI/ConfigTabGroup.cs`
- `UXAssist/UI/LayoutHelper.cs`
- `UXAssist/UI/MyCheckButton.cs`
- `UXAssist/UI/MyCheckbox.cs`
- `UXAssist/UI/MyComboBox.cs`
- `UXAssist/UI/MyConfigWindow.cs`
- `UXAssist/UI/MyCornerComboBox.cs`
- `UXAssist/UI/MyFlatButton.cs`
- `UXAssist/UI/MyKeyBinder.cs`
- `UXAssist/UI/MySideSlider.cs`
- `UXAssist/UI/MySlider.cs`
- `UXAssist/UI/MyWindow.cs`
- `UXAssist/UI/Util.cs`

### ModsCompat

- `UXAssist/ModsCompat/AuxilaryfunctionWrapper.cs`
- `UXAssist/ModsCompat/BulletTimeWrapper.cs`
- `UXAssist/ModsCompat/CommonAPIWrapper.cs`
- `UXAssist/ModsCompat/PlanetVeinUtilization.cs`

### root

- `UXAssist/UIConfigWindow.cs`
- `UXAssist/UXAssist.cs`
- `UXAssist/UXAssist.csproj`
