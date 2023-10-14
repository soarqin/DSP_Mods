# CheatEnabler

#### Add various cheat functions while disabling abnormal determinants
#### 添加一些作弊功能，同时屏蔽异常检测

## Changlog
* 2.3.2
  + Birth star options moved to [UniverseGenTweaks](https://dsp.thunderstore.io/package/soarqin/UniverseGenTweaks/)
  + `Fast Mining` ensures full output of oil extractors now.
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

* Config panel is unified with UXAssist, tabs with text between star(*) are configurations for CheatEnabler.
* There are also buttons on title screen and planet minimap area to call up the config panel.
* Features:
  + General:
    + Enable Dev Shortcuts (check config panel for usage)
    + Disable Abnormal Checks
    + Unlock techs with key-modifiers (Ctrl/Alt/Shift)
    + Assign game to currrnet account
  + Factory:
    + Finish build immediately
    + Architect mode (Infinite buildings)
    + Build without condition
    + No collision
    + Belt signal item generation
      + Count all raws and intermediates in statistics
      + Belt signal alt format
    + Remove space limit between wind turbines and solar panels
    + Boost power generations for kinds of power generators
  + Planet:
    + Infinite Natural Resources
    + Fast Mining
    + Pump Anywhere
    + Terraform without enought sands
  + Dyson Sphere:
    + Skip bullet period
    + Skip absorption period
    + Quick absorb
    + Eject anyway

## Notes
* Please upgrade `BepInEx` 5.4.21 or later if using with [BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/) to avoid possible conflicts.
  + You can download `BepInEx` [here](https://github.com/bepinex/bepinex/releases/latest)(choose x64 edition).
  + If using with r2modman, you can upgrade `BepInEx` by clicking `Settings` -> `Browse profile folder`, then extract downloaded zip to the folder and overwrite existing files.

## CREDITS
* [Dyson Sphere Program](https://store.steampowered.com/app/1366540): The great game
* [BepInEx](https://bepinex.dev/): Base modding framework
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): Some cheat functions

## 更新日志
* 2.3.2
  + 母星系的选项移动到了[UniverseGenTweaks](https://dsp.thunderstore.io/package/soarqin/UniverseGenTweaks/)
  + `高速采集`现在可以保证油井的最大产出
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

* 配置面板复用UXAssist，打开UXAssist配置面板后带有星号(*)的分页是CheatEnabler的配置
* 标题界面和行星小地图旁也有按钮呼出主面板。
* 功能：
  + 常规：
    + 启用开发模式快捷键(使用说明见设置面板)
    + 屏蔽异常检测
    + 使用组合键解锁科技（Ctrl/Alt/Shift）
    + 将游戏绑定给当前账号
  + 工厂：
    + 建造秒完成
    + 建筑师模式(无限建筑)
    + 无条件建造
    + 无碰撞
    + 传送带信号物品生成
      + 统计面板中计算所有原材料和中间产物
      + 传送带信号替换格式
    + 风力发电机和太阳能板无间距限制
    + 提升各种发电设备发电量
  + 行星：
    + 自然资源采集不消耗
    + 高速采集
    + 平地抽水
    + 沙土不够时依然可以整改地形
  + 戴森球：
    + 跳过子弹阶段
    + 跳过吸收阶段
    + 快速吸收
    + 全球弹射

## 注意事项
* 如果和[BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)一起使用，请升级`BepInEx`到5.4.21或更高版本，以避免可能的冲突。
  + 你可以在[这里](https://github.com/bepinex/bepinex/releases/latest)（选择x64版本）下载`BepInEx`。
  + 如果使用r2modman，你可以点击`Settings` -> `Browse profile folder`，然后将下载的zip解压到该文件夹并覆盖现有文件。

## 鸣谢
* [戴森球计划](https://store.steampowered.com/app/1366540): 伟大的游戏
* [BepInEx](https://bepinex.dev/): 基础模组框架
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): 一些作弊功能
