# CompressSave

#### Compress game saves to reduce space use and boost save speed
#### Original by [@bluedoom](https://github.com/bluedoom/DSP_Mod)(till 1.1.11) and [@starfi5h](https://github.com/starfi5h/DSP_CompressSave)(1.1.12), I just update it to support latest game version.
#### 压缩游戏存档以降低空间使用并提升保存速度
#### 原作者 [@bluedoom](https://github.com/bluedoom/DSP_Mod)(直到1.1.11) 和 [@starfi5h](https://github.com/starfi5h/DSP_CompressSave)(1.1.12)，本人继续更新以支持最新游戏版本。

## Updates

### 1.3.0
* Separate config entries for manual save and auto save.
* Now you can still get speed benefit while setting compression type to `None` for auto saves, and for manual saves if using the new `Save` button.
  * Adds a `nonewrap.dll` for this function.
* Update `LZ4` and `Zstd` library to latest version.
* `lz4wrap.dll` and `zstdwrap.dll` are compiled using `-O3` instead of `-Os`, expect to be slightly faster but larger.

### 1.2.2
* Fix #4, a bug caused by non-ASCII UTF-8 characters.
* Remove use of Harmony.UnpatchAll() to avoid warnings in BepInEx log.

### 1.2.1
* Simplified codes to display compression type and `Decompress` button on save/load UI, making CompressSave compatible with other MODs(like GalacticScale) which override `UILoadGameWindow::OnSelectedChange()`.
* Add compression level -5 to -1 for zstd, which makes it working better than lz4(which is actually lz4hc used by lz4frame) now:
  * -5 gets faster compression speed than lz4 with still a little better compression ratio.
  * -1 has almost the same speed against lz4 with greater compression ratio.
  * Due to bug of r2modman UI which does not support negative integer, the config value of compression level is not limited any more. 
* move native wrapper DLLs into `x64` folder to avoid warning logs on loading BepInEx plugins.

### 1.2.0
* Match game version 0.9.27.15033.
* Add new compression type: zstd (a bit slower but get better compression ratio than lz4).
* Add config to set compression type and level(Don't use high compression levels for zstd as they are very slow).
* Hide decompress button for normal save files.
* Optimize native dlls for other compression library support:
  * Unified naming rules for filenames and export functions.
  * Add compression level support.

### 1.1.14
* Fix Sandbox info on Save/Load Panel.
* Fix DLL version info.

### 1.1.13

* Match game version 0.9.26.13026.
* Move "Sandbox Mode" checkbox on Save Panel to avoid overlap.
* Avoid warning message on "Continue" button of main menu.

### 1.1.12

* Match game version 0.9.25.12007.

### 1.1.11

* Fix 1.1.10 package issue.

### 1.1.10

* Fix 1.1.8 Archive corruption with DIY System, corrupted archives can be fixed by using \[Fix118\] mod

  Fix118: https://github.com/bluedoom/DSP_Mod/blob/master/Fix118

### 1.1.9

* CompressSave is temporarily disabled due to some error with the DIY system.

### 1.1.8

* Match game version 0.9.24.11029

### 1.1.7

* Fix incorrect data on statistic panel.
* Improve performance.

### 1.1.6

* fix memory leak

### 1.1.5 (Game Version 0.8.22)

* Match game version 0.8.22.
* Thanks [@starfi5h] for
    - PatchSave now use transpiler for better robustness.
    - Change version check to soft warning.
    - Add PeekableReader so other mods can use BinaryReader.PeekChar().
    - Change LZ4DecompressionStream.Position behavior. Position setter i - available now.

### 1.1.4 (Game Version 0.8.19)

* Match game version 0.8.19.

### 1.1.3 (2021/05/29) (Game Version 0.7.18)

* Match game version 0.7.18.
* Fix memory leak.

### 1.1.2 (2021/03/24) (Game Version 0.6.17)

* Handle lz4 library missing Error

### 1.1.1 (2021/03/17) (Game Version 0.6.17)

* Fix Load Error

### 1.1.0 (2021/03/17) (Game Version 0.6.17)

* Add UI button

## Introduction

* Reduce archive size by 30% / save time by 75% (On HDD + i7-4790K@4.4G + DDR3 2400MHz)

  | Before | After |
  | - | - |
  | 50MB 0.8s | 30MB 0.2s |
  | 220M 3.6s | 147M 0.7s |
  | 1010M 19.1s | 690M 3.6s |

## Usage

* You can set compression type for manual saves and auto saves individually.
* Manual saves are compressed while using the new `Save` button.
* You can still get speed benefit while setting compression type to `None` for auto saves, and for manual saves if using the new `Save` button.
* You can decompress saves on load panel.
* Remember to backup your save(use original save button) before updating game to avoid loading failure.

## 介绍

* 减少存档容量30% / 存档用时75% (测试环境：机械硬盘 + i7-4790K@4.4G + DDR3 2400MHz)  

  | 原存档 | 压缩后 |
  | - | - |
  | 50MB 0.8s | 30MB 0.2s |
  | 220M 3.6s | 147M 0.7s |
  | 1010M 19.1s | 690M 3.6s |

## 使用说明

* 手动和自动存档都可以分开设置压缩方式。
* 手动存档使用新加的保存按钮即可压缩保存。
* 即使设置为不压缩，自动存档、以及使用新加的保存按钮手动保存也可以获得速度提升。
* 可以在读取存档面板解压存档。
* 如果游戏有版本更新记得先备份存档(使用原保存按钮)以免更新后无法读取存档。
