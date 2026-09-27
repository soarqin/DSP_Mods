# UXAssist Automatic Blueprint Description Plan

Status: Updated implementation specification, September 27, 2026.

The maintained production reference is [ProductionArchitecture.md](ProductionArchitecture.md). This document records the current behavior and acceptance criteria for the automatic blueprint description.

Keep shared production changes focused on this feature, update affected callers and tests, and preserve existing full-load accounting.

## 1. Confirmed Behavior and Display

Add an **Auto-fill description** button and a **Proliferation** checkbox to the shared blueprint inspector used in browser, copy, and paste modes. Place the checkbox immediately to the right of the button in one compact row above every description box, including the smaller description fields and the main description. On generation, replace the existing main description text with nonempty missing, excess, input, output, and research lines; use at most one native custom field for power. Preserve the author, version, short description, and unrelated custom fields.

| Purpose | English label | Simplified Chinese label | Value format |
| --- | --- | --- | --- |
| Generate or update | Auto-fill description | `自动填写说明` | Button |
| Calculation setting | Proliferation | `增产` | Checkbox |
| Optional power field | Power | `耗电` | Present factory and/or logistics consumption only |
| Description missing line | Missing: | `缺少：` | Intermediate shortages, with the label shown only once |
| Description excess line | Excess: | `多余：` | True overbuild, with the label shown only once |
| Description input line | Input: | `输入：` | External inputs only, at estimated steady-state rates |
| Description output line | Output: | `输出：` | Ordinary final products and necessary coproduct surplus |
| Description research line | Research: | `研究：` | Net positive matrix output accepted by selected research-mode labs |

The Chinese power example is `工厂耗电 2.5GW | 物流耗电 600MW` when both categories consume power. Omit a category without consumption; if neither consumes power, do not create the power field and remove an obsolete matching one. Keep a present but unmeasurable category marked as `未知`. Each material entry has the form `<item icon>Item name x100`. All material rates remain items per minute, but no description label includes the time unit. Separate items with commas, write each label only once per line, and put newlines only between nonempty lines in this order: missing, excess, input, output, research. Omit empty categories even when material data is incomplete; a warning-only description has no empty input line.

Use the native multiline description so long material lines wrap within the large editor. The power custom field remains single-line; do not add line breaks to custom fields, custom newline tokens, hidden ownership markers, sidecar metadata, a custom serialization format, or a mod-specific display protocol. Do not extend the native text renderer.

Use native item icon/name tags so the stored text remains compatible with ordinary blueprint saving and sharing. Round only for display, omit unnecessary trailing zeros, and do not display an unknown value as zero. Register UI strings, line labels, and the power field title through UXAssist's localization registration and use translation keys at call sites.

## 2. Inspector Integration and Safe Field Updates

### Proliferation setting

- Initialize the checkbox from whether the current blueprint contains a Spray Coater. Use the building's native prefab capability rather than its localized name.
- While the user has not overridden the setting, update that default when the blueprint contents change. Preserve a manual override for the current editing session; opening or switching to another blueprint resets the default.
- Changing the checkbox does not rewrite the blueprint. Replace the description and update the optional power field only when the button is clicked.
- When enabled, use the highest proliferation level supported by the captured catalog and preserve each building's extra-products or acceleration mode.
- Retain the black box analyzer's existing assumption that input coating is supplied externally. The presence of a Spray Coater only initializes the checkbox; it does not establish spray routing or justify adding an inferred proliferator-consumption flow.

### Click-to-write flow

1. Synchronize pending custom-field edits before reading native fields, so clicking the button does not discard unsaved manual field changes. The existing main description is intentionally replaced, including pending text there.
2. Read the inspector's current blueprint. Capture its buildings on the game thread with `BlueprintSelectionReader.FromBlueprint`, then create a request using the checkbox value and call `FactoryBlackBoxAnalyzer.Analyze`. Do not substitute the current planet's entire factory or live inventories.
3. Format a power value and up to five labeled description lines (Missing, Excess, Input, Output, Research), omitting empty categories. Keep calculation and formatting separate from Unity controls so their behavior can be checked with synthetic data. Refuse descriptions longer than the native 2,000-character limit before making generated changes.
4. Match only existing power custom fields by exact registered English or Chinese titles. Update one power field in place or append one when needed; merge duplicate matching power fields, and remove matching power fields when neither category consumes power. Do not inspect or delete input/output custom fields. Preserve every other field and its order, and do not search ordinary description prose for loose matches.
5. Check the native field capacity before changing generated data. If a required new power field cannot fit, explain the limit and leave the existing description and fields unchanged.
6. Commit both the prepared custom-field string and replacement `blueprint.desc`, update the native description input, and refresh the inspector through its native modification path. Refresh the displayed description, fields, and share-code preview, but do not save or overwrite a file automatically. The native save and copy buttons remain responsible for those actions.

Use existing UXAssist UI helpers and native styling. Reserve one compact row above all description fields for the button and its right-hand checkbox, not between smaller fields and the main description. Shift the first inspector group and following groups down together, increase the scroll content height, and size localized labels using the established UI utilities. Keep creation, event registration, refresh, and cleanup idempotent across inspector reuse and game sessions. Do not run the calculation repeatedly in a logic tick or per-frame update.

### Native integration reference

These integration points were checked in the original installed game DLL. Recheck their signatures and behavior against the original DLL after game updates, following the repository's Steam-install discovery guidance. Do not use stripped publicized method bodies as evidence.

- `UIBlueprintInspector` is shared by its `Browser`, `Copy`, and `Paste` usages. It stores the current `blueprint` and provides native refresh and custom-field editing paths.
- Native `Refresh` recalculates `descGroupTrans.anchoredPosition`, `group1.sizeDelta`, the following groups' positions, and `contentTrans.sizeDelta`, but does not reset `group1.anchoredPosition`. Move `group1` down once while the controls are active, position their row above its first native field, offset the following groups and scroll height after each native refresh, and restore the one-time shift on teardown. Do not move only `descGroupTrans`: that puts the new row between description sections.
- `BlueprintData.desc` stores the multiline description, including native item tags. Assign the inspector's `descTextInput.text` when replacing it because `Refresh(forModify: true)` does not reload that input from the blueprint. `BlueprintData.externalFields` stores custom fields; apply `ExternalFieldEscape` and `ExternalFieldUnescape` to power names and values.
- `UIBlueprintInspector.CollectExternalFields`, `OnCustomFieldEndEdit`, and `Refresh` establish the native collection and refresh sequence. Collect current edits before replacing generated values, not afterward.
- `UIBlueprintInspector.kMaxFieldCount` supplies the field limit; it was 20 in the inspected game build. A power update requires no new slot; unrelated custom fields are never removed to make room. The inspector rejects a `desc` longer than 2,000 characters; refuse that generated text instead of leaving the blueprint unsavable.
- `BlueprintData.Validate` removes literal newlines from native custom-field content, and the inspector lays out those fields at a fixed row height. It does not strip newlines from the main `desc`. Keep multiline text there, not in custom fields, without globally patching validation.
- `UIInputField` uses `LDB.signals.IconTag`; its `includeName` option supplies the native icon and localized item name together in the description text. The blueprint's normal serialization escapes and restores that text.

## 3. Material Classification and Shared API Changes

### Preserve the full-load report

Calculate from the buildings, recipes, and settings actually present in the blueprint. Keep all selected buildings at their modeled full load, even when upstream production is insufficient. Do not simulate belt reachability, logistics throughput, startup inventories, or runtime throttling.

Keep `GrossProduction`, `GrossConsumption`, `NetFlow`, producer/consumer attribution, and the original power calculations available. Material rates remain `double` items per minute and power remains watts.

For classification, account for a process's net production and consumption of each item. A material returned or recirculated within the same recipe must not become a false external requirement or surplus merely because it appears in both gross ledgers. Preserve the gross ledgers themselves for existing callers.

### Separate shortages, true overbuild, and coproducts

Extend the shared report with numeric results for:

- Intermediate material shortages requiring additional external supply.
- Net surplus attributable to genuinely redundant whole buildings.
- Net coproduct surplus left by necessary joint production after internal use.

One item can have both overbuild surplus and necessary coproduct surplus. Represent the amounts separately; a mutually exclusive item classification or a single boolean is insufficient. The existing per-item `IsExcessIntermediate` threshold alone does not prove that a building is redundant.

Use all products of a production group together when checking redundancy:

- Group equivalent buildings and process them in a stable order based on recipe, building, and operating settings so blueprint array order does not affect the result.
- Iteratively test whole-building reductions against the shared material balance. A reduction qualifies only if it does not lower final-product capacity and does not create or enlarge an internal material shortage.
- All items share the same selected redundant-building set and material budget. Do not independently claim the same surplus for several alternative reductions.
- Attribute overbuild and coproduct quantities to the original report without double-counting mixed single-output and multi-output producers. Do not classify a material solely by its position in the recipe's result array.
- Preserve the existing treatment of sub-building single-product ratio headroom as normal balance rather than an overbuild warning.

The whole-building reduction is only a classification calculation. Keep the actual blueprint's gross full-load inputs, outputs, original external-supply ledger, and rated power in the shared report. This feature does not remove buildings or become a separate factory-layout optimizer.

### Steady-state input demand

The displayed input rates must account for upstream production blocked by surplus intermediates. After identifying redundant whole buildings, allow fractional reductions of the remaining processes until each intermediate's production meets downstream demand. Apply each process reduction to all its inputs and joint products together; preserve rated final-product capacity and never enlarge an existing intermediate shortage. Propagate reductions through multiple upstream stages using a stable process order. The resulting external input is a separate steady-state estimate, not a replacement for gross full-load ledgers, output classification, or rated power. When other materials are incomplete, still cap independently known upstream processes against their known downstream demand and label the resulting input as a known portion. Do not mistake unmodeled downstream demand for zero: protect an unresolved process's known inputs, or all known products if its possible inputs are unknown. Keep whole-building overbuild and coproduct conclusions unknown until the material selection is complete.

A blueprint research-mode lab is a terminal outlet for all six native matrix types manufactured inside that same blueprint. One selected lab is enough to preserve the full rated output of every matrix producer, even when other known processes use some of those matrices. Classify positive net matrix production as final research output and place it on the separate Research line, not the ordinary Output line or an excess/coproduct entry. Subtract other buildings' known matrix consumption from the displayed research quantity, but do not add virtual research consumption to the gross material ledger or invent external matrix inputs. Other buildings' known matrix requirements still count, including external supply when no matrix is produced. Without a selected research-mode lab, use ordinary material classification and upstream throttling. This is a blueprint-planning assumption about where manufactured matrices can go, not a claim that a real lab has unlimited research throughput.

### Description lines

- If present, place intermediate shortages on a `Missing: ` / `缺少：` line and true overbuild on a separate `Excess: ` / `多余：` line, in that order before Input. Put each label only once before its comma-separated item list, not before every item. Do not mix warnings into Input or repeat a shortage as an ordinary input.
- Put only external raw-material inputs in the Input line, using the estimated steady-state rate rather than theoretical gross consumption.
- Put ordinary final products and necessary net coproduct surplus in the Output line. Do not include the overbuild portion again or duplicate research matrices here.
- Put positive net matrix output accepted by a selected research-mode lab in the separate Research line, after Output. For example, 12 matrices produced per minute minus 1 consumed by another selected process yields 11 per minute in Research, not 12; omit this line if no matrices are made or none remain.
- Merge entries for the same item within a category and sort each category by item ID. Use commas between entries and keep all rates in items per minute even though the labels omit the unit. Omit an entire line if it has no entries; do not generate `None` or `Unknown` as a stand-alone material line. Mark known entries as partial when material results are incomplete.

### Required joint-production example

Each upstream building produces A60 and B60 per minute. Internal downstream requirements are A120 and B30 per minute, and downstream capacity remains fixed.

| Selected upstream buildings | Original A/B production | True overbuild shown in Excess | Necessary coproduct shown in Output |
| --- | --- | --- | --- |
| 2 | A120, B120 | None | B90 |
| 3 | A180, B180 | A60, B60 | B90 |

In the three-building case, preserve gross consumption and rated power for all three buildings, but display external input for only the two upstream buildings needed by downstream demand. Do not reclassify the entire B150 net surplus as overbuild, and do not report B150 as coproduct output while also warning about the redundant B60.

## 4. Power, Scope Boundaries, and Incomplete Results

### Independent power categories

Extend `ProductionReport` with independently usable factory-consumption and logistics-consumption results and completeness information. Preserve the existing total-power API and migrate affected callers without weakening their accounting.

- Factory consumption includes production buildings and non-logistics auxiliary demand, including sorters and Spray Coaters.
- Logistics consumption uses stored configured charging limits, or native prefab defaults where no charging setting is applied, for the selected logistics facilities. Do not use transient draw.
- Do not subtract generation from consumption. Keep accumulator exchange and logistics charging distinct and count charging demand only once. Do not add a second logistics charging term to advanced miners or grid consumption for gas collectors' self-fueling.
- Unknown factory power must not discard known logistics power, or vice versa. For example, `工厂耗电 未知 | 物流耗电 600MW` is valid when only the factory total is unknown.
- A research-mode lab blueprint does not store its current technology or research speed. Capture native `LabComponent.matrixIds` with the catalog and mark blueprint labs as `ResearchMatrixSink`; do not consult current technology, technology requirements, unlock state, or research speed to determine matrix use. An assumed lab has no numeric matrix input or hash throughput in the report. Other callers without this blueprint assumption still need explicit `TechId` and `ResearchSpeed` to calculate exact research flows. If native matrix item IDs are unavailable, retain an incomplete material result instead of treating the lab as unable to accept matrices.
- Budget every selected research-mode lab at its full prefab working consumption, including labs currently unable to research or with invalid technology data. Apply the selected proliferation power multiplier when available; use base working power when no spray level is supported. This deliberately conservative estimate protects blueprint power capacity and keeps factory and total power independent of material-rate assumptions.
- Omit a power category when its demand is known to be zero; do not show `0W` for an absent factory or logistics consumer. An unknown category tied to a selected building remains visible as `Unknown`. If neither category has consumption, do not create a power field and remove an earlier generated one.

### Blueprint-only scope and missing data

- Do not generate description entries or requested-parameter lists for veins or other scene objects that are not part of a blueprint. Do not expand this feature into a resource-context or planet-analysis interface.
- If a building actually present in the blueprint still lacks a necessary operating parameter, retain independently calculable results and explicitly mark the affected result incomplete. The research-mode matrix outlet assumption above is an intentional exception; do not invent other external conditions.
- Preserve the common analyzer's resource and operating-parameter requirements for its other callers; the description feature's scope does not authorize globally suppressing those diagnostics.
- Keep material and power completeness independent. Mark partial material values as known portions, cap known upstream inputs against known demand, and suppress any final-output, overbuild, or coproduct conclusion that unresolved downstream consumers could invalidate.
- An invalid blueprint, unavailable catalog, calculation exception, overlong description, or insufficient field capacity must leave the existing description and custom fields unchanged and show a useful localized explanation. A failed or unavailable calculation is not a complete empty report.

## 5. Validation and Maintenance

### Synthetic checks

Extend `UXAssist/tools/ProductionCheck` with focused tests for:

- The two- and three-building joint-production example above.
- A shared item supplied by both single-output and multi-output processes, including simultaneous coproduct and overbuild amounts without duplication.
- Whole-building thresholds, fractional ratio headroom, internal deficits, and stable results after reordering blueprint buildings.
- Internal material return and cyclic recipes without losing the original gross production/consumption ledgers.
- Proliferation on/off, the highest captured supported level, and preservation of building acceleration/extra-product modes.
- Independent material, factory-power, and logistics-power completeness, including one research lab accepting all six manufactured matrix types without technology data, invented external matrix inputs, or hash rates; preserve full-load power with and without supported proliferation.
- No research station, no matrix production, one produced matrix type, multiple labs, and a separate building that genuinely needs external matrices. Classify positive net matrix production as final research output, preserve known gross flows, prevent false matrix coproduct or overbuild warnings, and avoid throttling producers whose matrix output has an assumed research outlet.
- Separate steady-state external inputs for whole and fractional upstream excess, multi-stage dependencies, shared producers, order stability, and shortages; cap known upstream inputs in partially known layouts while protecting unresolved downstream demand. Preserve gross ledgers, independently known rated output, and power.
- Pure formatting for separate nonempty Missing, Excess, Input, Output, and Research lines in that order, including multiple items per warning line without repeated labels. Verify labels omit the per-minute suffix without changing rate units. Cover long material lists, independent power-category omission, and present-but-unknown power; check that matrix output appears exactly once in Research at its net rate while non-matrix final output stays in Output.
- Pure field updates for first generation, repeated updates, exact bilingual power-title recognition, duplicate power fields, preservation of all other fields including input/output titles, and native-capacity rejection without partial writes. Check the description length before committing either change.

Keep game-dependent capture and inspector code separate from the pure-managed sources compiled by these checks.

Run the narrow checks and affected builds from the repository root:

```powershell
dotnet run --project UXAssist/tools/ProductionCheck/ProductionCheck.csproj -c Release
dotnet build UXAssist/UXAssist.csproj -c Release --no-restore
dotnet build CheatEnabler/CheatEnabler.csproj -c Release --no-restore
dotnet build UniverseGenTweaks/UniverseGenTweaks.csproj -c Release --no-restore
git diff --check
```

### Manual game acceptance

- Check browser, copy, and paste inspectors in English and Simplified Chinese. Confirm the checkbox sits directly to the right of the button without overlap, their row precedes all small and main description boxes, and the native description wraps long icon/name material lines. Reopen, refresh, and change language to ensure the row stays at the top and the original layout returns when the feature is disabled.
- Check checkbox defaults with and without a Spray Coater, blueprint-content changes before a manual override, preservation of a manual override during refresh, and reset after switching blueprints.
- Generate with no existing fields, generate again, and generate with unrelated custom fields, including fields titled Input or Output. Confirm the main description replaces its previous text, at most one power field remains, and other fields and pending manual custom-field edits survive unchanged.
- Try factory-only, logistics-only, both, neither, and partially unknown consumption. Confirm absent categories and the entire empty power field disappear, while genuinely unknown demand stays labeled. Confirm Missing and Excess each use one label followed by comma-separated items before Input, with no per-minute suffix on any description label; empty material categories produce no lines.
- Reach the native field limit. Confirm a missing power field causes an all-or-nothing refusal without deleting other fields, while an existing power field can be updated and description-only generation remains possible. Generate a description over 2,000 characters and confirm it is rejected without changing the blueprint.
- Save, reopen, copy a share code, and import it again through the native UI. Confirm description newlines, native item icons, the optional power field, and unrelated fields survive without a custom format or a modified renderer.
- Confirm no file is written just by toggling proliferation or clicking generation, and that errors preserve the previous description and fields.
- Compare full-load rates and configured logistics charging limits under suitable conditions. Do not compare against transient, starved, or output-blocked production.
- With no external outlet for surplus intermediates, compare the description's input after upstream output buffers fill against downstream demand, including when an unrelated building lacks operating data. Confirm that unresolved downstream consumers prevent unsupported input reductions while known rated output and power remain independent.
- Generate blueprints with one research-mode lab and six matrix types made inside the blueprint, then with only one matrix type or none. Confirm every manufactured matrix has an outlet and its positive net rate appears once in the Research line, after the unrelated final products in Output. Known consumption by another blueprint building reduces the Research rate; do not estimate lab matrix use, hashes, or external matrix demand. Vary current technology and research speed, add more labs, and toggle proliferation: matrix throughput must not change, but each lab must still contribute its full applicable working power. Without a lab, ordinary matrix output and overbuild rules apply.

Game validation is manual. Do not add automated gameplay or save-file parsing as an acceptance requirement.

### Documentation and repository hygiene

Keep the maintained English production architecture documentation and aligned English/Chinese changelog notes consistent with this behavior and the repository's version rules. Put only durable standards or maintained architecture references in `AGENTS.md`, not this task's progress, investigation evidence, or completed plan. Do not stage unrelated files, create a branch, or commit without an explicit request.
