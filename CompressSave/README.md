# CompressSave

## Updates

### 1.1.13

* Match game version 0.9.26.13026.
* Move "Sandbox Mode" checkbox on Save Panel to avoid overlap.
* Avoid warning message on "Continue" button of main menu.

### 1.1.12 (by [@starfi5h](https://github.com/starfi5h/DSP_CompressSave))

* Match game version 0.9.25.12007.

### 1.1.11 (by [@bluedoom](https://github.com/bluedoom/DSP_Mod) till this version)

* fix 1.1.10 package issue.

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

* All autosaves are compressed
* Manual saves are compressed while using the new `Save` button.
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

* 所有自动存档都会被压缩。
* 手动存档使用新加的保存按钮即可压缩保存。
* 可以在读取存档面板解压存档。
* 如果游戏有版本更新记得先备份存档(使用原保存按钮)以免更新后无法读取存档。
