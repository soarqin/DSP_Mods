# Blueprint Description Validation

This document records the native layout evidence for the UXAssist description-header entry and the checks needed for in-game acceptance. Compilation and managed production checks do not establish compatibility with other blueprint mods.

## Native evidence

Inspected on 2026-11-01 after discovering Steam through `HKCU`'s Steam path and `steamapps/libraryfolders.vdf`:

- Original assembly: `D:/Steam/steamapps/common/Dyson Sphere Program/DSPGAME_Data/Managed/Assembly-CSharp.dll`, SHA-256 `EF314425DADE51F529F184F07BE136CD3C2660A6A81515603C7226FBE7AB1D8C`.
- Original `UnityEngine.UI.dll`, SHA-256 `C5ACC25EBD597B586D323AF506FB88E760322550940F1A3B0EA4FC8143FCB26C`.
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

The native `descGroupTrans` in all three inspectors contains a description label at the top and an input beginning 22 pixels below it. The compact UXAssist entry follows the native title's measured text width plus 8 pixels and reserves 8 pixels before the actual icon button's left edge, including when the icon is inactive. It hides if there is insufficient room. Options open next to the actual entry and follow it after layout and scrolling; only owned transforms are positioned. Native labels, inputs, groups, scroll content, and the separate settings panel retain their native layout.

`UIBlueprintInspector.Refresh(forModify: true)` does not reload `descTextInput`. Successful generation must assign both the blueprint description and its input before using the native modification refresh. Pending custom fields must first pass through the native field collection and escaping rules. Native copy and save handlers serialize the current blueprint, so they remain the actions used to share or persist a generated description.

Decompiled methods and extracted rectangle data are development artifacts under `UXAssist/obj/native-inspector/`. Reinspect the installed original assembly and assets after a game update; the publicized compile-time references omit method bodies.

## Anchored options and native conventions

The original DLL and `level0` hashes and installed version above were rechecked on 2026-11-05. The options use the entry's world corners, transformed into the popup parent's coordinates, and actual root-canvas bounds. Placement prefers below the entry with a 6-pixel gap and aligned right edges, flips above when needed, and clamps against an 8-pixel canvas inset. A panel too large for that area is closed. `Canvas.willRenderCanvases` reconciles placement after native layout only while open. An inactive or fully clipped entry closes its popup.

The panel has an `Auto-fill description` title and native X, proliferation, production basis with help, and one footer `Generate` action. Content insets include the actual sliced sprite's transparent trim before adding the desired visible gaps. Preferred glyph sizes determine label, row, item, and button heights; the 20-pixel checkbox keeps an independently sized label with an 8-pixel gap. Panel and footer height follow the measured rows and expanded list.

The X comes from `inserterWindow/panel-bg/btn-box/close-wnd-btn`: a 40-by-30 native button with a centered 16-by-16 icon and a transparent 30-by-19 raycast child. Its original serialized `Button.onClick` targets the original inserter window's `_Close`. The owned clone replaces the entire `Button.ButtonClickedEvent` before adding its popup callback; removing runtime listeners alone would leave that persistent callback active.

The source production-basis combo also carries source-specific behavior. Its root `Localizer` uses `wParamKey = "恒星信息面板组合框宽度"`, `sizeApplyTrans = null`, and `def_w = 0`. `OnEnable()` and `OnLanguageChange()` call `Refresh()`, which writes the root's width. Native locale parameters are 200 in `Locale/1033/parameters.txt` and 90 in `Locale/2052/parameters.txt`, both at line 28. The owned clone disables and removes this component before assigning dimensions, and disables `autoWidth` before setting items and index. Its main button fills the owned root, with selected and row-template text aligned left. Correct native horizontal anchors are retained. Native combo row `PointerDown` first selects the item, then closes the list; no extra submit-close callback is added.

| Original source | Behavior used by the popup |
| --- | --- |
| `UIRoot._OnUpdate()` and `EscapeInputLogic()` | Give native message boxes Escape priority before the game UI updates. |
| `UIGame._OnUpdate()` | Updates blueprint inspectors before later native picker and function-window Escape handlers. The owned inspector postfix consumes Escape after closing an expanded dropdown or its popup. |
| `UIItemPicker._OnUpdate()` | Tests left, right, and middle clicks outside a screen rectangle using the overlay canvas camera. The owned test also accepts the entry and an open dropdown. |
| `UIPopupMenu.SetState()` | Constrains popup placement to game UI bounds and brings it to the front. |
| Native blueprint settings, color-palette input rows, and inspector footer | Supply 20-pixel checkbox, 28-pixel input-row, and 30-pixel primary-action conventions. |
| Native inserter close-button subtree and combo row event routing | Supply the actual X artwork/hit area and the select-then-close event order. |

The evidence bundle `2026-11-04-UXAssist-BlueprintDescriptionLayout/anchored-options/` retains the native close-button and event-routing captures. The current `2026-11-05-UXAssist-BlueprintTextLayout/` bundle contains the actual reported screenshots, `native-text-layout-evidence.md`, focused font/locale/header captures, original UI and text-generator decompilations, and sprite analysis. The sliced background has approximately 13-pixel horizontal and 11-pixel vertical trim before pixels-per-unit conversion. The original checkbox's 26-pixel label was shortened to 20 while retaining `Truncate`; native font metrics and generator rules explain the discarded first line. Native header captures also confirm the description input's icon occupies the rightmost 20 pixels of its title row. These are original-asset and managed-rule findings, not an executed Unity rendering test. Investigation reports and the headless-test feasibility assessment remain outside the repository.

## Recorded checks

The temporary placement harness extracts the actual private `TryGetPopupPosition` method body from `BlueprintDescriptionPatch.cs` and compiles it with data-only `Rect` and `Vector2` substitutes. It does not mirror the placement algorithm in a second implementation. Sources and results are preserved under `anchored-options/actual-placement-check/` and `actual-placement-check.json`.

The placement harness passed 8 exact cases, 3 oversized-panel rejection cases, and 1,920 native-parent fixtures, totaling 20,191 assertions. Fixtures cover the 392- and 394-pixel native inspector frames, translated and scaled parent coordinates, scales 0.75/1/1.25/1.5, scrolling, below/above placement, canvas edges, and supplied panel sizes. Widths of 320/360/440/520 pixels are test inputs, rather than measured English or Chinese font widths. The footer checks verify a 16-pixel gap after the list. These checks validate placement for supplied bounds, not current text sizing or Unity rendering.

On 2026-11-05, `native-text-regression-results.json` recorded 48 passing analytical cases using the original embedded fonts, sprite data, and native header bounds. They reproduced the old transparent-margin overflow, 20-pixel checkbox truncation, and native icon overlap, then checked the updated geometry. Embedded glyph outlines and metrics do not reproduce Unity's runtime preferred-size calculation, rasterization, or interaction. The Release build also passed with 0 warnings and 0 errors, and all 569 `ProductionCheck` cases passed again.

Development checks recorded on 2026-11-04:

| Check | Result |
| --- | --- |
| Normal Release build, using the command below | Passed with 0 warnings and 0 errors. |
| `ProductionCheck` | 569 checks passed. |
| Actual-source placement harness | All cases and assertions above passed on the final source. |
| Whole-diff code review and `git diff --check` | Passed. |

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

1. Open each inspector in English and Simplified Chinese with different custom-field counts. Focus the description input so its native icon button appears; the Auto-fill entry must remain after the title and before that icon. Open the options and confirm the proliferation label, button captions, and dropdown text are visible inside the background's visible fill, with a title, native X, and one footer action. At screen edges, confirm it flips or shifts into available canvas space.
2. Scroll while the popup is open. Confirm it follows the entry after native layout and closes when the entry is fully clipped. Expand native foundation, fill-only, and reform settings; all native fields must retain their positions and sizes, and scrolling must reach every field.
3. Open the production-basis list. Confirm both readable, left-aligned rows fit within the expanded background above the footer. Select each item and confirm it closes the list without generating text. Reopen the popup and change language; selector dimensions and footer spacing must remain valid.
4. Close the popup with its X; the inspector and an open inserter window must remain open. Reopen and click outside with the left, right, and middle mouse buttons: the popup should close, and the clicked native control or world target should retain its normal behavior. Press Esc with the list expanded: only the list should close. Press Esc again: the popup should close while the inspector stays open. With a native message box present, its Escape handler must retain priority.
5. Reopen each inspector and repeatedly refresh or change the blueprint selection. Confirm there is one entry, options are initially closed, the production basis resets to final-product output on opening, Spray Coaters set the automatic proliferation default, and a manual checkbox choice survives content refreshes in the current session. Successful generation, a replacement blueprint, and inspector closure must dismiss the popup.
6. Generate both production bases for an unbalanced chain. Final-product output should retain rated final capacity and show the missing intermediate; lowest intermediate throughput should reduce the common final-output mix, use correspondingly reduced external inputs, and show no internal shortage when the chain is complete and balanceable. Power should remain the same full-load estimate. Check necessary coproduct output separately from unused intermediate capacity.
7. Edit the description and unrelated custom fields before generating. Generate twice, including with 20 custom fields and with existing English or Chinese power fields. Confirm unrelated edits survive, duplicate power fields are consolidated, and failed generation leaves the prior description and fields intact. Include missing building settings, an unavailable bottleneck basis, and the native description length limit.
8. Save, copy the share code, and reopen or paste the blueprint through the native buttons. Confirm the generated main description, power field, and unrelated fields persist correctly. With a research-mode lab, confirm net produced matrices appear under Research without fabricated matrix imports or research speed.
9. Restart the test profile without UXAssist, then restore it and restart again. Confirm the entry and options are absent without UXAssist, native fields keep their normal layout in both cases, and repeated opening and generation work with the installed blueprint mods. `OnDisable` and inspector-destruction cleanup are code-review checks unless a development harness exercises them; a supported user-facing hot-disable workflow is not assumed.
