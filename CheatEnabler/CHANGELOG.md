<details>
<summary>Read me in English</summary>

## Changlog

* 2.3.30
  + Fix a warning issue while `No condition build` or `No collision` is enabled.
  + Optimized `Finish build immediately` for better performance.
* 2.3.29
  + Fix compatibility with game update 0.10.32.25779
* 2.3.28
  + New feature: `Instant hand-craft`.
  + Fix some panels' display while `Infinite Natural Resources` is enabled.
* 2.3.27
  + `Skip bullet period` & `Eject anyway`: Fix compatibility with `Dyson Sphere Program v0.10.32.25496`.
* 2.3.26
  + Refactor codes to adapt to UXAssist 1.2.0
    - You should update UXAssist to 1.2.0 or later before using this version.
  + `Complete Dyson Sphere Shells instantly`: Fix possible wrong production records.
* 2.3.25
  + New feature: `Enable warp without space warpers`
  + New feature: `Wind Turbines do global power coverage`
  + Fix an issue that `Complete Dyson Sphere Shells instantly` does not generate production records for solar sails.
* 2.3.24
  + `Complete Dyson Sphere Shells instantly`: Fix a bug that may cause negative power in some cases
* 2.3.23
  + New feature: `Complete Dyson Sphere Shells instantly`
  + Fix a crash when config panel is opened before game is fully loaded
* 2.3.22
  + Fix `Pump Anywhere`
* 2.3.21
  + `Retrieve/Place items from/to remote planets on logistics control panel`: Items are put back to player's inventory when a slot is removed from the logistics station on remote planet.
  + `Dev Shortcuts`: Camera Pose related shortcurts are working now.
* 2.3.20
  + New feature: `Retrieve/Place items from/to remote planets on logistics control panel`
* 2.3.19
  + New features:
    + `Remove all metadata consumption records`
    + `Remove metadata consumption record in current game`
    + `Clear metadata flag which bans achievements`
* 2.3.18
  + New features:
    + `Teleport to outer space`, this will teleport you to the outer space which is 50 LYs far from the farthest star.
    + `Teleport to selected astronomical`
  + Fix logic of `Unlock techs with key-modifiers`.
  + `No condition build` does not hide rotation info of belts now.
* 2.3.17
  + Make compatible with game version 0.10.30.23292
* 2.3.16
  + Add 2 options to `Belt signal item generation`:
    - `Count generations as production in statistics`
    - `Count removals as consumption in statistics`
  + New feature: `Increase maximum power usage in Logistic Stations and Advanced Mining Machines`
    - Logistic Stations: Increased max charging power to 3GW(ILS) and 600MW(PLS) (10x of original)
    - Advanced Mining Machines: Increased max mining speed to 1000%
* 2.3.15
  + New features:
    - `Instant teleport (like that in Sandbox mode)`
    - `Mecha and Drones/Fleets invicible`
    - `Buildings invicible`
* 2.3.14
  + Remove default shortcut key for `No condition build` and `No collision`, to avoid misoperation. You can still set them in system settings window manually if needed.
  + Fix translation issue.
* 2.3.13
  + Fix a bug that shortcuts are not working and have display issue on settings window.
* 2.3.12
  + Add a shortcut to toggle `No collision`, you can modify the shortcut on system settings window.
  + Add realtime tips when toggling `No condition build` and `No collision` with shortcuts.
* 2.3.11
  + Add a shortcut to toggle `No condition build`, you can modify the shortcut on system settings window. This depends on [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist) 1.0.15 or later.
* 2.3.10
  + Fix following functions not working in new game updates:
    - `Pump Anywhere`
    - `Terraform without enough soil piles`
* 2.3.9
  + Support game version 0.10.28.21219
* 2.3.8
  + Fix a crash on starting new games while `Finish build immediately` is enabled.
  + Fix UI button width.
* 2.3.7
  + Support game version 0.10.28.20759
  + Fix belt signal that items' generation speed is not fit to number set sometimes.
* 2.3.6
  + Support for UXAssist's new function within `Finish build immediately`.
  + Add a warning message when `Build without condition` is enabled.
  + Fix an issue in `Finish build immediately` that some buildings are not finished immediately.
* 2.3.5
  + Fix another crash in `Skip bullet period`.
* 2.3.4
  + Use new tab layout of UXAssist 1.0.2
  + Minor bug fixes
* 2.3.3
  + Fix a crash in `Skip bullet period`.
  + Unlock techs with Alt unlocks VeinUtil to 10000 instead of 7200 now, as bug fixed in UXAssist.
* 2.3.2
  + Birth star options moved to [UniverseGenTweaks](https://dsp.thunderstore.io/package/soarqin/UniverseGenTweaks/)
  + Optimize `Quick absorb`, consumes less CPU time and take turns firing to nodes.
  + `Fast Mining` ensures full output of oil extractors now.
  + Fix issue that `Belt signal generator` not working after switched off then on again.
  + Fix absorption issue by `Quick absorb` and `Skip bullet period` enabled at the same time.
  + Crash fix for some options
* 2.3.1
  + Add UXAssist to dependencies in manifest.
* 2.3.0
  + Move some functions to an individual mod: [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist)
  + Depends on [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist) now, so that config panel is unified with UXAssist.
  + Remove `LCtrl+A` from Dev Shortcuts, to avoid misoperation.
  + Infinite bots/drones/vessels in `Architect mode` now.
* 2.2.7
  + New function: `Construct only nodes but frames`
  + Opening config panel does not close inventory panel now
  + Remove `Input direction conflict` check while using `Remove some build conditions`
  + Fix a bug that prevents `Belt signal alt format` from switching number formats for current belt signals
* 2.2.6
  + New function: `Stop ejectors when available nodes are all filled up`
  + Fix a bug that absorb solar sails on unfinised nodes
* 2.2.5
  + Skip all intermediate states and absorb solar sails instantly while enable `Quick absorb`, `Skip bullet period` and `Skip absorption period` at the same time.
  + Fix a problem that `Quick absorb` does not absorb all solar sails instantly when most nodes are full.
  + Fix crash while using with some mods
* 2.2.4
  + New function: `Enable player actions in globe view`
  + Fix UI bug
* 2.2.3
  + New function: `Remove some build conditions`
  + Fix compatibility with some mods
* 2.2.2
  + New function: `Assign gamesave to currrnet account`
  + New subfunction: `Belt signal alt format`
  + Fix a crash on using `Initialize this Planet`
  + Fix belt build in `Finish build immediately`
* 2.2.1
  + Check condition for miners even when `Build without condition` is enabled.
  + Fix a patch issue that may cause `Build without condition` not working.
* 2.2.0
  + Add some power related functions
  + Add a subfunction to belt signal item generation, which simulates production process of raws and intermediates on statistics
  + Split some functions from Architect mode
* 2.1.0
  + Belt signal item generation
  + Fix window display priority which may cause tips to be covered by main window
* 2.0.0
  + Refactorying codes
  + UI implementation
  + Add a lot of functions
* 1.0.0
  + Initial release

</details>

<details>
<summary>中文读我</summary>

## 更新日志

* 2.3.30
  + 修复了启用`无条件建造`或`无碰撞`时的警告问题
  + 优化了`立即完成建造`的性能表现
* 2.3.29
  + 修复了与游戏更新0.10.32.25779的兼容性
* 2.3.28
  + 新功能：`快速手动制造`
  + 修复了启用`自然资源采集不消耗`时部分面板的显示问题
* 2.3.27
  + `跳过子弹阶段`和`全球弹射`：修复了与`戴森球计划 v0.10.32.25496`的兼容性
* 2.3.26
  + 重构代码以适应UXAssist 1.2.0
    - 在使用此版本之前，您应先更新UXAssist到1.2.0或更高版本。
  + `立即完成戴森壳建造`：修复了可能导致错误的生产记录的问题
* 2.3.25
  + 新功能：`无需空间翘曲器即可曲速飞行`
  + 新功能：`风力涡轮机供电覆盖全球`
  + 修复了`立即完成戴森壳建造`未生成太阳帆生产记录的问题
* 2.3.24
  + `立即完成戴森壳建造`：修复了在某些情况下可能导致发电为负的问题
* 2.3.23
  + 新功能：`立即完成戴森壳建造`
  + 修复了在游戏完全加载前打开配置面板可能导致的崩溃问题
* 2.3.22
  + 修复了`平地抽水`
* 2.3.21
  + `在物流总控面板上可以从非本地行星取放物品`：当从非本地星球的物流站移除槽位时，物品会放回玩家的背包
  + `开发模式快捷键`：摄像机位(Pose)相关的快捷键现在生效了
* 2.3.20
  + 新功能：`在物流总控面板上可以从非本地行星取放物品`
* 2.3.19
  + 新功能：
    + `移除所有元数据消耗记录`
    + `移除当前存档的元数据消耗记录`
    + `解除当前存档因使用元数据导致的成就限制`
* 2.3.18
  + 新功能：
    + `传送到外太空`，这会将你传送到距离最远星球50光年的外太空
    + `传送到选中天体`
  + 修复了`组合键解锁科技`的逻辑
  + `无条件建造`现在不会隐藏传送带的旋转信息了
* 2.3.17
  + 适配游戏版本0.10.30.23292
* 2.3.16
  + 为`传送带信号物品生成`添加了两个选项：
    - `统计信息里将生成计算为产物`
    - `统计信息里将移除计算为消耗`
  + 新功能：`提升物流塔和大型采矿机的最大功耗`
    - 物流塔：将最大充电功率提高到3GW(星际物流塔)和600MW(行星物流塔)（原来的10倍）
    - 大型采矿机：将最大采矿速度提高到1000%
* 2.3.15
  + 新功能：
    - `快速传送(和沙盒模式一样)`
    - `机甲和战斗无人机无敌`
    - `建筑无敌`
* 2.3.14
  + 移除了`无条件建造`和`无碰撞`的默认快捷键，以避免误操作。如有需要请手动在系统选项窗口中设置。
  + 修复了翻译问题。
* 2.3.13
  + 修复了快捷键无效和设置窗口上的按键显示问题
* 2.3.12
  + 添加了一个快捷键来切换`无碰撞`，你可以在系统设置面板中修改快捷键。
  + 在使用快捷键切换`无条件建造`和`无碰撞`时添加了实时提示信息。
* 2.3.11
  + 添加了一个快捷键来切换`无条件建造`，你可以在系统设置面板中修改快捷键。这依赖于[UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist) 1.0.15或更高版本
* 2.3.10
  + 修复了以下功能在新游戏版本中不生效的问题：
    - `平地抽水`
    - `沙土不够时依然可以整改地形`
* 2.3.9
  + 支持游戏版本0.10.28.21219
* 2.3.8
  + 修复了启用`建造秒完成`时开新游戏可能导致崩溃的问题
  + 修复了UI按钮宽度
* 2.3.7
  + 支持游戏版本0.10.28.20759
  + 修复了传送带信号有时候物品生成速度和设置不匹配的问题
* 2.3.6
  + 在`建造秒完成`中支持UXAssist的新功能
  + 在启用`无条件建造`时添加警告信息
  + 修复了`建造秒完成`可能导致部分建筑无法立即完成的问题
* 2.3.5
  + 修复了`跳过子弹阶段`可能导致崩溃的问题
* 2.3.4
  + 使用UXAssist 1.0.2的新页签布局
  + 修复了一些小bug
* 2.3.3
  + 修复了`跳过子弹阶段`可能导致崩溃的问题
  + 使用Alt解锁科技时，现在`矿物利用`的科技解锁到10000级而不是7200级，因为UXAssist已修复对应bug
* 2.3.2
  + 母星系的选项移动到了[UniverseGenTweaks](https://dsp.thunderstore.io/package/soarqin/UniverseGenTweaks/)
  + 优化了`快速吸收`，现在消耗更少的CPU，并且会轮流打向各节点
  + `高速采集`现在可以保证油井的最大产出
  + 修复了`传送带信号物品生成`在选项关闭后再次启用时不生效的问题
  + 修复了`快速吸收`和`跳过子弹阶段`同时启用时可能导致吸收计算错误的问题
  + 修复了一些选项可能导致崩溃的问题
* 2.3.1
  + 在manifest中添加UXAssist到依赖
* 2.3.0
  + 将部分功能移动到单独的mod：[UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist)
  + 现在依赖[UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist)，因此配置面板与UXAssist合并
  + 从开发模式快捷键中移除`LCtrl+A`，以避免误操作
  + 现在`建筑师模式`中配送机/物流机/物流船也无限了
* 2.2.7
  + 新功能：`只建造节点不建造框架`
  + 打开设置面板时不再关闭背包面板
  + 在`移除部分不影响游戏逻辑的建造条件`启用时移除`输入方向冲突`的检查条件
  + 修复导致`传送带信号替换格式`不切换传送带信号数字格式的问题
* 2.2.6
  + 新功能：`可用节点全部造完时停止弹射`
  + 修复了在未完成的节点上吸收太阳帆的问题
* 2.2.5
  + 在同时启用`快速吸收`、`跳过子弹阶段`和`跳过吸收阶段`时，所有弹射的太阳帆会跳过所有中间环节立即吸收
  + 修复了`快速吸收`在大部分节点已满时无法立即吸收所有太阳帆的问题
  + 修复了与一些mod的兼容性问题
* 2.2.4
  + 新功能：`在行星视图中允许玩家操作`
  + 修复了UI显示问题
* 2.2.3
  + 新功能：`移除部分不影响游戏逻辑的建造条件`
  + 修复了与一些mod的兼容性问题
* 2.2.2
  + 新功能：`将游戏存档绑定给当前账号`
  + 新子功能：`传送带信号替换格式`
  + 修复了`初始化本行星`可能导致崩溃的问题
  + 修复了`建造秒完成`中传送带建造的问题
* 2.2.1
  + 即使在启用`无条件建造`时依然检查矿机的建造条件
  + 修复一个可能导致`无条件建造`不生效的问题
* 2.2.0
  + 添加了一些发电相关功能
  + 为传送带信号物品生成添加了一个子功能，在统计面板模拟了原材料和中间产物的生产过程
  + 从建筑师模式中分离了一些功能
* 2.1.0
  + 传送带信号物品生成
  + 修复窗口显示优先级可能导致提示信息被主窗口遮挡的问题
* 2.0.0
  + 重构代码
  + UI实现
  + 添加了很多功能
* 1.0.0
  + 初始版本

</details>
