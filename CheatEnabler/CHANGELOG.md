# Changelog / 更新日志

<details>
<summary>Read in English</summary>

This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Changed

- CheatEnabler now requires UXAssist 1.6.3 or later to load.

### Fixed

- `Mecha and Drones/Fleets invincible`: Fix mecha HP and energy shield protection failing when enabled after taking damage, and restore protection for space fleets.
- `Caps Lock` only toggles tech downgrade mode while the tech tree is open.

## [2.4.8] - 2026-10-31

### Changed

- `Belt signal item generation`:
  * Improve raw-material, intermediate, by-product and proliferator statistics using current recipes, with production and consumption counted separately. Incomplete recipe data only pauses the affected upstream statistics; item generation continues.
  * Fix signal belts built or destroyed on other planets, such as by Battlefield Analysis Base drones or Dark Fog attacks, affecting belts on the current planet. Destroyed signal belts no longer cause replacement belts to keep generating items.

### Fixed

- `Finish build immediately`: Fix possible errors when starting a new game. Construction begins after the opening cutscene ends or is skipped.

## [2.4.7] - 2026-10-18

### Added

- Extend `Unlock Tech with Key-Modifiers` with a downgrade mode.
  * Press `Caps Lock` to toggle downgrade mode. Modifier clicks lower tech levels and revert their bonuses and dependent tech levels; `Alt` returns the tech to locked.
  * The current mode is shown by a popup and warning banner. A help button in the tech tree explains the key combinations.

### Fixed

- `Dev Shortcuts`: Work immediately when enabled after loading a save, and process each key press once per frame at accelerated game speeds.

## [2.4.6] - 2026-09-29

### Fixed

- `Architect mode`: Restore unlimited use of buildings, Logistics Bots, Logistics Drones, and Logistics Vessels, and increase the default displayed count from 100 to 999.
- `Wind Turbines do global power coverage`: Fix a possible startup crash when enabled.

## [2.4.5] - 2026-09-24

### Added

- New button `Generate illegal dyson shells for all layers without nodes and shells`, which is used to extend dyson layers.
  * You must enable `IllegalDysonShellFunctionsEnabled` of `DysonSphere` section in config to see it.

### Changed

- Support Dyson Sphere Program 0.10.35.29057.
- Update `Terraform anyway` for the separate foundation and Restore Terrain workflows. Preserve soil gains and prevent negative soil balances when resources are insufficient.

### Fixed

- `Finish build immediately`: Fix a crash when pasting a blueprint containing the first Planetary Shield Generator on a planet.

## [2.4.4] - 2026-03-22

### Fixed

- `Finish build immediately`: Fix a possible crash.

## [2.4.3] - 2026-03-16

### Added

- New option for `Belt signal item generation`: `Use proliferators for raws/intermediates and finished products`
  * Following items use extra products: `Titanium Alloy`, `Prism`, `Frame Material`, `Proliferator Mk.II`, `Proliferator Mk.III`, `Magnetic Coil`, `Electric Motor`, `Electromagnetic Turbine`, `Super-magnetic Ring`, `Circuit Board`, `Thruster`, `Reinforced Thruster`, `Plasma Exciter`, `Particle Broadband`, `Graviton Lens`, `Quantum Chip`, `Annihilation Constraint Sphere`, `Deuteron Fuel Rod`, `Space Warper`, `Dyson Sphere Component`, `Small Carrier Rocket`, `Electromagnetic Matrix`, `Structure Matrix`, `Information Matrix`, `Gravity Matrix`, `Universe Matrix`
  * Following items does not use proliferators: `Casimir Crystal`, `Energy Matrix`
  * Other items use speed up production.

### Fixed

- `Duplicate shells from that with highest production`: Fix crash on using with frameless shells.
- `Keep max production shells and remove others`: Fix wrong behaviour when left count > 1.
- `Quick absorb`: Fix ant issue that it is not working while `Skip absorption period` is enabled.
- `Finish build immediately`: Fixed an issue where prebuilt buildings were not constructed immediately when the building quantity became sufficient.
- Fix some other minor bugs found by LLM AI.

## [2.4.2] - 2026-01-28

### Fixed

- Fix critical bugs:
  * Dyson Power is not updated actively when `Skip bullet period` or `Skip absorption period` is enabled and no rocket is in fire.
  * Dyson Power is not updated actively on clicking `Complete Dyson Sphere Shells instantly` or `Remove all frames on Dyson Sphere`.

## [2.4.1] - 2026-01-23

### Added

- New feature: `Remove all frames on Dyson Sphere`
  * Can save rockets with very little power generation impact.
  * Will keep nodes and shells and does not affect solar sails' absorption.

### Changed

- `Terraform without enough soil piles`: Working for blueprint paste and burying Dark Fog Core Driller now.

### Fixed

- `Belt signal item generation`: Fix mod compatibility by resolving recursive recipes.

## [2.4.0] - 2025-09-29

### Changed

- Support game version 0.10.33
- `Generate illegal dyson shell`: This function is open to all users now, enabling certain config entry is not needed any more.

## [2.3.32] - 2025-06-07

### Fixed

- `Complete Dyson Sphere Shells instantly`: Fix a crash.

## [2.3.31] - 2025-06-04

### Added

- New feature: `Unlock Dyson Sphere max orbit radius`
- Add 3 buttons for creating illegal Dyson Sphere Shells, you must enable `IllegalDysonShellFunctionsEnabled` of `DysonSphere` section in config to see them.
  * `Generate an illegal dyson shell`
  * `Keep max production shells and remove others`
  * `Duplicate shells from that with highest production`

### Fixed

- `Remove metadata consumption record in current game`: Fix implementation

## [2.3.30] - 2025-04-26

### Changed

- Increase performance for `Finish build immediately` greatly on pasting large blueprints.

### Fixed

- Fix a warning issue while `No condition build` or `No collision` is enabled.

## [2.3.29] - 2025-03-31

### Fixed

- Fix compatibility with game update 0.10.32.25779

## [2.3.28] - 2025-03-25

### Added

- New feature: `Instant hand-craft`.

### Fixed

- Fix some panels' display while `Infinite Natural Resources` is enabled.

## [2.3.27] - 2025-01-24

### Fixed

- `Skip bullet period` & `Eject anyway`: Fix compatibility with `Dyson Sphere Program v0.10.32.25496`.

## [2.3.26] - 2024-09-18

### Changed

- Refactor codes to adapt to UXAssist 1.2.0
  * You should update UXAssist to 1.2.0 or later before using this version.

### Fixed

- `Complete Dyson Sphere Shells instantly`: Fix possible wrong production records.

## [2.3.25] - 2024-09-04

### Added

- New feature: `Enable warp without space warpers`
- New feature: `Wind Turbines do global power coverage`

### Fixed

- Fix an issue that `Complete Dyson Sphere Shells instantly` does not generate production records for solar sails.

## [2.3.24] - 2024-08-30

### Fixed

- `Complete Dyson Sphere Shells instantly`: Fix a bug that may cause negative power in some cases

## [2.3.23] - 2024-08-30

### Added

- New feature: `Complete Dyson Sphere Shells instantly`

### Fixed

- Fix a crash when config panel is opened before game is fully loaded

## [2.3.22] - 2024-08-23

### Fixed

- Fix `Pump Anywhere`

## [2.3.21] - 2024-08-23

### Changed

- `Retrieve/Place items from/to remote planets on logistics control panel`: Items are put back to player's inventory when a slot is removed from the logistics station on remote planet.

### Fixed

- `Dev Shortcuts`: Camera Pose related shortcurts are working now.

## [2.3.20] - 2024-08-23

### Added

- New feature: `Retrieve/Place items from/to remote planets on logistics control panel`

## [2.3.19] - 2024-08-21

### Added

- New features:
  * `Remove all metadata consumption records`
  * `Remove metadata consumption record in current game`
  * `Clear metadata flag which bans achievements`

## [2.3.18] - 2024-08-17

### Added

- New features:
  * `Teleport to outer space`, this will teleport you to the outer space which is 50 LYs far from the farthest star.
  * `Teleport to selected astronomical`

### Fixed

- Fix logic of `Unlock techs with key-modifiers`.
- `No condition build` does not hide rotation info of belts now.

## [2.3.17] - 2024-08-16

### Changed

- Make compatible with game version 0.10.30.23292

## [2.3.16] - 2024-05-24

### Added

- Add 2 options to `Belt signal item generation`:
  * `Count generations as production in statistics`
  * `Count removals as consumption in statistics`
- New feature: `Increase maximum power usage in Logistic Stations and Advanced Mining Machines`
  * Logistic Stations: Increased max charging power to 3GW(ILS) and 600MW(PLS) (10x of original)
  * Advanced Mining Machines: Increased max mining speed to 1000%

## [2.3.15] - 2024-05-05

### Added

- New features:
  * `Instant teleport (like that in Sandbox mode)`
  * `Mecha and Drones/Fleets invicible`
  * `Buildings invicible`

## [2.3.14] - 2024-03-28

### Removed

- Remove default shortcut key for `No condition build` and `No collision`, to avoid misoperation. You can still set them in system settings window manually if needed.

### Fixed

- Fix translation issue.

## [2.3.13] - 2024-03-23

### Fixed

- Fix a bug that shortcuts are not working and have display issue on settings window.

## [2.3.12] - 2024-03-23

### Added

- Add a shortcut to toggle `No collision`, you can modify the shortcut on system settings window.
- Add realtime tips when toggling `No condition build` and `No collision` with shortcuts.

## [2.3.11] - 2024-03-10

### Added

- Add a shortcut to toggle `No condition build`, you can modify the shortcut on system settings window. This depends on [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist) 1.0.15 or later.

## [2.3.10] - 2024-02-18

### Fixed

- Fix following functions not working in new game updates:
  * `Pump Anywhere`
  * `Terraform without enough soil piles`

## [2.3.9] - 2024-01-04

### Changed

- Support game version 0.10.28.21219

## [2.3.8] - 2023-12-18

### Fixed

- Fix a crash on starting new games while `Finish build immediately` is enabled.
- Fix UI button width.

## [2.3.7] - 2023-12-15

### Changed

- Support game version 0.10.28.20759

### Fixed

- Fix belt signal that items' generation speed is not fit to number set sometimes.

## [2.3.6] - 2023-10-31

### Added

- Add a warning message when `Build without condition` is enabled.

### Changed

- Support for UXAssist's new function within `Finish build immediately`.

### Fixed

- Fix an issue in `Finish build immediately` that some buildings are not finished immediately.

## [2.3.5] - 2023-10-23

### Fixed

- Fix another crash in `Skip bullet period`.

## [2.3.4] - 2023-10-22

### Changed

- Use new tab layout of UXAssist 1.0.2

### Fixed

- Minor bug fixes

## [2.3.3] - 2023-10-20

### Fixed

- Fix a crash in `Skip bullet period`.
- Unlock techs with Alt unlocks VeinUtil to 10000 instead of 7200 now, as bug fixed in UXAssist.

## [2.3.2] - 2023-10-16

### Changed

- Optimize `Quick absorb`, consumes less CPU time and take turns firing to nodes.
- `Fast Mining` ensures full output of oil extractors now.

### Removed

- Birth star options moved to [UniverseGenTweaks](https://dsp.thunderstore.io/package/soarqin/UniverseGenTweaks/)

### Fixed

- Fix issue that `Belt signal generator` not working after switched off then on again.
- Fix absorption issue by `Quick absorb` and `Skip bullet period` enabled at the same time.
- Crash fix for some options

## [2.3.1] - 2023-10-10

### Changed

- Add UXAssist to dependencies in manifest.

## [2.3.0] - 2023-10-10

### Added

- Infinite bots/drones/vessels in `Architect mode` now.

### Changed

- Depends on [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist) now, so that config panel is unified with UXAssist.

### Removed

- Move some functions to an individual mod: [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist)
- Remove `LCtrl+A` from Dev Shortcuts, to avoid misoperation.

## [2.2.7] - 2023-10-07

### Added

- New function: `Construct only nodes but frames`

### Changed

- Opening config panel does not close inventory panel now

### Removed

- Remove `Input direction conflict` check while using `Remove some build conditions`

### Fixed

- Fix a bug that prevents `Belt signal alt format` from switching number formats for current belt signals

## [2.2.6] - 2023-09-28

### Added

- New function: `Stop ejectors when available nodes are all filled up`

### Fixed

- Fix a bug that absorb solar sails on unfinised nodes

## [2.2.5] - 2023-09-27

### Changed

- Skip all intermediate states and absorb solar sails instantly while enable `Quick absorb`, `Skip bullet period` and `Skip absorption period` at the same time.

### Fixed

- Fix a problem that `Quick absorb` does not absorb all solar sails instantly when most nodes are full.
- Fix crash while using with some mods

## [2.2.4] - 2023-09-25

### Added

- New function: `Enable player actions in globe view`

### Fixed

- Fix UI bug

## [2.2.3] - 2023-09-25

### Added

- New function: `Remove some build conditions`

### Fixed

- Fix compatibility with some mods

## [2.2.2] - 2023-09-24

### Added

- New function: `Assign gamesave to currrnet account`
- New subfunction: `Belt signal alt format`

### Fixed

- Fix a crash on using `Initialize this Planet`
- Fix belt build in `Finish build immediately`

## [2.2.1] - 2023-09-19

### Changed

- Check condition for miners even when `Build without condition` is enabled.

### Fixed

- Fix a patch issue that may cause `Build without condition` not working.

## [2.2.0] - 2023-09-16

### Added

- Add some power related functions
- Add a subfunction to belt signal item generation, which simulates production process of raws and intermediates on statistics

### Changed

- Split some functions from Architect mode

## [2.1.0] - 2023-09-14

### Added

- Belt signal item generation

### Fixed

- Fix window display priority which may cause tips to be covered by main window

## [2.0.0] - 2023-09-13

### Added

- UI implementation
- Add a lot of functions

### Changed

- Refactorying codes

## [1.0.0] - 2022-09-06

### Added

- Initial release

</details>

<details>
<summary>简体中文</summary>

本更新日志采用 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 格式。

## [Unreleased]

### 变更

- CheatEnabler 现在需要 UXAssist 1.6.3 或更高版本才能加载。

### 修复

- `机甲和战斗无人机无敌`：修复受伤后启用时机甲耐久度和能量护盾保护可能失效的问题，并恢复太空编队的无敌保护。
- 仅在科技树打开时，`Caps Lock` 才能切换科技降级模式。

## [2.4.8] - 2026-10-31

### 变更

- `传送带信号物品生成`：
  * 按当前配方完善原料、中间产物、副产物和增产剂的统计，分别记录生产和消耗。配方数据不完整时，仅暂停受影响物品的上游统计，物品仍会正常生成。
  * 修复其他星球上的信号传送带在建造或被摧毁时，错误影响当前星球上传送带的问题，例如战场分析基站无人机建造或黑雾摧毁传送带。被摧毁的信号传送带也不会再让之后替换它的传送带继续生成物品。

### 修复

- `建造秒完成`：修复开始新游戏时可能报错的问题。建造会在开场动画播放结束或跳过后开始。

## [2.4.7] - 2026-10-18

### 新增

- `使用组合键点击解锁科技` 新增降级模式。
  * 按 `Caps Lock` 切换降级模式。按住组合键点击可降低科技等级，同时回退升级效果及后继科技等级；`Alt` 将科技回退到未解锁。
  * 弹窗和警告横幅显示当前模式，科技树中的帮助按钮提供组合键说明。

### 修复

- `开发模式快捷键`：修复载入存档后启用时不生效的问题；加速游戏时每次按键仍只执行一次。

## [2.4.6] - 2026-09-29

### 修复

- `建筑师模式`：修复建筑、配送运输机、物流运输机和星际物流运输船无法无限使用的问题，默认显示数量由 100 提高至 999。
- `风力涡轮机供电覆盖全球`：修复启用后可能导致启动崩溃的问题。

## [2.4.5] - 2026-09-24

### 新增

- 新按钮 `为所有没有节点和壳的层级生成仙术戴森壳`，用于扩展新戴森壳层面的时候。
  * 你必须在设置文件里开启`DysonSphere`分类的`IllegalDysonShellFunctionsEnabled`才能看到它。

### 变更

- 支持戴森球计划 0.10.35.29057。
- 更新`强行改造地形`以适配独立地基和恢复地形的工作流程。在资源不足时保留沙土收益并防止负沙土平衡。

### 修复

- `立即完成建造`：修复在行星上粘贴包含首座行星护盾发生器的蓝图时可能崩溃的问题。

## [2.4.4] - 2026-03-22

### 修复

- `立即完成建造`: 修复一个可能的崩溃问题。

## [2.4.3] - 2026-03-16

### 新增

- `传送带信号物品生成`新增选项：`原料/中间品和成品使用增产剂`
  * 以下物品会使用额外产出：`钛合金`、`棱镜`、`框架材料`、`增产剂 Mk.II`、`增产剂 Mk.III`、`磁线圈`、`电动机`、`电磁涡轮`、`超级磁场环`、`电路板`、`推进器`、`加力推进器`、`电浆激发器`、`粒子宽带`、`引力透镜`、`量子芯片`、`湮灭约束球`、`氘燃料棒`、`空间翘曲器`、`戴森球组件`、`小型运载火箭`、`电磁矩阵`、`结构矩阵`、`信息矩阵`、`引力矩阵`、`宇宙矩阵`
  * 以下物品不使用增产剂：`卡西米尔晶体`、`能量矩阵`
  * 其他物品使用加速生产

### 修复

- `从发电量最高的壳复制戴森壳`：修复在无框壳体时可能崩溃的问题。
- `保留发电量最高的戴森壳并移除其他戴森壳`：修复当剩余壳体数量大于1时的异常行为。
- `快速吸收`：修复开启`跳过吸收阶段`时不生效的问题。
- `立即完成建造`：修复建筑数量补足时，之前预建造状态的建筑未能立即完成的问题。
- 修复一些由 LLM AI 发现的其他小问题。

## [2.4.2] - 2026-01-28

### 修复

- 修复了严重的系列BUG:
  * 在启用`跳过子弹阶段`或`跳过吸收阶段`并且没有火箭发射时，戴森球的发电量未实时更新
  * 点击`立即完成戴森壳建造`或`移除戴森球上的所有框架`时，戴森球的发电量未实时更新

## [2.4.1] - 2026-01-23

### 新增

- 新增功能：`移除戴森球上的所有框架`
  * 可以节省火箭，且几乎不影响发电效率
  * 会保留节点和壳面，不影响太阳帆的吸收

### 变更

- `沙土不够时依然可以整改地形`：现在支持蓝图粘贴和掩埋黑雾核心钻机

### 修复

- `传送带信号物品生成`：修复对递归配方的支持，兼容一些mod

## [2.4.0] - 2025-09-29

### 变更

- 支持游戏版本 0.10.33
- `生成仙术戴森壳`：此功能现在对所有用户开放，不再需要启用特定的配置项。

## [2.3.32] - 2025-06-07

### 修复

- `立即完成戴森壳建造`：修复了一个崩溃问题

## [2.3.31] - 2025-06-04

### 新增

- 新功能：`解锁戴森球最大轨道半径`
- 增加了3个用于制作仙术戴森壳的按钮，你必须在设置文件里开启`DysonSphere`分类的`IllegalDysonShellFunctionsEnabled`才能看到它们
  * `生成单层仙术戴森壳`
  * `保留发电量最高的戴森壳并移除其他戴森壳`
  * `从发电量最高的壳复制戴森壳`

### 修复

- `移除当前存档的元数据消耗记录`：修复实现

## [2.3.30] - 2025-04-26

### 变更

- 粘贴大规模蓝图时大幅提升`立即完成建造`的性能表现

### 修复

- 修复了启用`无条件建造`或`无碰撞`时的警告问题

## [2.3.29] - 2025-03-31

### 修复

- 修复了与游戏更新0.10.32.25779的兼容性

## [2.3.28] - 2025-03-25

### 新增

- 新功能：`快速手动制造`

### 修复

- 修复了启用`自然资源采集不消耗`时部分面板的显示问题

## [2.3.27] - 2025-01-24

### 修复

- `跳过子弹阶段`和`全球弹射`：修复了与`戴森球计划 v0.10.32.25496`的兼容性

## [2.3.26] - 2024-09-18

### 变更

- 重构代码以适应UXAssist 1.2.0
  * 在使用此版本之前，您应先更新UXAssist到1.2.0或更高版本。

### 修复

- `立即完成戴森壳建造`：修复了可能导致错误的生产记录的问题

## [2.3.25] - 2024-09-04

### 新增

- 新功能：`无需空间翘曲器即可曲速飞行`
- 新功能：`风力涡轮机供电覆盖全球`

### 修复

- 修复了`立即完成戴森壳建造`未生成太阳帆生产记录的问题

## [2.3.24] - 2024-08-30

### 修复

- `立即完成戴森壳建造`：修复了在某些情况下可能导致发电为负的问题

## [2.3.23] - 2024-08-30

### 新增

- 新功能：`立即完成戴森壳建造`

### 修复

- 修复了在游戏完全加载前打开配置面板可能导致的崩溃问题

## [2.3.22] - 2024-08-23

### 修复

- 修复了`平地抽水`

## [2.3.21] - 2024-08-23

### 变更

- `在物流总控面板上可以从非本地行星取放物品`：当从非本地星球的物流站移除槽位时，物品会放回玩家的背包

### 修复

- `开发模式快捷键`：摄像机位(Pose)相关的快捷键现在生效了

## [2.3.20] - 2024-08-23

### 新增

- 新功能：`在物流总控面板上可以从非本地行星取放物品`

## [2.3.19] - 2024-08-21

### 新增

- 新功能：
  * `移除所有元数据消耗记录`
  * `移除当前存档的元数据消耗记录`
  * `解除当前存档因使用元数据导致的成就限制`

## [2.3.18] - 2024-08-17

### 新增

- 新功能：
  * `传送到外太空`，这会将你传送到距离最远星球50光年的外太空
  * `传送到选中天体`

### 修复

- 修复了`组合键解锁科技`的逻辑
- `无条件建造`现在不会隐藏传送带的旋转信息了

## [2.3.17] - 2024-08-16

### 变更

- 适配游戏版本0.10.30.23292

## [2.3.16] - 2024-05-24

### 新增

- 为`传送带信号物品生成`添加了两个选项：
  * `统计信息里将生成计算为产物`
  * `统计信息里将移除计算为消耗`
- 新功能：`提升物流塔和大型采矿机的最大功耗`
  * 物流塔：将最大充电功率提高到3GW(星际物流塔)和600MW(行星物流塔)（原来的10倍）
  * 大型采矿机：将最大采矿速度提高到1000%

## [2.3.15] - 2024-05-05

### 新增

- 新功能：
  * `快速传送(和沙盒模式一样)`
  * `机甲和战斗无人机无敌`
  * `建筑无敌`

## [2.3.14] - 2024-03-28

### 移除

- 移除了`无条件建造`和`无碰撞`的默认快捷键，以避免误操作。如有需要请手动在系统选项窗口中设置。

### 修复

- 修复了翻译问题。

## [2.3.13] - 2024-03-23

### 修复

- 修复了快捷键无效和设置窗口上的按键显示问题

## [2.3.12] - 2024-03-23

### 新增

- 添加了一个快捷键来切换`无碰撞`，你可以在系统设置面板中修改快捷键。
- 在使用快捷键切换`无条件建造`和`无碰撞`时添加了实时提示信息。

## [2.3.11] - 2024-03-10

### 新增

- 添加了一个快捷键来切换`无条件建造`，你可以在系统设置面板中修改快捷键。这依赖于[UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist) 1.0.15或更高版本

## [2.3.10] - 2024-02-18

### 修复

- 修复了以下功能在新游戏版本中不生效的问题：
  * `平地抽水`
  * `沙土不够时依然可以整改地形`

## [2.3.9] - 2024-01-04

### 变更

- 支持游戏版本0.10.28.21219

## [2.3.8] - 2023-12-18

### 修复

- 修复了启用`建造秒完成`时开新游戏可能导致崩溃的问题
- 修复了UI按钮宽度

## [2.3.7] - 2023-12-15

### 变更

- 支持游戏版本0.10.28.20759

### 修复

- 修复了传送带信号有时候物品生成速度和设置不匹配的问题

## [2.3.6] - 2023-10-31

### 新增

- 在启用`无条件建造`时添加警告信息

### 变更

- 在`建造秒完成`中支持UXAssist的新功能

### 修复

- 修复了`建造秒完成`可能导致部分建筑无法立即完成的问题

## [2.3.5] - 2023-10-23

### 修复

- 修复了`跳过子弹阶段`可能导致崩溃的问题

## [2.3.4] - 2023-10-22

### 变更

- 使用UXAssist 1.0.2的新页签布局

### 修复

- 修复了一些小bug

## [2.3.3] - 2023-10-20

### 修复

- 修复了`跳过子弹阶段`可能导致崩溃的问题
- 使用Alt解锁科技时，现在`矿物利用`的科技解锁到10000级而不是7200级，因为UXAssist已修复对应bug

## [2.3.2] - 2023-10-16

### 变更

- 优化了`快速吸收`，现在消耗更少的CPU，并且会轮流打向各节点
- `高速采集`现在可以保证油井的最大产出

### 移除

- 母星系的选项移动到了[UniverseGenTweaks](https://dsp.thunderstore.io/package/soarqin/UniverseGenTweaks/)

### 修复

- 修复了`传送带信号物品生成`在选项关闭后再次启用时不生效的问题
- 修复了`快速吸收`和`跳过子弹阶段`同时启用时可能导致吸收计算错误的问题
- 修复了一些选项可能导致崩溃的问题

## [2.3.1] - 2023-10-10

### 变更

- 在manifest中添加UXAssist到依赖

## [2.3.0] - 2023-10-10

### 新增

- 现在`建筑师模式`中配送机/物流机/物流船也无限了

### 变更

- 现在依赖[UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist)，因此配置面板与UXAssist合并

### 移除

- 将部分功能移动到单独的mod：[UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist)
- 从开发模式快捷键中移除`LCtrl+A`，以避免误操作

## [2.2.7] - 2023-10-07

### 新增

- 新功能：`只建造节点不建造框架`

### 变更

- 打开设置面板时不再关闭背包面板

### 移除

- 在`移除部分不影响游戏逻辑的建造条件`启用时移除`输入方向冲突`的检查条件

### 修复

- 修复导致`传送带信号替换格式`不切换传送带信号数字格式的问题

## [2.2.6] - 2023-09-28

### 新增

- 新功能：`可用节点全部造完时停止弹射`

### 修复

- 修复了在未完成的节点上吸收太阳帆的问题

## [2.2.5] - 2023-09-27

### 变更

- 在同时启用`快速吸收`、`跳过子弹阶段`和`跳过吸收阶段`时，所有弹射的太阳帆会跳过所有中间环节立即吸收

### 修复

- 修复了`快速吸收`在大部分节点已满时无法立即吸收所有太阳帆的问题
- 修复了与一些mod的兼容性问题

## [2.2.4] - 2023-09-25

### 新增

- 新功能：`在行星视图中允许玩家操作`

### 修复

- 修复了UI显示问题

## [2.2.3] - 2023-09-25

### 新增

- 新功能：`移除部分不影响游戏逻辑的建造条件`

### 修复

- 修复了与一些mod的兼容性问题

## [2.2.2] - 2023-09-24

### 新增

- 新功能：`将游戏存档绑定给当前账号`
- 新子功能：`传送带信号替换格式`

### 修复

- 修复了`初始化本行星`可能导致崩溃的问题
- 修复了`建造秒完成`中传送带建造的问题

## [2.2.1] - 2023-09-19

### 变更

- 即使在启用`无条件建造`时依然检查矿机的建造条件

### 修复

- 修复一个可能导致`无条件建造`不生效的问题

## [2.2.0] - 2023-09-16

### 新增

- 添加了一些发电相关功能
- 为传送带信号物品生成添加了一个子功能，在统计面板模拟了原材料和中间产物的生产过程

### 变更

- 从建筑师模式中分离了一些功能

## [2.1.0] - 2023-09-14

### 新增

- 传送带信号物品生成

### 修复

- 修复窗口显示优先级可能导致提示信息被主窗口遮挡的问题

## [2.0.0] - 2023-09-13

### 新增

- UI实现
- 添加了很多功能

### 变更

- 重构代码

## [1.0.0] - 2022-09-06

### 新增

- 初始版本

</details>

[Unreleased]: https://github.com/soarqin/DSP_Mods/compare/3580e96...master
[2.4.8]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.4.8/
[2.4.7]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.4.7/
[2.4.6]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.4.6/
[2.4.5]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.4.5/
[2.4.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.4.4/
[2.4.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.4.3/
[2.4.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.4.2/
[2.4.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.4.1/
[2.4.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.4.0/
[2.3.32]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.32/
[2.3.31]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.31/
[2.3.30]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.30/
[2.3.29]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.29/
[2.3.28]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.28/
[2.3.27]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.27/
[2.3.26]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.26/
[2.3.25]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.25/
[2.3.24]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.24/
[2.3.23]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.23/
[2.3.22]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.22/
[2.3.21]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.21/
[2.3.20]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.20/
[2.3.19]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.19/
[2.3.18]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.18/
[2.3.17]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.17/
[2.3.16]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.16/
[2.3.15]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.15/
[2.3.14]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.14/
[2.3.13]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.13/
[2.3.12]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.12/
[2.3.11]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.11/
[2.3.10]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.10/
[2.3.9]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.9/
[2.3.8]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.8/
[2.3.7]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.7/
[2.3.6]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.6/
[2.3.5]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.5/
[2.3.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.4/
[2.3.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.3/
[2.3.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.2/
[2.3.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.1/
[2.3.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.3.0/
[2.2.7]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.2.7/
[2.2.6]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.2.6/
[2.2.5]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.2.5/
[2.2.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.2.4/
[2.2.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.2.3/
[2.2.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.2.2/
[2.2.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.2.1/
[2.2.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.2.0/
[2.1.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.1.0/
[2.0.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/2.0.0/
[1.0.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/CheatEnabler/v/1.0.0/
