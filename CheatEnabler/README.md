# CheatEnabler

<details>
<summary>Read me in English</summary>

***Add various cheat functions while disabling abnormal determinants***

## Usage

* Config panel is unified with UXAssist.
* There are also buttons on title screen and planet minimap area to call up the config panel.
* Features:
  * General:
    * Enable Dev Shortcuts (check config panel for usage)
    * Disable Abnormal Checks
    * Quickly upgrade/downgrade tech with key-modifiers (click the tech tree while holding Ctrl/Alt/Shift)
      * Hold `Shift` / `Ctrl` / `Ctrl+Shift` to change the level by 1 / 10 / 100; `Alt` upgrades to the maximum or returns the tech to locked in downgrade mode.
      * `Caps Lock` toggles session downgrade mode, shown by a realtime tip and a critical warning banner. Leaving the game or disabling this feature resets the mode.
      * Upgrading unlocks required prerequisites. Downgrading reverts the corresponding bonuses and removes dependent upgrade levels and research progress when their prerequisites become unavailable.
      * A downgrade is refused if capacity reductions would hide inventory or delivery contents, pending deliveries or equipped/deployed fleets, or if too few construction drones are idle. Empty affected slots, recall and empty affected fleets, or wait for drones to return before retrying.
      * The `?` button in the tech tree shows the full key reference.
    * Remove all metadata consumption records
    * Remove metadata consumption record in current game
    * Clear metadata flag which bans achievements
    * Assign gamesave to current account
  * Factory:
    * Finish build immediately
      * Completes pending buildings as soon as their required building items are available
    * Architect mode
      * Unlimited use of buildings, Logistics Bots, Logistics Drones and Interstellar Logistics Vessels
      * Construction and inventory availability checks show at least 999 of each supported item.
    * Build without condition
    * No collision
    * Belt signal item generation
      * Count generations as production in statistics
      * Count removals as consumption in statistics
      * Count all raws and intermediates in statistics
      * Count proliferators used for raw materials, intermediates and finished products
        * Input calculations use recipe-specific extra-product or speed-up bonuses. Casimir Crystals and Energy Matrices are excluded; the option's help tooltip lists the extra-product recipes.
      * Belt signal alt format
    * Increase maximum power usage in Logistic Stations and Advanced Mining Machines
      * Logistic Stations: Increased max charging power to 3GW(ILS) and 600MW(PLS) (10x of original)
      * Advanced Mining Machines: Increased max mining speed to 1000%
    * Retrieve/Place items from/to remote planets on logistics control panel
    * Remove spacing limits for Wind Turbines and Geothermal Power Stations
    * Wind Turbines do global power coverage
    * Boost power generations for kinds of power generators
  * Planet:
    * Instant hand-craft
    * Infinite Natural Resources
    * Fast Mining
    * Pump Anywhere
    * Terraform without enough soil piles
      * Supports foundation placement, Restore Terrain, blueprint foundations and burying Dark Fog Core Drillers
      * Soil gains still take effect, and insufficient soil does not leave a negative balance.
    * Reform the entire planet or revert all terrain reforms from the config panel
    * Instant teleport (like that in Sandbox mode)
  * Dyson Sphere:
    * Skip bullet period
      * Optionally fire all buffered Solar Sails at once
    * Skip absorption period
    * Quick absorb
    * Eject anyway
    * Overclock Ejectors and Silos (10x firing speed)
    * Unlock Dyson Sphere max orbit radius
    * Complete Dyson Sphere Shells instantly
    * Remove all frames on Dyson Sphere
      * Keeps nodes and shells, preserving Solar Sail absorption while reducing rocket use for frames
    * Generate illegal Dyson Sphere Shells
      * The default quick-generation button creates new layers with the selected shell count.
      * Enable `IllegalDysonShellFunctions` in the `DysonSphere` config section to access the advanced controls:
        * Generate an illegal Dyson shell
        * Generate illegal shells for all existing layers without nodes and shells
        * Keep the highest-production shells and remove the others
        * Duplicate the highest-production shells with a configurable shell count
  * Mecha/Combat:
    * Mecha and Drones/Fleets invicible
    * Buildings invicible
    * Enable warp without space warpers
    * Teleport to outer space
    * Teleport to selected astronomical

## Notes

* Please upgrade `BepInEx` 5.4.21 or later if using with [BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/) to avoid possible conflicts.
  * You can download [BepInEx here](https://github.com/bepinex/bepinex/releases/latest)(choose x64 edition).
  * If using with r2modman, you can upgrade `BepInEx` by clicking `Settings` -> `Browse profile folder`, then extract downloaded zip to the folder and overwrite existing files.

## CREDITS

* [Dyson Sphere Program](https://store.steampowered.com/app/1366540): The great game
* [BepInEx](https://bepinex.dev/): Base modding framework
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): Some cheat functions

</details>

<details>
<summary>中文读我</summary>

***添加一些作弊功能，同时屏蔽异常检测***

## 使用说明

* 配置面板复用UXAssist
* 标题界面和行星小地图旁也有按钮呼出主面板
* 功能：
  * 常规：
    * 启用开发模式快捷键(使用说明见设置面板)
    * 屏蔽异常检测
    * 使用组合键快速升降级科技（按住 Ctrl/Alt/Shift 点击科技树）
      * 按住 `Shift` / `Ctrl` / `Ctrl+Shift` 可改变 1 / 10 / 100 级；`Alt` 升到最大等级，降级模式下则回退到未解锁。
      * `Caps Lock` 切换本次游戏中的降级模式，并通过实时弹窗和严重警告横幅提示。离开游戏或关闭此功能后，降级模式重置。
      * 升级时解锁所需前置科技；降级时回退对应升级效果，前置条件失效的后继科技也会失去已应用等级和研究进度。
      * 缩容会隐藏机舱或物流清单中的物品、未完成的配送、有战斗机或尚未召回的编队，或空闲建设无人机数量不足时，会拒绝整次降级。清空相关格子、召回并清空相关编队，或等待建设无人机返回后再重试。
      * 科技树界面的 `?` 按钮显示完整组合键说明。
    * 移除所有元数据消耗记录
    * 移除当前存档的元数据消耗记录
    * 解除当前存档因使用元数据导致的成就限制
    * 将游戏存档绑定给当前账号
  * 工厂：
    * 建造秒完成
      * 所需建筑物品齐备后立即完成待建造建筑
    * 建筑师模式
      * 建筑、配送运输机、物流运输机和星际物流运输船可无限使用
      * 建造及库存可用数量检查中，每种受支持物品至少显示 999 个。
    * 无条件建造
    * 无碰撞
    * 传送带信号物品生成
      * 统计信息里将生成计算为产物
      * 统计信息里将移除计算为消耗
      * 统计面板中计算所有原材料和中间产物
      * 统计原料、中间产物和成品使用的增产剂
        * 计算原料需求时使用对应配方的额外产出或加速效果；卡西米尔晶体和能量矩阵不使用增产剂。此选项的帮助提示列出使用额外产出的配方。
      * 传送带信号替换格式
    * 提升物流塔和大型采矿机的最大功耗
      * 物流塔：将最大充电功率提高到3GW(星际物流塔)和600MW(行星物流塔)（原来的10倍）
      * 大型采矿机：将最大采矿速度提高到1000%
    * 在物流总控面板上可以从非本地行星取放物品
    * 移除风力涡轮机和地热发电站的间距限制
    * 风力涡轮机供电覆盖全球
    * 提升各种发电设备发电量
  * 行星：
    * 快速手动制造
    * 自然资源采集不消耗
    * 高速采集
    * 平地抽水
    * 沙土不够时依然可以整改地形
      * 支持铺设地基、还原地形、蓝图地基和掩埋黑雾核心钻机
      * 沙土收益仍正常生效，沙土不足时不会扣成负数。
    * 可在配置面板铺满星球地基或还原全部地形
    * 快速传送(和沙盒模式一样)
  * 戴森球：
    * 跳过子弹阶段
      * 可选择一次弹射所有已缓存的太阳帆
    * 跳过吸收阶段
    * 快速吸收
    * 全球弹射
    * 高速弹射器和高速发射井（10 倍射速）
    * 解锁戴森球最大轨道半径
    * 立即完成戴森壳建造
    * 移除戴森球上的所有框架
      * 保留节点和戴森壳，太阳帆吸收仍正常进行，可减少框架使用的火箭。
    * 生成仙术戴森壳
      * 默认的快速生成按钮会按所选壳面数量创建新的戴森球层级。
      * 在配置文件的 `DysonSphere` 分类中启用 `IllegalDysonShellFunctions`，可使用以下高级操作：
        * 生成单层仙术戴森壳
        * 为所有尚无节点和戴森壳的现有层级生成仙术戴森壳
        * 保留发电量最高的戴森壳并移除其他戴森壳
        * 从发电量最高的壳复制戴森壳，可设置壳面数量
  * 机甲/战斗：
    * 机甲和战斗无人机无敌
    * 建筑无敌
    * 无需空间翘曲器即可曲速飞行
    * 传送到外太空
    * 传送到选定的天体

## 注意事项

* 如果和[BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)一起使用，请升级`BepInEx`到5.4.21或更高版本，以避免可能的冲突
  * 你可以在[这里](https://github.com/bepinex/bepinex/releases/latest)（选择x64版本）下载`BepInEx`
  * 如果使用r2modman，你可以点击`Settings` -> `Browse profile folder`，然后将下载的zip解压到该文件夹并覆盖现有文件

## 鸣谢

* [戴森球计划](https://store.steampowered.com/app/1366540): 伟大的游戏
* [BepInEx](https://bepinex.dev/): 基础模组框架
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): 一些作弊功能

</details>
