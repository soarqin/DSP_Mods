# Changelog / 更新日志

<details>
<summary>Read in English</summary>

This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Other

- Update integration with UXAssist for universe generation settings.

## [1.2.11] - 2025-10-20

### Fixed

- Fix issues which cause universe settings not applied on staring a new game (again again).

## [1.2.10] - 2025-09-16

### Changed

- Reset star distance/flatten settings on entering galaxy creation screen.

### Fixed

- Fix issues which cause universe settings not applied on staring a new game or loading game (again).

## [1.2.9] - 2025-09-10

### Changed

- Always show Black hole/Neutron star on preview galaxy map while creating new game.

### Fixed

- Fix some issues that universe settings not applied on staring a new game or loading game.

## [1.2.8] - 2024-12-01

### Fixed

- Fix compatiblity for game version 0.10.31.24632.

## [1.2.7] - 2024-05-30

### Fixed

- `Epic difficulty`: Fix compatibility for 0.10.30, with new resource multiplier 0.3x supported.

## 1.2.6 (No public release recorded)

### Changed

- Larger maximum value in combat settings (except `Aggressiveness` and `Max Density`).

### Fixed

- Fix possible crash or wrong stars data when loading save file with changed generation settings but with `Enable more settings on UniverseGen` disabled on config window.

## [1.2.5] - 2023-12-31

### Changed

- Thanks to [kremnev8](https://github.com/kremnev8)'s work on new version of [DSPModSave](https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/), universe generation options are stored in save file now.

### Fixed

- Fix text display issue on new game screen

## [1.2.4] - 2023-12-17

### Fixed

- Fix a crash while setting star count greater than 256 (again)
- Fix bug that collider check is not enabled on stars with ID greater than 255

## [1.2.3] - 2023-12-16

### Fixed

- Fix a crash while setting star count greater than 256

## [1.2.2] - 2023-12-15

### Changed

- Support game version 0.10.28.20759

## [1.2.1] - 2023-10-22

### Changed

- Use new tab layout of UXAssist 1.0.2

## [1.2.0] - 2023-10-16

### Added

- Add `Birth star` options
- Config tab added to UXAssist config panel.

### Changed

- Depends on [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist/) now.

## [1.1.0] - 2023-09-05

### Added

- Add epic difficulty

### Changed

- `More options` and `Epic difficulty` can be enabled individually now.

### Fixed

- Fix a crash while setting `Star Distance Min` to larger than `Step Distance Min`.

## [1.0.0] - 2022-11-07

### Added

- Initial release

</details>

<details>
<summary>简体中文</summary>

本更新日志采用 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 格式。

## [Unreleased]

### 其他

- 更新宇宙生成设置与 UXAssist 的集成。

## [1.2.11] - 2025-10-20

### 修复

- (再次再次)修复新建游戏时宇宙设置没有应用的问题

## [1.2.10] - 2025-09-16

### 变更

- 进入宇宙创建界面时重置星系间距/扁平度设置

### 修复

- (再次)修复新建游戏或加载存档时宇宙设置没有应用的问题

## [1.2.9] - 2025-09-10

### 变更

- 在新建游戏时的星系预览图中始终显示黑洞/中子星

### 修复

- 修复了一些在新建游戏或加载存档时宇宙设置没有应用的问题

## [1.2.8] - 2024-12-01

### 修复

- 修复了对游戏版本0.10.31.24632的兼容性

## [1.2.7] - 2024-05-30

### 修复

- `史诗难度`: 修复了对0.10.30版本的兼容性，支持新的资源倍率0.3x

## 1.2.6 (无公开发布记录)

### 变更

- 在星系生成时的战斗设置面板上提升了各选项的最大值(`黑雾攻击性`和`最大黑雾密度`除外`)

### 修复

- 修复了在存档中更改了生成参数但是在配置面板中禁用了`启用更多宇宙生成设置`时可能崩溃或者星系数据错误的问题

## [1.2.5] - 2023-12-31

### 变更

- 感谢[kremnev8](https://github.com/kremnev8)对[DSPModSave](https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/)的更新，现在宇宙生成选项会被保存到存档中

### 修复

- 修复新建游戏界面文本显示问题

## [1.2.4] - 2023-12-17

### 修复

- 修复了设置星系数大于256时崩溃的问题(再次)
- 修复了ID大于255的星系没有启用碰撞体检测的问题

## [1.2.3] - 2023-12-16

### 修复

- 修复了设置星系数大于256时崩溃的问题

## [1.2.2] - 2023-12-15

### 变更

- 支持游戏版本0.10.28.20759

## [1.2.1] - 2023-10-22

### 变更

- 使用UXAssist 1.0.2的新页签布局

## [1.2.0] - 2023-10-16

### 新增

- 增加`母星系`选项
- 在UXAssist的配置面板中增加了一个页签

### 变更

- 现在依赖于[UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist/)

## [1.1.0] - 2023-09-05

### 新增

- 增加史诗难度

### 变更

- `更多选项`和`史诗难度`现在可以单独启用

### 修复

- 修复了将`恒星最小距离`设置为大于`步进最小距离`时崩溃的问题

## [1.0.0] - 2022-11-07

### 新增

- 初始版本

</details>

[Unreleased]: https://github.com/soarqin/DSP_Mods/compare/77f5803...master
[1.2.11]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.11/
[1.2.10]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.10/
[1.2.9]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.9/
[1.2.8]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.8/
[1.2.7]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.7/
[1.2.5]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.5/
[1.2.4]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.4/
[1.2.3]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.3/
[1.2.2]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.2/
[1.2.1]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.1/
[1.2.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.2.0/
[1.1.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.1.0/
[1.0.0]: https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UniverseGenTweaks/v/1.0.0/
