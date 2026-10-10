# GitHub issue review — 2026-10-11

Reviewed all 44 issues that were open at the start of this task. Old-version reports were checked against the locally installed DSP 0.10.35.29104, Steam build 25599610, using the original game assembly.

Eight independent fixes cover 11 issues. Seven feature-request issues are recorded in the owning mod's TODO.md; the batching proposal in #97 is also recorded while its crash cause remains unconfirmed. Eleven existing fixes, withdrawn/installation reports, and usage questions are ready for reply and closure. Four reports need current reproduction details. Eleven compatibility investigations are deferred without replies or TODO entries.

Newly fixed issues remain open after their commit replies. The dispositions below are applied on GitHub after pushing the source changes.

| Issue | Mod | Disposition | Result / reference |
| --- | --- | --- | --- |
| [#98](https://github.com/soarqin/DSP_Mods/issues/98) | UXAssist | fixed | [ac16794](https://github.com/soarqin/DSP_Mods/commit/ac16794) |
| [#97](https://github.com/soarqin/DSP_Mods/issues/97) | UXAssist | needs evidence | Crash cause unconfirmed; batching proposal is in UXAssist/TODO.md. |
| [#96](https://github.com/soarqin/DSP_Mods/issues/96) | UXAssist | already fixed | Existing implementation verified; reply and close. |
| [#95](https://github.com/soarqin/DSP_Mods/issues/95) | UXAssist | fixed | [06a8445](https://github.com/soarqin/DSP_Mods/commit/06a8445) |
| [#93](https://github.com/soarqin/DSP_Mods/issues/93) | CheatEnabler | question | Answer with direct usage instructions and close. |
| [#91](https://github.com/soarqin/DSP_Mods/issues/91) | Dustbin | fixed | [2cb351e](https://github.com/soarqin/DSP_Mods/commit/2cb351e) |
| [#89](https://github.com/soarqin/DSP_Mods/issues/89) | UXAssist | feature request | [TODO.md](../UXAssist/TODO.md): Hide litter pickup popups. |
| [#86](https://github.com/soarqin/DSP_Mods/issues/86) | UXAssist | feature request | [TODO.md](../UXAssist/TODO.md): Scale protection thresholds with mining consumption. |
| [#85](https://github.com/soarqin/DSP_Mods/issues/85) | UXAssist / Weaver | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#83](https://github.com/soarqin/DSP_Mods/issues/83) | UXAssist | already fixed | Existing implementation verified; reply and close. |
| [#82](https://github.com/soarqin/DSP_Mods/issues/82) | UXAssist | already fixed | Existing implementation verified; reply and close. |
| [#81](https://github.com/soarqin/DSP_Mods/issues/81) | CheatEnabler | question | Answer with direct usage instructions and close. |
| [#80](https://github.com/soarqin/DSP_Mods/issues/80) | Unspecified power option | needs evidence | Current reproduction details are needed; leave open. |
| [#78](https://github.com/soarqin/DSP_Mods/issues/78) | UXAssist / CheatEnabler | needs evidence | Current reproduction details are needed; leave open. |
| [#77](https://github.com/soarqin/DSP_Mods/issues/77) | CheatEnabler | needs evidence | Current reproduction details are needed; leave open. |
| [#76](https://github.com/soarqin/DSP_Mods/issues/76) | UXAssist | fixed | [2903019](https://github.com/soarqin/DSP_Mods/commit/2903019) |
| [#75](https://github.com/soarqin/DSP_Mods/issues/75) | CheatEnabler | feature request | [TODO.md](../CheatEnabler/TODO.md): Vegetation choices and colored planetary guide lines. |
| [#74](https://github.com/soarqin/DSP_Mods/issues/74) | CheatEnabler | feature request | [TODO.md](../CheatEnabler/TODO.md): Instant producer-to-consumer item transfer. |
| [#73](https://github.com/soarqin/DSP_Mods/issues/73) | UXAssist | already fixed | Existing implementation verified; reply and close. |
| [#72](https://github.com/soarqin/DSP_Mods/issues/72) | UXAssist / Galactic Scale | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#71](https://github.com/soarqin/DSP_Mods/issues/71) | Dustbin | already fixed | Existing implementation verified; reply and close. |
| [#70](https://github.com/soarqin/DSP_Mods/issues/70) | Dustbin | fixed | [2cb351e](https://github.com/soarqin/DSP_Mods/commit/2cb351e) |
| [#69](https://github.com/soarqin/DSP_Mods/issues/69) | Dustbin | fixed | [2cb351e](https://github.com/soarqin/DSP_Mods/commit/2cb351e) |
| [#68](https://github.com/soarqin/DSP_Mods/issues/68) | UXAssist | withdrawn | Reporter withdrew the attribution; reply and close. |
| [#67](https://github.com/soarqin/DSP_Mods/issues/67) | UXAssist / CheatEnabler / modpack | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#66](https://github.com/soarqin/DSP_Mods/issues/66) | CheatEnabler | fixed | [47ba294](https://github.com/soarqin/DSP_Mods/commit/47ba294) |
| [#65](https://github.com/soarqin/DSP_Mods/issues/65) | CheatEnabler / FractionateEverything | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#64](https://github.com/soarqin/DSP_Mods/issues/64) | UXAssist | feature request | [TODO.md](../UXAssist/TODO.md): Configurable belt elevation increments. |
| [#63](https://github.com/soarqin/DSP_Mods/issues/63) | Dustbin | fixed | [d778367](https://github.com/soarqin/DSP_Mods/commit/d778367), [063daff](https://github.com/soarqin/DSP_Mods/commit/063daff) |
| [#61](https://github.com/soarqin/DSP_Mods/issues/61) | UXAssist / Galactic Scale / Nebula | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#60](https://github.com/soarqin/DSP_Mods/issues/60) | UXAssist / modpack | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#59](https://github.com/soarqin/DSP_Mods/issues/59) | UXAssist | feature request | [TODO.md](../UXAssist/TODO.md): Limit launches using orbital sail counts. |
| [#55](https://github.com/soarqin/DSP_Mods/issues/55) | UniverseGenTweaks / TheyComeFromVoid | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#51](https://github.com/soarqin/DSP_Mods/issues/51) | UXAssist | fixed | [ededc3e](https://github.com/soarqin/DSP_Mods/commit/ededc3e) |
| [#43](https://github.com/soarqin/DSP_Mods/issues/43) | UXAssist | question | Answer with direct usage instructions and close. |
| [#42](https://github.com/soarqin/DSP_Mods/issues/42) | UXAssist | already fixed | Existing implementation verified; reply and close. |
| [#39](https://github.com/soarqin/DSP_Mods/issues/39) | Dustbin | installation | Required preloader not loaded; answer and close. |
| [#38](https://github.com/soarqin/DSP_Mods/issues/38) | UXAssist / Galactic Scale | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#35](https://github.com/soarqin/DSP_Mods/issues/35) | UXAssist | feature request | [TODO.md](../UXAssist/TODO.md): Dismantle filters by building type. |
| [#31](https://github.com/soarqin/DSP_Mods/issues/31) | Dustbin | fixed | [063daff](https://github.com/soarqin/DSP_Mods/commit/063daff) |
| [#30](https://github.com/soarqin/DSP_Mods/issues/30) | PoolOpt / DSPAutoSorter | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#27](https://github.com/soarqin/DSP_Mods/issues/27) | CheatEnabler / Auxilaryfunction | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#14](https://github.com/soarqin/DSP_Mods/issues/14) | Dustbin / Nebula | compatibility deferred | Leave unchanged; no reply and no TODO entry, per the requested scope. |
| [#5](https://github.com/soarqin/DSP_Mods/issues/5) | CheatEnabler | fixed | [47ba294](https://github.com/soarqin/DSP_Mods/commit/47ba294) |

## Validation

- Normal Release builds of CheatEnabler (including UXAssist) and Dustbin (including DustbinPreloader) pass with zero warnings and errors.
- Managed regression checks reproduce and verify the single-tank stall, splitter-to-storage bypass, late-enabled dev actions, late-enabled alternating pole previews, and protected-vein display errors.
- Belt checks use the current native factory-frame target and cargo methods, covering stacked/proliferated disposal, consumption statistics, high factory indices, and removal in the owning factory.
- Mining and pole-preview transpilers were inspected against current native method bodies and branch destinations. Mining display checks cover overlapping miners, protected oil, and productive zero-consumption resources.
- Headless tests use an unstripped copy of the original game assembly with publicized access and Dustbin's injected fields. Pole tests provide the item catalog directly instead of invoking Unity resource loading. Full Unity gameplay, the affected large save, and the CrossOver memory failure were not reproduced.
- `git diff --check` passes. Version numbers are unchanged.
