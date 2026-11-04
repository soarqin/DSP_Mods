# Blueprint Description Validation

This document records the native layout evidence for the UXAssist description-header entry and the checks needed for in-game acceptance. Compilation and managed production checks do not establish compatibility with other blueprint mods.

## Native evidence

Inspected on 2026-11-01 after discovering Steam through `HKCU`'s Steam path and `steamapps/libraryfolders.vdf`:

- Original assembly: `D:/Steam/steamapps/common/Dyson Sphere Program/DSPGAME_Data/Managed/Assembly-CSharp.dll`, SHA-256 `EF314425DADE51F529F184F07BE136CD3C2660A6A81515603C7226FBE7AB1D8C`.
- Native UI assets: `DSPGAME_Data/level0`, SHA-256 `04655A46BBEB113113688BCDC0E0EA8EC5F128B348E32194FC9743ADB48C8E91`.
- The installed version history ends at `0.10.35.29151`; the Steam manifest reports build ID `25955035`. Assembly file version `0.0.0.0` alone does not identify this game build.

The original `UIBlueprintInspector.Refresh` sizes the description and native groups, places groups 2 through 4, and updates the scroll content height. It does not restore group 1's position. The previous UXAssist layout shifted group 1 and repeatedly offset the later groups and content height, making both the native refresh and other layout patches share ownership of those transforms.

The relevant original methods can be reproduced by decompiling the following types from the original assembly:

| Native method | Observed behavior |
| --- | --- |
| `UIBlueprintInspector.Refresh(bool forModify, bool refreshComponent, bool refreshCode, bool loadExistHead = false)` | Controls native group sizes, later group positions, and content height; reloads the main description input only when `forModify` is false. |
| `UIBlueprintInspector._OnOpen()` and `_OnClose()` | Refresh and open native settings for the relevant mode; clear the blueprint on close. |
| `ManualBehaviour._Close()` | Deactivates the inspector GameObject, hiding children. |
| `UIBlueprintInspector.OnCustomFieldEndEdit(string str)` and `CollectExternalFields(int excludeTargetId = -1)` | Collect, escape, and validate pending custom-field edits through the native path. |
| `UIBlueprintInspector.OnCopyClick()`, `OnSaveChangesClick()`, and `OnSaveClick()` | Serialize or save the current blueprint with the game's native workflow. |

The native `descGroupTrans` in all three inspectors contains a description label at the top and an input beginning 22 pixels below it. The compact UXAssist entry occupies the right side of this existing title row; it does not move the title, input, groups, or scroll content. Options appear only on request inside the inspector and close after successful generation or inspector closure. The browser's file grid remains outside the UXAssist controls. Following the separate native settings panel is unnecessary, and no per-frame positioning patch is used.

`UIBlueprintInspector.Refresh(forModify: true)` does not reload `descTextInput`. Successful generation must assign both the blueprint description and its input before using the native modification refresh. Pending custom fields must first pass through the native field collection and escaping rules. Native copy and save handlers serialize the current blueprint, so they remain the actions used to share or persist a generated description.

Decompiled methods and extracted rectangle data are development artifacts under `UXAssist/obj/native-inspector/`. Reinspect the installed original assembly and assets after a game update; the publicized compile-time references omit method bodies.

## Options layout correction on 2026-11-04

The reported screenshot shows the production-basis selector displaced to the right and its expanded list wider than the options panel. The correction applies to this selector in the existing on-demand options.

The selector is cloned from `UIRoot.instance.uiGame.starDetail.displayCombo`. Its root inherits a `Localizer` with `wParamKey = "恒星信息面板组合框宽度"`, `sizeApplyTrans = null`, and `def_w = 0`. Original `Localizer.OnEnable()` subscribes to language changes and immediately calls `Refresh()`; `OnLanguageChange()` also calls `Refresh()`. With no separate size target, `Refresh()` writes the translated positive width to the combo root's `sizeDelta.x`. The blueprint selector uses horizontal stretch with an intended width delta of `-24`, so this source-specific width assignment expands the root beyond its parent. Reopening the options re-enables the inherited component, explaining why an initial size assignment alone does not hold.

The original DLL and `level0` hashes and the installed version above were rechecked on 2026-11-04. The native locale parameter is 200 in `Locale/1033/parameters.txt` and 90 in `Locale/2052/parameters.txt`, both at line 28. With the original 392- and 394-pixel inspector variants, the inherited assignment produces combo widths of 568/570 in English or 458/460 in Simplified Chinese. The corrected stretched root is 344/346 pixels wide, and the main button, list, content, row template, and selection share its horizontal bounds.

The original combo also has a right-anchored, right-pivoted main button with its own fixed width and right-aligned selected and item-template text. The correction disables and removes only the owned clone root's `Localizer` before setting its geometry, disables `autoWidth` before setting items or index, stretches the main button to its owned root, and left-aligns the selected and template text. Source dropdown, content, template, and selection rectangles already use horizontal stretch with zero width delta and are retained. Original `UIComboBox.SetState()` adjusts list and content heights and row positions, preserving their width deltas; it does not repair a source-specific root width.

The popup remains 152 pixels high, with a 28-pixel selector starting 108 pixels below its top. Native dropdown placement adds a 2-pixel gap and uses two 18-pixel rows with 4 pixels of total padding. The expanded list therefore reaches 178 pixels below the popup top, extending 26 pixels below the background as a native dropdown overlay. Check this overlay in-game at the target UI scale.

The persistent task evidence bundle `2026-11-04-UXAssist-BlueprintDescriptionLayout` retains `reported-layout.png`, the native combo and background JSON captures, focused `Localizer` and `UIComboBox` decompilations, `inspect_native_layout.py`, `reproduce_geometry.py`, and `geometry-lifecycle-replay.json`. Rectangle calculations or schematic previews derived from these files are evidence of geometry, not screenshots of a repaired running game.

Recorded checks for this correction:

| Check | Result |
| --- | --- |
| Normal Release build, using the command below | Passed with 0 warnings and 0 errors. |
| `ProductionCheck` | 569 checks passed. |
| Original-asset geometry replay | 64 analytical combinations passed: inspector widths 392/394, English/Simplified Chinese, first opening/two reopenings/language change, and scales 0.75/1/1.25/1.5. |
| Focused UI code review and `git diff --check` | Passed. |

The geometry replay calculates rectangle bounds from captured assets and the original `Localizer` rules. It does not execute Unity enable or language-change events, measure font rendering or preferred text width, or test mouse interaction. The evidence bundle's `test-build/UXAssist.dll` has SHA-256 `3645ebfe03fb5ed7bc50459f25e0e6557d903db852624d640eafbd588e735ef5`.

## Managed production checks

Run the production checks and normal UXAssist build from the repository root:

```powershell
dotnet run --project UXAssist/tools/ProductionCheck/ProductionCheck.csproj -c Release
dotnet build UXAssist/UXAssist.csproj -c Release --no-restore
git diff --check
```

The production-basis cases check an undersupplied chain, recipe-ratio conversion, shared intermediate demand, joint products, a cycle with an output, research-matrix outlets, no internal intermediate, power-only selections, very different capacity scales, and unavailable material settings or final output. A successful bottleneck report must conserve material, normalize inputs and outputs to the same throughput, preserve rated group capacity and power, and avoid internal shortages. Existing default-mode expectations must continue to pass.

## Manual acceptance

No in-game, Unity event-loop, or third-party mod integration test has been performed for this change. Repeat these checks at the target profile's resolutions and UI scales, with each installed blueprint UI mod. The header entry avoids native layout writes but another mod may use the same extension space.

1. Open all three inspectors in English and Simplified Chinese. Confirm the header entry is readable without covering the title or editable description. Open, close, and reopen its options, and switch languages while they are open. The production-basis selector and both dropdown rows must retain matching widths inside the inspector, with readable left-aligned labels. Generate and close the inspector; options must not remain over the file grid or survive an inspector close.
2. Scroll the native description and custom fields and expand native foundation, fill-only, and reform settings. All native fields must retain their positions and sizes, and scrolling must reach every field.
3. Reopen each inspector and repeatedly refresh or change the blueprint selection. Confirm there is one entry, options are initially closed, the production basis resets to final-product output on opening, Spray Coaters set the automatic proliferation default, and a manual checkbox choice survives content refreshes in the current session.
4. Generate both production bases for an unbalanced chain. Final-product output should retain rated final capacity and show the missing intermediate; lowest intermediate throughput should reduce the common final-output mix, use correspondingly reduced external inputs, and show no internal shortage when the chain is complete and balanceable. Power should remain the same full-load estimate. Check necessary coproduct output separately from unused intermediate capacity.
5. Edit the description and unrelated custom fields before generating. Generate twice, including with 20 custom fields and with existing English or Chinese power fields. Confirm unrelated edits survive, duplicate power fields are consolidated, and failed generation leaves the prior description and fields intact. Include missing building settings, an unavailable bottleneck basis, and the native description length limit.
6. Save, copy the share code, and reopen or paste the blueprint through the native buttons. Confirm the generated main description, power field, and unrelated fields persist correctly. With a research-mode lab, confirm net produced matrices appear under Research without fabricated matrix imports or research speed.
7. Restart the test profile without UXAssist, then restore it and restart again. Confirm the entry and options are absent without UXAssist, native fields keep their normal layout in both cases, and repeated opening and generation work with the installed blueprint mods. `OnDisable` and inspector-destruction cleanup are code-review checks unless a development harness exercises them; a supported user-facing hot-disable workflow is not assumed.
