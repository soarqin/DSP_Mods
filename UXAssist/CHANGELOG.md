# Changelog / 更新日志

<details>
<summary>Read in English</summary>

This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [1.6.4] - 2026-11-27

### Added

- `Reverse conveyor belt`: Enable it on the Factory tab and press `Alt+R` over a built belt to reverse it without opening its panel. The shortcut can be changed in the system options.
- Blueprint descriptions now offer `Lowest intermediate throughput` as a production basis. It scales the final-product mix and external inputs to the bottleneck. Complete, balanceable chains show unused intermediate capacity as surplus and have no internal shortages. Power demand still assumes full load. If the bottleneck cannot be determined, the existing description is kept. `Final-product output` remains the default.

### Changed

- Align `Auto-fill` with the description title in the blueprint browser, copy, and paste inspectors, leaving room for the native icon. The generation popup follows the entry while scrolling and adjusts at screen edges, with the existing fields in place and the file list visible. The standard rectangular `Production basis` dropdown opens over the footer while the popup and `Generate` button stay in place. Close the popup with the X, a click outside, or Esc. Esc closes an open dropdown first.

### Fixed

- `Hide battle-related techs in Peace mode`: Restore tech-tree arrows when switching from a Peace-mode save to a Combat-mode save or disabling the option in another Peace-mode save. Disabling it at the title screen also restores battle-related techs for the next game.
- `Auto-cruise`: Fix the mecha diving back into the ground right after taking off from a moon that is close to its gas giant.
- Quick build Mining Machines now checks final vein coverage before placing miners so each miner still covers at least three veins from the selected group after planned burials.

### Other

- Quick build Mining Machines now saves diagnostic snapshots only in debug builds.

## [1.6.3] - 2026-10-31

### Added

- `Quick build Mining Machines`:
  * Add a Factory-tab button to arrange ordinary Mining Machines densely around the nearest solid vein group. Each miner covers at least 3 nodes from that group, without including nearby groups. Layouts are not limited by stock; missing miners remain as ghosts awaiting materials. Placement respects construction range and does not include belts or power facilities. Click again to cancel a search.
  * `Allow restoring/burying veins` is off by default, using only visible nodes without restoring or burying any. Enable it to restore unobstructed nodes away from Dark Fog Planetary Bases and allow unused nodes to be buried for better coverage. Nodes used by existing miners or miner ghosts are protected.
- `Blueprint descriptions`:
  * Add `Auto-fill description` to the blueprint browser, copy, and paste panels. It replaces the main description with full-load material flows per minute, separating shortages, surpluses, inputs, outputs, and research use, and adds factory and logistics power demand to a `Power` custom field.
  * A `Proliferation` checkbox controls whether calculations include proliferation, defaulting to on when the blueprint contains a Spray Coater. Large rates use K/M/G where display precision is preserved, and numbers with four or more integer digits use comma thousands separators, including before suffixes; unknown values are marked separately. Other custom fields are preserved, and the blueprint still needs to be saved manually.

### Changed

- `Sunlight at night`: Lighting now follows the camera, including planet view, and stays consistent across buildings, terrain, solar panels, and belts. Restore lighting correctly after switching planets, and return to native lighting when sailing, disabling the feature, or using white rendering mode.

### Fixed

- `Protect veins from exhaustion`: Fix the option failing to take effect in 1.6.2, restoring protection of configured vein reserves and minimum oil rates, along with the corresponding mining statistics.

## [1.6.2] - 2026-10-18

### Changed

- `Dismantle blueprint selected buildings`: Respect sandbox instant-dismantle settings when discarding logistics station contents, and return them normally outside instant dismantling.
- `Belt signals for buy out dark fog items automatically`: Use the current in-game material names and retain the original exchange ratios.

### Fixed

- `Increase maximum power usage in Logistic Stations and Advanced Mining Machines`: Fix zero or corrupted mining speeds when adjusting an Advanced Mining Machine's speed slider with the option enabled.
- `Tweak building buffers`: Restore the Ray Receiver Graviton Lens buffer setting on DSP 0.10.35.29104 and later, and correct the displayed native buffer value to 2.
- `Drag building power poles`: Fix invalid alternating previews when enabling the option after loading a save, and restore original spacing when disabling it.
- Close building and belt hover popups when opening the pause menu so they cannot obscure menu controls.
- `Protect veins from exhaustion`: Exclude protected veins from mining-rate estimates, mining resource counts, and miner hover counts while retaining their remaining reserves.
- `Treat stack items as single in monitor components`: Fix the option taking effect at game startup when disabled, until toggled on and off.
- `Building options`: Honor saved toggle states at startup for off-grid construction, power-pole dragging, fast tank transfers, and Ctrl+Shift belt pickup.
- `Initialize This Planet`: Fix an occasional array index error after initializing a planet with Holo Beacons.
- `Planet vein utilization`: Restore the original vertical alignment of vein-group counts and keep resource names in their original positions.

## [1.6.1] - 2026-09-29

### Changed

- `Planet vein utilization`: Move vein-group counts to the left of resource names to avoid overlapping the game's new buttons.
- `Auto-cruise`: Align the settings sliders and reduce spacing between their tracks and values.

### Fixed

- `Larger area for terraform`: Restore 11x11 through 30x30 foundation and Restore Terrain brushes after DSP 0.10.35.29104.
- Fix the title-screen background turning black, even when the UXAssist window is closed.
- `Settings help`: Restore the game's native tips on question-mark buttons.
- `Real-time logistic stations info panel`: Fix lingering panels after dismantling stations or initializing a planet, and keep panels updating while typing.
- `Do not render factory entities (except belts and sorters)`: Fix the option not taking effect after updating to DSP 0.10.35.29057.

## [1.6.0] - 2026-09-24

### Added

- New feature: `Orbital Collector product limit`, with an `Apply to universe` option.
- `Do not render factory entities (except belts and sorters)`: Add sub-option `Hide sorters too` to also hide sorters, leaving only belts visible.

### Changed

- Support Dyson Sphere Program 0.10.35.29057.
- `Auto-cruise`: Reworked navigation with a new algorithm and more options:
  * `Stop on manual input`, `Use warp` (with minimum energy), `Auto boost` (with minimum energy), `Dark Fog Hive follow distance` and `Dark Fog Carrier follow distance`.
  * The button now shows the configured shortcut key and active status, replacing the separate on-screen hint.
- `Auto-construct`: Improved routing, obstacle recovery, and drone travel efficiency.
- `Tweak building buffers`: Now shows original in-game default values for reference.
- Update Ray Receiver buffers and item refunds, vein-depletion alerts, handcrafting shortcuts, and batch technology buyout controls.
- Preserve soil progression when hiding soil-change tips and safely restore saved brush sizes when expanded brushes are disabled.

### Fixed

- Fix UI color issues on some panels and controls.
- Restore expanded foundation brushes, Restore Terrain support, and player actions in globe view.

## [1.5.8] - 2026-05-30

### Removed

- `Remember window position and size on last exit`: Remove a window hook that are not useful anymore.

### Fixed

- `Initialize This Planet`: Fix a possible crash.
- `Real-time logistic stations info panel`: Fix display issue (finally!) when count of logistic towers > 128.

## [1.5.7] - 2026-03-20

### Fixed

- Fix compatibility with game update 0.10.34.28505.

## [1.5.6] - 2026-03-16

### Fixed

- `Initialize This Planet`: Fix known crashes and infinite loop.
- `Show recent milkyway upload results`: Fix an issue that it only shows earliest results but latest ones.
- Fix some other minor bugs found by LLM AI.

## [1.5.5] - 2026-03-07

### Added

- Enable `Hide UI` function(`F11` by default) while on Star Map view: Hide all other elements in universe.

### Changed

- `Initialize This Planet`:
  * Fix crash issue.
  * Add 3 options to return certain items.

### Fixed

- Fix warning from abnormal check for following features:
  * `Unlock all techs with metadata`
  * `Disable battle-related techs in Peace mode`
  * ``Restore upgrades of `Sorter Cargo Stacking` on panel``

## [1.5.4] - 2026-02-15

### Changed

- `Auto-contruct`:
  * Add a UI option to hide it completely.
  * Fix an issue that may cause circling.

### Fixed

- `Auto-cruise`: Fix some user experience related logics.

## [1.5.3] - 2026-02-10

### Added

- New feature: `Auto-contruct`
  * Fly to buildings to be contructed automatically.

### Changed

- `Auto-cruise`: Do not try to fly away planet when `Drive Engine 2` is not researched.
- `Belt signals for buy out dark fog items automatically`: Grid position on panel is moved to avoid conflict with newly added signals.

### Fixed

- `Initialize This Planet`: Fix logistic station ships rendering issue.
- Copying Wireless Power Tower works as expected when `Drag building power poles in maximum connection range` and `Build Tesla Tower and Wireless Power Tower alternately` are enabled.
- Fix implementation of ``Restore upgrades of `Sorter Cargo Stacking` on panel`` to make it working again.

## [1.5.2] - 2026-01-30

### Fixed

- Fix crash while error occurs on milkyway login/upload.

## [1.5.1] - 2026-01-30

### Changed

- Support game version 0.10.34.28392

## [1.5.0] - 2026-01-23

### Added

- New features:
  * `Show top players in milkyway`: The button is available on top-left corner of Milkyway View
  * `Show recent milkyway upload results`: The button is on UXAssist `General` tab.

### Changed

- Support game version 0.10.34
- `Enhanced control for logistic storage capacities`: Working for Logistics Control Panel now.

### Fixed

- `Night Sunlight`: Fix bugs that sunlight angle is not updated after loading a save.
- `Protect veins from exhaustion`: Fix a minor bug.
- `Initialize This Planet`: Improve stability and fix some bugs.

## [1.4.5] - 2025-11-14

### Added

- New feature: `Ctrl+Shift+Click to pick items from whole belts`, with 2 suboptions: `Include branches of belts` and `Include connected inserters`

### Fixed

- Patch [BlueprintTweaks](https://thunderstore.io/c/dyson-sphere-program/p/kremnev8/BlueprintTweaks/) temporarily to fix `Drag Remove` functions (And wait for limoka to merge the [Pull Request](https://github.com/limoka/DSP-Mods/pull/133)):
  * Ground rendering is correct now.
  * Dismantle logic follows offcial's implementation to avoid possible crashes.
- Fixed CommonAPI patch to make shortcut key settings saved as expect now, sorry for wrong implementation in last version.

## [1.4.4] - 2025-11-06

### Fixed

- `Remember window position and size on last exit`: Fix compatiblity for game patch 0.10.33
- `Real-time logistic stations info panel`: Try to fix a display issue.
- Patch [CommonAPI](https://thunderstore.io/c/dyson-sphere-program/p/CommonAPI/CommonAPI/) temporarily to fix a known issue that shortcut key settings are not saved to config, while its [PR](https://github.com/limoka/CommonAPI/pull/14) is not merged yet.

## [1.4.3] - 2025-10-04

### Added

- `Build Tesla Tower and Wireless Power Tower alternately`:
  * Fix wrong implementation for latest game patch.
  * Cannot use Tesla Tower as start Power Tower now, due to new rectangular area build mechanism.

### Changed

- `Planet Vein Untilization`: Support mods that add new vein types.
- `Real-time logistic stations info panel`: Try to fix possible crash.

## [1.4.2] - 2025-09-30

### Fixed

- Fixed a crash issue.

## [1.4.1] - 2025-09-29

### Fixed

- Fixed a compatible issue with latest game patch.

## [1.4.0] - 2025-09-29

### Added

- Embedded [Planet Vein Untilization](https://thunderstore.io/c/dyson-sphere-program/p/testpushpleaseignore/Planet_Vein_Utilization/) due to its lack of maintainance, with minor bug fixes.

### Changed

- `Sort blueprint structures before saving`: Improved sorting rules.
- `Starmap filter`: Now star indices (as in galaxy generation order) are displayed as prefix.
- Now build in C# `Debug` Configuration, to avoid some issues caused by optimizations in `Release` Configuration.

### Removed

- Support game version 0.10.33, with some features removed:
  * Remove `Scale up mouse cursor`: Unity 2022 set cursor size from system settings, software rendering does not affect its size now.
  * Remove `Set enabled CPU threads`: They are officially supported.

### Fixed

- `Dismantle blueprint selected buildings`: Fixed an issue that proliferator points are lost for items dropped from logstic stroages.
- `Remove some build conditions`: Fix a wrong logic.
- `Real-time logistic stations info panel`: Fix some display issues.
- Fix background image issue for tab buttons on config window.

## 1.3.7 (No public release recorded)

### Added

- `Auto-config logistic stations`: Add `Set default remote logic to storage`

### Fixed

- `Initialize This Planet`: Fix a possible crash.

## [1.3.6] - 2025-06-01

### Added

- `Tweak building buffer`: add buffer tweaking for 2 new buildings
  * `Ejector Solar Sails buffer count`: Range 5-400 (step by 5), default is 20 (same as game)
  * `Silo Rockets buffer count`: Range 1-20, default is 20 (same as game)

### Changed

- `Dismantle blueprint selected buildings`:
  * Fix a crash on dismantling preview buildings.
  * Rename to `Shortcut keys for Blueprint Copy mode`, while adding a shortcut key to select all buildings (Ctrl+A by default).
- `Allow overflow for Logistic Stations and Advanced Mining Machines`: Working for Logistics Control Panel now.

## [1.3.5] - 2025-05-10

### Fixed

- `Mod manager profile based save folder`: Fix crash on game startup

## [1.3.4] - 2025-05-09

### Fixed

- `Auto-config logistic stations`: Fix a bug that some settings are not applied to Advanced Mining Machines and Logistics Distributors

## [1.3.3] - 2025-05-09

### Added

- `Auto-config logistic stations`: Can set Max. Charging Power for Battlefield Analysis Base now.

### Changed

- `Starmap filter`: Hide top overlaping windows while the filter UI is shown.
- `Auto navigation on sailings`:
  * Add a button to enable/disable `Auto-cruise` quickly.
  * Do not auto-use Warper if required Tech is not researched.
- `Enhanced control for logistic storage capacities` & `Allow overflow for Logistic Stations and Advanced Mining Machines`:
  * Logistic storage capacities are not scaled on upgrading `Logistics Carrier Capacity`, if they are already greater than upgraded maximum capacity.
  * Logistic storage capacities will be reduced to tech capacity limits on pasting blueprints.
- `Real-time logistic stations info panel`: Support for mods that change slot count of logistic stations.

### Fixed

- `Initialize This Planet`: Fix a crash.
- `Dismantle blueprint selected buildings`: Fix an issue that belt connected buildings are dismantled unexpectly.
- `Mod manager profile based save folder`: Fix compatibility with [SaveTheWindows](https://thunderstore.io/c/dyson-sphere-program/p/starfi5h/SaveTheWindows/).

## [1.3.2] - 2025-04-28

### Added

- New feature: `Disable battle-related techs in Peace mode`
- New button: `Unlock all techs with metadata`
- Add a checkbox to make union of results in starmap filter.

### Fixed

- Fix some starmap vein/planet filter conditions.
- Fix a crash caused by `Initialize This Planet` in peace mode.
- Fix compatibility with `NebulaMultiplayerMod`.

## [1.3.1] - 2025-04-26

### Changed

- Tweak star name filter's planet type list.

### Fixed

- Fix an issue that some UI elements are hidden while hitting the newly added combobox on Starmap.
- Fix an issue that star name filter is not applied if `Shortcut keys for showing stars` is not enabled.
- `Dismantle blueprint selected buildings`: Fix an issue that items in Logistic Station slots are not dropped out.

## [1.3.0] - 2025-04-26

### Added

- New feature for starmap view:
  * Add a star name filter, you can filter displayed star names by ores or planet types now.
  * Add a dropdown box to show all stars' distance and/or planet count.
- `Auto-config logistic stations`: Add an option `Limit auto-replenish count to config values`

### Changed

- `Remember window position and size on last exit`: Optimized implementation
- Optimized some UI codes.

### Fixed

- `Cut conveyor belt`: Fix input issue.
- `Shortcut keys for showing stars`: Fix an issue that toggle key is read when Starmap View is not opened.
- `Dismantle blueprint selected buildings`: Fix an issue that preview buildings are not dismantled.

## [1.2.20] - 2025-04-21

### Added

- New feature: `Dismantle blueprint selected buildings`
  * Press shortcut key in blueprint copy mode to dismantle selected buildings.
  * The default shortcut key is Ctrl+X, you can set it in system options panel.
- New feature: `Auto-config logistic stations`
  * Auto-config buildings include: Logistics Distributor, PLS, ILS, Advanced Mining Machine.

### Fixed

- `Night Sunlight`: Fix bugs that sunlight angle is not updated as expected again.

## [1.2.19] - 2025-04-16

### Added

- New feature: `Tweak building buffer`
  * Factory recipe buffer formula: take the larger value between `Assembler buffer time multiplier(in seconds) * items needed per second` and `Assembler buffer minimum multiplier * items needed per recipe`
    * `Assembler buffer time multiplier(in seconds)`: Range 2-10, default is 4 (same as game)
    * `Assembler buffer minimum multiplier`: Range 2-10, default is 2 (same as game)
  * Matrix Lab assembly mode formula: Default buffer is `Buffer count for assembling in labs`, when using Self-evolution Lab, if recipe's original production time is not greater than 9 seconds, add `Extra buffer count for Self-evolution Labs` * (`Lab speed` - 1)
    * `Buffer count for assembling in labs`: Range 2-20, default is 6 (same as game)
    * `Extra buffer count for Self-evolution Labs`: Range 1-10, default is 3 (same as game)
  * `Buffer count for researching in labs`: Range 2-20, default is 10 (same as game)
  * `Ray Receiver Graviton Lens buffer count`: Range 1-20, default is 1 (game default is 20)
- New feature: `Shortcut keys for showing stars' name`
  * Add a shortcut key to always show all star names in starmap when holding, default is `Alt`
  * Add a shortcut key to toggle between three star name display states in starmap: `Original state`, `Show all names`, `Hide all names`, default is `Tab`, will restore to original state when closing starmap

### Fixed

- `Cut conveyor belt`: Fix a bug that entity logic connection is not cut so that belt is not cut off on copying as a blueprint.

## [1.2.18] - 2025-04-10

### Changed

- `Protect veins from exhaustion`: Optimized implementation, now veins will not be protected once you have upgrade `Veins Utilization` to level 390+, while the cost rate becomes absolute 0.

### Fixed

- `Night Sunlight`: Fix bugs that sunlight angle is not updated as expected.

## [1.2.17] - 2025-04-04

### Fixed

- Fix wrong implementation of `Protect veins from exhaustion` which causes wrong display of vein stats and veins not consumed.

## [1.2.16] - 2025-03-31

### Added

- New feature: `Cut conveyor belt`
  * Press shortcut key to cut conveyor belt under cursor.
  * The default shortcut key is Alt+X, you can set it in system options panel.
- New feature: `Profile based option`
  * Option file is stored as `Options\<ProfileName>.xml`.

### Fixed

- Fix compatibility with game update 0.10.32.25779

## [1.2.15] - 2025-02-19

### Fixed

- `Off-grid building and stepped rotation`: Fix compatibility with DSP 0.10.32.25682. (#57)
- `Enhanced control for logistic storage capacities`: Try to fix possible crash. (#54)

## [1.2.14] - 2025-01-24

### Fixed

- Fix an issue that an unexpected menu icon is shown in the top-right corner of the config panel.
- `Stop ejectors when available nodes are all filled up`: Fix compatibility with `Dyson Sphere Program v0.10.32.25496`.

## [1.2.13] - 2025-01-12

### Changed

- `Logistics Control Panel Improvement`: Auto apply filter with in-hand item now.

### Fixed

- `Belt signals for buy out dark fog items automatically`: Fix possible crashes.
- Fix an alignment issue on UI panel.

## [1.2.12] - 2024-12-22

### Fixed

- `Construct only structure points but frames`: Fix a bug that frames are still not constructed when this function is disabled.
- `Drag building power poles in maximum connection range`: Fix a bug that single power pole cannot be placed at some positions.

## [1.2.11] - 2024-12-14

### Fixed

- Fix an issue caused by game update: tips are not shown when mouse hovering on tips button.

## [1.2.10] - 2024-12-01

### Fixed

- `Set enabled CPU threads`: Fix hybrid-architect check for CPUs without hyper-threading
- `Re-initialize Dyson Spheres` and `Quick dismantle Dyson Shells`: Fix possible crashes and a display issue, while Dyson Sphere panel is actived.

## [1.2.9] - 2024-11-26

### Changed

- `Protect veins from exhaustion`:
  * Fix a bug that vein protection causes crashes (#50).
  * Fix a bug that minimum oil speed in config is not working (#50).
  * Fix a bug that oil is not extracted when vein protection is enabled in infinite resource mode (#52).

## [1.2.8] - 2024-11-12

### Added

- New feature: `Fast fill in to and take out from tanks`
  * You can set multiplier for tanks' operation speed
  * This affects manually fill in to and/or take out from tanks, as well as transfer from upper to lower level.

### Fixed

- Fixes to `Append mod profile name to game window title`:
  * Fix a bug that window title is not set correctly when multiple instance is launched.
  * Fix a bug that window title is not set correctly if BepInEx debug console is enabled.
- `Real-time logistic stations info panel`: Fix a bug that item status bar appears unexpectedly.

## [1.2.7] - 2024-09-26

### Fixed

- Fix some minor issues

## [1.2.6] - 2024-09-26

### Added

- `Remember window position and size on last exit`
  * Fix a bug that window position is restored even the option is disabled.
  * Fix a bug that the last window position is wrongly remembere when game is closed at minimized state.

## [1.2.5] - 2024-09-25

### Added

- New feature: `Set process priority`
- New feature: `Set enabled CPU threads`
- `Drag building power poles in maximum connection range`: Add a new config option `Build Tesla Tower and Wireless Power Tower alternately`

## [1.2.4] - 2024-09-23

### Changed

- `Sunlight at night`:
  * Fix flickering issue while mecha is sailing.
  * Can configure the light angles now.
- Some minor fixes and tweaks.

### Removed

- Does not patch `BulletTime`'s speed control now, as `BulletTime` has been updated to support configurable maximum speed.

### Fixed

- `Scale up mouse cursor`: Fix known issues.
- `Buy out techs with their prerequisites`: Fix a bug that warning popup from invalid data.

## [1.2.3] - 2024-09-21

### Fixed

- `Real-time logistic stations info panel`: Fix bar length not match with item amount when item amount is more than capacity.
- `Sunlight at night`: Fix not working.

## [1.2.2] - 2024-09-20

### Changed

- `Logical Frame Rate`: Set default shortcut key to `Ctrl`+`-/+` to avoid conflict with other shortcut keys

### Fixed

- `Real-time logistic stations info panel`: Fix text color mismatch sometimes

## [1.2.1] - 2024-09-19

### Changed

- `Off-grid building and stepped rotation`:
  * Fix off-grid building's default shortcut key for belts
  * Fix coordinate display issue

## [1.2.0] - 2024-09-18

### Added

- New feature: `Logical Frame Rate`
  * This will change game running speed, down to 0.1x slower and up to 10x faster.
  * A pair of shortcut keys (`-` and `+`) to change the logical frame rate by -0.5x and +0.5x.
  * Note:
    * High logical frame rate is not guaranteed to be stable, especially when factories are under heavy load.
    * This will not affect some game animations.
    * When set game speed in mod `Auxilaryfunction`, this feature will be disabled.
    * When mod `BulletTime` is installed, this feature will be hidden, but patch `BulletTime`'s speed control, to make its maximum speed 10x.

### Changed

- `Off-grid building and stepped rotation`: Due to conflict with shortcut key in new game update, the shortcut key for belts is changed to `Ctrl` by default, and can be set in system options now.
- Codes refactored, for better maintainability.

### Fixed

- `Real-time logistic stations info panel`: Fix a crash issue.
- `Dyson Sphere "Auto Fast Build"`: Fix possible wrong production records.

## [1.1.6] - 2024-09-04

### Added

- New feature: `Scale up mouse cursor`
  * Note: This will enable software cursor mode, which may cause mouse movement lag on heavy load.
- New feature: `Real-time logistic stations info panel`
  * Note: This function will be hidden if you enabled `Show station info` in mod `Auxilaryfunction`.

### Removed

- Remove use of AssetBundle, move all icons into `Assembly Resources`, for better flexibility.

### Fixed

- Fix an issue that `Dyson Sphere "Auto Fast Build"` does not generate production records for solar sails.

## [1.1.5] - 2024-08-30

### Added

- New feature: `Logistics Control Panel Improvement`
  * Auto apply filter with item under mouse cursor while opening the panel
  * Quick-set item filter while right-clicking item icons in storage list on the panel
- New feature: `Dyson Sphere "Auto Fast Build" speed multiplier`
  * Note: this only applies to `Dyson Sphere "Auto Fast Build"` in sandbox mode
- New feature: `Mod manager profile based save folder`
  * Save files are stored in `Save\&lt;ProfileName&gt;` folder.
  * Will use original save location if matching default profile name.

### Changed

- `Quick build and dismantle stacking labs`: works for storages and tanks now
- `Remember window position and size on last exit`: Do not resize window on applying game options if resolution related config entries are not changed.
- Auto resize panel to fit content, for better support of multilanguages and mods dependent on UX Assist config panel functions.

### Fixed

- `Enable game window resize`: Keep window resizable on applying game options.

## [1.1.4] - 2024-08-23

### Fixed

- Fix `Remove some build conditions`

## [1.1.3] - 2024-08-21

### Changed

- UI texts are updated following game settings now

### Fixed

- Fix hover area for checkboxes in config panel
- Fix an issue which makes `Convert Peace-Mode saves to Combat-Mode on loading` not working

## [1.1.2] - 2024-08-17

### Fixed

- `Belt signals for buy out dark fog items automatically`: Always add belt signals to the panel to fix missing belt icons when disabled.

## [1.1.1] - 2024-08-17

### Fixed

- Fix assetbundle loading issue

## [1.1.0] - 2024-08-17

### Added

- `Stop ejectors when available nodes are all filled up`: Show `No node to fill` on ejector panel when all dyson sphere nodes are filled up.
- Append mod profile name to game window title, if using mod managers (`Thunderstore Mod Manager` or `r2modman`).
- New features:
  * `Buy out techs with their prerequisites`: This enables batch buying out techs with their prerequisites. Buy-out button is shown for all locked techs/upgrads.
  * `Belt signals for buy out dark fog items automatically`, while enabled:
    * 6 belt signals are added to the signal panel, which can be used to buy out dark fog items automatically.
    * Generated items are stacked in 4 items.
    * Exchange ratio is following the original game design, aka:
      * 1 Metaverse = 20 Dark Fog Matrices
      * 1 Metaverse = 60 Engery Shards
      * 1 Metaverse = 30 Silicon-based Neurons
      * 1 Metaverse = 30 Negentropy Singularities
      * 1 Metaverse = 30 Matter Recombinators
      * 1 Metaverse = 10 Core Elements

## [1.0.26] - 2024-06-05

### Added

- New features:
  * Restore upgrades of `Sorter Cargo Stacking` on panel
  * Set `Sorter Cargo Stacking` to unresearched state

### Changed

- Changes to `Protect veins from exhaustion` configuration:
  * The vein amount is protected at 1000 by default now
  * The maximum vein amount is changed to 10000, and the maximum oil speed is changed to 10.0/s

## [1.0.25] - 2024-05-24

### Fixed

- Fix an issue that building entites can not be clicked through when `Do not render factory entities (except belts and sorters)` is enabled

## [1.0.24] - 2024-05-24

### Added

- New feature: `Drag building power poles in maximum connection range`
- New feature: `Allow overflow for Logistic Stations and Advanced Mining Machines`
  * Allow overflow when trying to insert in-hand items
  * Allow `Enhanced control for logistic storage capacities` to exceed tech capacity limits
  * Remove logistic strorage capacity limit check on loading game

### Changed

- Changes to `Do not render factory entities (except belts and sorters)`
  * Add shortcut key in config panel to toggle this function
  * Can click on both belts and sorters now

## [1.0.23] - 2024-05-05

### Added

- New features:
  * `Do not render factory entities (except belts and sorters)`
    * This also makes players click though factory entities but belts
  * `Open Dark Fog Communicator` anywhere
- Belts can be built off-grid now, by pressing the shortcut key for `Switch Splitter model`(`Tab` by default)
- Add a suboption `Auto boost` to `Auto-cruise`

### Changed

- `Auto-cruise` does warp when core energy at least 80% now

## [1.0.22] - 2024-03-30

### Fixed

- Fix a crash issue caused by `Quick build and dismantle stacking labs`

## [1.0.21] - 2024-03-30

### Fixed

- Fix a bug that stepped rotation is not working in `Off-grid building and stepped rotation`, which is caused by latest game update
- Fix some issues in `Auto nativation` and `Auto-cruise`, now only boosts when core energy at least 10% and warps when core energy at least 50%

## [1.0.20] - 2024-03-29

### Fixed

- Fix an infinite-loop issue when `Quick build and dismantle stacking labs` and `No condition build` are both enabled
- Fix a crash caused by `Initialize This Planet` in combat mode

## [1.0.19] - 2024-03-28

### Added

- New functions:
  * `Quick build and dismantle stacking labs`
  * `Protect veins from exhaustion`
    * By default, the vein amount is protected at 100, and oil speed is protected at 1.0/s, you can set them yourself in config file.
    * When reach the protection value, veins/oils steeps will not be mined/extracted any longer.
    * Close this function to resume mining and pumping, usually when you have enough level on `Veins Utilization`

### Removed

- Remove default shortcut key for `Auto-cruise`, to avoid misoperation. Please set it in the system options window manually if needed.

## [1.0.18] - 2024-03-24

### Changed

- Auto-cruise does not bypass dark-fog hives if they are targeted.

### Fixed

- Fix crash while coursing to a dark-fog hive.

## [1.0.17] - 2024-03-23

### Added

- New function: `Auto navigation on sailings`, which is inspired by [CruiseAssist](https://dsp.thunderstore.io/package/tanu/CruiseAssist/) and its extension [AutoPilot](https://dsp.thunderstore.io/package/tanu/AutoPilot/)
  * It keeps Icarus on course to the target planet
  * It will try to bypass any obstacles(planets, stars or dark-fog hives) on the way
  * Furthermore, there is also a shortcut key which can be set in the system options window, which is used to toggle `Auto-cruise` that enables flying to targeted planets fully automatically.
    * Auto-cruise will start when you target a planet on star map
    * It will use warper to fly to the target planet if the planet is too far away, the range can be configured.
    * It will speed down when approaching the target planet, to avoid overshooting

### Changed

- `Off-grid building and stepped rotation`: Hide Z coordinate from display if it is zero

### Fixed

- Fix a crash caused by `Stop ejectors when available nodes are all filled up` in latest game update

## [1.0.16] - 2024-03-15

### Added

- New function: `Hide tips for soil piles changes`

### Fixed

- Add CommonAPI to package manifest dependencies(missing in last version)

## [1.0.15] - 2024-03-10

### Added

- Enable `Hide UI` function(`F11` by default) while on Star Map view
- New function: `Treat stack items as single in monitor components`

### Changed

- Move shortcut key settings to system options window, which depends on [CommonAPI](https://dsp.thunderstore.io/package/CommonAPI/CommonAPI)

## [1.0.14] - 2024-03-02

### Added

- `Off-grid building and stepped rotation`: Add Z coordinate to display, and adjust the precision to 4 decimal after point

### Fixed

- Fix crash in `Initialize This Planet` again

## [1.0.13] - 2024-02-18

### Added

- `Off-grid building and stepped rotation`: show building coordinates(relative to grids) on building preview and building info panel now

### Changed

- Increase maximum count of Metadata Instantiations to 20000 (from 2000)
- Increase capacity of player order queue to 128 (from 16)

### Fixed

- Fix issue caused by game updates
  * `Remove some build conditions`: fixed issue that some conditions are not eliminated
  * `Initialize This Planet`: fixed crash issue

## [1.0.12] - 2024-01-12

### Fixed

- Fix a bug that ejectors aimed at even-numbered orbits stop working when `Stop ejectors when available nodes are all filled up` is enabled.

## [1.0.11] - 2024-01-09

### Removed

- Remove `Better auto-save mechanism` due to conflicts with DSPModSave and some other mods.

## [1.0.10] - 2024-01-01

### Fixed

- Fix a button display bug
- Fix a possible crash while `Enhanced control for logistic storage capacities` is enabled

## [1.0.9] - 2023-12-23

### Added

- New function: `Better auto-save mechanism`
  * Auto saves are stored in 'Save\AutoSaves' folder, filenames are combined with cluster address and date-time
  * Note: this will sort gamesaves by modified time on save/load window, so you don't have to use [DSP_Save_Game_Sorter] anymore

## [1.0.8] - 2023-12-21

### Added

- New function: `Enhanced control for logistic storage capacities`

## [1.0.7] - 2023-12-18

### Changed

- Games saved in Peace-Mode after Dark-Fog update can also be loaded as Combat-Mode now.

### Fixed

- Fix a crash issue on choosing language other than English and Chinese

## [1.0.6] - 2023-12-16

### Changed

- Convert old saves to Combat-Mode on loading

## [1.0.5] - 2023-12-15

### Added

- Sort blueprint structures before saving, to reduce generated blueprint data size a little.

### Changed

- Support game version 0.10.28.20759

## [1.0.4] - 2023-11-18

### Added

- Add new function: `Off-grid building and stepped rotation`

### Fixed

- Fix an issue that window position not restored and can not be resized when function is enabled but game is started with different mod profiles.

## [1.0.3] - 2023-10-31

### Added

- Add new function: `Quick build Orbital Collectors`.
- Add confirmation popup for `Initialize This Planet`, `Quick dismantle all buildings`, `Re-initialize Dyson Spheres` and `Quick dismantle Dyson Shells`.

### Fixed

- Fix error on `Remove build count and range limit` when building a large amount of belts.
- Fix an issue that window position not saved correctly when quit game without using in-game menu.

## [1.0.2] - 2023-10-22

### Added

- Add 2 new options:
  * Enable game window resize.
  * Remember window position and size on last exit.

### Changed

- Redesign config tabs, for clearer layout.

## [1.0.1] - 2023-10-16

### Fixed

- Fix config button text and tips while returning to title menu.
- Fix that error occurs while returning to title menu, with `Stop ejectors when available nodes are all filled up` enabled.
- Add a patch to fix the bug that warning popup on `Veins Utilization` upgraded to level 8000+.

## [1.0.0] - 2023-10-10

### Added

- Initial release
- Functions moved from [MechaDronesTweaks](https://dsp.thunderstore.io/package/soarqin/MechaDronesTweaks/) and [CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/)

</details>

<details>
<summary>简体中文</summary>

本更新日志采用 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 格式。

## [Unreleased]

## [1.6.4] - 2026-11-27

### 新增

- `反转传送带`：在「工厂」页启用后，将鼠标移到已建成的传送带上，按 `Alt+R` 直接反转，无需打开面板；可在系统选项中修改快捷键。
- 蓝图说明新增「最低产量中间产物」基准，按瓶颈等比例调整各最终产物的产量，并据此估算外部输入。数据完整且可平衡的生产链不会显示内部缺口，未使用的中间产能列为「多余」。耗电仍按满载估算。无法确定瓶颈时保留原说明。默认基准仍为「最终产物产量」。

### 变更

- 将蓝图浏览、复制和粘贴面板的「自动填写」与说明标题对齐，为原生图标留出空间。生成选项面板随入口滚动，在屏幕边缘调整位置，现有字段保持原位，文件列表仍可见。「产量基准」使用原生矩形下拉框，列表展开时覆盖底部操作区，面板尺寸和「生成说明」按钮的位置保持不变。点击叉号、点击面板外部或按 Esc 可关闭面板。下拉列表展开时，Esc 先收起列表。

### 修复

- `在和平模式下隐藏战斗相关科技`：从和平模式存档切换到战斗模式存档，或在另一个和平模式存档中关闭选项时，恢复科技树连线并修复数组越界。在标题画面关闭选项后，下次进入游戏也会恢复战斗相关科技。
- `自动巡航`：修复从紧邻气态巨星的卫星上起飞后，机甲又扎回地面的问题。
- 批量建造采矿机时，在放置前检查最终覆盖范围，确保按计划掩埋后，每台采矿机仍覆盖所选矿脉组中的至少 3 个矿点。

### 其他

- 批量建造采矿机仅在调试版中保存诊断快照。

## [1.6.3] - 2026-10-31

### 新增

- `快速铺设采矿机`：
  * 在「工厂」页增加按钮，自动在最近的固体矿脉周围密集铺设普通采矿机。每台至少覆盖选中矿脉的 3 个矿点，不混入附近其他矿脉。布局数量不受库存限制，采矿机不足的位置保留待建造虚影。遵守建造范围，不自动铺设传送带和电力设施。计算期间再次点击可取消。
  * 「允许恢复/掩埋矿物」默认关闭，仅使用可见矿点，不恢复或掩埋矿点。开启后可恢复未被建筑阻挡、且远离黑雾行星基地的矿点，并允许掩埋未使用的矿点来提高覆盖数；已有矿机和待建造矿机使用的矿点会受到保护。
- `蓝图说明`：
  * 在蓝图浏览、复制和粘贴界面增加「自动填写说明」按钮，用满负荷运行时每分钟的物料流量替换原说明，分别列出「缺少」「多余」「输入」「输出」「研究」，并将工厂与物流耗电写入「耗电」自定义字段。
  * 可用「增产」复选框选择是否计入增产效果，蓝图含喷涂机时默认勾选。较大的流量在保留显示精度的前提下使用 K/M/G 单位；整数部分达到四位的数字用逗号每三位分隔，带单位时也适用。无法计算的数值单独标注。保留其他自定义字段，填写后仍需手动保存蓝图。

### 变更

- `夜间日光灯`：光照改为随镜头变化，行星视图也适用；建筑、地表、太阳能板和传送带的光照方向保持一致。切换行星后正确恢复光照，航行、关闭功能或进入白模渲染时恢复原版光照。

### 修复

- `保护矿脉不会枯竭`：修复 1.6.2 中开启后仍不生效的问题，恢复设定的矿脉剩余量、油井最低流速保护及相应开采统计。

## [1.6.2] - 2026-10-18

### 变更

- `拆除蓝图选中的建筑`：按沙盒模式的瞬间拆除设置销毁物流站库存；常规拆除时仍正常返还库存。
- `用于自动购买黑雾物品的传送带信号`：统一使用当前游戏中的材料名称，保留原有兑换比例。

### 修复

- `提升物流塔和大型采矿机的最大功耗`：修复启用后调整大型采矿机开采速度时出现 0% 或异常速度的问题。
- `调整建筑输入缓冲`：修复 DSP 0.10.35.29104 及后续版本中射线接收器透镜缓冲设置不生效的问题，并将显示的游戏原始值更正为 2。
- `拖建电线杆`：修复载入存档后才启用功能时交替放置的预览异常，并在关闭时恢复原有间距。
- 打开暂停菜单时关闭建筑和传送带的悬浮提示，避免提示遮挡菜单。
- `保护矿脉不会枯竭`：开采速度估算、开采中资源数量和采矿机悬浮信息不再把受保护的矿脉计入开采；仍保留剩余储量显示。
- `在流速计中将堆叠物品视为单个物品`：修复未勾选时仍在启动游戏后生效、必须切换一次才能关闭的问题。
- `建造选项`：修复不对齐网格建造、拖建电线杆、储液罐快速存取和 Ctrl+Shift 拾取整段传送带物品在启动时忽略已保存开关的问题。
- `初始化本行星`：修复行星上存在全息信标时，初始化后可能出现数组越界报错的问题。
- `宇宙视图行星/星系矿脉数量显示`：修复矿脉数量文字偏下和矿物名称向右偏移的问题。

## [1.6.1] - 2026-09-29

### 变更

- `宇宙视图行星/星系矿脉数量显示`：将矿脉数量移至矿物名称左侧，避免与游戏新版按钮重叠。
- `自动巡航`：统一设置滑条的起始位置，并缩小滑条与数值之间的间距。

### 修复

- `范围铺设地基的最大区域扩大`：适配 DSP 0.10.35.29104，恢复 11x11 至 30x30 的铺设地基与恢复地形范围。
- 修复标题画面背景变黑的问题，包括关闭 UXAssist 窗口后背景仍不显示的情况。
- `设置提示`：修复问号按钮的游戏内提示不显示的问题。
- `物流站实时信息面板`：修复拆除物流站或初始化本行星后面板残留的问题，输入文字时也会保持刷新。
- `不渲染工厂建筑实体(除了传送带和分拣器)`：修复更新到戴森球计划 0.10.35.29057 后该选项不生效的问题。

## [1.6.0] - 2026-09-24

### 新增

- 新功能：`轨道采集器产物上限`，支持`应用到全宇宙`。
- `不渲染工厂建筑实体(除了传送带和分拣器)`：新增子选项`同时隐藏分拣器`，可一并隐藏分拣器，只保留传送带可见。

### 变更

- 支持戴森球计划 0.10.35.29057。
- `自动巡航`：重写了导航算法并新增更多选项：
  * `手动输入时停止`、`启用曲速`（含最低能量）、`自动加速`（含最低能量）、`黑雾巢穴跟踪距离`和`黑雾火种跟踪距离`。
  * 按钮上显示当前快捷键和状态，不再显示单独的屏幕提示。
- `自动建造`：改进了路径规划、障碍恢复和运输效率。
- `调整建筑输入缓冲`：现在显示游戏内原始默认值供参考。
- 更新射线接收器缓存和物品返还、矿脉枯竭提醒、手动制造快捷键以及批量购买科技控件。
- 隐藏沙土变动提示时保留沙土进度，禁用扩展刷子时安全恢复已保存的刷子大小。

### 修复

- 修复部分面板和控件的显示颜色问题。
- 恢复扩展地基刷子、恢复地形支持以及球面视图中的玩家操作。

## [1.5.8] - 2026-05-30

### 移除

- `记住上次退出时的窗口位置和大小`：移除不再有用的窗口钩子。

### 修复

- `初始化本行星`：修复可能的崩溃。
- `物流站实时信息面板`：修复物流塔数量 > 128 时的显示问题。

## [1.5.7] - 2026-03-20

### 修复

- 修复了与游戏更新0.10.34.28505的兼容性。

## [1.5.6] - 2026-03-16

### 修复

- `初始化本行星`：修复已知的崩溃和无限循环问题。
- `显示最近银河系上传结果`：修复仅显示最早上传结果而非最新结果的问题。
- 修复一些由 LLM AI 发现的其他小问题。

## [1.5.5] - 2026-03-07

### 新增

- 在星图视图中启用`隐藏UI`功能(默认按键为`F11`): 隐藏所有其他宇宙元素。

### 变更

- `初始化本行星`:
  * 修复崩溃问题。
  * 增加了3个可返还特定物品的选项。

### 修复

- 修复以下功能的异常检查警告:
  * `使用元数据解锁所有科技`
  * `在和平模式下隐藏战斗相关科技`
  * ``在升级面板上恢复`分拣器货物堆叠`的升级``

## [1.5.4] - 2026-02-15

### 变更

- `自动建造`：
  * 增加了一个 UI 选项，可完全隐藏该功能。
  * 修复了可能导致盘旋的问题。

### 修复

- `自动巡航`：修复了一些用户体验相关的逻辑问题。

## [1.5.3] - 2026-02-10

### 新增

- 新功能：`自动建造`
  * 自动飞行到待建造的建筑处进行建造。

### 变更

- `自动巡航`：在未研究`驱动引擎 Lv2`时，不再尝试飞离行星
- `用于自动购买黑雾物品的传送带信号`：移动了其面板网格位置，避免与新添加的信号冲突

### 修复

- `初始化行星`：修复物流塔运输船显示异常的问题
- 当启用`拖动建造电线杆时自动使用最大连接距离间隔`和`交替建造电力感应塔和无线输电塔`时，对复制无线输电塔也生效了
- 修复``在升级面板上恢复`分拣器货物堆叠`的升级``的实现，使其重新工作

## [1.5.2] - 2026-01-30

### 修复

- 修复在银河系登录/上传出错时崩溃的问题

## [1.5.1] - 2026-01-30

### 变更

- 支持游戏版本 0.10.34.28392

## [1.5.0] - 2026-01-23

### 新增

- 新功能：
  * `显示银河系发电量排行`：按钮位于银河视图左上角
  * `显示最近银河系上传结果`：按钮在UXAssist的`常规`标签页内

### 变更

- 支持游戏版本 0.10.34
- `物流塔存储数量限制控制改进`：现在可用于物流控制面板

### 修复

- `夜间日光灯`：修复加载存档后日光角度没有更新的问题
- `保护矿脉不会耗尽`：修复了一个小问题
- `初始化本行星`：提升稳定性并修复部分Bug

## [1.4.5] - 2025-11-14

### 新增

- 新功能：`按住Ctrl+Shift点击从整条传送带抓取物品`，包含两个子选项：`包含传送带分支` 和 `包含连接的分拣器`

### 修复

- 临时修复 [BlueprintTweaks](https://thunderstore.io/c/dyson-sphere-program/p/kremnev8/BlueprintTweaks/) 的 `拖拽拆除` 功能（等待 limoka 合并 [Pull Request](https://github.com/limoka/DSP-Mods/pull/133)）：
  * 地面绘制现在已正确
  * 拆除逻辑遵循官方实现，避免潜在崩溃可能
- 修正了 CommonAPI 的补丁，现在快捷键设置可以如预期保存了，不好意思上一版本实现有误

## [1.4.4] - 2025-11-06

### 修复

- `记住上次退出时的窗口位置和大小`：修复对游戏补丁0.10.33的兼容性
- `物流站实时信息面板`：尝试修复一个显示问题
- 对[CommonAPI](https://thunderstore.io/c/dyson-sphere-program/p/CommonAPI/CommonAPI/)临时打补丁，修复快捷键设置未能保存到配置文件的已知问题（其[PR](https://github.com/limoka/CommonAPI/pull/14)仍未合并）

## [1.4.3] - 2025-10-04

### 新增

- `交替建造电力感应塔和无线输电塔`:
  * 修复了在最新游戏补丁中的错误实现
  * 由于新的矩形建造机制，现在无法使用电力感应塔作为起始电塔

### 变更

- `宇宙视图矿脉数量显示`：兼容添加矿脉类型的mod
- `物流站实时信息面板`：尝试修复可能的崩溃问题

## [1.4.2] - 2025-09-30

### 修复

- 修复了一个崩溃问题

## [1.4.1] - 2025-09-29

### 修复

- 修复了与最新游戏补丁的兼容性问题

## [1.4.0] - 2025-09-29

### 新增

- 由于缺乏维护，整合内置了[Planet Vein Untilization](https://thunderstore.io/c/dyson-sphere-program/p/testpushpleaseignore/Planet_Vein_Utilization/)，并修复了一些小问题

### 变更

- `保存蓝图前对建筑进行排序`：改进了排序规则
- `星图过滤器`：现在星系编号（按星系生成顺序）显示为前缀
- 现在使用C#的`Debug`配置构建，以避免`Release`配置中的优化导致的一些问题

### 移除

- 支持游戏版本 0.10.33，移除了一些功能：
  * 移除`放大鼠标指针`：Unity 2022 读取系统设置里的鼠标指针大小，软件渲染不再影响其大小
  * 移除`设置使用的CPU线程`：因为官方已支持此功能

### 修复

- `拆除蓝图选中的建筑`：修复了从物流站中掉落的物品丢失增产点数的问题
- `移除部分不影响游戏逻辑的建造条件`：修复了错误的逻辑
- `物流站实时信息面板`：修复了一些显示问题
- 修复了配置窗口标签按钮的背景图像问题

## 1.3.7 (无公开发布记录)

### 新增

- `自动配置物流站`: 增加`设置默认远程逻辑为仓储`

### 修复

- `初始化行星`: 修复可能导致崩溃的问题

## [1.3.6] - 2025-06-01

### 新增

- `调整建筑缓冲区`：为2个新建筑添加缓冲区调整
  * `弹射太阳帆缓冲区数量`：范围5-400（步进值为5），默认值为20（与游戏相同）
  * `发射井火箭缓冲区数量`：范围1-20，默认值为20（与游戏相同）

### 变更

- `拆除蓝图选中的建筑`：
  * 修复了拆除虚影建筑时崩溃的问题
  * 重命名为`蓝图复制模式快捷键`，同时添加了选择所有建筑的快捷键（默认为Ctrl+A）
- `允许物流站和大型采矿机物品溢出`：现在也适用于物流控制面板

## [1.3.5] - 2025-05-10

### 修复

- `基于mod管理器配置档案的存档文件夹`：修复游戏启动时崩溃的问题

## [1.3.4] - 2025-05-09

### 修复

- `自动配置物流站`：修复了高级采矿机和物流配送器的一些设置未被正确应用的问题

## [1.3.3] - 2025-05-09

### 新增

- `自动配置物流站`：现在可以为战场分析基站设置最大充能功率

### 变更

- `星图过滤器`：当过滤器UI显示时隐藏顶部重叠窗口
- `航行时自动导航`：
  * 添加快速启用/禁用`自动巡航`的按钮
  * 如果所需科技未研究则不自动使用翘曲器
- `物流存储容量控制改进`和`允许物流站和大型采矿机物品溢出`：
  * 如果物流存储容量已经超过升级后的最大容量，则在升级`物流运输机容量`时不会按比例提升
  * 粘贴蓝图时物流存储容量将降低至科技容量限制
- `物流站实时信息面板`：支持修改物流站槽位数的mod

### 修复

- `初始化行星`：修复崩溃问题
- `拆除蓝图选中建筑`：修复传送带连接建筑意外被拆除的问题
- `基于mod管理器配置档案的存档文件夹`：修复与[SaveTheWindows](https://thunderstore.io/c/dyson-sphere-program/p/starfi5h/SaveTheWindows/)的兼容性

## [1.3.2] - 2025-04-28

### 新增

- 新功能：`在和平模式下隐藏战斗相关科技`
- 新按钮：`使用元数据解锁所有科技`
- 在星图过滤器中添加复选框以合并结果

### 修复

- 修复了一些星图矿脉/行星过滤条件
- 修复了在和平模式下`初始化本行星`导致的崩溃问题
- 修复了与`NebulaMultiplayerMod`的兼容性问题

## [1.3.1] - 2025-04-26

### 变更

- 调整了星系名称过滤器中的行星类型列表

### 修复

- 修复了在星图上点击新增的下拉框时部分UI元素被隐藏的问题
- 修复了未启用`显示星系名称快捷键`时星系名称过滤器不生效的问题
- `拆除蓝图选中的建筑`：修复了物流站中的物品未被丢出的问题

## [1.3.0] - 2025-04-26

### 新增

- 在星图上添加新功能：
  * 添加星系名过滤器，现在可以按矿物或行星类型过滤显示的星系名
  * 添加了一个下拉框用以切换显示所有星系的距离和/或行星数量
- `自动配置物流站`: 增加了一个选项`限制自动补充数量为配置的值`

### 变更

- `记住上次退出时的窗口位置和大小`：优化实现
- 优化了一些UI代码

### 修复

- `切割传送带`：修复了输入问题
- `启用显示所有星系名称的快捷键`：修复了在未打开星图视图时读取切换键的问题
- `拆除蓝图选中的建筑`：修复了预建造建筑未被拆除的问题

## [1.2.20] - 2025-04-21

### 新增

- 新功能：`拆除蓝图选中的建筑`
  * 在蓝图复制模式下按快捷键拆除选中的建筑
  * 默认快捷键是Ctrl+X，可以在系统选项面板中设置
- 新功能：`自动配置物流站`
  * 自动配置的建筑包括：物流配送器、行星物流站、星际物流站、高级采矿机

### 修复

- `夜间日光灯`：再次修复了光照角度未正确更新的问题

## [1.2.19] - 2025-04-16

### 新增

- 新功能：`调整建筑输入缓冲`
  * 工厂配方计算公式，在`工厂配方缓冲时间倍率秒数x每秒需要的原料数量`和`工厂配方缓冲最小倍率x每生产一次配方需要的原料数量`中取更大的那个值
    * `工厂配方缓冲时间倍率(秒)`：范围2-10，默认为4(同游戏)
    * `工厂配方缓冲最小倍率`：范围2-10，默认为2(同游戏)
  * 研究站矩阵合成模式计算公式，默认缓存`研究站矩阵合成模式缓存数量`个，当使用自演化研究站时，如果配方的原始生产时间不大于9秒，则增加`自演化研究站矩阵额外缓冲数量`*(`研究站速度倍率`-1)
    * `研究站矩阵合成模式缓存数量`：范围2-20，默认为6(同游戏)
    * `自演化研究站矩阵额外缓冲数量`：范围1-10，默认为3(同游戏)
  * `研究站科研模式缓存数量`：范围2-20，默认为10(同游戏)
  * `射线接收器透镜缓冲数量`：范围1-20，默认为1(游戏默认为20)
- 新功能：`启用显示所有星系名称的快捷键`
  * 新增一个快捷键，按住后始终在星图显示所有星系名称，默认为`Alt`
  * 新增一个快捷键，在星图视图切换三种星系名称显示状态：`原始显示状态`，`显示所有名称`，`隐藏所有名称`，默认为`Tab`，关闭星图时会恢复到原始状态

### 修复

- `切割传送带`：修复了实体逻辑连接未切断导致复制为蓝图时传送带未被切断的问题。

## [1.2.18] - 2025-04-10

### 变更

- `保护矿脉不会耗尽`：优化实现，当`矿物利用`升级到390级以上时消耗速度变为0时，矿脉将不再被保护。

### 修复

- `夜间日光灯`：修复了光照角度未正确更新的问题。

## [1.2.17] - 2025-04-04

### 修复

- 修复了`保护矿脉不会耗尽`导致矿脉状态显示错误和矿脉未被消耗的错误实现

## [1.2.16] - 2025-03-31

### 新增

- 新功能：`切割传送带`
  * 按快捷键切割光标位置的传送带
  * 默认快捷键是Alt+X，可以在系统选项面板中设置
- 新功能：`基于mod管理器配置档案名`
  * 选项文件存储在`Options\<ProfileName>.xml`中

### 修复

- 修复了与游戏更新0.10.32.25779的兼容性

## [1.2.15] - 2025-02-19

### 修复

- `脱离网格建造和小角度旋转`：修复了与0.10.32.25682的兼容性 (#57)
- `物流塔存储数量限制控制改进`：修复了可能导致崩溃的问题 (#54)

## [1.2.14] - 2025-01-24

### 修复

- 修正设置窗口右上角多出一个菜单图标的问题
- `当可用节点全部造完时停止弹射`：修复了与`戴森球计划 v0.10.32.25496`的兼容性

## [1.2.13] - 2025-01-12

### 变更

- `物流控制面板改进`：现在也自动将拿着的物品设为筛选条件

### 修复

- `用于自动购买黑雾物品的传送带信号`：修复了可能导致崩溃的问题
- 修复了UI面板上的对齐问题

## [1.2.12] - 2024-12-22

### 修复

- `只建造节点不建造框架`：修复了关闭此功能时框架不进行建造的问题
- `拖动建造电线杆时自动使用最大连接距离间隔`：修复了某些位置无法放置单个电线杆的问题

## [1.2.11] - 2024-12-14

### 修复

- 修复了游戏更新导致的提示按钮鼠标悬停时不显示提示文字的问题

## [1.2.10] - 2024-12-01

### 修复

- `设置使用的CPU线程`：修复了对没有超线程的CPU的大小核检查
- `初始化戴森球`和`快速拆除戴森壳`：修复了在戴森球面板激活时可能导致崩溃的问题，以及显示错误的问题。

## [1.2.9] - 2024-11-26

### 变更

- `保护矿脉不会耗尽`：
  * 修复了矿脉保护导致崩溃的问题(#50)
  * 修复了配置中的最小采油速度不起作用的问题(#50)
  * 修复了无限资源模式下油井保护导致无法采油的问题(#52)

## [1.2.8] - 2024-11-12

### 新增

- 新功能：`储液罐快速注入和抽取液体`
  * 你可以设置储液罐操作速度的倍率
  * 影响手动注入和抽取，以及从储液罐上层传输到下层的速度

### 修复

- 在游戏窗口标题中追加mod配置档案名的修复：
  * 修复了多实例启动时窗口标题未正确设置的问题
  * 修复了启用BepInEx调试控制台时窗口标题未正确设置的问题
- `物流运输站实时信息面板`：修复了一个物品状态条意外显示的问题

## [1.2.7] - 2024-09-26

### 修复

- 修复了一些小问题

## [1.2.6] - 2024-09-26

### 新增

- `记住上次退出时的窗口位置和大小`
  * 修复了即使选项被禁用也恢复窗口位置的问题
  * 修复了窗口最小化时关闭游戏导致窗口位置被错误记录的问题

## [1.2.5] - 2024-09-25

### 新增

- 新功能：`设置进程优先级`
- 新功能：`设置使用的CPU线程`
- `拖动建造电线杆时自动使用最大连接距离间隔`：添加一个新的设置项`交替建造电力感应塔和无线输电塔`

## [1.2.4] - 2024-09-23

### 变更

- `夜间日光灯`：
  * 修复了航行时闪烁的问题
  * 现在可以配置入射光线角度了
- 一些小修复和调整

### 移除

- 不再对`BulletTime`的速度控制打补丁，因为`BulletTime`已更新支持可配置最大速度

### 修复

- `放大鼠标指针`：修复已知问题
- `买断科技也同时买断所有前置科技`：修复了数据错误警告弹窗的问题

## [1.2.3] - 2024-09-21

### 修复

- `物流运输站实时信息面板`：修复了物品数量超过容量限制时条长度不匹配的问题
- `夜间日光灯`：修复了不起作用的问题

## [1.2.2] - 2024-09-20

### 变更

- `逻辑帧倍率`：将默认快捷键设置为`Ctrl`+`-/+`，以避免与其他快捷键冲突

### 修复

- `物流运输站实时信息面板`：修复了文本颜色不匹配的问题

## [1.2.1] - 2024-09-19

### 变更

- `脱离网格建造和小角度旋转`：
  * 修复了传送带脱离网格建造的默认快捷键
  * 修复了坐标显示问题

## [1.2.0] - 2024-09-18

### 新增

- 新功能：`逻辑帧倍率`
  * 这将改变游戏运行速度，最慢0.1倍，最快10倍
  * 设置了一对快捷键(`-`和`+`)，可以-/+0.5倍改变逻辑帧倍率
  * 注意：
    * 高逻辑帧倍率不能保证稳定性，特别是在工厂负载较重时
    * 这不会影响一些游戏动画
    * 当在`Auxilaryfunction`mod中设置游戏速度时，此功能将被禁用
    * 当安装了`BulletTime`mod时，此功能将被隐藏，但会对`BulletTime`的速度控制打补丁，使其最大速度变为10倍

### 变更

- `脱离网格建造和小角度旋转`：由于与新游戏更新中的快捷键冲突，传送带脱离网格建造的快捷键默认更改为`Ctrl`，并且现在可以在系统选项中设置
- 代码重构，以获得更好的可维护性

### 修复

- `物流运输站实时信息面板`：修复了一个崩溃问题
- `戴森球自动快速建造`：修复了可能出现的错误生产记录

## [1.1.6] - 2024-09-04

### 新增

- 新功能：`放大鼠标指针`
  * 注意：这将启用软件指针模式，可能会在CPU负载较重时导致鼠标移动延迟
- 新功能：`物流运输站实时信息面板`
  * 注意：如果你启用了`Auxilaryfunction`中的`展示物流站信息`，此功能将被隐藏

### 移除

- 移除了AssetBundle的使用，将所有图标移入`Assembly资源`，以获得更好的灵活性

### 修复

- 修复了`戴森球自动快速建造`未生成太阳帆生产记录的问题

## [1.1.5] - 2024-08-30

### 新增

- 新功能：`物流控制面板改进`
  * 打开面板时自动将鼠标指向物品设为筛选条件
  * 在控制面板物流塔列表中右键点击物品图标快速设置为筛选条件
- 新功能：`戴森球自动快速建造速度倍率`
  * 注意：这仅适用于沙盒模式下的`戴森球自动快速建造`功能
- 新功能：`基于mod管理器配置档案名的存档文件夹`
  * 存档文件会存储在`Save\&lt;ProfileName&gt;`文件夹中
  * 如果匹配默认配置档案名则使用原始存档位置

### 变更

- `快速建造和拆除堆叠研究站`：现在也支持储物仓和储液罐
- `记住上次退出时的窗口位置和大小`：如果分辨率相关的配置项未改变，则在应用游戏选项时不调整窗口大小
- 自动调整面板大小适应内容，以更好地支持多语言和依赖于UX助手配置面板功能的mod

### 修复

- `允许调整游戏窗口大小`：在应用游戏选项时保持窗口可调整大小

## [1.1.4] - 2024-08-23

### 修复

- 修复了`移除部分不影响游戏逻辑的建造条件`

## [1.1.3] - 2024-08-21

### 变更

- 界面文本现在完全跟随游戏语言设置改变

### 修复

- 修复了配置面板中勾选框的鼠标悬停区域
- 修复了`加载和平模式存档时将其转换为战斗模式`不起作用的问题

## [1.1.2] - 2024-08-17

### 修复

- `用于自动购买黑雾物品的传送带信号`: 总是将传送带信号添加到面板，以修复禁用时传送带图标丢失的问题。

## [1.1.1] - 2024-08-17

### 修复

- 修复了资源包加载问题

## [1.1.0] - 2024-08-17

### 新增

- `可用节点全部造完时停止弹射`: 当所有戴森球节点都造完时，在弹射器面板上显示`没有可建造节点`
- 如果使用mod管理器(`Thunderstore Mod Manager`或`r2modman`)启动游戏，在游戏窗口标题中追加mod配置档案名
- 新功能：
  * `买断科技也同时买断所有前置科技`：可以批量买断科技及其所有前置科技。所有未解锁的科技/升级都会显示买断按钮。
  * `用于自动购买黑雾物品的传送带信号`，启用时：
    * 在信号面板上添加了6个传送带信号，可以用于自动购买黑雾道具。
    * 生成的物品堆叠数为4。
    * 兑换比率遵循原始游戏设计，即：
      * 1个元宇宙 = 20个黑雾矩阵
      * 1个元宇宙 = 60个能量碎片
      * 1个元宇宙 = 30个硅基神经元
      * 1个元宇宙 = 30个负熵奇点
      * 1个元宇宙 = 30个物质重组器
      * 1个元宇宙 = 10个核心素

## [1.0.26] - 2024-06-05

### 新增

- 新功能：
  * 在升级面板上恢复`分拣器货物堆叠`的升级
  * 将`分拣器货物堆叠`设为未研究状态

### 变更

- `保护矿脉不会耗尽`配置的改动：
  * 现在默认矿脉数量保护在1000
  * 最大矿脉数量改为10000，最大采油速度改为10.0/s

## [1.0.25] - 2024-05-24

### 修复

- 修复了`不渲染工厂建筑实体(除了传送带和分拣器)`启用时无法点穿工厂实体的问题

## [1.0.24] - 2024-05-24

### 新增

- 新功能：`拖动建造电线杆时自动使用最大连接距离间隔`
- 新功能：`允许物流塔和大型采矿机物品溢出`
  * 当尝试塞入手中物品时允许溢出
  * 允许`物流塔存储数量限制控制改进`超过科技容量限制
  * 在加载游戏时移除物流塔容量限制检查

### 变更

- `不渲染工厂建筑实体(除了传送带和分拣器)`的改动
  * 在配置面板中添加了一个快捷键来切换此功能
  * 现在也可以点击到分拣器了

## [1.0.23] - 2024-05-05

### 新增

- 新功能：
  * `不渲染工厂建筑实体(除了传送带和分拣器)`
    * 这使得玩家可以点穿工厂实体直接点到传送带
  * 在任意位置`打开黑雾通讯器`
- 传送带现在可以脱离网格建造了，通过按住`切换分流器样式`的快捷键(默认`Tab`)
- 为`自动巡航`添加一个子选项`自动加速`

### 变更

- `自动巡航`现在在核心能量至少80%时才加速

## [1.0.22] - 2024-03-30

### 修复

- 修复了`快速建造和拆除堆叠研究站`导致的崩溃问题

## [1.0.21] - 2024-03-30

### 修复

- 修复了`脱离网格建造和小角度旋转`在最新游戏更新后无法小角度旋转的问题
- 修复了`航行时自动导航`和`自动巡航`的一些问题。现在只有能量至少10%时才加速，能量至少50%时才启动曲速

## [1.0.20] - 2024-03-29

### 修复

- 修复了`快速建造和拆除堆叠研究站`和`无条件建造`同时启用时可能导致的逻辑死循环问题
- 修复了在战斗模式下`初始化本行星`导致的崩溃问题

## [1.0.19] - 2024-03-28

### 新增

- 新功能：
  * `快速建造和拆除堆叠研究站`
  * `保护矿脉不会耗尽`
    * 默认矿脉数量保护于剩余100，采油速保护于速度1.0/s，你可以在配置文件中自行设置。
    * 当达到保护值时，矿脉和油井将不再被开采。
    * 关闭此功能以恢复开采，一般是当你在`矿物利用`上有足够的等级时。

### 移除

- 移除了`自动巡航`的默认快捷键，以避免误操作。如有需要请手动在系统选项窗口中设置。

## [1.0.18] - 2024-03-24

### 变更

- 当黑雾巢穴是目标时，自动导航不会绕过它

### 修复

- 修复了以黑雾巢穴为目标时导致崩溃的问题

## [1.0.17] - 2024-03-23

### 新增

- 新功能：`航行时自动导航`，想法来自[CruiseAssist](https://dsp.thunderstore.io/package/tanu/CruiseAssist/)及其扩展[AutoPilot](https://dsp.thunderstore.io/package/tanu/AutoPilot/)
  * 它会保持伊卡洛斯飞向目标星球
  * 它会尝试绕过途中的任何障碍物(行星、恒星或黑雾巢穴)
  * 此外，还有一个快捷键可以在系统选项窗口中设置，用于切换`自动巡航`，实现完全自动化的飞行至目标星球。
    * 当你选择目标星球后，自动巡航就会开始
    * 如果目标星球距离过远会自动使用曲速(超过5AU)，你可以在面板上更改这个值。
    * 它会在接近目标星球时减速，以避免发生越过目标的情况

### 变更

- `脱离网格建造和小角度旋转`：如果Z坐标为零则从显示中隐藏

### 修复

- 修复了最新游戏更新后`当可用节点全部造完时停止弹射`引起崩溃问题

## [1.0.16] - 2024-03-15

### 新增

- 新功能：`隐藏沙土数量变动的提示`

### 修复

- 添加了对CommonAPI的包依赖(上个版本忘记加了)

## [1.0.15] - 2024-03-10

### 新增

- 在星图视图中启用`隐藏UI`功能(默认按键为`F11`)
- 新功能：`在流速计中将堆叠物品视为单个物品`

### 变更

- 将快捷键设置移动到系统选项窗口，依赖于[CommonAPI](https://dsp.thunderstore.io/package/CommonAPI/CommonAPI)

## [1.0.14] - 2024-03-02

### 新增

- `脱离网格建造和小角度旋转`：现在显示建筑Z坐标，并将精度调整为小数点后4位

### 修复

- 再次尝试修复`初始化本行星`导致的崩溃问题

## [1.0.13] - 2024-02-18

### 新增

- `脱离网格建造和小角度旋转`：现在在建造预览和建筑信息面板上显示建筑坐标(相对于网格)

### 变更

- 将元数据提取的最大数量增加到20000(原来为2000)
- 将玩家指令队列的容量增加到128(原来为16)

### 修复

- 修复了游戏更新导致的问题
  * `移除部分不影响游戏逻辑的建造条件`：修复了一些条件未被移除的问题
  * `初始化本行星`：修复了崩溃问题

## [1.0.12] - 2024-01-12

### 修复

- 修复了当`当可用节点全部造完时停止弹射`选项启用时，瞄准偶数轨道的弹射器停止工作的bug

## [1.0.11] - 2024-01-09

### 移除

- 移除`更好的自动保存机制`，因为与DSPModSave和其他一些mod冲突

## [1.0.10] - 2024-01-01

### 修复

- 修复了一个按钮显示错误
- 修复了`物流塔存储数量限制控制改进`启用时可能导致的崩溃问题

## [1.0.9] - 2023-12-23

### 新增

- 新功能：`更好的自动保存机制`
  * 自动存档会以星区地址和日期时间组合为文件名存储在'Save\AutoSaves'文件夹中
  * 注意：此功能会在保存/读取菜单按最后修改时间对存档进行排序，因此你不再需要[DSP_Save_Game_Sorter]了

## [1.0.8] - 2023-12-21

### 新增

- 新功能：`物流塔存储数量限制控制改进`

## [1.0.7] - 2023-12-18

### 变更

- 黑雾更新后使用和平模式保存的存档现在也可以转换为战斗模式了

### 修复

- 修复了选择英文和中文以外的语言时的崩溃问题

## [1.0.6] - 2023-12-16

### 变更

- 在加载旧存档时将其转换为战斗模式

## [1.0.5] - 2023-12-15

### 新增

- 保存蓝图前对建筑进行排序，以减少生成的蓝图数据大小

### 变更

- 支持游戏版本0.10.28.20759

## [1.0.4] - 2023-11-18

### 新增

- 添加了新功能：`脱离网格建造和小角度旋转`

### 修复

- 修复了当功能启用但游戏使用不同的mod配置文件启动时窗口位置无法正确恢复和不可拖动改变大小的问题

## [1.0.3] - 2023-10-31

### 新增

- 添加了新功能：`快速建造轨道采集器`
- 为`初始化行星`，`快速拆除所有建筑`，`初始化戴森球`和`快速拆除戴森壳`添加了确认弹窗

### 修复

- 修复了`移除建造数量和范围限制`在建造大量传送带时可能导致的错误
- 修复了在不使用游戏内菜单退出游戏时窗口位置无法正确保存的问题

## [1.0.2] - 2023-10-22

### 新增

- 添加了两个新选项：
  * 可调整游戏窗口大小(可最大化和拖动边框)
  * 记住上次退出时的窗口位置和大小

### 变更

- 重新设计了配置面板，使布局更清晰

## [1.0.1] - 2023-10-16

### 修复

- 修复了返回标题界面后设置按钮文本和提示信息不正确的问题
- 修复了`当可用节点全部造完时停止弹射`选项启用时返回标题界面可能导致崩溃的问题
- 添加了一个补丁，修复了`矿物利用`升级到8000级以上时弹出警告的bug

## [1.0.0] - 2023-10-10

### 新增

- 初始版本
- 从[MechaDronesTweaks](https://dsp.thunderstore.io/package/soarqin/MechaDronesTweaks/)和[CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/)移动了部分功能过来

</details>

[1.6.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.6.4/
[1.6.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.6.3/
[1.6.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.6.2/
[1.6.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.6.1/
[1.6.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.6.0/
[1.5.8]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.5.8/
[1.5.7]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.5.7/
[1.5.6]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.5.6/
[1.5.5]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.5.5/
[1.5.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.5.4/
[1.5.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.5.3/
[1.5.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.5.2/
[1.5.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.5.1/
[1.5.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.5.0/
[1.4.5]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.4.5/
[1.4.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.4.4/
[1.4.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.4.3/
[1.4.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.4.2/
[1.4.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.4.1/
[1.4.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.4.0/
[1.3.6]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.3.6/
[1.3.5]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.3.5/
[1.3.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.3.4/
[1.3.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.3.3/
[1.3.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.3.2/
[1.3.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.3.1/
[1.3.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.3.0/
[1.2.20]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.20/
[1.2.19]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.19/
[1.2.18]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.18/
[1.2.17]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.17/
[1.2.16]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.16/
[1.2.15]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.15/
[1.2.14]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.14/
[1.2.13]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.13/
[1.2.12]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.12/
[1.2.11]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.11/
[1.2.10]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.10/
[1.2.9]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.9/
[1.2.8]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.8/
[1.2.7]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.7/
[1.2.6]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.6/
[1.2.5]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.5/
[1.2.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.4/
[1.2.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.3/
[1.2.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.2/
[1.2.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.1/
[1.2.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.2.0/
[1.1.6]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.1.6/
[1.1.5]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.1.5/
[1.1.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.1.4/
[1.1.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.1.3/
[1.1.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.1.2/
[1.1.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.1.1/
[1.1.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.1.0/
[1.0.26]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.26/
[1.0.25]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.25/
[1.0.24]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.24/
[1.0.23]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.23/
[1.0.22]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.22/
[1.0.21]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.21/
[1.0.20]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.20/
[1.0.19]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.19/
[1.0.18]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.18/
[1.0.17]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.17/
[1.0.16]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.16/
[1.0.15]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.15/
[1.0.14]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.14/
[1.0.13]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.13/
[1.0.12]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.12/
[1.0.11]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.11/
[1.0.10]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.10/
[1.0.9]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.9/
[1.0.8]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.8/
[1.0.7]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.7/
[1.0.6]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.6/
[1.0.5]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.5/
[1.0.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.4/
[1.0.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.3/
[1.0.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.2/
[1.0.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.1/
[1.0.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/v/1.0.0/
