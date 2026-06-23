# Phase 5 — Transpiler Robustness & Code Quality Gates

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Harmony transpilers easier to diagnose on game updates, consolidate mod-compatibility reflection behind a single helper API, and add build-level quality gates.

**Architecture:**
- A small `TranspilerGuard` helper standardizes the `CodeMatcher.IsInvalid` fallback pattern so transpilers can return original instructions and log a warning instead of producing invalid IL.
- A public `ModCompatHelper` in `UXAssist.Common.ModCompat` centralizes BepInEx plugin detection, external-type resolution, and member lookup; wrappers and DysonSphere reflection consumers migrate to it.
- A root `.editorconfig`, stricter MSBuild warning settings, and a GitHub Actions build workflow provide guardrails without blocking the existing obsolete-warning surface.

**Tech Stack:** C# / .NET Framework 4.7.2 / BepInEx 5 / HarmonyLib / MSBuild / GitHub Actions

---

## Task 1: Transpiler comments & fallback guards

**Files:**
- Create: `UXAssist/Common/Patching/TranspilerGuard.cs`
- Modify: all files listed in `docs/TranspilerAudit.md` (26 files, 115 transpilers)

- [ ] **Step 1.1: Create `TranspilerGuard`**

```csharp
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace UXAssist.Common.Patching;

public static class TranspilerGuard
{
    public static IEnumerable<CodeInstruction> Finish(
        this CodeMatcher matcher,
        IEnumerable<CodeInstruction> originalInstructions,
        ManualLogSource logger,
        string transpilerName)
    {
        if (matcher.IsInvalid)
        {
            logger?.LogWarning($"Transpiler '{transpilerName}' failed to match; returning original instructions.");
            return originalInstructions;
        }
        return matcher.InstructionEnumeration();
    }
}
```

- [ ] **Step 1.2: Add standardized header comments to every transpiler**

For each transpiler method, insert a comment block immediately above the method:

```csharp
// Harmony transpiler target: <Full.TypeName>.<MethodName>(<signature>)
// Purpose: <one-line description>
// Fragile matches: <magic constants / local slots / field names>
// Fallback: <returns original instructions via TranspilerGuard if matcher becomes invalid / or "None — patch will fail loudly if target method changes">
```

Use the audit in `docs/TranspilerAudit.md` (produced by exploration) to fill Fragile matches.

- [ ] **Step 1.3: Add fallback guards where missing**

For transpilers that currently call `matcher.InstructionEnumeration()` without checking `IsInvalid`, change the last lines to:

```csharp
return matcher.Finish(instructions, UXAssist.Logger, nameof(<TranspilerMethodName>));
```

Preserve the return type `IEnumerable<CodeInstruction>`. Where a method already checks `matcher.IsInvalid`/`IsValid`, keep the existing logic and only add the comment.

- [ ] **Step 1.4: Build after comment/guard pass**

Run: `dotnet build DSP_Mods.sln -c Release`
Expected: 0 errors, 0 new warnings.

---

## Task 2: Centralize mod-compatibility reflection

**Files:**
- Create: `UXAssist/Common/ModCompat/ModCompatHelper.cs`
- Create: `UXAssist/Common/Utils/DysonSphereReflection.cs`
- Modify: `UXAssist/ModsCompat/AuxilaryfunctionWrapper.cs`
- Modify: `UXAssist/ModsCompat/BlueprintTweaks.cs`
- Modify: `UXAssist/ModsCompat/BulletTimeWrapper.cs`
- Modify: `UXAssist/ModsCompat/CommonAPIWrapper.cs`
- Modify: `UXAssist/ModsCompat/PlanetVeinUtilization.cs`
- Modify: `UXAssist/Patches/DysonSpherePatch.cs`
- Modify: `CheatEnabler/Functions/DysonSphere/ShellCompletionFunctions.cs`
- Modify: `CheatEnabler/Functions/DysonSphere/FrameRemovalFunctions.cs`

- [ ] **Step 2.1: Create `ModCompatHelper`**

```csharp
using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;

namespace UXAssist.Common.ModCompat;

public static class ModCompatHelper
{
    public static bool TryGetLoadedPluginInfo(string guid, out PluginInfo pluginInfo)
        => Chainloader.PluginInfos.TryGetValue(guid, out pluginInfo) && pluginInfo != null;

    public static bool TryGetPluginType(PluginInfo pluginInfo, string typeName, out Type type)
    {
        type = null;
        if (pluginInfo?.Instance == null) return false;
        try
        {
            type = pluginInfo.Instance.GetType().Assembly.GetType(typeName, throwOnError: false);
        }
        catch { /* ignored */ }
        return type != null;
    }

    public static bool TryGetPluginType(string guid, string typeName, out Type type)
    {
        type = null;
        return TryGetLoadedPluginInfo(guid, out var pluginInfo) && TryGetPluginType(pluginInfo, typeName, out type);
    }

    public static bool TryGetFieldValue<T>(Type type, string fieldName, object instance, out T value)
    {
        value = default;
        if (type == null) return false;
        var field = AccessTools.Field(type, fieldName);
        if (field == null) return false;
        try
        {
            var result = field.GetValue(instance);
            if (result is T t)
            {
                value = t;
                return true;
            }
        }
        catch { /* ignored */ }
        return false;
    }

    public static bool TryGetMethod(Type type, string methodName, out MethodInfo method)
    {
        method = null;
        if (type == null) return false;
        method = AccessTools.Method(type, methodName);
        return method != null;
    }

    public static bool TryGetPropertySetter(Type type, string propertyName, out MethodInfo setter)
    {
        setter = null;
        if (type == null) return false;
        var property = AccessTools.Property(type, propertyName);
        if (property == null) return false;
        setter = property.GetSetMethod(nonPublic: true);
        return setter != null;
    }
}
```

- [ ] **Step 2.2: Refactor mod-compat wrappers to use `ModCompatHelper`**

Replace the duplicated `Chainloader.PluginInfos.TryGetValue` + `pluginInfo.Instance.GetType().Assembly.GetType(...)` + `AccessTools.Field/Method/PropertySetter` patterns with calls to `ModCompatHelper`. Keep the existing public static fields (e.g., `HasBulletTime`, `ShowStationInfo`) and init timing (`Start`/`Run`).

- [ ] **Step 2.3: Create `DysonSphereReflection`**

```csharp
using System.Reflection;
using HarmonyLib;

namespace UXAssist.Common.Utils;

public static class DysonSphereReflection
{
    private static readonly FieldInfo TotalNodeSpField = AccessTools.Field(typeof(DysonSphereLayer), "totalNodeSP");
    private static readonly FieldInfo TotalFrameSpField = AccessTools.Field(typeof(DysonSphereLayer), "totalFrameSP");
    private static readonly FieldInfo TotalCpField = AccessTools.Field(typeof(DysonSphereLayer), "totalCP");

    public static long? GetTotalNodeSP(DysonSphereLayer layer)
        => layer != null && TotalNodeSpField != null ? (long?)TotalNodeSpField.GetValue(layer) : null;

    public static long? GetTotalFrameSP(DysonSphereLayer layer)
        => layer != null && TotalFrameSpField != null ? (long?)TotalFrameSpField.GetValue(layer) : null;

    public static long? GetTotalCP(DysonSphereLayer layer)
        => layer != null && TotalCpField != null ? (long?)TotalCpField.GetValue(layer) : null;

    public static bool IsAvailable => TotalNodeSpField != null && TotalFrameSpField != null && TotalCpField != null;
}
```

- [ ] **Step 2.4: Migrate DysonSphere field consumers**

Update `UXAssist/Patches/DysonSpherePatch.cs`, `CheatEnabler/Functions/DysonSphere/ShellCompletionFunctions.cs`, and `CheatEnabler/Functions/DysonSphere/FrameRemovalFunctions.cs` to call `DysonSphereReflection` instead of resolving the fields locally. Remove duplicate `AccessTools.Field` declarations.

- [ ] **Step 2.5: Build after reflection refactor**

Run: `dotnet build DSP_Mods.sln -c Release`
Expected: 0 errors, 0 new warnings.

---

## Task 3: Code quality gates

**Files:**
- Create: `.editorconfig`
- Modify: `Directory.Build.props`
- Create: `.github/workflows/build.yml`

- [ ] **Step 3.1: Add root `.editorconfig`**

```ini
root = true

[*]
charset = utf-8
indent_style = space
indent_size = 4
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

[*.md]
trim_trailing_whitespace = false

[*.{cs,vb}]
# Suggestion-only so existing code does not break the build
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion
dotnet_style_require_accessibility_modifiers = for_non_interface_members:suggestion
dotnet_style_readonly_field = true:suggestion
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = true:suggestion
csharp_prefer_simple_default_expression = true:suggestion
csharp_style_pattern_local_over_anonymous_function = true:suggestion
```

- [ ] **Step 3.2: Harden `Directory.Build.props`**

Add to the existing `<PropertyGroup>` (do not remove existing properties):

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<NoWarn>0618</NoWarn>
<Nullable>disable</Nullable>
<AnalysisLevel>none</AnalysisLevel>
<EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>
```

`NoWarn>0618` suppresses the expected `[Obsolete]` usage warnings from the old `Util` facade; any new warning class will fail the build.

- [ ] **Step 3.3: Add GitHub Actions build workflow**

Create `.github/workflows/build.yml`:

```yaml
name: Build

on:
  push:
    branches: [main, master, refactor/*]
  pull_request:
    branches: [main, master]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore DSP_Mods.sln

      - name: Build Release
        run: dotnet build DSP_Mods.sln -c Release --no-restore

      - name: Package mods
        run: dotnet build -t:ZipMod -c Release --no-restore
```

- [ ] **Step 3.4: Verify quality gates**

Run:
```bash
dotnet clean DSP_Mods.sln
dotnet build DSP_Mods.sln -c Release
```
Expected: 0 errors, 0 warnings.

Run:
```bash
dotnet build -t:ZipMod -c Release
```
Expected: All zips produced; no errors.

---

## Task 4: Documentation & checkpoint

**Files:**
- Modify: `AGENTS.md`

- [ ] **Step 4.1: Update `AGENTS.md`**

Append a "Phase 5 — Transpiler Robustness & Code Quality Gates" subsection under the project overview describing:
- `UXAssist.Common.Patching.TranspilerGuard`
- `UXAssist.Common.ModCompat.ModCompatHelper`
- `UXAssist.Common.Utils.DysonSphereReflection`
- `.editorconfig`, `TreatWarningsAsErrors`, and the GitHub Actions build workflow.

- [ ] **Step 4.2: Tag checkpoint**

```bash
git add -A
git commit -m "refactor: phase 5 transpiler robustness, mod-compat helpers, and build quality gates"
git tag refactor-phase5
```

Expected: tag `refactor-phase5` exists on the new commit and build remains clean.
