# UXAssist / CheatEnabler / UniverseGenTweaks Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve readability and maintainability of `UXAssist`, `CheatEnabler`, and `UniverseGenTweaks` by splitting oversized files, centralizing constants/localization, cleaning static mutable state, and hardening transpilers. Only `UXAssist.UI` and `UXAssist.Common` public APIs are treated as a stable contract; everything else may be refactored freely, and downstream projects are updated to consume the new structure.

**Architecture:** Keep the existing BepInEx + Harmony stack. Introduce a reusable, public mod-feature lifecycle abstraction in `UXAssist.Common.ModFeatures` that replaces the current namespace-based reflection on static `Functions`/`Patches` classes. Each static feature class declares `[ModFeature]` and exposes `Init`/`Start`/`Uninit`/`OnInputUpdate`/`OnUpdate` as needed; `ModFeatureRegistry.Discover` finds and registers them, then drives their lifecycle. `CheatEnabler` and `UniverseGenTweaks` adopt the same abstraction, so they benefit from the same architecture rather than using `InternalsVisibleTo`.

Split monolithic `Patches/*.cs` and `Functions/*.cs` files into focused classes grouped by subsystem (e.g., `UXAssist.Patches.Factory.*`). Refactor the internals of `UXAssist.UI` and `UXAssist.Common` into smaller helpers while keeping the existing public surface intact (old members become thin forwarding facades or are marked `[Obsolete]`). Introduce `ConfigProvider` helpers and `GameConstants` classes to decouple UI from patch internals. Clean static state through explicit `ResetState` callbacks registered on `GameLogic.OnGameEnd`. Add version/fallback annotations to transpilers to survive game updates.

**Tech Stack:** C# (`net472`), BepInEx 5.x, HarmonyLib, SDK-style MSBuild, PowerShell `Compress-Archive`.

---

## Phase 1 — Structural Split + Public Lifecycle Abstraction

### Task 1: Document the public API surface that must stay stable

**Files:**
- Create: `docs/PublicApiSurface.md`

- [ ] **Step 1: Inventory `UXAssist.UI` and `UXAssist.Common` public members**

  Read every file under `UXAssist/UI/` and `UXAssist/Common/` and list every `public` class/struct/enum/delegate/method/property/event/field that is reachable from `CheatEnabler` or `UniverseGenTweaks`.

  At minimum, the list must include:
  - `UXAssist.Common.I18N` (`Add`, `Apply`, `Translate`, `Init`, `OnInitialized`)
  - `UXAssist.Common.GameLogic` (`Enable`, `OnDataLoaded`, `OnGameBegin`, `OnGameEnd`, `OnFactoryFrameBegin`)
  - `UXAssist.Common.PatchImpl<T>` and `PatchGuidAttribute`
  - `UXAssist.Common.Util` (`GetTypesFiltered`, `GetTypesInNamespace`, `LoadEmbeddedResource`, `LoadEmbeddedTexture`, `LoadEmbeddedSprite`, `PluginFolder`)
  - `UXAssist.UI.MyConfigWindow` (`OnUICreated`, `OnUpdateUI`, `CreateInstance`, `DestroyInstance`)
  - `UXAssist.UI.MyWindow` (`InitBaseObject`, `Create`, `AddText`, `AddText2`, `AddButton`, `AddTipsButton`, `AddTipsButton2`, `Open`, `Close`, `TryClose`, `AutoFitWindowSize`)
  - `UXAssist.UI.MyWindowWithTabs` (`AddTabGroup`, `AddTab`)
  - `UXAssist.UI.MyCheckBox.CreateCheckBox`
  - `UXAssist.UI.MySlider.CreateSlider`
  - `UXAssist.UI.MyWindowManager` (`InitBaseObjects`, `Enable`)

- [ ] **Step 2: Commit the inventory**

  ```bash
  git add docs/PublicApiSurface.md
  git commit -m "docs: inventory UXAssist.UI/Common public API surface"
  ```

---

### Task 2: Introduce public `ModFeature` lifecycle abstraction

**Files:**
- Create: `UXAssist/Common/ModFeatures/ModFeatureAttribute.cs`
- Create: `UXAssist/Common/ModFeatures/IModFeature.cs`
- Create: `UXAssist/Common/ModFeatures/ModFeatureRegistry.cs`
- Modify: `UXAssist/Common/Util.cs`

- [ ] **Step 1: Add namespace-prefix reflection helper**

  In `UXAssist/Common/Util.cs`:
  ```csharp
  public static Type[] GetTypesInNamespacePrefix(Assembly assembly, string prefix)
  {
      return GetTypesFiltered(assembly, t => t.Namespace != null && t.Namespace.StartsWith(prefix, StringComparison.Ordinal));
  }
  ```

- [ ] **Step 2: Define the attribute**

  ```csharp
  namespace UXAssist.Common.ModFeatures;

  [AttributeUsage(AttributeTargets.Class, Inherited = false)]
  public sealed class ModFeatureAttribute : Attribute
  {
      public string Name { get; }
      public int Order { get; set; }

      public ModFeatureAttribute(string name = null)
      {
          Name = name;
      }
  }
  ```

- [ ] **Step 3: Define the optional instance interface**

  ```csharp
  namespace UXAssist.Common.ModFeatures;

  public interface IModFeature
  {
      void Init();
      void Start();
      void Uninit();
      void OnInputUpdate();
      void OnUpdate();
  }
  ```

- [ ] **Step 4: Implement the registry**

  ```csharp
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;

  namespace UXAssist.Common.ModFeatures;

  public static class ModFeatureRegistry
  {
      private static readonly List<Type> _staticFeatures = [];
      private static readonly List<IModFeature> _instanceFeatures = [];
      private static readonly HashSet<Assembly> _discoveredAssemblies = [];

      public static void Discover(Assembly assembly)
      {
          if (!_discoveredAssemblies.Add(assembly)) return;

          var staticTypes = Util.GetTypesFiltered(assembly, t =>
              t.IsClass && t.IsAbstract && t.IsSealed &&
              Attribute.IsDefined(t, typeof(ModFeatureAttribute)));

          foreach (var type in staticTypes.OrderBy(GetOrder))
          {
              if (!_staticFeatures.Contains(type))
                  _staticFeatures.Add(type);
          }
      }

      public static void Register<T>() where T : class, IModFeature, new()
      {
          var instance = new T();
          _instanceFeatures.Add(instance);
      }

      public static void InitAll()
      {
          ForEachStatic("Init");
          foreach (var f in _instanceFeatures) f.Init();
      }

      public static void StartAll()
      {
          ForEachStatic("Start");
          foreach (var f in _instanceFeatures) f.Start();
      }

      public static void UninitAll()
      {
          ForEachStatic("Uninit");
          foreach (var f in _instanceFeatures) f.Uninit();
      }

      public static void OnInputUpdateAll()
      {
          ForEachStatic("OnInputUpdate");
          foreach (var f in _instanceFeatures) f.OnInputUpdate();
      }

      public static void OnUpdateAll()
      {
          ForEachStatic("OnUpdate");
          foreach (var f in _instanceFeatures) f.OnUpdate();
      }

      private static void ForEachStatic(string methodName)
      {
          foreach (var type in _staticFeatures)
          {
              var method = type.GetMethod(methodName,
                  BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
              method?.Invoke(null, null);
          }
      }

      private static int GetOrder(Type type)
      {
          return type.GetCustomAttribute<ModFeatureAttribute>()?.Order ?? 0;
      }
  }
  ```

- [ ] **Step 5: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```
  Expected: 0 errors.

- [ ] **Step 6: Commit**

  ```bash
  git add UXAssist/Common/ModFeatures/ UXAssist/Common/Util.cs
  git commit -m "feat(UXAssist): public ModFeature lifecycle registry"
  ```

---

### Task 3: Split `UXAssist/Patches/FactoryPatch.cs` and mark features

**Files:**
- Create: `UXAssist/Patches/Factory/FactoryPatch.cs` (config coordinator)
- Create: `UXAssist/Patches/Factory/ImmediateBuildPatch.cs`
- Create: `UXAssist/Patches/Factory/ArchitectModePatch.cs`
- Create: `UXAssist/Patches/Factory/BuildToolPatch.cs`
- Create: `UXAssist/Patches/Factory/BeltSignalPatch.cs`
- Create: `UXAssist/Patches/Factory/BuildingBufferPatch.cs`
- Create: `UXAssist/Patches/Factory/VeinProtectionPatch.cs`
- Create: `UXAssist/Patches/Factory/PowerGenerationPatch.cs`
- Create: `UXAssist/Patches/Factory/RenderingPatch.cs`
- Delete: `UXAssist/Patches/FactoryPatch.cs`

- [ ] **Step 1: Move nested patch classes to new files**

  Use `git mv` semantics: copy the contents of each nested `PatchImpl<T>` class from the old file into a new file under `UXAssist/Patches/Factory/`. Keep each class `internal` unless it must be public. Use namespace `UXAssist.Patches.Factory`.

- [ ] **Step 2: Create the coordinator `FactoryPatch`**

  Mark it as a mod feature and expose the public `ConfigEntry<T>` fields:
  ```csharp
  using UXAssist.Common.ModFeatures;

  namespace UXAssist.Patches.Factory;

  [ModFeature("Factory", Order = 10)]
  public static class FactoryPatch
  {
      public static ConfigEntry<bool> UnlimitInteractiveEnabled { get; internal set; }
      // ... all other public ConfigEntry fields from the original file

      public static void Init()
      {
          ImmediateBuildPatch.Init();
          ArchitectModePatch.Init();
          // ...
      }

      public static void Start()
      {
          ImmediateBuildPatch.Start();
          // ...
      }

      public static void Uninit()
      {
          ImmediateBuildPatch.Uninit();
          // ...
      }

      public static void OnInputUpdate() => BeltSignalPatch.OnInputUpdate();
      public static void Export(BinaryWriter w) => BeltSignalPatch.Export(w);
      public static void Import(BinaryReader r) => BeltSignalPatch.Import(r);
  }
  ```

  `Awake()` in `UXAssist.cs` already assigns every `ConfigEntry`; leave those assignments untouched.

- [ ] **Step 3: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/Patches/Factory/
  git rm UXAssist/Patches/FactoryPatch.cs
  git commit -m "refactor(UXAssist): split FactoryPatch into focused classes"
  ```

---

### Task 4: Split `UXAssist/Patches/LogisticsPatch.cs` and mark features

**Files:**
- Create: `UXAssist/Patches/Logistics/LogisticsPatch.cs`
- Create: `UXAssist/Patches/Logistics/AutoConfigPatch.cs`
- Create: `UXAssist/Patches/Logistics/CapacityPatch.cs`
- Create: `UXAssist/Patches/Logistics/OverflowPatch.cs`
- Create: `UXAssist/Patches/Logistics/RealtimeInfoPanelPatch.cs`
- Delete: `UXAssist/Patches/LogisticsPatch.cs`

- [ ] **Step 1: Move nested classes and create coordinator**

  Same pattern as Task 3. Coordinator is marked `[ModFeature("Logistics", Order = 11)]` and forwards `OnInputUpdate` / `OnUpdate` to sub-classes.

- [ ] **Step 2: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 3: Commit**

  ```bash
  git add UXAssist/Patches/Logistics/
  git rm UXAssist/Patches/LogisticsPatch.cs
  git commit -m "refactor(UXAssist): split LogisticsPatch into focused classes"
  ```

---

### Task 5: Split `UXAssist/Functions/UIFunctions.cs` and mark features

**Files:**
- Create: `UXAssist/Functions/UI/StarmapFilterUI.cs`
- Create: `UXAssist/Functions/UI/MilkyWayUI.cs`
- Create: `UXAssist/Functions/UI/AutoCruiseUI.cs`
- Create: `UXAssist/Functions/UI/MenuButtonUI.cs`
- Modify: `UXAssist/Functions/UIFunctions.cs` (coordinator, marked `[ModFeature("UI", Order = 12)]`)
- Modify: `UXAssist/UXAssist.cs`

- [ ] **Step 1: Move features to sub-files**

  Namespace `UXAssist.Functions.UI`. Keep `internal` where possible. Keep public `ConfigEntry<T>` fields on `UIFunctions` if bound in `UXAssist.Awake()`.

- [ ] **Step 2: Update `UXAssist.Awake()` export/import calls**

  Replace `UIFunctions.ExportClusterUploadResults(w)` with a coordinator call or direct `MilkyWayUI.Export(w)`.

- [ ] **Step 3: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/Functions/UI/ UXAssist/Functions/UIFunctions.cs UXAssist/UXAssist.cs
  git commit -m "refactor(UXAssist): split UIFunctions into UI sub-features"
  ```

---

### Task 6: Update `UXAssist.cs` to drive features through `ModFeatureRegistry`

**Files:**
- Modify: `UXAssist/UXAssist.cs`

- [ ] **Step 1: Discover features from UXAssist assembly**

  In `Awake()`, after config binding:
  ```csharp
  ModFeatureRegistry.Discover(Assembly.GetExecutingAssembly());
  ModFeatureRegistry.InitAll();
  ```

- [ ] **Step 2: Replace lifecycle calls**

  In `Start()`:
  ```csharp
  ModFeatureRegistry.StartAll();
  ```

  In `OnDestroy()`:
  ```csharp
  ModFeatureRegistry.UninitAll();
  ```

- [ ] **Step 3: Replace hard-coded update calls**

  In `Update()`:
  ```csharp
  if (VFInput.inputing) return;
  if (DSPGame.IsMenuDemo)
  {
      ModFeatureRegistry.OnInputUpdateAll();
      return;
  }
  ModFeatureRegistry.OnInputUpdateAll();
  ModFeatureRegistry.OnUpdateAll();
  ```

  Remove the explicit per-class calls (`LogisticsPatch.OnInputUpdate()`, `UIFunctions.OnInputUpdate()`, `GamePatch.OnInputUpdate()`, `FactoryPatch.OnInputUpdate()`, `PlayerPatch.OnInputUpdate()`, `LogisticsPatch.OnUpdate()`).

- [ ] **Step 4: Keep compat initialization separate**

  Keep the `ModsCompat` scan in `Awake()` and `Start()`; compat wrappers are not mod features because they receive a `Harmony` argument.

- [ ] **Step 5: Centralize save participants**

  Add a simple registry in `UXAssist.cs`:
  ```csharp
  private static readonly List<Action<BinaryWriter>> _exporters = [];
  private static readonly List<Action<BinaryReader>> _importers = [];

  public static void RegisterExporter(Action<BinaryWriter> e) => _exporters.Add(e);
  public static void RegisterImporter(Action<BinaryReader> i) => _importers.Add(i);
  ```

  Update `Export`/`Import` to iterate `_exporters`/`_importers`. Remove direct `FactoryPatch.Export` / `UIFunctions.ExportClusterUploadResults` calls; the coordinators register themselves during `Init`.

- [ ] **Step 6: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 7: Commit**

  ```bash
  git add UXAssist/UXAssist.cs
  git commit -m "refactor(UXAssist): drive lifecycle via ModFeatureRegistry"
  ```

---

### Task 7: Decouple `UXAssist.UIConfigWindow` from patch internals

**Files:**
- Create: `UXAssist/Common/Config/FactoryConfigProvider.cs`
- Create: `UXAssist/Common/Config/LogisticsConfigProvider.cs`
- Modify: `UXAssist/UIConfigWindow.cs`

- [ ] **Step 1: Create config providers**

  Each provider exposes only the `ConfigEntry<T>` references that the UI needs:
  ```csharp
  public static class FactoryConfigProvider
  {
      public static ConfigEntry<bool> NightLightEnabled => Factory.FactoryPatch.NightLightEnabled;
      // ...
  }
  ```

- [ ] **Step 2: Update `UIConfigWindow.cs`**

  Replace direct references like `FactoryPatch.NightLightEnabled` with `FactoryConfigProvider.NightLightEnabled`.

- [ ] **Step 3: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/Common/Config/ UXAssist/UIConfigWindow.cs
  git commit -m "refactor(UXAssist): introduce config providers between UI and patches"
  ```

---

### Task 8: Adopt `ModFeatureRegistry` in `CheatEnabler`

**Files:**
- Modify: `CheatEnabler/CheatEnabler.cs`

- [ ] **Step 1: Replace exact-namespace reflection with feature discovery**

  In `Awake()`:
  ```csharp
  ModFeatureRegistry.Discover(Assembly.GetExecutingAssembly());
  ModFeatureRegistry.InitAll();
  ```

  In `Start()`:
  ```csharp
  ModFeatureRegistry.StartAll();
  ```

  In `OnDestroy()`:
  ```csharp
  ModFeatureRegistry.UninitAll();
  ```

  In `Update()`:
  ```csharp
  if (VFInput.inputing) return;
  ModFeatureRegistry.OnInputUpdateAll();
  ```

  Remove `_patches` and the reflection-based `Init`/`Start`/`Uninit` loop.

- [ ] **Step 2: Build CheatEnabler**

  ```bash
  dotnet build CheatEnabler/CheatEnabler.csproj -c Release
  ```

- [ ] **Step 3: Commit**

  ```bash
  git add CheatEnabler/CheatEnabler.cs
  git commit -m "refactor(CheatEnabler): adopt ModFeatureRegistry from UXAssist"
  ```

---

### Task 9: Split `CheatEnabler/Patches/FactoryPatch.cs`

**Files:**
- Create: `CheatEnabler/Patches/Factory/FactoryPatch.cs`
- Create: `CheatEnabler/Patches/Factory/ImmediateBuildPatch.cs`
- Create: `CheatEnabler/Patches/Factory/ArchitectModePatch.cs`
- Create: `CheatEnabler/Patches/Factory/BeltSignalPatch.cs`
- Create: `CheatEnabler/Patches/Factory/PowerBoostPatch.cs`
- Create: `CheatEnabler/Patches/Factory/LogisticsControlPatch.cs`
- Delete: `CheatEnabler/Patches/FactoryPatch.cs`

- [ ] **Step 1: Apply the coordinator pattern and `[ModFeature]`**

  Namespace `CheatEnabler.Patches.Factory`. Coordinator exposes the public `ConfigEntry<T>` fields so `CheatEnabler.Awake()` continues to compile without changes. Mark coordinator `[ModFeature("CheatFactory", Order = 10)]`.

- [ ] **Step 2: Build CheatEnabler**

  ```bash
  dotnet build CheatEnabler/CheatEnabler.csproj -c Release
  ```

- [ ] **Step 3: Commit**

  ```bash
  git add CheatEnabler/Patches/Factory/
  git rm CheatEnabler/Patches/FactoryPatch.cs
  git commit -m "refactor(CheatEnabler): split FactoryPatch into focused classes"
  ```

---

### Task 10: Split `CheatEnabler/Functions/DysonSphereFunctions.cs`

**Files:**
- Create: `CheatEnabler/Functions/DysonSphere/DysonSphereResolver.cs`
- Create: `CheatEnabler/Functions/DysonSphere/ShellCompletionFunctions.cs`
- Create: `CheatEnabler/Functions/DysonSphere/FrameRemovalFunctions.cs`
- Create: `CheatEnabler/Functions/DysonSphere/IllegalShellFunctions.cs`
- Create: `CheatEnabler/Functions/DysonSphere/GeometryHelpers.cs`
- Modify: `CheatEnabler/Functions/DysonSphereFunctions.cs` (coordinator, or delete if empty)
- Modify: `CheatEnabler/Patches/DysonSpherePatch.cs` if it calls helpers

- [ ] **Step 1: Extract repeated "current sphere / star" logic**

  ```csharp
  [ModFeature("DysonSphereResolver")]
  public static class DysonSphereResolver
  {
      public static (DysonSphere sphere, StarData star)? ResolveCurrent()
      {
          var star = GameMain.localStar;
          if (star == null) return null;
          var sphere = GameMain.data.dysonSpheres[star.index];
          if (sphere == null) return null;
          return (sphere, star);
      }
  }
  ```

- [ ] **Step 2: Move shell actions to dedicated files**

  Replace duplicated cleanup blocks with a shared helper:
  ```csharp
  public static void NotifyShellChanged(DysonSphere sphere, DysonSphereLayer layer)
  {
      layer?.RecalculateModels();
      sphere?.swarm?.RecalculateModels();
  }
  ```

- [ ] **Step 3: Build CheatEnabler**

  ```bash
  dotnet build CheatEnabler/CheatEnabler.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add CheatEnabler/Functions/DysonSphere/
  git rm CheatEnabler/Functions/DysonSphereFunctions.cs
  git commit -m "refactor(CheatEnabler): split DysonSphereFunctions into focused helpers"
  ```

---

### Task 11: Adopt `ModFeatureRegistry` in `UniverseGenTweaks`

**Files:**
- Modify: `UniverseGenTweaks/UniverseGenTweaks.cs`

- [ ] **Step 1: Replace explicit Init/Uninit with feature discovery**

  In `Awake()`:
  ```csharp
  ModFeatureRegistry.Discover(Assembly.GetExecutingAssembly());
  ModFeatureRegistry.InitAll();
  ```

  In `OnDestroy()`:
  ```csharp
  ModFeatureRegistry.UninitAll();
  ```

  Remove explicit `MoreSettings.Init()`, `EpicDifficulty.Init()`, `BirthPlanetPatch.Init()` and their `Uninit()` counterparts.

- [ ] **Step 2: Build UniverseGenTweaks**

  ```bash
  dotnet build UniverseGenTweaks/UniverseGenTweaks.csproj -c Release
  ```

- [ ] **Step 3: Commit**

  ```bash
  git add UniverseGenTweaks/UniverseGenTweaks.cs
  git commit -m "refactor(UniverseGenTweaks): adopt ModFeatureRegistry from UXAssist"
  ```

---

### Task 12: Split `UniverseGenTweaks/MoreSettings.cs`

**Files:**
- Create: `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs`
- Create: `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs`
- Create: `UniverseGenTweaks/Patches/CombatSettingsPatch.cs`
- Create: `UniverseGenTweaks/Functions/GalaxyGenSave.cs`
- Modify: `UniverseGenTweaks/MoreSettings.cs` (coordinator, or delete)

- [ ] **Step 1: Move UI construction to `GalaxySelectUIPatch`**

  Mark it `[ModFeature("GalaxySelectUI")]`. Keep slider/text creation helpers there.

- [ ] **Step 2: Move transpilers to `GalaxyGenSettingsPatch`**

  Mark it `[ModFeature("GalaxyGenSettings")]`. Keep the `Init()`/`Uninit()` pattern and the `Harmony` instance local to this class.

- [ ] **Step 3: Move combat settings to `CombatSettingsPatch`**

  Mark it `[ModFeature("CombatSettings")]`. Replace the seven near-identical slider-changed prefix methods with a table-driven mapper.

- [ ] **Step 4: Move save serialization to `GalaxyGenSave`**

  Mark it `[ModFeature("GalaxyGenSave")]`. Implement `Export(BinaryWriter)` / `Import(BinaryReader)` and call them from `UniverseGenTweaks.Export`/`Import`.

- [ ] **Step 5: Build UniverseGenTweaks**

  ```bash
  dotnet build UniverseGenTweaks/UniverseGenTweaks.csproj -c Release
  ```

- [ ] **Step 6: Commit**

  ```bash
  git add UniverseGenTweaks/Patches/ UniverseGenTweaks/Functions/
  git rm UniverseGenTweaks/MoreSettings.cs
  git commit -m "refactor(UniverseGenTweaks): split MoreSettings into focused classes"
  ```

---

### Task 13: Phase 1 cross-project build verification

- [ ] **Step 1: Clean and build all three projects**

  ```bash
  dotnet clean UXAssist/UXAssist.csproj -c Release
  dotnet clean CheatEnabler/CheatEnabler.csproj -c Release
  dotnet clean UniverseGenTweaks/UniverseGenTweaks.csproj -c Release
  dotnet build UXAssist/UXAssist.csproj -c Release
  dotnet build CheatEnabler/CheatEnabler.csproj -c Release
  dotnet build UniverseGenTweaks/UniverseGenTweaks.csproj -c Release
  ```

  Expected: all three succeed with no compilation errors.

- [ ] **Step 2: Produce packages**

  ```bash
  dotnet build -t:ZipMod -c Release
  dotnet build -t:CopyToParentPackage -c Release
  ```

  Expected: `UXAssist/package/`, `CheatEnabler/package/`, `UniverseGenTweaks/package/`, and `Dustbin/package/patchers/` (if building the full solution) are generated.

- [ ] **Step 3: Public API diff check**

  Use `dotnet build` output or `ildasm` to verify that every member listed in `docs/PublicApiSurface.md` still exists with the same signature.

- [ ] **Step 4: Commit a Phase 1 checkpoint tag**

  ```bash
  git tag refactor-phase1
  ```

---

## Phase 2 — UI / Common Internal Refactoring (public surface preserved)

### Task 14: Split `UXAssist/Common/Util.cs` into focused helpers

**Files:**
- Create: `UXAssist/Common/Util/ReflectionUtil.cs`
- Create: `UXAssist/Common/Util/ResourceUtil.cs`
- Create: `UXAssist/Common/Util/PathUtil.cs`
- Modify: `UXAssist/Common/Util.cs`

- [ ] **Step 1: Move implementations to focused helpers**

  `ReflectionUtil.cs`:
  ```csharp
  public static class ReflectionUtil
  {
      public static Type[] GetTypesFiltered(Assembly assembly, Func<Type, bool> predicate) { ... }
      public static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace) { ... }
      public static Type[] GetTypesInNamespacePrefix(Assembly assembly, string prefix) { ... }
  }
  ```

  `ResourceUtil.cs`:
  ```csharp
  public static class ResourceUtil
  {
      public static byte[] LoadEmbeddedResource(...) { ... }
      public static Texture2D LoadEmbeddedTexture(...) { ... }
      public static Sprite LoadEmbeddedSprite(...) { ... }
  }
  ```

  `PathUtil.cs`:
  ```csharp
  public static class PathUtil
  {
      public static string PluginFolder(Assembly assembly = null) { ... }
  }
  ```

- [ ] **Step 2: Keep `Util` as a public forwarding facade**

  ```csharp
  public static class Util
  {
      [Obsolete("Use ReflectionUtil.GetTypesFiltered")]
      public static Type[] GetTypesFiltered(Assembly assembly, Func<Type, bool> predicate)
          => ReflectionUtil.GetTypesFiltered(assembly, predicate);

      [Obsolete("Use ReflectionUtil.GetTypesInNamespace")]
      public static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
          => ReflectionUtil.GetTypesInNamespace(assembly, nameSpace);

      [Obsolete("Use ResourceUtil.LoadEmbeddedResource")]
      public static byte[] LoadEmbeddedResource(string path, Assembly assembly = null)
          => ResourceUtil.LoadEmbeddedResource(path, assembly);

      // ... forward all other existing methods
  }
  ```

  New code inside UXAssist may call `ReflectionUtil`/`ResourceUtil`/`PathUtil` directly.

- [ ] **Step 3: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/Common/Util/
  git commit -m "refactor(UXAssist): split Util into focused helpers with forwarding facade"
  ```

---

### Task 15: Refactor `UXAssist/Common/GameLogic.cs` event invocation

**Files:**
- Create: `UXAssist/Common/GameEvent.cs`
- Modify: `UXAssist/Common/GameLogic.cs`

- [ ] **Step 1: Introduce a small safe-event wrapper**

  ```csharp
  public static class GameEvent
  {
      public static void InvokeSafe(this Action action, ManualLogSource logger, string name)
      {
          if (action == null) return;
          foreach (var d in action.GetInvocationList())
          {
              try { d.DynamicInvoke(); }
              catch (Exception ex) { logger?.LogWarning($"GameEvent '{name}' handler failed: {ex}"); }
          }
      }
  }
  ```

- [ ] **Step 2: Use the wrapper inside `GameLogic`**

  Keep the public event fields unchanged. Replace manual invocation loops with `OnDataLoaded.InvokeSafe(UXAssist.Logger, nameof(OnDataLoaded));`.

- [ ] **Step 3: Add XML documentation to all public members**

- [ ] **Step 4: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 5: Commit**

  ```bash
  git add UXAssist/Common/GameEvent.cs UXAssist/Common/GameLogic.cs
  git commit -m "refactor(UXAssist): safe GameEvent wrapper and XML docs"
  ```

---

### Task 16: Refactor `UXAssist/Common/I18N.cs` internals

**Files:**
- Modify: `UXAssist/Common/I18N.cs`

- [ ] **Step 1: Preserve public API**

  Keep `Add`, `Apply`, `Translate`, `Init`, `OnInitialized` signatures exactly as they are.

- [ ] **Step 2: Internal cleanup**

  Split the internal storage into a private `LocalizedString` record and a dictionary keyed by the English key. Add a public convenience overload:
  ```csharp
  public static void Add(string key, string en, string zh)
  ```
  (this is the existing signature; keep it).

- [ ] **Step 3: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/Common/I18N.cs
  git commit -m "refactor(UXAssist): clean up I18N internals without changing public API"
  ```

---

### Task 17: Extract layout helpers from `UXAssist/UI/MyWindow.cs`

**Files:**
- Create: `UXAssist/UI/LayoutHelper.cs`
- Modify: `UXAssist/UI/MyWindow.cs`

- [ ] **Step 1: Move static layout helpers**

  Move `AddText`, `AddTipsButton`, `AddButton` (static overloads), and `AddElement` into `LayoutHelper`.

  ```csharp
  public static class LayoutHelper
  {
      public static Text AddText(float x, float y, RectTransform parent, string label, int fontSize = 14, string objName = "label") { ... }
      public static UIButton AddTipsButton(...) { ... }
      public static UIButton AddButton(...) { ... }
  }
  ```

- [ ] **Step 2: Keep `MyWindow` instance methods as forwarding facades**

  ```csharp
  public Text AddText2(float x, float y, RectTransform parent, string label, int fontSize = 14, string objName = "label")
  {
      var text = LayoutHelper.AddText(x, y, parent, label, fontSize, objName);
      _maxX = Math.Max(_maxX, x + text.rectTransform.sizeDelta.x);
      MaxY = Math.Max(MaxY, y + text.rectTransform.sizeDelta.y);
      return text;
  }
  ```

  Mark the public static helpers on `MyWindow` as `[Obsolete("Use LayoutHelper")]` if desired, but keep them working.

- [ ] **Step 3: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/UI/LayoutHelper.cs UXAssist/UI/MyWindow.cs
  git commit -m "refactor(UXAssist): extract UI layout helpers from MyWindow"
  ```

---

### Task 18: Refactor `UXAssist/UI/MyConfigWindow.cs` tab management

**Files:**
- Create: `UXAssist/UI/ConfigTabGroup.cs`
- Modify: `UXAssist/UI/MyConfigWindow.cs`
- Modify: `UXAssist/UI/MyWindowWithTabs.cs`

- [ ] **Step 1: Extract tab group logic**

  Move the data structure that tracks tab groups into `ConfigTabGroup` so `MyConfigWindow` does not mix tab state with window lifecycle.

- [ ] **Step 2: Preserve public events and methods**

  Keep `OnUICreated`, `OnUpdateUI`, `CreateInstance`, and `DestroyInstance` on `MyConfigWindow` with identical signatures.

- [ ] **Step 3: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/UI/ConfigTabGroup.cs UXAssist/UI/MyConfigWindow.cs UXAssist/UI/MyWindowWithTabs.cs
  git commit -m "refactor(UXAssist): split tab management out of MyConfigWindow"
  ```

---

### Task 19: Phase 2 cross-project build verification

- [ ] **Step 1: Build all three projects**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  dotnet build CheatEnabler/CheatEnabler.csproj -c Release
  dotnet build UniverseGenTweaks/UniverseGenTweaks.csproj -c Release
  ```

- [ ] **Step 2: Public API diff check**

  Confirm every member in `docs/PublicApiSurface.md` still exists with the same signature. New `[Obsolete]` facades are acceptable.

- [ ] **Step 3: Commit a Phase 2 checkpoint tag**

  ```bash
  git tag refactor-phase2
  ```

---

## Phase 3 — Constants & Localization

### Task 20: Create centralized constants files

**Files:**
- Create: `UXAssist/Common/GameConstants/ItemIds.cs`
- Create: `UXAssist/Common/GameConstants/TechIds.cs`
- Create: `UXAssist/Common/GameConstants/LogisticsConstants.cs`
- Create: `UXAssist/Common/GameConstants/DysonSphereConstants.cs`
- Create: `UXAssist/Common/GameConstants/UniverseGenConstants.cs`

- [ ] **Step 1: Extract item IDs**

  From `CheatEnabler/Patches/Factory/*.cs` and `UXAssist/Patches/Factory/BeltSignalPatch.cs`, collect hard-coded item IDs:
  ```csharp
  public static class ItemIds
  {
      public const int IronOre = 1001;
      public const int CopperOre = 1002;
      // ...
  }
  ```

- [ ] **Step 2: Extract tech IDs**

  From `UXAssist/Patches/TechPatch.cs`:
  ```csharp
  public static class TechIds
  {
      public const int SorterCargoStacking = 3608;
      public static readonly HashSet<int> CombatTechs = [3301, 3302, ...];
  }
  ```

- [ ] **Step 3: Extract logistics constants**

  From `UXAssist/Patches/Logistics/CapacityPatch.cs`:
  ```csharp
  public static class LogisticsConstants
  {
      public const int DefaultLocalStorageMax = 5000;
      public const int DefaultRemoteStorageMax = 10000;
  }
  ```

- [ ] **Step 4: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 5: Commit**

  ```bash
  git add UXAssist/Common/GameConstants/
  git commit -m "refactor: centralize game constants"
  ```

---

### Task 21: Replace magic numbers in UXAssist

**Files:**
- Modify: `UXAssist/Patches/Factory/*.cs`
- Modify: `UXAssist/Patches/Logistics/*.cs`
- Modify: `UXAssist/Patches/TechPatch.cs`
- Modify: `UXAssist/Patches/DysonSpherePatch.cs`

- [ ] **Step 1: Replace literal IDs and capacities with constants**

  For example, change:
  ```csharp
  if (itemId == 1001) { ... }
  ```
  to:
  ```csharp
  if (itemId == ItemIds.IronOre) { ... }
  ```

- [ ] **Step 2: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 3: Commit**

  ```bash
  git add UXAssist/Patches/
  git commit -m "refactor(UXAssist): replace magic numbers with constants"
  ```

---

### Task 22: Replace magic numbers in CheatEnabler and UniverseGenTweaks

**Files:**
- Modify: `CheatEnabler/Patches/Factory/*.cs`
- Modify: `CheatEnabler/Functions/DysonSphere/*.cs`
- Modify: `CheatEnabler/Functions/PlanetFunctions.cs`
- Modify: `UniverseGenTweaks/Patches/GalaxyGenSettingsPatch.cs`
- Modify: `UniverseGenTweaks/Patches/CombatSettingsPatch.cs`
- Modify: `UniverseGenTweaks/BirthPlanetPatch.cs`

- [ ] **Step 1: Reference UXAssist constants where appropriate**

  CheatEnabler/UniverseGenTweaks already reference `UXAssist.csproj`, so they can use `UXAssist.Common.GameConstants.ItemIds`, etc.

- [ ] **Step 2: Build both projects**

  ```bash
  dotnet build CheatEnabler/CheatEnabler.csproj -c Release
  dotnet build UniverseGenTweaks/UniverseGenTweaks.csproj -c Release
  ```

- [ ] **Step 3: Commit**

  ```bash
  git add CheatEnabler/ UniverseGenTweaks/
  git commit -m "refactor(CheatEnabler/UniverseGen): consume centralized constants"
  ```

---

### Task 23: Localization key governance

**Files:**
- Modify: `UXAssist/UIConfigWindow.cs`
- Modify: `CheatEnabler/UIConfigWindow.cs`
- Modify: `UniverseGenTweaks/UIConfigWindow.cs`
- Modify: `CheatEnabler/Functions/PlayerFunctions.cs`
- Modify: `UXAssist/Patches/Factory/BeltSignalPatch.cs`

- [ ] **Step 1: Replace hard-coded Chinese `.Translate()` keys with `I18N.Add` entries**

  Example:
  ```csharp
  // Before
  var btn = MyWindow.AddButton(..., "确定", ...);
  // After
  I18N.Add("OK", "OK", "确定");
  var btn = MyWindow.AddButton(..., "OK", ...);
  ```

- [ ] **Step 2: Translate Chinese comments to English**

  Run a search for `// ` followed by CJK characters and translate or delete stale comments.

  ```bash
  grep -RInP '//.*[\x{4e00}-\x{9fff}]' UXAssist/ CheatEnabler/ UniverseGenTweaks/ --include='*.cs' || true
  ```

- [ ] **Step 3: Build all three projects**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  dotnet build CheatEnabler/CheatEnabler.csproj -c Release
  dotnet build UniverseGenTweaks/UniverseGenTweaks.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/ CheatEnabler/ UniverseGenTweaks/
  git commit -m "refactor: localize hard-coded strings and translate comments"
  ```

---

## Phase 4 — Static State & Lifecycle Cleanup

### Task 24: Inventory static mutable state

**Files:**
- Create: `docs/StaticStateInventory.md`

- [ ] **Step 1: List all static mutable fields in the three projects**

  Search:
  ```bash
  grep -RInP 'private static (?!readonly)\S+' UXAssist/Patches/ UXAssist/Functions/ CheatEnabler/Patches/ CheatEnabler/Functions/ UniverseGenTweaks/ --include='*.cs' > docs/StaticStateInventory.md
  grep -RInP 'public static (?!readonly)\S+' UXAssist/Patches/ UXAssist/Functions/ CheatEnabler/Patches/ CheatEnabler/Functions/ UniverseGenTweaks/ --include='*.cs' >> docs/StaticStateInventory.md
  ```

- [ ] **Step 2: Classify each field by subsystem**

  Mark each as:
  - `LifecycleSafe` — read-only after `Awake`
  - `NeedsReset` — must be cleared on `GameLogic.OnGameEnd`
  - `CandidateForInstancing` — should be owned by a per-game context class

- [ ] **Step 3: Commit**

  ```bash
  git add docs/StaticStateInventory.md
  git commit -m "docs: inventory static mutable state"
  ```

---

### Task 25: Add lifecycle reset callbacks

**Files:**
- Modify: `UXAssist/Patches/Factory/BeltSignalPatch.cs`
- Modify: `UXAssist/Patches/Factory/VeinProtectionPatch.cs`
- Modify: `UXAssist/Patches/DysonSpherePatch.cs`
- Modify: `UXAssist/Functions/UI/MilkyWayUI.cs`
- Modify: `CheatEnabler/Patches/Factory/BeltSignalPatch.cs`
- Modify: `UniverseGenTweaks/Patches/GalaxySelectUIPatch.cs`

- [ ] **Step 1: Add `ResetState` methods**

  Example:
  ```csharp
  internal static void ResetState()
  {
      _signalBelts = null;
      _someCache.Clear();
  }
  ```

- [ ] **Step 2: Register resets in `Init`**

  ```csharp
  public static void Init()
  {
      GameLogic.OnGameEnd += ResetState;
      // ...
  }
  ```

- [ ] **Step 3: Build all three projects**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  dotnet build CheatEnabler/CheatEnabler.csproj -c Release
  dotnet build UniverseGenTweaks/UniverseGenTweaks.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/ CheatEnabler/ UniverseGenTweaks/
  git commit -m "refactor: register static-state resets on game end"
  ```

---

## Phase 5 — Transpiler Robustness & Quality Gates

### Task 26: Annotate transpilers with target version and fallback

**Files:**
- Modify: all `*Transpiler` methods in `UXAssist/Patches/`, `CheatEnabler/Patches/`, `UniverseGenTweaks/Patches/`

- [ ] **Step 1: Add header comments**

  ```csharp
  // Target game version: 0.10.34.28505
  // Patches: EjectorComponent.InternalUpdate
  // Falls back to original IL if the pattern is not matched.
  private static IEnumerable<CodeInstruction> ...
  ```

- [ ] **Step 2: Wrap `CodeMatcher` finalization**

  Replace bare `.InstructionEnumeration()` with:
  ```csharp
  return matcher.ReportFailure(original, Logger)?.InstructionEnumeration() ?? instructions;
  ```

  Implement extension:
  ```csharp
  public static CodeMatcher ReportFailure(this CodeMatcher matcher, MethodBase original, ManualLogSource logger)
  {
      if (matcher.IsValid) return matcher;
      logger?.LogWarning($"Transpiler failed for {original.DeclaringType?.Name}.{original.Name}");
      return null;
  }
  ```

- [ ] **Step 3: Build all three projects**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  dotnet build CheatEnabler/CheatEnabler.csproj -c Release
  dotnet build UniverseGenTweaks/UniverseGenTweaks.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/ CheatEnabler/ UniverseGenTweaks/
  git commit -m "refactor: add transpiler version/fallback annotations"
  ```

---

### Task 27: Centralize third-party compat reflection targets

**Files:**
- Create: `UXAssist/ModsCompat/CompatTargets.cs`
- Modify: `UXAssist/ModsCompat/AuxilaryfunctionWrapper.cs`
- Modify: `UXAssist/ModsCompat/BulletTimeWrapper.cs`
- Modify: `UXAssist/ModsCompat/BlueprintTweaks.cs`

- [ ] **Step 1: Extract magic strings**

  ```csharp
  internal static class CompatTargets
  {
      public const string Auxilaryfunction = "auxilaryfunction.Auxilaryfunction";
      public const string SpeedUpPatch = "Auxilaryfunction.SpeedUpPatch";
      // ...
  }
  ```

- [ ] **Step 2: Replace literal strings with constants**

- [ ] **Step 3: Build UXAssist**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add UXAssist/ModsCompat/
  git commit -m "refactor(UXAssist): centralize compat reflection targets"
  ```

---

### Task 28: Add `.editorconfig` and run `dotnet format`

**Files:**
- Create: `.editorconfig`
- Modify: all touched `.cs` files (format-only changes)

- [ ] **Step 1: Create `.editorconfig`**

  Minimal content:
  ```ini
  root = true

  [*.cs]
  indent_style = space
  indent_size = 4
  charset = utf-8-bom
  end_of_line = crlf
  insert_final_newline = true
  dotnet_sort_system_directives_first = true
  dotnet_separate_import_directive_groups = false
  ```

- [ ] **Step 2: Run format**

  ```bash
  dotnet format UXAssist/UXAssist.csproj
  dotnet format CheatEnabler/CheatEnabler.csproj
  dotnet format UniverseGenTweaks/UniverseGenTweaks.csproj
  ```

- [ ] **Step 3: Build all three projects**

  ```bash
  dotnet build UXAssist/UXAssist.csproj -c Release
  dotnet build CheatEnabler/CheatEnabler.csproj -c Release
  dotnet build UniverseGenTweaks/UniverseGenTweaks.csproj -c Release
  ```

- [ ] **Step 4: Commit**

  ```bash
  git add .editorconfig
  git add -u UXAssist/ CheatEnabler/ UniverseGenTweaks/
  git commit -m "style: add editorconfig and run dotnet format"
  ```

---

## Final Verification

- [ ] **Step 1: Full solution build**

  ```bash
  dotnet build DSP_Mods.sln -c Release
  ```

  Expected: 0 errors.

- [ ] **Step 2: Package all mods**

  ```bash
  dotnet build -t:ZipMod -c Release
  dotnet build -t:CopyToParentPackage -c Release
  ```

  Expected: all `package/` outputs generated.

- [ ] **Step 3: Public API verification**

  Re-run the `docs/PublicApiSurface.md` checklist and confirm every listed member still exists.

- [ ] **Step 4: Update `AGENTS.md`**

  If any project conventions changed (e.g., new folder structure, naming rules, `.editorconfig`), update `AGENTS.md` accordingly.

- [ ] **Step 5: Final commit / tag**

  ```bash
  git tag refactor-complete
  ```

---

## Self-Review Checklist

- [ ] Every task references exact file paths.
- [ ] No `TODO`, `TBD`, or placeholder steps remain.
- [ ] Public API contract (`UXAssist.UI` + `UXAssist.Common`) is preserved throughout.
- [ ] `CheatEnabler` and `UniverseGenTweaks` compile after each phase.
- [ ] Code snippets use types/methods defined in earlier tasks.
