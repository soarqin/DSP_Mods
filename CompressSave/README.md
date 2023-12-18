# CompressSave

#### Compress game saves to reduce space use and boost save speed
#### Original by [@bluedoom](https://github.com/bluedoom/DSP_Mod)(till 1.1.11) and [@starfi5h](https://github.com/starfi5h/DSP_CompressSave)(1.1.12), I just update it to support latest game version.
#### 压缩游戏存档以降低空间使用并提升保存速度
#### 原作者 [@bluedoom](https://github.com/bluedoom/DSP_Mod)(直到1.1.11) 和 [@starfi5h](https://github.com/starfi5h/DSP_CompressSave)(1.1.12)，本人继续更新以支持最新游戏版本。

## Changelog

### 1.3.5
* Fix a crash issue on choosing language other than English and Chinese.

### 1.3.4
* Support for game version 0.10.28.20759.

### 1.3.3
* Fix a display issue on combobox of compression type.

### 1.3.2
* Add config UI on Save Game dialog, to set compression types.
* Change button text to `Save (Compress)` for better understanding.

### 1.3.1
* Add config to disable feature for auto saves.
* Fix bug that first save after game start is always compressed in Zstd.

### 1.3.0
* Separate config entries for manual save and auto save.
* Now you can still get speed benefit while setting compression type to `None` for auto saves, and for manual saves if using the new `Save` button.
  + Adds a `nonewrap.dll` for this function.
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

* Reduce archive size by 30% / save time by 75% (Compressed by LZ4, on HDD + i7-4790K@4.4G + DDR3 2400MHz)

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

## 更新日志

### 1.3.5
* 修复了选择英文和中文以外的语言时的崩溃问题。

### 1.3.4
* 支持游戏版本 0.10.28.20759。

### 1.3.3
* 修复压缩类型下拉框显示问题。

### 1.3.2
* 在保存面板上增加设置压缩方式的UI。
* 将按钮文本改为`压缩保存`以区分功能。

### 1.3.1
* 增加在自动存档中禁用压缩的设置项。
* 修复一个导致游戏开始后第一次保存总是使用Zstd压缩的bug。

### 1.3.0
* 分离手动存档和自动存档的设置项。
* 现在在自动存档设置压缩类型为`存储`也可以获得速度提升，手动存档也可以在使用新的`保存`按钮后获得速度提升。
  + 为此增加了`nonewrap.dll`。
* 更新`LZ4`和`Zstd`库到最新版本。
* `lz4wrap.dll`和`zstdwrap.dll`使用`-O3`编译而不是`-Os`，速度略有提升但体积变大。

### 1.2.2
* 修复 #4，一个导致非ASCII UTF-8字符导致的bug。
* 移除使用Harmony.UnpatchAll()以避免在BepInEx日志中出现警告。

### 1.2.1
* 简化代码以在存档读取面板上显示压缩类型和`解压`按钮，使得CompressSave与其他MOD(如GalacticScale)兼容，因为它们都覆盖了`UILoadGameWindow::OnSelectedChange()`。
* 为zstd添加了压缩等级-5到-1，现在它比lz4(实际上是lz4frame)表现更好了：
  * -5比lz4更快，但压缩比略有提升。
  * -1和lz4几乎一样快，但压缩比更高。
  * 由于r2modman UI的bug，压缩等级的设置项不再限制范围。
* 将本地的wrapper DLL移动到`x64`

## 介绍

* 减少存档容量30% / 存档用时75% (LZ4压缩，测试环境：机械硬盘 + i7-4790K@4.4G + DDR3 2400MHz)  

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
