# UXAssist

#### Some functions and patches for better user experience  
#### 一些提升用户体验的功能和补丁

## Changlog
* 1.0.0
  + Initial release

## Usage

* Press `` LAlt+`(BackQuote) `` to call up the config panel. You can change the shortcut on the panel.
* There are also buttons on title screen and planet minimap area to call up the config panel.
* Features:
  + Strict hotkey dectection for build menu, thus building hotkeys(0~9, F1~F10, X, U) are not triggered while holding Ctrl/Alt/Shift.
  + Unlimited interactive range
  + Remove some build conditions
  + Sunlight at night
  + Enable player actions in globe view
  + Re-intialize planet (without reseting veins)
  + Quick dismantle all buildings (without drops)
  + Stop ejectors when available nodes are all filled up
  + Construct only nodes but frames
  + Re-initialize Dyson Spheres
  + Quick dismantle Dyson Shells

## Notes
* Please upgrade `BepInEx` 5.4.21 or later if using with [BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/) to avoid possible conflicts.
  + You can download `BepInEx` [here](https://github.com/bepinex/bepinex/releases/latest)(choose x64 edition).
  + If using with r2modman, you can upgrade `BepInEx` by clicking `Settings` -> `Browse profile folder`, then extract downloaded zip to the folder and overwrite existing files.

## CREDITS
* [Dyson Sphere Program](https://store.steampowered.com/app/1366540): The great game
* [Multifunction_mod](https://github.com/blacksnipebiu/Multifunction_mod): Some cheat functions
* [LSTM](https://github.com/hetima/DSP_LSTM) & [PlanetFinder](https://github.com/hetima/DSP_PlanetFinder): UI implementations

## 更新日志
* 1.0.0
  + 初始版本

## 使用说明

* 按 `` 左Alt+`(反引号) `` 键呼出主面板，可以在面板上修改快捷键。
* 标题界面和行星小地图旁也有按钮呼出主面板。
* 功能：
  + 更严格的建造菜单热键检测，因此在按住Ctrl/Alt/Shift时不再会触发建造热键(0~9, F1~F10, X, U)
  + 无限交互距离
  + 移除部分不影响游戏逻辑的建造条件
  + 夜间日光灯
  + 在行星视图中允许玩家操作
  + 初始化本行星（不重置矿脉）
  + 快速拆除所有建筑（不掉落）
  + 可用节点全部造完时停止弹射
  + 只建造节点不建造框架
  + 初始化戴森球
  + 快速拆除戴森壳

## 注意事项
* 如果和[BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)一起使用，请升级`BepInEx`到5.4.21或更高版本，以避免可能的冲突。
  + 你可以在[这里](https://github.com/bepinex/bepinex/releases/latest)（选择x64版本）下载`BepInEx`。
  + 如果使用r2modman，你可以点击`Settings` -> `Browse profile folder`，然后将下载的zip解压到该文件夹并覆盖现有文件。

## 鸣谢
* [戴森球计划](https://store.steampowered.com/app/1366540): 伟大的游戏
* [BepInEx](https://bepinex.dev/): 基础模组框架
* [LSTM](https://github.com/hetima/DSP_LSTM) & [PlanetFinder](https://github.com/hetima/DSP_PlanetFinder): UI实现
