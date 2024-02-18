# MechaDronesTweaks

#### Some tweaks for mecha drones(Successor to FastDrones MOD)
#### 机甲建设机调整(FastDrones MOD的后继者)

## Updates
* 1.1.4
  + Fixed support for game version 0.10.29

* 1.1.3
  + Support for game version 0.10.28.20759+
  + Fixed a minor bug that `RemoveSpeedLimitForStage1` not working while `UseFixedSpeed` set to false and `SpeedMultiplier` set to 1

* 1.1.2
  + `RemoveBuildRangeLimit`, `LargerAreaForUpgradeAndDismantle` and `LargerAreaForTerraform` are moved to [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist)

* 1.1.1
  + Fixed crash in `LargeAreaForTerraform` functions.

* 1.1.0
  + Added `RemoveBuildRangeLimit`, `LargerAreaForUpgradeAndDismantle` and `LargerAreaForTerraform` options (Check Usage for details).

## Usage
* Inspired by [FastDrones](https://dsp.thunderstore.io/package/dkoppstein/FastDrones/), but patching IL codes, consuming less CPU to reduce lags on massive builds especially blueprints' put.
* Does not affect current game-saves, which means:
  * All values are patched in memory but written to game-saves so that you can play with normal mecha drone parameters while disabling this MOD.
  * You can take benefit from this MOD on any game-saves after enabling this MOD.
* Config entries:
  * `[MechaDrones]`
    * `UseFixedSpeed` [Default Value: false]:
      * If enabled: Use `FixedSpeed` for mecha drones, which makes related Upgrades not used any more.
      * If disabled: Use `SpeedMultiplier` for mecha drones.
    * `SkipStage1` [Default Value: false]: Skip mecha drones' 1st stage (flying away from mecha in ~1/3 speed for several frames).
    * `RemoveSpeedLimitForStage1` [Default Value: true]: Remove speed limit for 1st stage (has a speed limit @ ~10m/s originally).
    * `FixedSpeed` [Default Value: 300]: Fixed flying speed for mecha drones.
    * `SpeedMultiplier` [Default Value: 4]: Speed multiplier for mecha drones.
    * `EnergyMultiplier` [Default Value: 0.1]: Energy consumption multiplier for mecha drones.
* Note: This MOD will disable `FastDrones` if the MOD is installed, to avoid conflict in functions.

## 更新日志
* 1.1.4
  + 修复了对游戏版本0.10.29的支持

* 1.1.3
  + 支持游戏版本0.10.28.20759+
  + 修复了当`UseFixedSpeed`设置为false且`SpeedMultiplier`设置为1时`RemoveSpeedLimitForStage1`无效的问题

* 1.1.2
  + `RemoveBuildRangeLimit`, `LargerAreaForUpgradeAndDismantle` 和 `LargerAreaForTerraform` 移动到了 MOD [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist) 中

* 1.1.1
  + 修复了 `LargeAreaForTerraform` 功能可能导致崩溃的问题。

* 1.1.0
  + 添加了 `RemoveBuildRangeLimit`, `LargerAreaForUpgradeAndDismantle` 和 `LargerAreaForTerraform` 选项 (详情见使用说明)。

## 使用说明
* 功能参考 [FastDrones](https://dsp.thunderstore.io/package/dkoppstein/FastDrones/)，但主要对IL代码进行Patch因此消耗更少的CPU，尤其在大规模建造比如放置蓝图的时候可以大大减少卡顿。
* 不影响当前游戏存档:
  * 所有修改参数都在内存中Patch不会写入存档，禁用此MOD后可恢复正常建设机参数。
  * 启用本MOD后可以在已经游玩的游戏存档上享受参数的改动。
* 设置选项:
  * `[MechaDrones]`
    * `UseFixedSpeed` [默认值: false]:
      * 启用: 使用 `FixedSpeed`，固定速度，这将覆盖对应机甲建设机速度的升级数值。
      * 禁用: 使用 `SpeedMultiplier`
    * `SkipStage1` [默认值: false]: 跳过建设机起飞的第一阶段(以大约1/3速度从机甲身上飞出，持续若干帧).
    * `RemoveSpeedLimitForStage1` [默认值: true]: 移除第一阶段的速度限制 (原本大约有10m/s的限制).
    * `FixedSpeed` [默认值: 300]: 固定速度。
    * `SpeedMultiplier` [默认值: 4]: 速度倍数。
    * `EnergyMultiplier` [默认值: 0.1]: 能量消耗倍数。
* 说明: 如果安装了`FastDrones`本MOD会将其禁用避免功能冲突。
