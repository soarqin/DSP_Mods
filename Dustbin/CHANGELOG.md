## Changelog
* 1.4.0
  * Refactorying Tank input logic codes, for better performance, and resolve a bug [#53](https://github.com/soarqin/DSP_Mods/issues/53)
  + Remove use of AssetBundle, move the belt signal icon into `Assembly Resources`, for better flexibility.

* 1.3.3
  + Support for NebulaMultiplayerModApi 2.0.0

* 1.3.2
  + Fix a display issue that the dustbin checkbox is overlapped with the filter button in storage UI.

* 1.3.1
  + Support for game version 0.10.28.20759

* 1.3.0
  + Add a belt signal(you can find it in first tab of signal selection panel) as dustbin, which is the simplest way to destroy items.
  + Reworked dustbin support for Tanks, to improve performance and resolve known bugs.
    - Be note that the whole tank logic is optimized which may get a slight better performance even if you don't use them as dustbin.
  + Config entry for soil piless gain from destroyed items are changed to a more flexible format.
  + [Nebula Mupltiplayer Mod](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/) and bug fixes from [ModFixerOne](https://dsp.thunderstore.io/package/starfi5h/ModFixerOne/) by [starfi5h](https://github.com/starfi5h/).

* 1.2.1
  + Fix dynamic array bug in codes, which causes various bugs and errors.

* 1.2.0
  + Use [DSPModSave](https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/) to save dustbin specified data now, which fixes [#1](https://github.com/soarqin/DSP_Mods/issues/1).
  + Fix issue for storages on multiple planets.
  + Fix issue for multi-level tanks.
  + Add a note in README for known bug on tank.

* 1.1.0
  + Rewrite whole plugin, make a checkbox on UI so that you can turn storages into dustbin by just ticking it.
  + Can turn tank into dustbin now.

## 更新日志
* 1.4.0
  + 重构储液罐的输入逻辑代码，以提高性能并解决bug [#53](https://github.com/soarqin/DSP_Mods/issues/53)
  + 移除了AssetBundle的使用，将传送带信号图标移入`Assembly资源`，以获得更好的灵活性

* 1.3.3
  + 支持NebulaMultiplayerModApi 2.0.0

* 1.3.2
  + 修正了储物仓UI中的垃圾桶勾选框与筛选按钮重叠的显示问题

* 1.3.1
  + 支持游戏版本 0.10.28.20759

* 1.3.0
  + 添加了一个传送带信号(可以在信号选择面板的第一个页签中找到)作为垃圾桶，这是目前销毁物品最简单的方法
  + 重写了储液罐的垃圾桶实现，以提高性能并解决已知的bug
    - 注意：整个储液罐逻辑都被优化了，即使你不把他们作为垃圾桶使用，也可能会获得轻微的性能提升
  + 从销毁的物品中获得沙子的配置已变为更灵活的设置项格式
  + [Nebula Mupltiplayer Mod](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/)支持和Bug修正来自[starfi5h](https://github.com/starfi5h/)的[ModFixerOne](https://dsp.thunderstore.io/package/starfi5h/ModFixerOne/)

* 1.2.1
  + 修正了代码中的动态数组Bug，该Bug可能导致各种问题

* 1.2.0
  + 现在使用[DSPModSave](https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/)来保存垃圾桶的数据，修正了[#1](https://github.com/soarqin/DSP_Mods/issues/1)
  + 修正了多星球上的储物仓问题
  + 修正了多层储液罐的问题
  + 在README中添加了一个已知储液罐Bug的说明

* 1.1.0
  + 重写了整个插件，现在可以在仓储类建筑的UI上勾选来将其转变为垃圾桶
  + 现在可以将储液罐转变为垃圾桶
