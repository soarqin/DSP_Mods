# UniverseGenTweak

#### Universe Generation Tweak
#### 宇宙生成参数调节

## Changelog
* 1.2.6
  + Fix possible crash or wrong stars data when loading save file with changed generation settings but with `Enable more settings on UniverseGen` disabled on config window.
  + Larger maximum value in combat settings (except `Aggressiveness` and `Max Density`).
* 1.2.5
  + Thanks to [kremnev8](https://github.com/kremnev8)'s work on new version of [DSPModSave](https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/), universe generation options are stored in save file now.
  + Fix text display issue on new game screen
* 1.2.4
  + Fix a crash while setting star count greater than 256 (again)
  + Fix bug that collider check is not enabled on stars with ID greater than 255
* 1.2.3
  + Fix a crash while setting star count greater than 256
* 1.2.2
  + Support game version 0.10.28.20759
* 1.2.1
  + Use new tab layout of UXAssist 1.0.2
* 1.2.0
  + Depends on [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist/) now.
  + Add `Birth star` options
  + Config tab added to UXAssist config panel.
* 1.1.0
  + Add epic difficulty
  + `More options` and `Epic difficulty` can be enabled individually now.
  + Fix a crash while setting `Star Distance Min` to larger than `Step Distance Min`.
* 1.0.0
  + Initial release

## Features
* Config entries are placed on [UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist/)'s config panel.
* More options on universe generation
  + Can set maximum star count(128 by default, up to 1024) in config file.
    - Note: there is performance issue on galaxy view with large amount of stars.
  + Larger maximum value in combat settings (except `Aggressiveness` and `Max Density`).
* Epic difficulty
  * 0.01x resources and 0.25x oils (very hard difficulty has 0.5x oils).
  * Same oil mining speed as very hard difficuly
* Birth star
  * Rare resources on birth planet
  * Solid flat on birth planet
  * High luminosity for birth star

## 更新日志
* 1.2.6
  + 修复了在存档中更改了生成参数但是在配置面板中禁用了`启用更多宇宙生成设置`时可能崩溃或者星系数据错误的问题
  + 在星系生成时的战斗设置面板上提升了各选项的最大值(`黑雾攻击性`和`最大黑雾密度`除外`)
* 1.2.5
  + 感谢[kremnev8](https://github.com/kremnev8)对[DSPModSave](https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/)的更新，现在宇宙生成选项会被保存到存档中
  + 修复新建游戏界面文本显示问题
* 1.2.4
  + 修复了设置星系数大于256时崩溃的问题(再次)
  + 修复了ID大于255的星系没有启用碰撞体检测的问题
* 1.2.3
  + 修复了设置星系数大于256时崩溃的问题
* 1.2.2
  + 支持游戏版本0.10.28.20759
* 1.2.1
  + 使用UXAssist 1.0.2的新页签布局
* 1.2.0
  + 现在依赖于[UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist/)
  + 增加`母星系`选项
  + 在UXAssist的配置面板中增加了一个页签
* 1.1.0
  + 增加史诗难度
  + `更多选项`和`史诗难度`现在可以单独启用
  + 修复了将`恒星最小距离`设置为大于`步进最小距离`时崩溃的问题
* 1.0.0
  + 初始版本

## 功能
* 设置选项在[UXAssist](https://dsp.thunderstore.io/package/soarqin/UXAssist/)的配置面板上
* 生成宇宙时提供更多选项
  +可以在配置文件中设置最大恒星数(默认128, 最多1024)
    - 注意: 大量恒星会导致宇宙视图出现性能问题
  + 在星系生成时的战斗设置面板上提升了各选项的最大值(`黑雾攻击性`和`最大黑雾密度`除外`)
* 史诗难度
  * 资源0.01倍，油井储量0.25倍(极难是0.5倍)
  * 采油速度和极难相同
* 母星系
  * 母星有稀有资源
  * 母星是纯平的
  * 母星系恒星高亮
