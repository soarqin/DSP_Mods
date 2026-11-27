# Changelog / 更新日志

<details>
<summary>Read in English</summary>

This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed

- Move the storage dustbin checkbox to the title bar to prevent overlap with the English automation input label.

## [1.5.0] - 2026-10-31

### Fixed

- `Storage dustbin`: Destroy splitter input to a storage placed above it, including stacked and proliferated cargo. Enabling dustbin mode also clears cached full-storage state.
- `Tank dustbin`: Fix single tanks blocking liquid input after receiving their first stack, both with and without dustbin mode.
- `Belt signal dustbin`: Restore item disposal and startup compatibility with the current game simulation loop after DSP 0.10.33/0.10.34 updates.

## [1.4.0] - 2025-03-25

### Changed

- Refactorying Tank input logic codes, for better performance, and resolve a bug [#53](https://github.com/soarqin/DSP_Mods/issues/53)
- Remove use of AssetBundle, move the belt signal icon into `Assembly Resources`, for better flexibility.

## [1.3.3] - 2024-02-19

### Changed

- Support for NebulaMultiplayerModApi 2.0.0

## [1.3.2] - 2023-12-18

### Fixed

- Fix a display issue that the dustbin checkbox is overlapped with the filter button in storage UI.

## [1.3.1] - 2023-12-15

### Changed

- Support for game version 0.10.28.20759

## [1.3.0] - 2023-11-04

### Added

- Add a belt signal(you can find it in first tab of signal selection panel) as dustbin, which is the simplest way to destroy items.

### Changed

- Reworked dustbin support for Tanks, to improve performance and resolve known bugs.
  * Be note that the whole tank logic is optimized which may get a slight better performance even if you don't use them as dustbin.
- Config entry for soil piless gain from destroyed items are changed to a more flexible format.
- [Nebula Mupltiplayer Mod](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/) and bug fixes from [ModFixerOne](https://dsp.thunderstore.io/package/starfi5h/ModFixerOne/) by [starfi5h](https://github.com/starfi5h/).

## [1.2.1] - 2022-12-07

### Fixed

- Fix dynamic array bug in codes, which causes various bugs and errors.

## [1.2.0] - 2022-12-06

### Added

- Add a note in README for known bug on tank.

### Changed

- Use [DSPModSave](https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/) to save dustbin specified data now, which fixes [#1](https://github.com/soarqin/DSP_Mods/issues/1).

### Fixed

- Fix issue for storages on multiple planets.
- Fix issue for multi-level tanks.

## [1.1.0] - 2022-11-27

### Added

- Can turn tank into dustbin now.

### Changed

- Rewrite whole plugin, make a checkbox on UI so that you can turn storages into dustbin by just ticking it.

## [1.0.1] - 2022-09-11

### Removed

- Remove a debug log

## [1.0.0] - 2022-09-11

### Added

- Initial release. A storage at the top of a stack can destroy incoming items when its capacity is set to zero and its first slot is empty.

</details>

<details>
<summary>简体中文</summary>

本更新日志采用 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 格式。

## [Unreleased]

### 修复

- 将储物仓垃圾桶的复选框移至标题栏，避免与英文自动化输入说明重叠。

## [1.5.0] - 2026-10-31

### 修复

- `储物仓垃圾桶`：销毁四向分流器送入上方储物仓的物品，包括堆叠和喷涂增产剂的物品；启用垃圾桶时清除已满状态缓存。
- `储液罐垃圾桶`：修复单层储液罐装入第一批液体后停止输入的问题，普通储存和垃圾桶模式均可正常工作。
- `传送带信号垃圾桶`：适配当前游戏的模拟循环，修复 DSP 0.10.33/0.10.34 更新后的启动报错和物品无法销毁问题。

## [1.4.0] - 2025-03-25

### 变更

- 重构储液罐的输入逻辑代码，以提高性能并解决bug [#53](https://github.com/soarqin/DSP_Mods/issues/53)
- 移除了AssetBundle的使用，将传送带信号图标移入`Assembly资源`，以获得更好的灵活性

## [1.3.3] - 2024-02-19

### 变更

- 支持NebulaMultiplayerModApi 2.0.0

## [1.3.2] - 2023-12-18

### 修复

- 修正了储物仓UI中的垃圾桶勾选框与筛选按钮重叠的显示问题

## [1.3.1] - 2023-12-15

### 变更

- 支持游戏版本 0.10.28.20759

## [1.3.0] - 2023-11-04

### 新增

- 添加了一个传送带信号(可以在信号选择面板的第一个页签中找到)作为垃圾桶，这是目前销毁物品最简单的方法

### 变更

- 重写了储液罐的垃圾桶实现，以提高性能并解决已知的bug
  * 注意：整个储液罐逻辑都被优化了，即使你不把他们作为垃圾桶使用，也可能会获得轻微的性能提升
- 从销毁的物品中获得沙子的配置已变为更灵活的设置项格式
- [Nebula Mupltiplayer Mod](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/)支持和Bug修正来自[starfi5h](https://github.com/starfi5h/)的[ModFixerOne](https://dsp.thunderstore.io/package/starfi5h/ModFixerOne/)

## [1.2.1] - 2022-12-07

### 修复

- 修正了代码中的动态数组Bug，该Bug可能导致各种问题

## [1.2.0] - 2022-12-06

### 新增

- 在README中添加了一个已知储液罐Bug的说明

### 变更

- 现在使用[DSPModSave](https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/)来保存垃圾桶的数据，修正了[#1](https://github.com/soarqin/DSP_Mods/issues/1)

### 修复

- 修正了多星球上的储物仓问题
- 修正了多层储液罐的问题

## [1.1.0] - 2022-11-27

### 新增

- 现在可以将储液罐转变为垃圾桶

### 变更

- 重写了整个插件，现在可以在仓储类建筑的UI上勾选来将其转变为垃圾桶

## [1.0.1] - 2022-09-11

### 移除

- 移除一条调试日志

## [1.0.0] - 2022-09-11

### 新增

- 初始版本。堆叠顶部的储物仓在容量上限设为 0 且第一格为空时，可销毁送入的物品。

</details>

[Unreleased]: https://github.com/soarqin/DSP_Mods/compare/8fbb244...master
[1.5.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.5.0/
[1.4.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.4.0/
[1.3.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.3.3/
[1.3.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.3.2/
[1.3.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.3.1/
[1.3.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.3.0/
[1.2.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.2.1/
[1.2.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.2.0/
[1.1.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.1.0/
[1.0.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.0.1/
[1.0.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/Dustbin/v/1.0.0/
