# CheatEnabler

#### Add various cheat functions while disabling abnormal determinants
#### 添加一些作弊功能，同时屏蔽异常检测

## Changlog
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
  + New function: `Assign game to currrnet account`
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

## Usage

* Press `` LAlt+`(BackQuote) `` to call up the config panel. You can change the shortcut on the panel.
* There are also buttons on title screen and planet minimap area to call up the config panel.
* Features:
  + Strict hotkey dectection for build menu, thus building hotkeys(0~9, F1~F10, X, U) are not triggered while holding Ctrl/Alt/Shift.
  + General:
    + Enable Dev Shortcuts (check config panel for usage)
    + Disable Abnormal Checks
    + Unlock techs with key-modifiers (Ctrl/Alt/Shift)
    + Assign game to currrnet account
  + Factory:
    + Finish build immediately
    + Architect mode (Infinite buildings)
    + Unlimited interactive range
    + Build without condition
    + Remove some build conditions
    + No collision
    + Sunlight at night
    + Belt signal item generation
      + Count all raws and intermediates in statistics
      + Belt signal alt format
    + Remove space limit between wind turbines and solar panels
    + Boost power generations for kinds of power generators
  + Planet:
    + Enable player actions in globe view
    + Infinite Natural Resources
    + Fast Mining
    + Pump Anywhere
    + Terraform without enought sands
    + Re-intialize planet (without reseting veins)
    + Quick dismantle all buildings (without drops)
  + Dyson Sphere:
    + Stop ejectors when available nodes are all filled up   
    + Skip bullet period
    + Skip absorption period
    + Quick absorb
    + Eject anyway
    + Re-initialize Dyson Spheres
    + Quick dismantle Dyson Shells
  + Birth star:
    + Rare resources on birth planet
    + Solid flat on birth planet
    + High luminosity for birth star

## Notes
* Please upgrade `BepInEx` 5.4.21 or later if using with [BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/) to avoid possible conflicts.
  + You can download `BepInEx` [here](https://github.com/bepinex/bepinex/releases/latest)(choose x64 edition).
  + If using with r2modman, you can upgrade `BepInEx` by clicking `Settings` -> `Browse profile folder`, then extract downloaded zip to the folder and overwrite existing files.

## CREDITS
* [Dyson Sphere Program](https://store.steampowered.com/app/1366540): The great game
* [BepInEx](https://bepinex.dev/): Base modding framework
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): Some cheat functions
* [LSTM](https://github.com/hetima/DSP_LSTM) & [PlanetFinder](https://github.com/hetima/DSP_PlanetFinder): UI implementations

## 更新日志
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
  + 新功能：`将游戏绑定给当前账号`
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

## 使用说明

* 按 `` 左Alt+`(反引号) `` 键呼出主面板，可以在面板上修改快捷键。
* 标题界面和行星小地图旁也有按钮呼出主面板。
* 功能：
  + 更严格的建造菜单热键检测，因此在按住Ctrl/Alt/Shift时不再会触发建造热键(0~9, F1~F10, X, U)
  + 常规： 
    + 启用开发模式快捷键(使用说明见设置面板)
    + 屏蔽异常检测
    + 使用组合键解锁科技（Ctrl/Alt/Shift）
    + 将游戏绑定给当前账号
  + 工厂：
    + 建造秒完成
    + 建筑师模式(无限建筑)
    + 无限交互距离
    + 无条件建造
    + 移除部分不影响游戏逻辑的建造条件
    + 无碰撞
    + 夜间日光灯
    + 传送带信号物品生成
      + 统计面板中计算所有原材料和中间产物
      + 传送带信号替换格式
    + 风力发电机和太阳能板无间距限制
    + 提升各种发电设备发电量
  + 行星：
    + 在行星视图中允许玩家操作
    + 自然资源采集不消耗
    + 高速采集
    + 平地抽水
    + 沙土不够时依然可以整改地形
    + 初始化本行星（不重置矿脉）
    + 快速拆除所有建筑（不掉落）
  + 戴森球：
    + 可用节点全部造完时停止弹射
    + 跳过子弹阶段
    + 跳过吸收阶段
    + 快速吸收
    + 全球弹射
    + 初始化戴森球
    + 快速拆除戴森壳
  + 母星系：
    + 母星有稀有资源
    + 母星是纯平的
    + 母星系恒星高亮

## 注意事项
* 如果和[BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)一起使用，请升级`BepInEx`到5.4.21或更高版本，以避免可能的冲突。
  + 你可以在[这里](https://github.com/bepinex/bepinex/releases/latest)（选择x64版本）下载`BepInEx`。
  + 如果使用r2modman，你可以点击`Settings` -> `Browse profile folder`，然后将下载的zip解压到该文件夹并覆盖现有文件。

## 鸣谢
* [戴森球计划](https://store.steampowered.com/app/1366540): 伟大的游戏
* [BepInEx](https://bepinex.dev/): 基础模组框架
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): 一些作弊功能
* [LSTM](https://github.com/hetima/DSP_LSTM) & [PlanetFinder](https://github.com/hetima/DSP_PlanetFinder): UI实现
