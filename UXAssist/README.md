# UXAssist

#### Some functions and patches for better user experience

#### 一些提升用户体验的功能和补丁

## Usage

* Press `` Alt+`(BackQuote) `` to call up the config panel. You can change the shortcut on the panel.
* There are also buttons on title screen and planet minimap area to call up the config panel.
* Patches:
  + Strict hotkey dectection for build menu, thus building hotkeys(0~9, F1~F10, X, U) are not triggered while holding Ctrl/Alt/Shift.
  + Fix a bug that warning popup on `Veins Utilization` upgraded to level 8000+
  + Sort blueprint structures before saving, to reduce generated blueprint data size a little
  + Increase maximum count of Metadata Instantiations to 20000 (from 2000)
  + Increase capacity of player order queue to 128 (from 16)
  + Enable `Hide UI` function(`F11` by default) while on Star Map view
  + Append mod profile name to game window title, if using mod managers (`Thunderstore Mod Manager` or `r2modman`).
* Features:
  + General
    - Enable game window resize
    - Remember window position and size on last exit
    - Convert Peace-Mode saves to Combat-Mode on loading
  + Planet/Factory
    - Sunlight at night
    - Remove some build conditions
    - Remove build count and range limit
    - Larger area for upgrade and dismantle(30x30 at max)
    - Larger area for terraform(30x30 at max)
    - Treat stack items as single in monitor components
    - Enhanced control for logistic storage limits
      - Logistic storage limits are not scaled on upgrading `Logistics Carrier Capacity`, if they are not set to maximum capacity.
      - You can use arrow keys to adjust logistic storage limits gracefully.
    - Quick build and dismantle stacking labs/storages/tanks
    - Protect veins from exhaustion
      - By default, the vein amount is protected at 100, and oil speed is protected at 1.0/s, you can set them yourself in config file.
      - When reach the protection value, veins/oils steeps will not be mined/extracted any longer.
      - Close this function to resume mining and pumping, usually when you have enough level on `Veins Utilization`
    - Do not render factory entities (except belts and sorters)
      - This also makes players click though factory entities but belts and sorters
    - Drag building power poles in maximum connection range
    - Allow overflow for Logistic Stations and Advanced Mining Machines
      - Allow overflow when trying to insert in-hand items
      - Allow `Enhanced control for logistic storage limits` to exceed tech capacity limits
      - Remove logistic strorage limit check on loading game
    - Logistics Control Panel Improvement
      - Auto apply filter with item under mouse cursor while opening the panel
      - Quick-set item filter while right-clicking item icons in storage list on the panel
    - Re-intialize planet (without reseting veins)
    - Quick dismantle all buildings (without drops)
    - Quick build Orbital Collectors
    - Belt signals for buy out dark fog items automatically
      - 6 belt signals are added to the signal panel, which can be used to buy out dark fog items automatically.
      - Generated items are stacked in 4 items.
      - Exchange ratio is following the original game design, aka:
        - 1 Metaverse = 20 Dark Fog Matrices
        - 1 Metaverse = 60 Engery Shards
        - 1 Metaverse = 30 Silicon-based Neurons
        - 1 Metaverse = 30 Negentropy Singularities
        - 1 Metaverse = 30 Matter Recombinators
        - 1 Metaverse = 10 Core Elements
  + Player/Mecha
    - Unlimited interactive range
    - Enable player actions in globe view
    - Hide tips for soil piles changes
    - Enhanced count control for hand-make
    - Auto navigation on sailings
      - It keeps Icarus on course to the target planet
      - It will try to bypass any obstacles(planets, stars or dark-fog hives) on the way
      - Furthermore, you can set a shortcut key in the system options window, which is used to toggle `Auto-cruise` that enables flying to targeted planets fully automatically.
        - Auto-cruise will start when you select a planet as target
        - It will use warper to fly to the target planet if the planet is too far away, the range can be configured.
        - It will speed down when approaching the target planet, to avoid overshooting
  + Dyson Sphere
    - Stop ejectors when available nodes are all filled up
    - Construct only structure points but frames
    - Re-initialize Dyson Spheres
    - Quick dismantle Dyson Shells
  + Tech
    - Restore upgrades of `Sorter Cargo Stacking` on panel
    - Set `Sorter Cargo Stacking` to unresearched state
    - Buy out techs with their prerequisites
      - This enables batch buying out techs with their prerequisites. Buy-out button is shown for all locked techs/upgrads.
  + Combat
    - Open Dark Fog Communicator anywhere

## Notes

* Please upgrade `BepInEx` 5.4.21 or later if using with [BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/) to avoid possible conflicts.
  + You can download `BepInEx` [here](https://github.com/bepinex/bepinex/releases/latest)(choose x64 edition).
  + If using with r2modman, you can upgrade `BepInEx` by clicking `Settings` -> `Browse profile folder`, then extract downloaded zip to the folder and overwrite existing files.

## CREDITS

* [Dyson Sphere Program](https://store.steampowered.com/app/1366540): The great game
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): Some cheat functions
* [LSTM](https://github.com/hetima/DSP_LSTM) & [PlanetFinder](https://github.com/hetima/DSP_PlanetFinder): UI implementations
* [OffGridConstruction](https://github.com/Velociraptor115-DSPModding/OffGridConstruction): Off-grid building & stepped rotation implementations
* [CruiseAssist](https://dsp.thunderstore.io/package/tanu/CruiseAssist/) and its extension [AutoPilot](https://dsp.thunderstore.io/package/tanu/AutoPilot/): `Auto navigation on sailings` and `Auto-cruise` implementations

## 使用说明

* 按 `` Alt+`(反引号) `` 键呼出主面板，可以在面板上修改快捷键。
* 标题界面和行星小地图旁也有按钮呼出主面板。
* 补丁：
  + 更严格的建造菜单热键检测，因此在按住Ctrl/Alt/Shift时不再会触发建造热键(0~9, F1~F10, X, U)
  + 修复了`矿物利用`升级到8000级以上时弹出警告的bug
  + 保存蓝图前对建筑进行排序，以减少生成的蓝图数据大小
  + 将元数据提取的最大数量增加到20000(原来为2000)
  + 将玩家指令队列的容量增加到128(原来为16)
  + 在星图视图中启用`隐藏UI`功能(默认按键为`F11`)
  + 如果使用mod管理器(`Thunderstore Mod Manager`或`r2modman`)启动游戏，在游戏窗口标题中追加mod配置档案名
* 功能：
  + 通用
    - 可调整游戏窗口大小(可最大化和拖动边框)
    - 记住上次退出时的窗口位置和大小
    - 在加载和平模式存档时将其转换为战斗模式
    - 将元数据提取的最大数量增加到20000(原来为2000)
    - 将玩家指令队列的容量增加到128(原来为16)
  + 行星/工厂
    - 夜间日光灯
    - 移除部分不影响游戏逻辑的建造条件
    - 范围升级和拆除的最大区域扩大(最大30x30)
    - 范围铺设地基的最大区域扩大(最大30x30)
    - 在流速计中将堆叠物品视为单个物品
    - 物流塔存储数量限制控制改进
      - 当升级`运输机舱扩容`时，不会对各种物流塔的存储限制按比例提升，除非设置为最大允许容量。
      - 你可以使用方向键微调物流塔存储限制
    - 快速建造和拆除堆叠研究站/储物仓/储液罐
    - 保护矿脉不会耗尽
      - 默认矿脉数量保护在100，采油速保护在1.0/s，你可以在配置文件中自行设置。
      - 当达到保护值时，矿脉和油井将不再被开采。
      - 关闭此功能以恢复开采，一般是当你在`矿物利用`上有足够的等级时。
    - 不渲染工厂建筑实体(除了传送带和分拣器)
      - 这也使玩家可以点穿工厂实体直接点到传送带和分拣器
    - 拖动建造电线杆时自动使用最大连接距离间隔
    - 允许物流塔和大型采矿机物品溢出
      - 当尝试塞入手中物品时允许溢出
      - 允许`物流塔存储数量限制控制改进`超过科技容量限制
      - 在加载游戏时移除物流塔容量限制检查
    - 物流控制面板改进
      - 打开面板时自动将鼠标指向物品设为筛选条件
      - 在控制面板物流塔列表中右键点击物品图标快速设置为筛选条件
    - 初始化本行星（不重置矿脉）
    - 快速拆除所有建筑（不掉落）
    - 快速建造轨道采集器
    - 用于自动购买黑雾物品的传送带信号
      - 在信号面板上添加了6个传送带信号，可以用于自动购买黑雾道具。
      - 生成的物品堆叠数为4。
      - 兑换比率遵循原始游戏设计，即：
        - 1个元宇宙 = 20个黑雾矩阵
        - 1个元宇宙 = 60个能量碎片
        - 1个元宇宙 = 30个硅基神经元
        - 1个元宇宙 = 30个负熵奇点
        - 1个元宇宙 = 30个物质重组器
        - 1个元宇宙 = 10个核心素
  + 玩家/机甲
    - 无限交互距离
    - 移除建造数量和范围限制
    - 在行星视图中允许玩家操作
    - 隐藏沙土数量变动的提示
    - 手动制造物品的数量控制改进
    - 航行时自动导航
      - 它会保持伊卡洛斯飞向目标星球
      - 它会尝试绕过途中的任何障碍物(行星、恒星或黑雾巢穴)
      - 此外，可以在系统选项窗口中设置快捷键，用于切换`自动巡航`，实现完全自动化的飞行至目标星球。
        - 当你选择目标星球后，自动巡航就会开始
        - 如果目标星球距离过远会自动使用曲速(超过5AU)，你可以在面板上更改这个值。
        - 它会在接近目标星球时减速，以避免发生越过目标的情况
  + 戴森球
    - 可用节点全部造完时停止弹射
    - 只建造节点不建造框架
    - 初始化戴森球
    - 快速拆除戴森壳
  + 科研
    - 在升级面板上恢复`分拣器货物堆叠`的升级
    - 将`分拣器货物堆叠`设为未研究状态
    - 买断科技也同时买断所有前置科技
      - 这使得可以批量买断科技及其所有前置科技。所有未解锁的科技/升级都会显示买断按钮。
  + 战斗
    - 在任意位置打开黑雾通讯器

## 注意事项

* 如果和[BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)一起使用，请升级`BepInEx`到5.4.21或更高版本，以避免可能的冲突。
  + 你可以在[这里](https://github.com/bepinex/bepinex/releases/latest)（选择x64版本）下载`BepInEx`。
  + 如果使用r2modman，你可以点击`Settings` -> `Browse profile folder`，然后将下载的zip解压到该文件夹并覆盖现有文件。

## 鸣谢

* [戴森球计划](https://store.steampowered.com/app/1366540): 伟大的游戏
* [BepInEx](https://bepinex.dev/): 基础模组框架
* [LSTM](https://github.com/hetima/DSP_LSTM) & [PlanetFinder](https://github.com/hetima/DSP_PlanetFinder): UI实现
* [OffGridConstruction](https://github.com/Velociraptor115-DSPModding/OffGridConstruction): 脱离网格建造以及小角度旋转的实现
* [CruiseAssist](https://dsp.thunderstore.io/package/tanu/CruiseAssist/)及其扩展[AutoPilot](https://dsp.thunderstore.io/package/tanu/AutoPilot/): `航行时自动导航`和`自动巡航`的实现