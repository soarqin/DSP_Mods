# Blueprint Description Validation

This document records the native layout evidence for the UXAssist description-header entry and the checks needed for in-game acceptance. Compilation and managed production checks do not establish compatibility with other blueprint mods.

## Native evidence

Original installed DSP sources, rechecked on 2026-11-05 after discovery through the Steam registry and `steamapps/libraryfolders.vdf`:

- Original assembly: `D:/Steam/steamapps/common/Dyson Sphere Program/DSPGAME_Data/Managed/Assembly-CSharp.dll`, SHA-256 `EF314425DADE51F529F184F07BE136CD3C2660A6A81515603C7226FBE7AB1D8C`.
- Original `UnityEngine.UI.dll`, SHA-256 `C5ACC25EBD597B586D323AF506FB88E760322550940F1A3B0EA4FC8143FCB26C`.
- Native UI assets: `DSPGAME_Data/level0`, SHA-256 `04655A46BBEB113113688BCDC0E0EA8EC5F128B348E32194FC9743ADB48C8E91`.
- The installed version history ends at `0.10.35.29151`; the Steam manifest reports build ID `25955035`. Assembly file version `0.0.0.0` alone does not identify this game build.

Native `UIBlueprintInspector.Refresh` owns description and group sizes, later group positions, and scroll content height. UXAssist positions only its own entry and popup.

The relevant original methods can be reproduced by decompiling the following types from the original assembly:

| Native method | Observed behavior |
| --- | --- |
| `UIBlueprintInspector.Refresh(bool forModify, bool refreshComponent, bool refreshCode, bool loadExistHead = false)` | Controls native group sizes, later group positions, and content height; reloads the main description input only when `forModify` is false. |
| `UIBlueprintInspector._OnOpen()` and `_OnClose()` | Refresh and open native settings for the relevant mode; clear the blueprint on close. |
| `ManualBehaviour._Close()` | Deactivates the inspector GameObject, hiding children. |
| `UIBlueprintInspector.OnCustomFieldEndEdit(string str)` and `CollectExternalFields(int excludeTargetId = -1)` | Collect, escape, and validate pending custom-field edits through the native path. |
| `UIBlueprintInspector.OnCopyClick()`, `OnSaveChangesClick()`, and `OnSaveClick()` | Serialize or save the current blueprint with the game's native workflow. |

The native description input starts 22 pixels below its title. The entry copies the title's font, size, style, line spacing, and alignment, with zero top text inset, while its hit rectangle stays 22 pixels high. It follows the title's measured text width plus 8 pixels and reserves 8 pixels before the actual native icon, including when the icon is inactive. It hides if it cannot fit. The title's 30-pixel Text rectangle must not become the button's hit rectangle, which would extend into the input below. Native labels, inputs, groups, scroll content, and settings retain their layout.

`UIBlueprintInspector.Refresh(forModify: true)` does not reload `descTextInput`. Successful generation must assign both the blueprint description and its input before using the native modification refresh. Pending custom fields must first pass through the native field collection and escaping rules. Native copy and save handlers serialize the current blueprint, so they remain the actions used to share or persist a generated description.

Reinspect the installed original assemblies and assets after a game update; the publicized compile-time references omit method bodies.

## Anchored options and native conventions

The popup uses the entry's world corners, transformed into its parent's coordinates, and actual root-canvas bounds. Placement prefers below the entry with a 6-pixel gap and aligned right edges, flips above when needed, and clamps against an 8-pixel canvas inset. A panel too large for that area is closed. `Canvas.willRenderCanvases` reconciles placement while open. An inactive or fully clipped entry closes its popup.

The panel has a title and native X, proliferation, production basis with help, and one footer `Generate` action. Content insets include the sliced sprite's approximately 13-pixel horizontal and 11-pixel vertical trim before pixels-per-unit conversion. The title has extra left and top clearance from the visible border. The 20-pixel checkbox has an independently sized label with a 4-pixel gap and vertical overflow allowed; label height remains at least 26 pixels or preferred glyph height plus padding. The panel and footer are sized from the form alone. Opening the dropdown overlays the footer without resizing the popup or moving the action.

The X comes from `inserterWindow/panel-bg/btn-box/close-wnd-btn`: a 40-by-30 native button with a centered 16-by-16 icon and a transparent 30-by-19 raycast child. Its original serialized `Button.onClick` targets the original inserter window's `_Close`. The owned clone replaces the entire `Button.ButtonClickedEvent` before adding its popup callback; removing runtime listeners alone would leave that persistent callback active.

The production-basis selector directly clones the standard rectangular `optionWindow.resolutionComp`. Cloned runtime resolution rows are removed and submit/change events are replaced before the two production choices are assigned, so changing the owned selection cannot change native resolution settings. The main caption uses 8-pixel left and 30-pixel right insets; row text uses 8-pixel horizontal insets. Native row selection closes the list, with no extra submit-close handler. The shared combo helper is unchanged.

| Original source | Behavior used by the popup |
| --- | --- |
| `UIRoot._OnUpdate()` and `EscapeInputLogic()` | Give native message boxes Escape priority before the game UI updates. |
| `UIGame._OnUpdate()` | Updates blueprint inspectors before later native picker and function-window Escape handlers. The owned inspector postfix consumes Escape after closing an expanded dropdown or its popup. |
| `UIItemPicker._OnUpdate()` | Tests left, right, and middle clicks outside a screen rectangle using the overlay canvas camera. The owned test also accepts the entry and an open dropdown. |
| `UIPopupMenu.SetState()` | Constrains popup placement to game UI bounds and brings it to the front. |
| Native inserter close-button subtree and combo row event routing | Supply the actual X artwork/hit area and the select-then-close event order. |

Original font, sprite, header, standard-resolution-control, and close-button captures and focused decompilations are retained in the external task evidence under `2026-11-05-UXAssist-BlueprintTextLayout/`, including `standard-controls/`. Native generator rules support keeping glyph height independent of checkbox size; native header captures identify the icon's actual 20-pixel slot. Source analysis and embedded-font calculations do not execute Unity's runtime preferred-size calculation, rasterization, or interaction. Investigation reports remain outside the repository.

## Recorded checks

The placement harness extracts the actual private `TryGetPopupPosition` method and compiles it with data-only `Rect` and `Vector2` substitutes. It checks supplied bounds, including parent transforms, canvas edges, below/above placement, and oversized-panel rejection. This validates the placement calculation, not current text rendering. Run font and sprite analysis against the current source and captured standard control.

For this revision, the normal Release build passed with 0 warnings and 0 errors, and `git diff --check` passed. These checks do not execute Unity rendering or interaction.

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

1. Open each inspector in English and Simplified Chinese with different custom-field counts. Confirm the entry text shares the native title's baseline. Focus the description input so its icon appears; the entry must remain before that icon and its clickable area must not cover the input below. Open the options and check title clearance, compact checkbox spacing, visible captions, native X, and one footer action. At screen edges, confirm it flips or shifts into available canvas space.
2. Scroll while the popup is open. Confirm it follows the entry after native layout and closes when the entry is fully clipped. Expand native foundation, fill-only, and reform settings; all native fields must retain their positions and sizes, and scrolling must reach every field.
3. Open the rectangular production-basis list. Confirm text has clear left padding and space for the arrow. The list must overlay the footer without changing panel size or footer position. Select each item and confirm it closes the list without generating text or changing the native resolution setting. Reopen the popup and change language; selector dimensions and text spacing must remain valid.
4. Close the popup with its X; the inspector and an open inserter window must remain open. Reopen and click outside with the left, right, and middle mouse buttons: the popup should close, and the clicked native control or world target should retain its normal behavior. Press Esc with the list expanded: only the list should close. Press Esc again: the popup should close while the inspector stays open. With a native message box present, its Escape handler must retain priority.
5. Reopen each inspector and repeatedly refresh or change the blueprint selection. Confirm there is one entry, options are initially closed, the production basis resets to final-product output on opening, Spray Coaters set the automatic proliferation default, and a manual checkbox choice survives content refreshes in the current session. Successful generation, a replacement blueprint, and inspector closure must dismiss the popup.
6. Generate both production bases for an unbalanced chain. Final-product output should retain rated final capacity and show the missing intermediate; lowest intermediate throughput should reduce the common final-output mix, use correspondingly reduced external inputs, and show no internal shortage when the chain is complete and balanceable. Power should remain the same full-load estimate. Check necessary coproduct output separately from unused intermediate capacity.
7. Edit the description and unrelated custom fields before generating. Generate twice, including with 20 custom fields and with existing English or Chinese power fields. Confirm unrelated edits survive, duplicate power fields are consolidated, and failed generation leaves the prior description and fields intact. Include missing building settings, an unavailable bottleneck basis, and the native description length limit.
8. Save, copy the share code, and reopen or paste the blueprint through the native buttons. Confirm the generated main description, power field, and unrelated fields persist correctly. With a research-mode lab, confirm net produced matrices appear under Research without fabricated matrix imports or research speed.
9. Restart the test profile without UXAssist, then restore it and restart again. Confirm the entry and options are absent without UXAssist, native fields keep their normal layout in both cases, and repeated opening and generation work with the installed blueprint mods. `OnDisable` and inspector-destruction cleanup are code-review checks unless a development harness exercises them; a supported user-facing hot-disable workflow is not assumed.
