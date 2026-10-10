# UXAssist

<details>
<summary>Read me in English</summary>

***Some functions and patches for better user experience***

## Bug reports

* QQ group: 372754090

## Usage

* Press `` Alt+`(BackQuote) `` to call up the config panel. You can change the shortcut on the panel.
* There are also buttons on title screen and planet minimap area to call up the config panel.
* Patches:
  * Strict hotkey dectection for build menu, thus building hotkeys(0~9, F1~F10, X, U) are not triggered while holding Ctrl/Alt/Shift.
  * Fix a bug that warning popup on `Veins Utilization` upgraded to level 8000+
  * Sort blueprint structures before saving, to reduce generated blueprint data size a little
  * Increase maximum count of Metadata Instantiations to 20000 (from 2000)
  * Increase capacity of player order queue to 128 (from 16)
  * Enable `Hide UI` (`F11` by default) in Star Map view, hiding UI and other scene elements
  * Append mod profile name to game window title, if using mod managers (`Thunderstore Mod Manager` or `r2modman`).
* Features:
  * General
    * Enable game window resize
    * Remember window position and size on last exit
    * Convert Peace-Mode saves to Combat-Mode on loading
    * Mod manager profile based save folder
      * Save files are stored in `Save\<ProfileName>` folder.
      * Will use original save location if matching default profile name.
    * Mod manager profile based option
      * Option file is stored as `Options\<ProfileName>.xml`.
    * Logical Frame Rate
      * This will change game running speed, down to 0.1x slower and up to 10x faster.
      * Use `Ctrl+-` and `Ctrl+=` by default to decrease or increase the logical frame rate by 0.5x; change the bindings in the system options.
      * Note:
        * High logical frame rate is not guaranteed to be stable, especially when factories are under heavy load.
        * This will not affect some game animations.
        * When set game speed in mod `Auxilaryfunction`, this feature will be disabled.
        * When `BulletTime` is installed, this feature is hidden; use BulletTime's own speed controls.
    * Set process priority
    * Increase maximum count of Metadata Instantiations to 20000 (from 2000)
    * Increase capacity of player order queue to 128 (from 16)
  * Factory
    * Sunlight at night
    * Remove some build conditions
    * Remove build count and range limit
    * Larger area for upgrade and dismantle (up to 31x31)
    * Larger area for terraform (up to 30x30)
      * Applies to both foundation placement and Restore Terrain
    * Off-grid building and stepped rotation
    * Cut conveyor belt
      * Press shortcut key to cut conveyor belt under cursor.
      * The default shortcut key is Alt+X, you can set it in system options panel.
    * Treat stack items as single in monitor components
    * Quick build and dismantle stacking labs/storages/tanks
    * Fast fill in to and take out from tanks
      * You can set multiplier for tanks' operation speed
      * This affects manually fill in to and/or take out from tanks, as well as transfer from upper to lower level.
    * Protect veins from exhaustion
      * Default protection thresholds are 1,000 remaining vein units and an oil extraction rate of 1.0/s; change them in the config file.
      * Mining or pumping stops at the configured threshold. Disable protection to resume consuming the remaining resources.
      * Vein mining can continue at the threshold when `Veins Utilization` reduces the mining cost rate to zero.
    * Do not render factory entities (except belts and sorters)
      * This also makes players click though factory entities but belts and sorters
      * `Hide sorters too`: also hide sorters, leaving only belts (and their cargo) visible
    * Drag building power poles in maximum connection range
      * Optionally alternate Tesla Towers and Wireless Power Towers when dragging from a Wireless Power Tower
    * Auto-construct
      * Automatically flies toward pending buildings and routes around obstacles
      * Enable automatic construction in the settings. The separate button option shows a toggle and the pending-building count near the bottom of the screen.
      * Hiding the button does not disable automatic construction.
    * Shortcut keys for Blueprint Copy mode
      * `Ctrl+A` selects all buildings; `Ctrl+X` dismantles the selection by default. Change both bindings in the system options.
    * `Ctrl+Shift+Click` to pick items from whole belts
      * Optionally include belt branches and connected inserters
    * Initialize This Planet (without reseting veins)
      * Choose whether to return buildings, logistics storage items, and belt/factory items
    * Quick dismantle all buildings (without drops)
    * Quick build Orbital Collectors
      * Set a maximum number to build per operation; 0 builds as many as possible
    * Belt signals for buy out dark fog items automatically
      * 6 belt signals are added to the signal panel, which can be used to buy out dark fog items automatically.
      * Generated items are stacked in 4 items.
      * Exchange ratios follow the original game design. The native mixed bundle gives 10 each of Silicon-based Neurons, Negentropy Singularities, and Matter Recombinators for 1 Metaverse; a belt chooses one material at an equivalent rate of 30:
        * 1 Metaverse = 20 Dark Fog Matrices (item `5201`)
        * 1 Metaverse = 60 Energy Shards (item `5206`)
        * 1 Metaverse = 30 Silicon-based Neurons (item `5202`)
        * 1 Metaverse = 30 Negentropy Singularities (item `5204`)
        * 1 Metaverse = 30 Matter Recombinators (item `5203`)
        * 1 Metaverse = 10 Core Elements (item `5205`)
    * Tweak building buffer
      * The `?` help button shows original in-game buffer values for comparison.
      * Factory recipe buffer formula: take the larger value between `Assembler buffer time multiplier(in seconds) * items needed per second` and `Assembler buffer minimum multiplier * items needed per recipe`
        * `Assembler buffer time multiplier(in seconds)`: Range 2-10, default is 4 (same as game)
        * `Assembler buffer minimum multiplier`: Range 2-10, default is 4 (game default is 2)
      * Matrix Lab assembly mode formula: Default buffer is `Buffer count for assembling in labs`, when using Self-evolution Lab, if recipe's original production time is not greater than 9 seconds, add `Extra buffer count for Self-evolution Labs` * (`Lab speed` - 1)
        * `Buffer count for assembling in labs`: Range 2-20, default is 6 (same as game)
        * `Extra buffer count for Self-evolution Labs`: Range 1-10, default is 3 (same as game)
      * `Buffer count for researching in labs`: Range 2-20, default is 10 (same as game)
      * `Ray Receiver Graviton Lens buffer count`: Range 1-20, default is 1 (game default is 20)
      * `Ejector Solar Sails buffer count`: Range 5-400 (step by 5); the game's original buffer holds 20
      * `Silo Rockets buffer count`: Range 1-40; the game's original buffer holds 20
  * Logistics
    * Enhanced control for logistic storage capacities
      * Logistic storage capacities are not scaled on upgrading `Logistics Carrier Capacity`, if they are not set to maximum capacity or already greater than maximum capacity.
      * You can use arrow keys to adjust logistic storage capacities gracefully.
    * Logistics Control Panel Improvement
      * Auto apply filter with item under mouse cursor while opening the panel
      * Quick-set item filter while right-clicking item icons in storage list on the panel
    * Allow overflow for Logistic Stations and Advanced Mining Machines
      * Allow overflow when trying to insert in-hand items
      * Allow `Enhanced control for logistic storage capacities` to exceed tech capacity limits
      * Remove logistic strorage capacity limit check on loading game
    * Real-time logistic stations info panel
      * Optionally show status bars for stored items
      * This feature is hidden when `Show station info` is enabled in `Auxilaryfunction`.
    * Orbital Collector product limit
      * Set the storage limit per product: 1-999,999, default 20,000
      * New collectors use this value. Use `Apply to universe` to update all existing Orbital Collectors.
    * Auto-config logistic stations
      * Configure Logistics Distributors, Battlefield Analysis Bases, PLS, ILS, and Advanced Mining Machines when built
      * Set default remote slots to storage, and optionally limit automatic bot/drone/vessel replenishment to the configured counts
      * Apply settings to all existing facilities on the current planet
        * Each setting on the Logistics tab has an `Apply` button that pushes that single value to all facilities of that type on the current planet.
        * Each category header (Logistics Distributor, Battlefield Analysis Base, PLS, ILS, Advanced Mining Machine) has an `Apply All` button that pushes every setting of that category to all facilities of that type on the current planet.
  * Player/Mecha
    * Unlimited interactive range
    * Enable player actions in globe view
    * Hide tips for soil piles changes
      * Soil gains and consumption still take effect while the notifications are hidden.
    * Enhanced count control for hand-make
      * Raise the maximum crafting count to 1,000 and use Ctrl/Shift/Alt to adjust it quickly
    * Auto-cruise
      * Requires `Drive Engine` level 2 and a navigation target. Enable the feature in the settings, select a target, then use the on-screen button or the shortcut (`K` by default) to start or stop cruising.
      * The button shows the current status and configured shortcut; change the shortcut in the system options.
      * Flies toward planets or stars, or follows Dark Fog Hives and Carriers, while avoiding obstacles and slowing near the destination
      * Configure whether manual movement input stops cruising.
      * Optional warp: default minimum distance 2 AU and minimum core energy 800 MJ; requires the warp upgrade and a Space Warper
      * Optional auto boost: default minimum core energy 100 MJ
      * Configure follow distances for Dark Fog Hives (AU) and Carriers (m)
  * Dyson Sphere
    * Stop ejectors when available nodes are all filled up
    * Construct only structure points but frames
    * Re-initialize Dyson Spheres
    * Quick dismantle Dyson Shells
    * Dyson Sphere "Auto Fast Build" speed multiplier
      * Note: this only applies to `Dyson Sphere "Auto Fast Build"` in sandbox mode
  * Tech
    * Restore upgrades of `Sorter Cargo Stacking` on panel
    * Set `Sorter Cargo Stacking` to unresearched state
    * Buy out techs with their prerequisites
      * This enables batch buying out techs with their prerequisites. Buy-out buttons are shown for locked techs/upgrades.
    * Hide battle-related techs in Peace mode
    * Unlock all techs with metadata
      * Use the config-panel button to confirm the metadata cost and buy out eligible techs in a batch; unlimited upgrade chains are excluded.
  * Combat
    * Open Dark Fog Communicator anywhere
  * UI
    * Embedded [Planet Vein Untilization](https://thunderstore.io/c/dyson-sphere-program/p/testpushpleaseignore/Planet_Vein_Utilization/)
      * Shows vein-group counts before resource names in planet details; supports additional vein types provided by mods
    * Shortcut keys for showing stars' name
      * Add a shortcut key to always show all star names in starmap when holding, default is `Alt`
      * Add a shortcut key to toggle between three star name display states in starmap: `Original state`, `Show all names`, `Hide all names`, default is `Tab`, will restore to original state when closing starmap
    * Starmap view:
      * Filter displayed star names by ores or planet types, combining conditions with either intersection or union
      * Add a dropdown box to show all stars' distance and/or planet count.
    * Show top players in milkyway
      * The button is available on top-left corner of Milkyway View
    * Show recent milkyway upload results
      * The button is on UXAssist `General` tab.

## Notes

* Please upgrade `BepInEx` 5.4.21 or later if using with [BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/) to avoid possible conflicts.
  * You can download [BepInEx here](https://github.com/bepinex/bepinex/releases/latest)(choose x64 edition).
  * If using with r2modman, you can upgrade `BepInEx` by clicking `Settings` -> `Browse profile folder`, then extract downloaded zip to the folder and overwrite existing files.

## CREDITS

* [Dyson Sphere Program](https://store.steampowered.com/app/1366540): The great game
* [BepInEx](https://bepinex.dev/): Base modding framework
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): Some cheat functions
* [LSTM](https://github.com/hetima/DSP_LSTM) & [PlanetFinder](https://github.com/hetima/DSP_PlanetFinder): UI implementations
* [OffGridConstruction](https://github.com/Velociraptor115-DSPModding/OffGridConstruction): Off-grid building & stepped rotation implementations
* [CruiseAssist](https://dsp.thunderstore.io/package/tanu/CruiseAssist/) and its extension [AutoPilot](https://dsp.thunderstore.io/package/tanu/AutoPilot/): Auto-cruise
* [Planet Vein Untilization](https://thunderstore.io/c/dyson-sphere-program/p/testpushpleaseignore/Planet_Vein_Utilization/)

</details>

<details>
<summary>中文读我</summary>

***一些提升用户体验的功能和补丁***

## Bug 反馈

* QQ群：372754090

## 使用说明

* 按 `` Alt+`(反引号) `` 键呼出主面板，可以在面板上修改快捷键。
* 标题界面和行星小地图旁也有按钮呼出主面板。
* 补丁：
  * 更严格的建造菜单热键检测，因此在按住Ctrl/Alt/Shift时不再会触发建造热键(0~9, F1~F10, X, U)
  * 修复了`矿物利用`升级到8000级以上时弹出警告的bug
  * 保存蓝图前对建筑进行排序，以减少生成的蓝图数据大小
  * 将元数据提取的最大数量增加到20000(原来为2000)
  * 将玩家指令队列的容量增加到128(原来为16)
  * 在星图视图中启用 `隐藏 UI` 功能（默认按键为 `F11`），同时隐藏 UI 和场景中的其他元素
  * 如果使用mod管理器(`Thunderstore Mod Manager`或`r2modman`)启动游戏，在游戏窗口标题中追加mod配置档案名
* 功能：
  * 通用
    * 可调整游戏窗口大小(可最大化和拖动边框)
    * 记住上次退出时的窗口位置和大小
    * 在加载和平模式存档时将其转换为战斗模式
    * 基于mod管理器配置档案名的存档文件夹
      * 存档文件会存储在`Save\<ProfileName>`文件夹中
      * 如果匹配默认配置档案名则使用原始存档位置
    * 基于mod管理器配置档案名的选项文件
      * 选项文件存储为`Options\<ProfileName>.xml`
    * 逻辑帧倍率
      * 这将改变游戏运行速度，最慢0.1倍，最快10倍
      * 默认使用 `Ctrl+-` 和 `Ctrl+=` 将逻辑帧倍率降低或提高 0.5 倍；可在系统选项中修改快捷键
      * 注意：
        * 高逻辑帧倍率不能保证稳定性，特别是在工厂负载较重时
        * 这不会影响一些游戏动画
        * 当在`Auxilaryfunction`mod中设置游戏速度时，此功能将被禁用
        * 安装 `BulletTime` 后，此功能会隐藏，请使用 BulletTime 自身的速度控制
    * 设置进程优先级
    * 将元数据提取的最大数量增加到20000(原来为2000)
    * 将玩家指令队列的容量增加到128(原来为16)
  * 工厂
    * 夜间日光灯
    * 移除部分不影响游戏逻辑的建造条件
    * 移除建造数量和范围限制
    * 扩大范围升级和拆除区域（最大 31×31）
    * 扩大地形改造区域（最大 30×30）
      * 同时适用于铺设地基和还原地形
    * 脱离网格建造以及小角度旋转
    * 切割传送带
      * 按快捷键切割光标位置的传送带
      * 默认快捷键是Alt+X，可以在系统选项面板中设置
    * 在流速计中将堆叠物品视为单个物品
    * 快速建造和拆除堆叠研究站/储物仓/储液罐
    * 储液罐快速注入和抽取液体
      * 你可以设置储液罐操作速度的倍率
      * 影响手动注入和抽取，以及从储液罐上层传输到下层的速度
    * 保护矿脉不会耗尽
      * 默认保护阈值为矿脉剩余量 1,000、采油速度 1.0/s，可在配置文件中修改。
      * 达到阈值时停止采矿或抽油；关闭保护后可继续消耗剩余资源。
      * `矿物利用` 将采矿消耗降到零后，即使达到保护阈值也可继续采矿。
    * 不渲染工厂建筑实体(除了传送带和分拣器)
      * 这也使玩家可以点穿工厂实体直接点到传送带和分拣器
      * `同时隐藏分拣器`：连分拣器也一并隐藏，只保留传送带（及其上的货物）可见
    * 拖动建造电线杆时自动使用最大连接距离间隔
      * 从无线输电塔开始拖动时，可选择交替建造电力感应塔和无线输电塔
    * 自动建造
      * 自动飞向待建造的建筑，并尝试绕开障碍物
      * 在设置中启用自动建造；另有按钮显示选项，可在屏幕下方显示开关和待建造建筑数量。
      * 隐藏按钮不会关闭自动建造。
    * 蓝图复制模式快捷键
      * 默认 `Ctrl+A` 选择所有建筑，`Ctrl+X` 拆除选中的建筑；两个快捷键均可在系统选项中修改。
    * `Ctrl+Shift+Click` 拾取整条传送带上的物品
      * 可选择包含传送带分支和相连的分拣器
    * 初始化本行星（不重置矿脉）
      * 可分别选择返还建筑、物流设施库存，以及传送带和工厂内的物品
    * 快速拆除所有建筑（不掉落）
    * 快速建造轨道采集器
      * 可设置单次建造数量上限；设为 0 时尽可能多地建造
    * 用于自动购买黑雾物品的传送带信号
      * 在信号面板上添加了6个传送带信号，可以用于自动购买黑雾道具。
      * 生成的物品堆叠数为4。
      * 兑换比率遵循原始游戏设计。原生组合兑换用 1 个元宇宙换取硅基神经元、负熵奇点和物质重组器各 10 个；传送带只选择其中一种材料，等值兑换数量为 30 个：
        * 1 个元宇宙 = 20 个黑雾矩阵（物品 `5201`）
        * 1 个元宇宙 = 60 个能量碎片（物品 `5206`）
        * 1 个元宇宙 = 30 个硅基神经元（物品 `5202`）
        * 1 个元宇宙 = 30 个负熵奇点（物品 `5204`）
        * 1 个元宇宙 = 30 个物质重组器（物品 `5203`）
        * 1 个元宇宙 = 10 个核心素（物品 `5205`）
    * 调整建筑输入缓冲
      * `?` 帮助按钮列出游戏内原始缓冲值，便于对照。
      * 工厂配方计算公式，在`工厂配方缓冲时间倍率秒数x每秒需要的原料数量`和`工厂配方缓冲最小倍率x每生产一次配方需要的原料数量`中取更大的那个值
        * `工厂配方缓冲时间倍率(秒)`：范围2-10，默认为4(同游戏)
        * `工厂配方缓冲最小倍率`：范围 2 至 10，默认值为 4（游戏原始值为 2）
      * 研究站矩阵合成模式计算公式，默认缓存`研究站矩阵合成模式缓存数量`个，当使用自演化研究站时，如果配方的原始生产时间不大于9秒，则增加`自演化研究站矩阵额外缓冲数量`*(`研究站速度倍率`-1)
        * `研究站矩阵合成模式缓存数量`：范围2-20，默认为6(同游戏)
        * `自演化研究站矩阵额外缓冲数量`：范围1-10，默认为3(同游戏)
      * `研究站科研模式缓存数量`：范围2-20，默认为10(同游戏)
      * `射线接收器透镜缓冲数量`：范围1-20，默认为1(游戏默认为20)
      * `弹射太阳帆缓冲区数量`：范围 5 至 400（步进值为 5）；游戏原始缓冲数量为 20
      * `发射井火箭缓冲区数量`：范围 1 至 40；游戏原始缓冲数量为 20
  * 物流
    * 物流塔存储数量限制控制改进
      * 当升级`运输机舱扩容`时，不会对各种物流塔的存储限制按比例提升，除非设置为最大允许容量或者已经超过升级后的最大容量。
      * 你可以使用方向键微调物流塔存储限制
    * 物流控制面板改进
      * 打开面板时自动将鼠标指向物品设为筛选条件
      * 在控制面板物流塔列表中右键点击物品图标快速设置为筛选条件
    * 允许物流塔和大型采矿机物品溢出
      * 当尝试塞入手中物品时允许溢出
      * 允许`物流塔存储数量限制控制改进`超过科技容量限制
      * 在加载游戏时移除物流塔容量限制检查
    * 物流运输站实时信息面板
      * 可选择显示存储物品状态条
      * 启用 `Auxilaryfunction` 中的 `展示物流站信息` 后，此功能会隐藏。
    * 轨道采集器产物上限
      * 设置每种产物的存储上限：范围 1 至 999,999，默认值为 20,000
      * 新建轨道采集器使用此值；点击 `应用到全宇宙` 可更新所有已建成的轨道采集器。
    * 自动配置物流站
      * 建造时自动配置物流配送器、战场分析基站、行星物流站、星际物流站和大型采矿机
      * 可将远程槽位默认设为仓储，并将配送运输机、物流运输机及星际物流运输船的自动补充数量限制为配置值
      * 将设置应用到当前行星上所有已建成的物流设施
        * 物流标签页上每一项设置右侧都有一个`应用`按钮，按下后将该项数值应用到当前行星上所有该类型的物流设施。
        * 每个大分类标题（物流配送器、战场分析基站、行星物流站、星际物流站、大型采矿机）右侧都有一个`应用全部`按钮，按下后将该分类的所有设置数值应用到当前行星上所有该类型的物流设施。
  * 玩家/机甲
    * 无限交互距离
    * 在行星视图中允许玩家操作
    * 隐藏沙土数量变动的提示
      * 隐藏提示后，沙土的获得和消耗仍正常生效。
    * 手动制造物品的数量控制改进
      * 最大制造数量提高到 1,000，可按住 Ctrl/Shift/Alt 快速调整数量
    * 自动巡航
      * 需要 `驱动引擎` 2 级和导航目标。在设置中启用功能，选择目标后，使用屏幕上的按钮或快捷键（默认 `K`）启动或停止巡航。
      * 按钮显示当前状态和配置的快捷键；可在系统选项中修改快捷键。
      * 飞向行星或恒星，或跟踪黑雾巢穴和火种；途中避开障碍物，在接近目标时减速
      * 可设置手动移动输入是否停止巡航。
      * 可选择使用曲速：默认最小距离为 2 AU、最低核心能量为 800 MJ；需要曲速升级和空间翘曲器。
      * 可选择自动加速：默认最低核心能量为 100 MJ
      * 可设置黑雾巢穴（AU）和黑雾火种（m）的跟踪距离
  * 戴森球
    * 可用节点全部造完时停止弹射
    * 只建造节点不建造框架
    * 初始化戴森球
    * 快速拆除戴森壳
    * 戴森球自动快速建造速度倍率
      * 注意：这仅适用于沙盒模式下的`戴森球自动快速建造`功能
  * 科研
    * 在升级面板上恢复`分拣器货物堆叠`的升级
    * 将`分拣器货物堆叠`设为未研究状态
    * 买断科技也同时买断所有前置科技
      * 可批量买断科技及其前置科技，未解锁的科技和升级会显示买断按钮。
    * 在和平模式下隐藏战斗相关科技
    * 使用元数据解锁所有科技
      * 在配置面板点击按钮并确认元数据消耗，批量买断符合条件的科技；不包含无限升级科技链。
  * 战斗
    * 在任意位置打开黑雾通讯器
  * UI
    * 启用显示所有星系名称的快捷键
      * 新增一个快捷键，按住后始终在星图显示所有星系名称，默认为`Alt`
      * 新增一个快捷键，在星图视图切换三种星系名称显示状态：`原始显示状态`，`显示所有名称`，`隐藏所有名称`，默认为`Tab`，关闭星图时会恢复到原始状态
    * 星图：
      * 按矿物或行星类型过滤显示的星系名，可选择取筛选条件的交集或并集
      * 添加了一个下拉框用以切换显示所有星系的距离和/或行星数量
    * 内置 [Planet Vein Untilization](https://thunderstore.io/c/dyson-sphere-program/p/testpushpleaseignore/Planet_Vein_Utilization/)
      * 在行星详情的资源名称前显示矿脉组数，支持其他 MOD 提供的矿脉类型
    * 显示银河系发电量排行
      * 按钮位于银河视图左上角
    * 显示最近银河系上传结果
      * 按钮在UXAssist的`常规`标签页内

## 注意事项

* 如果和[BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)一起使用，请升级`BepInEx`到5.4.21或更高版本，以避免可能的冲突。
  * 你可以在[这里](https://github.com/bepinex/bepinex/releases/latest)（选择x64版本）下载`BepInEx`。
  * 如果使用r2modman，你可以点击`Settings` -> `Browse profile folder`，然后将下载的zip解压到该文件夹并覆盖现有文件。

## 鸣谢

* [戴森球计划](https://store.steampowered.com/app/1366540): 伟大的游戏
* [BepInEx](https://bepinex.dev/): 基础模组框架
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): 一些作弊功能
* [LSTM](https://github.com/hetima/DSP_LSTM) & [PlanetFinder](https://github.com/hetima/DSP_PlanetFinder): UI实现
* [OffGridConstruction](https://github.com/Velociraptor115-DSPModding/OffGridConstruction): 脱离网格建造以及小角度旋转的实现
* [CruiseAssist](https://dsp.thunderstore.io/package/tanu/CruiseAssist/) 及其扩展 [AutoPilot](https://dsp.thunderstore.io/package/tanu/AutoPilot/)：自动巡航
* [Planet Vein Untilization](https://thunderstore.io/c/dyson-sphere-program/p/testpushpleaseignore/Planet_Vein_Utilization/)

</details>
