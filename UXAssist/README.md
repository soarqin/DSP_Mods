# UXAssist

#### Some functions and patches for better user experience
#### 一些提升用户体验的功能和补丁

## Changlog
* 1.0.8
  + New function: `Enhanced control for logistic storage limits`
* 1.0.7
  + Fix a crash issue on choosing language other than English and Chinese
  + Games saved in Peace-Mode after Dark-Forg update can also be loaded as Combat-Mode now.
* 1.0.6
  + Convert old saves to Combat-Mode on loading
* 1.0.5
  + Support game version 0.10.28.20759
  + Sort blueprint structures before saving, to reduce generated blueprint data size a little.
* 1.0.4
  + Add new function: `Off-grid building and stepped rotation`
  + Fix an issue that window position not restored and can not be resized when function is enabled but game is started with different mod profiles.  
* 1.0.3
  + Add new function: `Quick build Orbital Collectors`. 
  + Add confirmation popup for `Re-intialize planet`, `Quick dismantle all buildings`, `Re-initialize Dyson Spheres` and `Quick dismantle Dyson Shells`.
  + Fix error on `Remove build count and range limit` when building a large amount of belts.
  + Fix an issue that window position not saved correctly when quit game without using in-game menu.
* 1.0.2
  + Redesign config tabs, for clearer layout.
  + Add 2 new options: 
    - Enable game window resize.
    - Remember window position and size on last exit.
* 1.0.1
  + Fix config button text and tips while returning to title menu.
  + Fix that error occurs while returning to title menu, with `Stop ejectors when available nodes are all filled up` enabled.
  + Add a patch to fix the bug that warning popup on `Veins Utilization` upgraded to level 8000+.
* 1.0.0
  + Initial release
  + Functions moved from [MechaDronesTweaks](https://dsp.thunderstore.io/package/soarqin/MechaDronesTweaks/) and [CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/)

## Usage

* Press `` LAlt+`(BackQuote) `` to call up the config panel. You can change the shortcut on the panel.
* There are also buttons on title screen and planet minimap area to call up the config panel.
* Patches:
  + Strict hotkey dectection for build menu, thus building hotkeys(0~9, F1~F10, X, U) are not triggered while holding Ctrl/Alt/Shift.
  + Fix a bug that warning popup on `Veins Utilization` upgraded to level 8000+
  + Sort blueprint structures before saving, to reduce generated blueprint data size a little
* Features:
  + General 
    - Enable game window resize
    - Remember window position and size on last exit
    - Convert Peace-Mode saves to Combat-Mode on loading
  + Planet/Factory
    - Unlimited interactive range
    - Sunlight at night
    - Remove some build conditions
    - Remove build count and range limit
    - Larger area for upgrade and dismantle(30x30 at max)
    - Larger area for terraform(30x30 at max)
    - Enable player actions in globe view
    - Enhanced control for logistic storage limits
      - Logistic storage limits are not scaled on upgrading `Logistics Carrier Capacity`, if they are not set to maximum capacity.
      - You can use arrow keys to adjust logistic storage limits gracefully.
    - Enhanced count control for hand-make
    - Re-intialize planet (without reseting veins)
    - Quick dismantle all buildings (without drops)
    - Quick build Orbital Collectors
  + Dyson Sphere
    - Stop ejectors when available nodes are all filled up
    - Construct only nodes but frames
    - Re-initialize Dyson Spheres
    - Quick dismantle Dyson Shells

## Notes
* Please upgrade `BepInEx` 5.4.21 or later if using with [BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/) to avoid possible conflicts.
  + You can download `BepInEx` [here](https://github.com/bepinex/bepinex/releases/latest)(choose x64 edition).
  + If using with r2modman, you can upgrade `BepInEx` by clicking `Settings` -> `Browse profile folder`, then extract downloaded zip to the folder and overwrite existing files.

## CREDITS
* [Dyson Sphere Program](https://store.steampowered.com/app/1366540): The great game
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): Some cheat functions
* [LSTM](https://github.com/hetima/DSP_LSTM) & [PlanetFinder](https://github.com/hetima/DSP_PlanetFinder): UI implementations
* [OffGridConstruction](https://github.com/Velociraptor115-DSPModding/OffGridConstruction): Off-grid building & stepped rotation implementations

## 更新日志
* 1.0.8
  + 新功能：`物流塔存储数量限制控制改进`
* 1.0.7
  + 修复了选择英文和中文以外的语言时的崩溃问题
  + 黑雾更新后使用和平模式保存的存档现在也可以转换为战斗模式了
* 1.0.6
  + 在加载旧存档时将其转换为战斗模式
* 1.0.5
  + 支持游戏版本0.10.28.20759
  + 保存蓝图前对建筑进行排序，以减少生成的蓝图数据大小
* 1.0.4
  + 添加了新功能：`脱离网格建造和小角度旋转`
  + 修复了当功能启用但游戏使用不同的mod配置文件启动时窗口位置无法正确恢复和不可拖动改变大小的问题
* 1.0.3
  + 添加了新功能：`快速建造轨道采集器`
  + 为`初始化行星`，`快速拆除所有建筑`，`初始化戴森球`和`快速拆除戴森壳`添加了确认弹窗
  + 修复了`移除建造数量和范围限制`在建造大量传送带时可能导致的错误
  + 修复了在不使用游戏内菜单退出游戏时窗口位置无法正确保存的问题
* 1.0.2
  + 重新设计了配置面板，使布局更清晰
  + 添加了两个新选项：
    - 可调整游戏窗口大小(可最大化和拖动边框)
    - 记住上次退出时的窗口位置和大小
* 1.0.1
  + 修复了返回标题界面后设置按钮文本和提示信息不正确的问题
  + 修复了`当可用节点全部造完时停止弹射`选项启用时返回标题界面可能导致崩溃的问题
  + 添加了一个补丁，修复了`矿物利用`升级到8000级以上时弹出警告的bug
* 1.0.0
  + 初始版本
  + 从[MechaDronesTweaks](https://dsp.thunderstore.io/package/soarqin/MechaDronesTweaks/)和[CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/)移动了部分功能过来

## 使用说明

* 按 `` 左Alt+`(反引号) `` 键呼出主面板，可以在面板上修改快捷键。
* 标题界面和行星小地图旁也有按钮呼出主面板。
* 补丁：
  + 更严格的建造菜单热键检测，因此在按住Ctrl/Alt/Shift时不再会触发建造热键(0~9, F1~F10, X, U)
  + 修复了`矿物利用`升级到8000级以上时弹出警告的bug
  + 保存蓝图前对建筑进行排序，以减少生成的蓝图数据大小
* 功能：
  + 通用
    - 可调整游戏窗口大小(可最大化和拖动边框)
    - 记住上次退出时的窗口位置和大小
    - 在加载和平模式存档时将其转换为战斗模式
  + 行星/工厂
    - 无限交互距离
    - 夜间日光灯
    - 移除部分不影响游戏逻辑的建造条件
    - 移除建造数量和范围限制
    - 范围升级和拆除的最大区域扩大(最大30x30)
    - 范围铺设地基的最大区域扩大(最大30x30)
    - 在行星视图中允许玩家操作
    - 物流塔存储数量限制控制改进
      - 当升级`运输机舱扩容`时，不会对各种物流塔的存储限制按比例提升，除非设置为最大允许容量。
      - 你可以使用方向键微调物流塔存储限制
    - 手动制造物品的数量控制改进
    - 初始化本行星（不重置矿脉）
    - 快速拆除所有建筑（不掉落）
    - 快速建造轨道采集器
  + 戴森球
    - 可用节点全部造完时停止弹射
    - 只建造节点不建造框架
    - 初始化戴森球
    - 快速拆除戴森壳

## 注意事项
* 如果和[BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)一起使用，请升级`BepInEx`到5.4.21或更高版本，以避免可能的冲突。
  + 你可以在[这里](https://github.com/bepinex/bepinex/releases/latest)（选择x64版本）下载`BepInEx`。
  + 如果使用r2modman，你可以点击`Settings` -> `Browse profile folder`，然后将下载的zip解压到该文件夹并覆盖现有文件。

## 鸣谢
* [戴森球计划](https://store.steampowered.com/app/1366540): 伟大的游戏
* [BepInEx](https://bepinex.dev/): 基础模组框架
* [LSTM](https://github.com/hetima/DSP_LSTM) & [PlanetFinder](https://github.com/hetima/DSP_PlanetFinder): UI实现
* [OffGridConstruction](https://github.com/Velociraptor115-DSPModding/OffGridConstruction): 脱离网格建造以及小角度旋转的实现
