# DSP 0.10.35 Compatibility

## Scope and sources

This update adapts UXAssist and CheatEnabler from DSP **0.10.34.28529** to the locally installed **0.10.35.29057**, Steam build **25482430**.

The comparison uses the original `DSPGAME_Data/Managed/Assembly-CSharp.dll` from the supplied previous-version depot and the Steam installation located through the registry and `steamapps/libraryfolders.vdf`. Version history comes from each installation's `Updates/Versions.txt`. In-game terminology comes from the installed `Locale/Names`, `Locale/1033`, and `Locale/2052` tables.

The publicized, stripped assemblies in `AssemblyFromGame/` are refreshed for compilation only. They are not used to inspect native method bodies.

## Assembly changes

The Assembly-CSharp metadata and IL comparison finds:

- 69 added types and 9 removed types.
- 784 added fields and 78 removed fields.
- 663 added method signatures and 46 removed method signatures.
- 475 changed bodies among methods whose signatures remain present.

These counts include compiler-generated members. New code includes `Blackbox*`, `CreativeTool*`, and vehicle-part infrastructure, as well as vegetation collection and terrain restoration. The presence of a type does not establish that its feature is exposed in the current game UI.

## Compatibility changes

| Area | Native change | Mod adaptation |
| --- | --- | --- |
| Terrain tools | `BuildTool_Reform.ReformAction` is removed. Brush input and range checks move to `ExecuteBrushAction`; execution splits into `DoFlattenExecute` and `DoRestoreExecute`. | Retarget UXAssist's range and brush-limit patches. Allocate sufficient preview buffers for 30 x 30 brushes before preparing either terrain operation. Clamp active brushes on disable and oversized saved brushes on restore when the feature is inactive. |
| Terraform anyway | Foundation placement and Restore Terrain now have separate soil deductions. | Bypass the foundation-count check in `DoFlattenExecute`. Clamp positive soil costs to the available balance in both execution methods, while retaining negative costs that award soil. Leave native terrain restoration and foundation refunds intact. |
| Globe-view actions | `EViewMode.Create` shifts subsequent enum values: Globe is now 4 and Starmap is 5. | Match and emit named enum values. Extend the movement/mining camera-conflict getter results rather than relying on local-variable positions. |
| Ray Receiver inventory | Stored catalysts move from accumulated `catalystPoint`/`catalystIncPoint` values to `catalystCount`/`catalystInc`. `catalystPoint` becomes the active catalyst's remaining lifetime. | Apply the configured buffer limit to the stored item count without multiplying by 3,600. Planet initialization returns stored catalysts and their proliferation points, matching native item take-back semantics. |
| Mineral exhaustion | `NotifyVeinExhausted` gains `hasMinerLostAllVeins` and honors the new alert preference. | Determine whether a miner loses its last vein before removing the vein. Pass that result to the notification; oil depletion passes false, as in the native implementation. |
| Handcrafting counts | Native Shift-click shortcuts bypass the old increment/decrement path. | Replace the count-button handlers while enhanced count control is enabled. Preserve normal steps of 1, Ctrl steps of 10, Shift steps of 100, Alt steps of 1,000, and the 1–1,000 bounds. |
| Soil-change tips | `Player.SetSandCount` now updates soil progression before notifying subscribers. | Suppress `UIGame.OnSandCountChanged` instead of returning early from the player setter. Native progression and other subscribers remain intact. |
| Batch technology buyout | The technology UI changes local-variable layout and adds prerequisite checks to buyout visibility. | Replace the visibility expression with a semantic predicate for locked, non-hidden, non-obsolete technologies. Preserve the native metadata toggle, sandbox exclusion, and surrounding selected/researching state handling. |

Affected transpilers use guarded matching and return the original instructions if their required pattern is absent. The mods now target the new terrain API; this is not a dual-version compatibility layer for 0.10.34.

## Validation

Both projects build in Release with zero warnings and zero errors after refreshing the game references:

```powershell
dotnet build UpdateGameDlls/UpdateGameDlls.csproj
dotnet build UXAssist/UXAssist.csproj -c Release --no-restore
dotnet build CheatEnabler/CheatEnabler.csproj -c Release --no-restore
git diff --check
```

An ignored standalone check harness in `obj/dsp-compat/` reads the original game DLL and uses the repository's HarmonyX/BepInEx dependencies without installing game detours. It checks:

- 116 prefix/postfix target and parameter bindings.
- 187 transpiler applications: 183 transformations and 4 unchanged results that also occur with the previous DLL. No target, matcher, branch-label, or operand checks fail.
- 55 behavior assertions covering handcrafting modifiers, brush bounds and buffer reuse, disabled-feature save restoration, soil shortages and gains, buyout visibility, UI-only soil-tip suppression, and missing-pattern fallbacks.
- The two `ExecuteBrushAction` transpilers compose in either order; these assertions are included in the behavior count.

These automated checks do not verify native detour installation or Unity rendering/input behavior. The user also confirmed that testing passed. Mod versions and package manifests are not changed; the changelog entries remain unreleased.

## In-game smoke checks

Use a disposable copy of a save for destructive reset/refund and mineral-depletion checks.

1. Test foundation placement and Restore Terrain with brush sizes 1, 10, 11, and 30, including line drawing. Disable expanded brushes and reload a save made with a large brush.
2. Enable Terraform anyway with insufficient foundations and soil. Check foundation placement, terrain restoration, blueprint foundations, and base-pit removal. Soil must stay nonnegative, and soil gains and native refunds must remain available.
3. Feed a Ray Receiver proliferated catalysts, check the configured buffer limit, and verify stored catalyst refunds through planet initialization on the disposable save.
4. Exercise the mineral-exhaustion alert modes, globe-view movement/mining, and normal/Ctrl/Shift/Alt handcrafting count changes.
5. Check batch buyout with missing prerequisites and verify hidden, obsolete, and already-unlocked technologies remain excluded. Hide soil tips and confirm soil-related progression still advances.
