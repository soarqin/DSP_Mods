<details>
<summary>Read me in English</summary>

## Changlog

* 1.4.4
  * `Remember window position and size on last exit`: Fix compatiblity for game patch 0.10.33
  * `Real-time logistic stations info panel`: Try to fix a display issue.
  * Patch [CommonAPI](https://thunderstore.io/c/dyson-sphere-program/p/CommonAPI/CommonAPI/) temporarily to fix a known issue that shortcut key settings are not saved to config, while its [PR](https://github.com/limoka/CommonAPI/pull/14) is not merged yet.
* 1.4.3
  * `Build Tesla Tower and Wireless Power Tower alternately`:
    * Fix wrong implementation for latest game patch.
    * Cannot use Tesla Tower as start Power Tower now, due to new rectangular area build mechanism.
  * `Planet Vein Untilization`: Support mods that add new vein types.
  * `Real-time logistic stations info panel`: Try to fix possible crash.
* 1.4.2
  * Fixed a crash issue.
* 1.4.1
  * Fixed a compatible issue with latest game patch.
* 1.4.0
  * Support game version 0.10.33, with some features removed:
    * Remove `Scale up mouse cursor`: Unity 2022 set cursor size from system settings, software rendering does not affect its size now.
    * Remove `Set enabled CPU threads`: They are officially supported.
  * `Dismantle blueprint selected buildings`: Fixed an issue that proliferator points are lost for items dropped from logstic stroages.
  * `Sort blueprint structures before saving`: Improved sorting rules.
  * `Starmap filter`: Now star indices (as in galaxy generation order) are displayed as prefix.
  * Embedded [Planet Vein Untilization](https://thunderstore.io/c/dyson-sphere-program/p/testpushpleaseignore/Planet_Vein_Utilization/) due to its lack of maintainance, with minor bug fixes.
  * `Remove some build conditions`: Fix a wrong logic.
  * `Real-time logistic stations info panel`: Fix some display issues.
  * Fix background image issue for tab buttons on config window.
  * Now build in C# `Debug` Configuration, to avoid some issues caused by optimizations in `Release` Configuration.
* 1.3.7
  * `Re-initialize planet`: Fix a possible crash.
  * `Auto-config logistic stations`: Add `Set default remote logic to storage`
* 1.3.6
  * `Dismantle blueprint selected buildings`:
    * Fix a crash on dismantling preview buildings.
    * Rename to `Shortcut keys for Blueprint Copy mode`, while adding a shortcut key to select all buildings (Ctrl+A by default).
  * `Allow overflow for Logistic Stations and Advanced Mining Machines`: Working for Logistics Control Panel now.
  * `Tweak building buffer`: add buffer tweaking for 2 new buildings
    * `Ejector Solar Sails buffer count`: Range 5-400 (step by 5), default is 20 (same as game)
    * `Silo Rockets buffer count`: Range 1-20, default is 20 (same as game)
* 1.3.5
  * `Mod manager profile based save folder`: Fix crash on game startup
* 1.3.4
  * `Auto-config logistic stations`: Fix a bug that some settings are not applied to Advanced Mining Machines and Logistics Distributors
* 1.3.3
  * `Starmap filter`: Hide top overlaping windows while the filter UI is shown.
  * `Auto-config logistic stations`: Can set Max. Charging Power for Battlefield Analysis Base now.
  * `Re-initialize planet`: Fix a crash.
  * `Auto navigation on sailings`:
    * Add a button to enable/disable `Auto-cruise` quickly.
    * Do not auto-use Warper if required Tech is not researched.
  * `Dismantle blueprint selected buildings`: Fix an issue that belt connected buildings are dismantled unexpectly.
  * `Mod manager profile based save folder`: Fix compatibility with [SaveTheWindows](https://thunderstore.io/c/dyson-sphere-program/p/starfi5h/SaveTheWindows/).
  * `Enhanced control for logistic storage capacities` & `Allow overflow for Logistic Stations and Advanced Mining Machines`:
    * Logistic storage capacities are not scaled on upgrading `Logistics Carrier Capacity`, if they are already greater than upgraded maximum capacity.
    * Logistic storage capacities will be reduced to tech capacity limits on pasting blueprints.
  * `Real-time logistic stations info panel`: Support for mods that change slot count of logistic stations.
* 1.3.2
  * New feature: `Disable battle-related techs in Peace mode`
  * New button: `Unlock all techs with metadata`
  * Add a checkbox to make union of results in starmap filter.
  * Fix some starmap vein/planet filter conditions.
  * Fix a crash caused by `Re-initialize planet` in peace mode.
  * Fix compatibility with `NebulaMultiplayerMod`.
* 1.3.1
  * Fix an issue that some UI elements are hidden while hitting the newly added combobox on Starmap.
  * Fix an issue that star name filter is not applied if `Shortcut keys for showing stars` is not enabled.
  * `Dismantle blueprint selected buildings`: Fix an issue that items in Logistic Station slots are not dropped out.
  * Tweak star name filter's planet type list.
* 1.3.0
  * New feature for starmap view:
    * Add a star name filter, you can filter displayed star names by ores or planet types now.
    * Add a dropdown box to show all stars' distance and/or planet count.
  * `Cut conveyor belt`: Fix input issue.
  * `Shortcut keys for showing stars`: Fix an issue that toggle key is read when Starmap View is not opened.
  * `Dismantle blueprint selected buildings`: Fix an issue that preview buildings are not dismantled.
  * `Remember window position and size on last exit`: Optimized implementation
  * `Auto-config logistic stations`: Add an option `Limit auto-replenish count to config values`
  * Optimized some UI codes.
* 1.2.20
  * New feature: `Dismantle blueprint selected buildings`
    * Press shortcut key in blueprint copy mode to dismantle selected buildings.
    * The default shortcut key is Ctrl+X, you can set it in system options panel.
  * New feature: `Auto-config logistic stations`
    * Auto-config buildings include: Logistics Distributor, PLS, ILS, Advanced Mining Machine.
  * `Night Sunlight`: Fix bugs that sunlight angle is not updated as expected again.
* 1.2.19
  * New feature: `Tweak building buffer`
    * Factory recipe buffer formula: take the larger value between `Assembler buffer time multiplier(in seconds) * items needed per second` and `Assembler buffer minimum multiplier * items needed per recipe`
      * `Assembler buffer time multiplier(in seconds)`: Range 2-10, default is 4 (same as game)
      * `Assembler buffer minimum multiplier`: Range 2-10, default is 2 (same as game)
    * Matrix Lab assembly mode formula: Default buffer is `Buffer count for assembling in labs`, when using Self-evolution Lab, if recipe's original production time is not greater than 9 seconds, add `Extra buffer count for Self-evolution Labs` * (`Lab speed` - 1)
      * `Buffer count for assembling in labs`: Range 2-20, default is 6 (same as game)
      * `Extra buffer count for Self-evolution Labs`: Range 1-10, default is 3 (same as game)
    * `Buffer count for researching in labs`: Range 2-20, default is 10 (same as game)
    * `Ray Receiver Graviton Lens buffer count`: Range 1-20, default is 1 (game default is 20)
  * New feature: `Shortcut keys for showing stars' name`
    * Add a shortcut key to always show all star names in starmap when holding, default is `Alt`
    * Add a shortcut key to toggle between three star name display states in starmap: `Original state`, `Show all names`, `Hide all names`, default is `Tab`, will restore to original state when closing starmap
  * `Cut conveyor belt`: Fix a bug that entity logic connection is not cut so that belt is not cut off on copying as a blueprint.
* 1.2.18
  * `Protect veins from exhaustion`: Optimized implementation, now veins will not be protected once you have upgrade `Veins Utilization` to level 390+, while the cost rate becomes absolute 0.
  * `Night Sunlight`: Fix bugs that sunlight angle is not updated as expected.
* 1.2.17
  * Fix wrong implementation of `Protect veins from exhaustion` which causes wrong display of vein stats and veins not consumed.
* 1.2.16
  * New feature: `Cut conveyor belt`
    * Press shortcut key to cut conveyor belt under cursor.
    * The default shortcut key is Alt+X, you can set it in system options panel.
  * New feature: `Profile based option`
    * Option file is stored as `Options\<ProfileName>.xml`.
  * Fix compatibility with game update 0.10.32.25779
* 1.2.15
  * `Off-grid building and stepped rotation`: Fix compatibility with DSP 0.10.32.25682. (#57)
  * `Enhanced control for logistic storage capacities`: Try to fix possible crash. (#54)
* 1.2.14
  * Fix an issue that an unexpected menu icon is shown in the top-right corner of the config panel.
  * `Stop ejectors when available nodes are all filled up`: Fix compatibility with `Dyson Sphere Program v0.10.32.25496`.
* 1.2.13
  * `Belt signals for buy out dark fog items automatically`: Fix possible crashes.
  * `Logistics Control Panel Improvement`: Auto apply filter with in-hand item now.
  * Fix an alignment issue on UI panel.
* 1.2.12
  * `Construct only structure points but frames`: Fix a bug that frames are still not constructed when this function is disabled.
  * `Drag building power poles in maximum connection range`: Fix a bug that single power pole cannot be placed at some positions.
* 1.2.11
  * Fix an issue caused by game update: tips are not shown when mouse hovering on tips button.
* 1.2.10
  * `Set enabled CPU threads`: Fix hybrid-architect check for CPUs without hyper-threading
  * `Re-initialize Dyson Spheres` and `Quick dismantle Dyson Shells`: Fix possible crashes and a display issue, while Dyson Sphere panel is actived.
* 1.2.9
  * `Protect veins from exhaustion`:
    * Fix a bug that vein protection causes crashes (#50).
    * Fix a bug that minimum oil speed in config is not working (#50).
    * Fix a bug that oil is not extracted when vein protection is enabled in infinite resource mode (#52).
* 1.2.8
  * New feature: `Fast fill in to and take out from tanks`
    * You can set multiplier for tanks' operation speed
    * This affects manually fill in to and/or take out from tanks, as well as transfer from upper to lower level.
  * Fixes to `Append mod profile name to game window title`:
    * Fix a bug that window title is not set correctly when multiple instance is launched.
    * Fix a bug that window title is not set correctly if BepInEx debug console is enabled.
  * `Real-time logistic stations info panel`: Fix a bug that item status bar appears unexpectedly.
* 1.2.7
  * Fix some minor issues
* 1.2.6
  * `Remember window position and size on last exit`
    * Fix a bug that window position is restored even the option is disabled.
    * Fix a bug that the last window position is wrongly remembere when game is closed at minimized state.
* 1.2.5
  * New feature: `Set process priority`
  * New feature: `Set enabled CPU threads`
  * `Drag building power poles in maximum connection range`: Add a new config option `Build Tesla Tower and Wireless Power Tower alternately`
* 1.2.4
  * `Sunlight at night`:
    * Fix flickering issue while mecha is sailing.
    * Can configure the light angles now.
  * `Scale up mouse cursor`: Fix known issues.
  * `Buy out techs with their prerequisites`: Fix a bug that warning popup from invalid data.
  * Does not patch `BulletTime`'s speed control now, as `BulletTime` has been updated to support configurable maximum speed.
  * Some minor fixes and tweaks.
* 1.2.3
  * `Real-time logistic stations info panel`: Fix bar length not match with item amount when item amount is more than capacity.
  * `Sunlight at night`: Fix not working.
* 1.2.2
  * `Real-time logistic stations info panel`: Fix text color mismatch sometimes
  * `Logical Frame Rate`: Set default shortcut key to `Ctrl`+`-/+` to avoid conflict with other shortcut keys
* 1.2.1
  * `Off-grid building and stepped rotation`:
    * Fix off-grid building's default shortcut key for belts
    * Fix coordinate display issue
* 1.2.0
  * New feature: `Logical Frame Rate`
    * This will change game running speed, down to 0.1x slower and up to 10x faster.
    * A pair of shortcut keys (`-` and `+`) to change the logical frame rate by -0.5x and +0.5x.
    * Note:
      * High logical frame rate is not guaranteed to be stable, especially when factories are under heavy load.
      * This will not affect some game animations.
      * When set game speed in mod `Auxilaryfunction`, this feature will be disabled.
      * When mod `BulletTime` is installed, this feature will be hidden, but patch `BulletTime`'s speed control, to make its maximum speed 10x.
  * `Off-grid building and stepped rotation`: Due to conflict with shortcut key in new game update, the shortcut key for belts is changed to `Ctrl` by default, and can be set in system options now.
  * `Real-time logistic stations info panel`: Fix a crash issue.
  * `Dyson Sphere "Auto Fast Build"`: Fix possible wrong production records.
  * Codes refactored, for better maintainability.
* 1.1.6
  * New feature: `Scale up mouse cursor`
    * Note: This will enable software cursor mode, which may cause mouse movement lag on heavy load.
  * New feature: `Real-time logistic stations info panel`
    * Note: This function will be hidden if you enabled `Show station info` in mod `Auxilaryfunction`.
  * Fix an issue that `Dyson Sphere "Auto Fast Build"` does not generate production records for solar sails.
  * Remove use of AssetBundle, move all icons into `Assembly Resources`, for better flexibility.
* 1.1.5
  * New feature: `Logistics Control Panel Improvement`
    * Auto apply filter with item under mouse cursor while opening the panel
    * Quick-set item filter while right-clicking item icons in storage list on the panel
  * New feature: `Dyson Sphere "Auto Fast Build" speed multiplier`
    * Note: this only applies to `Dyson Sphere "Auto Fast Build"` in sandbox mode
  * New feature: `Mod manager profile based save folder`
    * Save files are stored in `Save\&lt;ProfileName&gt;` folder.
    * Will use original save location if matching default profile name.
  * `Quick build and dismantle stacking labs`: works for storages and tanks now
  * `Enable game window resize`: Keep window resizable on applying game options.
  * `Remember window position and size on last exit`: Do not resize window on applying game options if resolution related config entries are not changed.
  * Auto resize panel to fit content, for better support of multilanguages and mods dependent on UX Assist config panel functions.
* 1.1.4
  * Fix `Remove some build conditions`
* 1.1.3
  * UI texts are updated following game settings now
  * Fix hover area for checkboxes in config panel
  * Fix an issue which makes `Convert Peace-Mode saves to Combat-Mode on loading` not working
* 1.1.2
  * `Belt signals for buy out dark fog items automatically`: Always add belt signals to the panel to fix missing belt icons when disabled.
* 1.1.1
  * Fix assetbundle loading issue
* 1.1.0
  * `Stop ejectors when available nodes are all filled up`: Show `No node to fill` on ejector panel when all dyson sphere nodes are filled up.
  * Append mod profile name to game window title, if using mod managers (`Thunderstore Mod Manager` or `r2modman`).
  * New features:
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
* 1.0.26
  * New features:
    * Restore upgrades of `Sorter Cargo Stacking` on panel
    * Set `Sorter Cargo Stacking` to unresearched state
  * Changes to `Protect veins from exhaustion` configuration:
    * The vein amount is protected at 1000 by default now
    * The maximum vein amount is changed to 10000, and the maximum oil speed is changed to 10.0/s
* 1.0.25
  * Fix an issue that building entites can not be clicked through when `Do not render factory entities (except belts and sorters)` is enabled
* 1.0.24
  * Changes to `Do not render factory entities (except belts and sorters)`
    * Add shortcut key in config panel to toggle this function
    * Can click on both belts and sorters now
  * New feature: `Drag building power poles in maximum connection range`
  * New feature: `Allow overflow for Logistic Stations and Advanced Mining Machines`
    * Allow overflow when trying to insert in-hand items
    * Allow `Enhanced control for logistic storage capacities` to exceed tech capacity limits
    * Remove logistic strorage capacity limit check on loading game
* 1.0.23
  * New features:
    * `Do not render factory entities (except belts and sorters)`
      * This also makes players click though factory entities but belts
    * `Open Dark Fog Communicator` anywhere
  * Belts can be built off-grid now, by pressing the shortcut key for `Switch Splitter model`(`Tab` by default)
  * Add a suboption `Auto boost` to `Auto-cruise`
  * `Auto-cruise` does warp when core energy at least 80% now
* 1.0.22
  * Fix a crash issue caused by `Quick build and dismantle stacking labs`
* 1.0.21
  * Fix a bug that stepped rotation is not working in `Off-grid building and stepped rotation`, which is caused by latest game update
  * Fix some issues in `Auto nativation` and `Auto-cruise`, now only boosts when core energy at least 10% and warps when core energy at least 50%
* 1.0.20
  * Fix an infinite-loop issue when `Quick build and dismantle stacking labs` and `No condition build` are both enabled
  * Fix a crash caused by `Re-initialize planet` in combat mode
* 1.0.19
  * New functions:
    * `Quick build and dismantle stacking labs`
    * `Protect veins from exhaustion`
      * By default, the vein amount is protected at 100, and oil speed is protected at 1.0/s, you can set them yourself in config file.
      * When reach the protection value, veins/oils steeps will not be mined/extracted any longer.
      * Close this function to resume mining and pumping, usually when you have enough level on `Veins Utilization`
  * Remove default shortcut key for `Auto-cruise`, to avoid misoperation. Please set it in the system options window manually if needed.
* 1.0.18
  * Fix crash while coursing to a dark-fog hive.
  * Auto-cruise does not bypass dark-fog hives if they are targeted.
* 1.0.17
  * New function: `Auto navigation on sailings`, which is inspired by [CruiseAssist](https://dsp.thunderstore.io/package/tanu/CruiseAssist/) and its extension [AutoPilot](https://dsp.thunderstore.io/package/tanu/AutoPilot/)
    * It keeps Icarus on course to the target planet
    * It will try to bypass any obstacles(planets, stars or dark-fog hives) on the way
    * Furthermore, there is also a shortcut key which can be set in the system options window, which is used to toggle `Auto-cruise` that enables flying to targeted planets fully automatically.
      * Auto-cruise will start when you target a planet on star map
      * It will use warper to fly to the target planet if the planet is too far away, the range can be configured.
      * It will speed down when approaching the target planet, to avoid overshooting
  * Fix a crash caused by `Stop ejectors when available nodes are all filled up` in latest game update
  * `Off-grid building and stepped rotation`: Hide Z coordinate from display if it is zero
* 1.0.16
  * Add CommonAPI to package manifest dependencies(missing in last version)
  * New function: `Hide tips for soil piles changes`
* 1.0.15
  * Move shortcut key settings to system options window, which depends on [CommonAPI](https://dsp.thunderstore.io/package/CommonAPI/CommonAPI)
  * Enable `Hide UI` function(`F11` by default) while on Star Map view
  * New function: `Treat stack items as single in monitor components`
* 1.0.14
  * Fix crash in `Re-initialize planet` again
  * `Off-grid building and stepped rotation`: Add Z coordinate to display, and adjust the precision to 4 decimal after point
* 1.0.13
  * `Off-grid building and stepped rotation`: show building coordinates(relative to grids) on building preview and building info panel now
  * Increase maximum count of Metadata Instantiations to 20000 (from 2000)
  * Increase capacity of player order queue to 128 (from 16)
  * Fix issue caused by game updates
    * `Remove some build conditions`: fixed issue that some conditions are not eliminated
    * `Re-initialize planet`: fixed crash issue
* 1.0.12
  * Fix a bug that ejectors aimed at even-numbered orbits stop working when `Stop ejectors when available nodes are all filled up` is enabled.
* 1.0.11
  * Remove `Better auto-save mechanism` due to conflicts with DSPModSave and some other mods.
* 1.0.10
  * Fix a button display bug
  * Fix a possible crash while `Enhanced control for logistic storage capacities` is enabled
* 1.0.9
  * New function: `Better auto-save mechanism`
    * Auto saves are stored in 'Save\AutoSaves' folder, filenames are combined with cluster address and date-time
    * Note: this will sort gamesaves by modified time on save/load window, so you don't have to use [DSP_Save_Game_Sorter] anymore
* 1.0.8
  * New function: `Enhanced control for logistic storage capacities`
* 1.0.7
  * Fix a crash issue on choosing language other than English and Chinese
  * Games saved in Peace-Mode after Dark-Fog update can also be loaded as Combat-Mode now.
* 1.0.6
  * Convert old saves to Combat-Mode on loading
* 1.0.5
  * Support game version 0.10.28.20759
  * Sort blueprint structures before saving, to reduce generated blueprint data size a little.
* 1.0.4
  * Add new function: `Off-grid building and stepped rotation`
  * Fix an issue that window position not restored and can not be resized when function is enabled but game is started with different mod profiles.
* 1.0.3
  * Add new function: `Quick build Orbital Collectors`.
  * Add confirmation popup for `Re-intialize planet`, `Quick dismantle all buildings`, `Re-initialize Dyson Spheres` and `Quick dismantle Dyson Shells`.
  * Fix error on `Remove build count and range limit` when building a large amount of belts.
  * Fix an issue that window position not saved correctly when quit game without using in-game menu.
* 1.0.2
  * Redesign config tabs, for clearer layout.
  * Add 2 new options:
    * Enable game window resize.
    * Remember window position and size on last exit.
* 1.0.1
  * Fix config button text and tips while returning to title menu.
  * Fix that error occurs while returning to title menu, with `Stop ejectors when available nodes are all filled up` enabled.
  * Add a patch to fix the bug that warning popup on `Veins Utilization` upgraded to level 8000+.
* 1.0.0
  * Initial release
  * Functions moved from [MechaDronesTweaks](https://dsp.thunderstore.io/package/soarqin/MechaDronesTweaks/) and [CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/)

</details>

<details>
<summary>中文读我</summary>

## 更新日志

* 1.4.4
  * `记住上次退出时的窗口位置和大小`：修复对游戏补丁0.10.33的兼容性
  * `物流站实时信息面板`：尝试修复一个显示问题
  * 对[CommonAPI](https://thunderstore.io/c/dyson-sphere-program/p/CommonAPI/CommonAPI/)临时打补丁，修复快捷键设置未能保存到配置文件的已知问题（其[PR](https://github.com/limoka/CommonAPI/pull/14)仍未合并）
* 1.4.3
  * `交替建造电力感应塔和无线输电塔`:
    * 修复了在最新游戏补丁中的错误实现
    * 由于新的矩形建造机制，现在无法使用电力感应塔作为起始电塔
  * `宇宙视图矿脉数量显示`：兼容添加矿脉类型的mod
  * `物流站实时信息面板`：尝试修复可能的崩溃问题
* 1.4.2
  * 修复了一个崩溃问题
* 1.4.1
  * 修复了与最新游戏补丁的兼容性问题
* 1.4.0
  * 支持游戏版本 0.10.33，移除了一些功能：
    * 移除`放大鼠标指针`：Unity 2022 读取系统设置里的鼠标指针大小，软件渲染不再影响其大小
    * 移除`设置使用的CPU线程`：因为官方已支持此功能
  * `拆除蓝图选中的建筑`：修复了从物流站中掉落的物品丢失增产点数的问题
  * `保存蓝图前对建筑进行排序`：改进了排序规则
  * `星图过滤器`：现在星系编号（按星系生成顺序）显示为前缀
  * 由于缺乏维护，整合内置了[Planet Vein Untilization](https://thunderstore.io/c/dyson-sphere-program/p/testpushpleaseignore/Planet_Vein_Utilization/)，并修复了一些小问题
  * `移除部分不影响游戏逻辑的建造条件`：修复了错误的逻辑
  * `物流站实时信息面板`：修复了一些显示问题
  * 修复了配置窗口标签按钮的背景图像问题
  * 现在使用C#的`Debug`配置构建，以避免`Release`配置中的优化导致的一些问题
* 1.3.7
  * `重新初始化行星`: 修复可能导致崩溃的问题
  * `自动配置物流站`: 增加`设置默认远程逻辑为仓储`
* 1.3.6
  * `拆除蓝图选中的建筑`：
    * 修复了拆除虚影建筑时崩溃的问题
    * 重命名为`蓝图复制模式快捷键`，同时添加了选择所有建筑的快捷键（默认为Ctrl+A）
  * `允许物流站和大型采矿机物品溢出`：现在也适用于物流控制面板
  * `调整建筑缓冲区`：为2个新建筑添加缓冲区调整
    * `弹射太阳帆缓冲区数量`：范围5-400（步进值为5），默认值为20（与游戏相同）
    * `发射井火箭缓冲区数量`：范围1-20，默认值为20（与游戏相同）
* 1.3.5
  * `基于mod管理器配置档案的存档文件夹`：修复游戏启动时崩溃的问题
* 1.3.4
  * `自动配置物流站`：修复了高级采矿机和物流配送器的一些设置未被正确应用的问题
* 1.3.3
  * `星图过滤器`：当过滤器UI显示时隐藏顶部重叠窗口
  * `自动配置物流站`：现在可以为战场分析基站设置最大充能功率
  * `重新初始化行星`：修复崩溃问题
  * `航行时自动导航`：
    * 添加快速启用/禁用`自动巡航`的按钮
    * 如果所需科技未研究则不自动使用翘曲器
  * `拆除蓝图选中建筑`：修复传送带连接建筑意外被拆除的问题
  * `基于mod管理器配置档案的存档文件夹`：修复与[SaveTheWindows](https://thunderstore.io/c/dyson-sphere-program/p/starfi5h/SaveTheWindows/)的兼容性
  * `物流存储容量控制改进`和`允许物流站和大型采矿机物品溢出`：
    * 如果物流存储容量已经超过升级后的最大容量，则在升级`物流运输机容量`时不会按比例提升
    * 粘贴蓝图时物流存储容量将降低至科技容量限制
  * `物流站实时信息面板`：支持修改物流站槽位数的mod
* 1.3.2
  * 新功能：`在和平模式下隐藏战斗相关科技`
  * 新按钮：`使用元数据解锁所有科技`
  * 在星图过滤器中添加复选框以合并结果
  * 修复了一些星图矿脉/行星过滤条件
  * 修复了在和平模式下`初始化本行星`导致的崩溃问题
  * 修复了与`NebulaMultiplayerMod`的兼容性问题
* 1.3.1
  * 修复了在星图上点击新增的下拉框时部分UI元素被隐藏的问题
  * 修复了未启用`显示星系名称快捷键`时星系名称过滤器不生效的问题
  * `拆除蓝图选中的建筑`：修复了物流站中的物品未被丢出的问题
  * 调整了星系名称过滤器中的行星类型列表
* 1.3.0
  * 在星图上添加新功能：
    * 添加星系名过滤器，现在可以按矿物或行星类型过滤显示的星系名
    * 添加了一个下拉框用以切换显示所有星系的距离和/或行星数量
  * `切割传送带`：修复了输入问题
  * `启用显示所有星系名称的快捷键`：修复了在未打开星图视图时读取切换键的问题
  * `拆除蓝图选中的建筑`：修复了预建造建筑未被拆除的问题
  * `记住上次退出时的窗口位置和大小`：优化实现
  * `自动配置物流站`: 增加了一个选项`限制自动补充数量为配置的值`
  * 优化了一些UI代码
* 1.2.20
  * 新功能：`拆除蓝图选中的建筑`
    * 在蓝图复制模式下按快捷键拆除选中的建筑
    * 默认快捷键是Ctrl+X，可以在系统选项面板中设置
  * 新功能：`自动配置物流站`
    * 自动配置的建筑包括：物流配送器、行星物流站、星际物流站、高级采矿机
  * `夜间日光灯`：再次修复了光照角度未正确更新的问题
* 1.2.19
  * 新功能：`调整建筑输入缓冲`
    * 工厂配方计算公式，在`工厂配方缓冲时间倍率秒数x每秒需要的原料数量`和`工厂配方缓冲最小倍率x每生产一次配方需要的原料数量`中取更大的那个值
      * `工厂配方缓冲时间倍率(秒)`：范围2-10，默认为4(同游戏)
      * `工厂配方缓冲最小倍率`：范围2-10，默认为2(同游戏)
    * 研究站矩阵合成模式计算公式，默认缓存`研究站矩阵合成模式缓存数量`个，当使用自演化研究站时，如果配方的原始生产时间不大于9秒，则增加`自演化研究站矩阵额外缓冲数量`*(`研究站速度倍率`-1)
      * `研究站矩阵合成模式缓存数量`：范围2-20，默认为6(同游戏)
      * `自演化研究站矩阵额外缓冲数量`：范围1-10，默认为3(同游戏)
    * `研究站科研模式缓存数量`：范围2-20，默认为10(同游戏)
    * `射线接收器透镜缓冲数量`：范围1-20，默认为1(游戏默认为20)
  * 新功能：`启用显示所有星系名称的快捷键`
    * 新增一个快捷键，按住后始终在星图显示所有星系名称，默认为`Alt`
    * 新增一个快捷键，在星图视图切换三种星系名称显示状态：`原始显示状态`，`显示所有名称`，`隐藏所有名称`，默认为`Tab`，关闭星图时会恢复到原始状态
  * `切割传送带`：修复了实体逻辑连接未切断导致复制为蓝图时传送带未被切断的问题。
* 1.2.18
  * `保护矿脉不会耗尽`：优化实现，当`矿物利用`升级到390级以上时消耗速度变为0时，矿脉将不再被保护。
  * `夜间日光灯`：修复了光照角度未正确更新的问题。
* 1.2.17
  * 修复了`保护矿脉不会耗尽`导致矿脉状态显示错误和矿脉未被消耗的错误实现
* 1.2.16
  * 新功能：`切割传送带`
    * 按快捷键切割光标位置的传送带
    * 默认快捷键是Alt+X，可以在系统选项面板中设置
  * 新功能：`基于mod管理器配置档案名`
    * 选项文件存储在`Options\<ProfileName>.xml`中
  * 修复了与游戏更新0.10.32.25779的兼容性
* 1.2.15
  * `脱离网格建造和小角度旋转`：修复了与0.10.32.25682的兼容性 (#57)
  * `物流塔存储数量限制控制改进`：修复了可能导致崩溃的问题 (#54)
* 1.2.14
  * 修正设置窗口右上角多出一个菜单图标的问题
  * `当可用节点全部造完时停止弹射`：修复了与`戴森球计划 v0.10.32.25496`的兼容性
* 1.2.13
  * `用于自动购买黑雾物品的传送带信号`：修复了可能导致崩溃的问题
  * `物流控制面板改进`：现在也自动将拿着的物品设为筛选条件
  * 修复了UI面板上的对齐问题
* 1.2.12
  * `只建造节点不建造框架`：修复了关闭此功能时框架不进行建造的问题
  * `拖动建造电线杆时自动使用最大连接距离间隔`：修复了某些位置无法放置单个电线杆的问题
* 1.2.11
  * 修复了游戏更新导致的提示按钮鼠标悬停时不显示提示文字的问题
* 1.2.10
  * `设置使用的CPU线程`：修复了对没有超线程的CPU的大小核检查
  * `初始化戴森球`和`快速拆除戴森壳`：修复了在戴森球面板激活时可能导致崩溃的问题，以及显示错误的问题。
* 1.2.9
  * `保护矿脉不会耗尽`：
    * 修复了矿脉保护导致崩溃的问题(#50)
    * 修复了配置中的最小采油速度不起作用的问题(#50)
    * 修复了无限资源模式下油井保护导致无法采油的问题(#52)
* 1.2.8
  * 新功能：`储液罐快速注入和抽取液体`
    * 你可以设置储液罐操作速度的倍率
    * 影响手动注入和抽取，以及从储液罐上层传输到下层的速度
  * 在游戏窗口标题中追加mod配置档案名的修复：
    * 修复了多实例启动时窗口标题未正确设置的问题
    * 修复了启用BepInEx调试控制台时窗口标题未正确设置的问题
  * `物流运输站实时信息面板`：修复了一个物品状态条意外显示的问题
* 1.2.7
  * 修复了一些小问题
* 1.2.6
  * `记住上次退出时的窗口位置和大小`
    * 修复了即使选项被禁用也恢复窗口位置的问题
    * 修复了窗口最小化时关闭游戏导致窗口位置被错误记录的问题
* 1.2.5
  * 新功能：`设置进程优先级`
  * 新功能：`设置使用的CPU线程`
  * `拖动建造电线杆时自动使用最大连接距离间隔`：添加一个新的设置项`交替建造电力感应塔和无线输电塔`
* 1.2.4
  * `夜间日光灯`：
    * 修复了航行时闪烁的问题
    * 现在可以配置入射光线角度了
  * `放大鼠标指针`：修复已知问题
  * `买断科技也同时买断所有前置科技`：修复了数据错误警告弹窗的问题
  * 不再对`BulletTime`的速度控制打补丁，因为`BulletTime`已更新支持可配置最大速度
  * 一些小修复和调整
* 1.2.3
  * `物流运输站实时信息面板`：修复了物品数量超过容量限制时条长度不匹配的问题
  * `夜间日光灯`：修复了不起作用的问题
* 1.2.2
  * `物流运输站实时信息面板`：修复了文本颜色不匹配的问题
  * `逻辑帧倍率`：将默认快捷键设置为`Ctrl`+`-/+`，以避免与其他快捷键冲突
* 1.2.1
  * `脱离网格建造和小角度旋转`：
    * 修复了传送带脱离网格建造的默认快捷键
    * 修复了坐标显示问题
* 1.2.0
  * 新功能：`逻辑帧倍率`
    * 这将改变游戏运行速度，最慢0.1倍，最快10倍
    * 设置了一对快捷键(`-`和`+`)，可以-/+0.5倍改变逻辑帧倍率
    * 注意：
      * 高逻辑帧倍率不能保证稳定性，特别是在工厂负载较重时
      * 这不会影响一些游戏动画
      * 当在`Auxilaryfunction`mod中设置游戏速度时，此功能将被禁用
      * 当安装了`BulletTime`mod时，此功能将被隐藏，但会对`BulletTime`的速度控制打补丁，使其最大速度变为10倍
  * `脱离网格建造和小角度旋转`：由于与新游戏更新中的快捷键冲突，传送带脱离网格建造的快捷键默认更改为`Ctrl`，并且现在可以在系统选项中设置
  * `物流运输站实时信息面板`：修复了一个崩溃问题
  * `戴森球自动快速建造`：修复了可能出现的错误生产记录
  * 代码重构，以获得更好的可维护性
* 1.1.6
  * 新功能：`放大鼠标指针`
    * 注意：这将启用软件指针模式，可能会在CPU负载较重时导致鼠标移动延迟
  * 新功能：`物流运输站实时信息面板`
    * 注意：如果你启用了`Auxilaryfunction`中的`展示物流站信息`，此功能将被隐藏
  * 修复了`戴森球自动快速建造`未生成太阳帆生产记录的问题
  * 移除了AssetBundle的使用，将所有图标移入`Assembly资源`，以获得更好的灵活性
* 1.1.5
  * 新功能：`物流控制面板改进`
    * 打开面板时自动将鼠标指向物品设为筛选条件
    * 在控制面板物流塔列表中右键点击物品图标快速设置为筛选条件
  * 新功能：`戴森球自动快速建造速度倍率`
    * 注意：这仅适用于沙盒模式下的`戴森球自动快速建造`功能
  * 新功能：`基于mod管理器配置档案名的存档文件夹`
    * 存档文件会存储在`Save\&lt;ProfileName&gt;`文件夹中
    * 如果匹配默认配置档案名则使用原始存档位置
  * `快速建造和拆除堆叠研究站`：现在也支持储物仓和储液罐
  * `允许调整游戏窗口大小`：在应用游戏选项时保持窗口可调整大小
  * `记住上次退出时的窗口位置和大小`：如果分辨率相关的配置项未改变，则在应用游戏选项时不调整窗口大小
  * 自动调整面板大小适应内容，以更好地支持多语言和依赖于UX助手配置面板功能的mod
* 1.1.4
  * 修复了`移除部分不影响游戏逻辑的建造条件`
* 1.1.3
  * 界面文本现在完全跟随游戏语言设置改变
  * 修复了配置面板中勾选框的鼠标悬停区域
  * 修复了`加载和平模式存档时将其转换为战斗模式`不起作用的问题
* 1.1.2
  * `用于自动购买黑雾物品的传送带信号`: 总是将传送带信号添加到面板，以修复禁用时传送带图标丢失的问题。
* 1.1.1
  * 修复了资源包加载问题
* 1.1.0
  * `可用节点全部造完时停止弹射`: 当所有戴森球节点都造完时，在弹射器面板上显示`没有可建造节点`
  * 如果使用mod管理器(`Thunderstore Mod Manager`或`r2modman`)启动游戏，在游戏窗口标题中追加mod配置档案名
  * 新功能：
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
* 1.0.26
  * 新功能：
    * 在升级面板上恢复`分拣器货物堆叠`的升级
    * 将`分拣器货物堆叠`设为未研究状态
  * `保护矿脉不会耗尽`配置的改动：
    * 现在默认矿脉数量保护在1000
    * 最大矿脉数量改为10000，最大采油速度改为10.0/s
* 1.0.25
  * 修复了`不渲染工厂建筑实体(除了传送带和分拣器)`启用时无法点穿工厂实体的问题
* 1.0.24
  * `不渲染工厂建筑实体(除了传送带和分拣器)`的改动
    * 在配置面板中添加了一个快捷键来切换此功能
    * 现在也可以点击到分拣器了
  * 新功能：`拖动建造电线杆时自动使用最大连接距离间隔`
  * 新功能：`允许物流塔和大型采矿机物品溢出`
    * 当尝试塞入手中物品时允许溢出
    * 允许`物流塔存储数量限制控制改进`超过科技容量限制
    * 在加载游戏时移除物流塔容量限制检查
* 1.0.23
  * 新功能：
    * `不渲染工厂建筑实体(除了传送带和分拣器)`
      * 这使得玩家可以点穿工厂实体直接点到传送带
    * 在任意位置`打开黑雾通讯器`
  * 传送带现在可以脱离网格建造了，通过按住`切换分流器样式`的快捷键(默认`Tab`)
  * 为`自动巡航`添加一个子选项`自动加速`
  * `自动巡航`现在在核心能量至少80%时才加速
* 1.0.22
  * 修复了`快速建造和拆除堆叠研究站`导致的崩溃问题
* 1.0.21
  * 修复了`脱离网格建造和小角度旋转`在最新游戏更新后无法小角度旋转的问题
  * 修复了`航行时自动导航`和`自动巡航`的一些问题。现在只有能量至少10%时才加速，能量至少50%时才启动曲速
* 1.0.20
  * 修复了`快速建造和拆除堆叠研究站`和`无条件建造`同时启用时可能导致的逻辑死循环问题
  * 修复了在战斗模式下`初始化本行星`导致的崩溃问题
* 1.0.19
  * 新功能：
    * `快速建造和拆除堆叠研究站`
    * `保护矿脉不会耗尽`
      * 默认矿脉数量保护于剩余100，采油速保护于速度1.0/s，你可以在配置文件中自行设置。
      * 当达到保护值时，矿脉和油井将不再被开采。
      * 关闭此功能以恢复开采，一般是当你在`矿物利用`上有足够的等级时。
  * 移除了`自动巡航`的默认快捷键，以避免误操作。如有需要请手动在系统选项窗口中设置。
* 1.0.18
  * 修复了以黑雾巢穴为目标时导致崩溃的问题
  * 当黑雾巢穴是目标时，自动导航不会绕过它
* 1.0.17
  * 新功能：`航行时自动导航`，想法来自[CruiseAssist](https://dsp.thunderstore.io/package/tanu/CruiseAssist/)及其扩展[AutoPilot](https://dsp.thunderstore.io/package/tanu/AutoPilot/)
    * 它会保持伊卡洛斯飞向目标星球
    * 它会尝试绕过途中的任何障碍物(行星、恒星或黑雾巢穴)
    * 此外，还有一个快捷键可以在系统选项窗口中设置，用于切换`自动巡航`，实现完全自动化的飞行至目标星球。
      * 当你选择目标星球后，自动巡航就会开始
      * 如果目标星球距离过远会自动使用曲速(超过5AU)，你可以在面板上更改这个值。
      * 它会在接近目标星球时减速，以避免发生越过目标的情况
  * 修复了最新游戏更新后`当可用节点全部造完时停止弹射`引起崩溃问题
  * `脱离网格建造和小角度旋转`：如果Z坐标为零则从显示中隐藏
* 1.0.16
  * 添加了对CommonAPI的包依赖(上个版本忘记加了)
  * 新功能：`隐藏沙土数量变动的提示`
* 1.0.15
  * 将快捷键设置移动到系统选项窗口，依赖于[CommonAPI](https://dsp.thunderstore.io/package/CommonAPI/CommonAPI)
  * 在星图视图中启用`隐藏UI`功能(默认按键为`F11`)
  * 新功能：`在流速计中将堆叠物品视为单个物品`
* 1.0.14
  * 再次尝试修复`初始化本行星`导致的崩溃问题
  * `脱离网格建造和小角度旋转`：现在显示建筑Z坐标，并将精度调整为小数点后4位
* 1.0.13
  * `脱离网格建造和小角度旋转`：现在在建造预览和建筑信息面板上显示建筑坐标(相对于网格)
  * 将元数据提取的最大数量增加到20000(原来为2000)
  * 将玩家指令队列的容量增加到128(原来为16)
  * 修复了游戏更新导致的问题
    * `移除部分不影响游戏逻辑的建造条件`：修复了一些条件未被移除的问题
    * `初始化本行星`：修复了崩溃问题
* 1.0.12
  * 修复了当`当可用节点全部造完时停止弹射`选项启用时，瞄准偶数轨道的弹射器停止工作的bug
* 1.0.11
  * 移除`更好的自动保存机制`，因为与DSPModSave和其他一些mod冲突
* 1.0.10
  * 修复了一个按钮显示错误
  * 修复了`物流塔存储数量限制控制改进`启用时可能导致的崩溃问题
* 1.0.9
  * 新功能：`更好的自动保存机制`
    * 自动存档会以星区地址和日期时间组合为文件名存储在'Save\AutoSaves'文件夹中
    * 注意：此功能会在保存/读取菜单按最后修改时间对存档进行排序，因此你不再需要[DSP_Save_Game_Sorter]了
* 1.0.8
  * 新功能：`物流塔存储数量限制控制改进`
* 1.0.7
  * 修复了选择英文和中文以外的语言时的崩溃问题
  * 黑雾更新后使用和平模式保存的存档现在也可以转换为战斗模式了
* 1.0.6
  * 在加载旧存档时将其转换为战斗模式
* 1.0.5
  * 支持游戏版本0.10.28.20759
  * 保存蓝图前对建筑进行排序，以减少生成的蓝图数据大小
* 1.0.4
  * 添加了新功能：`脱离网格建造和小角度旋转`
  * 修复了当功能启用但游戏使用不同的mod配置文件启动时窗口位置无法正确恢复和不可拖动改变大小的问题
* 1.0.3
  * 添加了新功能：`快速建造轨道采集器`
  * 为`初始化行星`，`快速拆除所有建筑`，`初始化戴森球`和`快速拆除戴森壳`添加了确认弹窗
  * 修复了`移除建造数量和范围限制`在建造大量传送带时可能导致的错误
  * 修复了在不使用游戏内菜单退出游戏时窗口位置无法正确保存的问题
* 1.0.2
  * 重新设计了配置面板，使布局更清晰
  * 添加了两个新选项：
    * 可调整游戏窗口大小(可最大化和拖动边框)
    * 记住上次退出时的窗口位置和大小
* 1.0.1
  * 修复了返回标题界面后设置按钮文本和提示信息不正确的问题
  * 修复了`当可用节点全部造完时停止弹射`选项启用时返回标题界面可能导致崩溃的问题
  * 添加了一个补丁，修复了`矿物利用`升级到8000级以上时弹出警告的bug
* 1.0.0
  * 初始版本
  * 从[MechaDronesTweaks](https://dsp.thunderstore.io/package/soarqin/MechaDronesTweaks/)和[CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/)移动了部分功能过来

</details>
