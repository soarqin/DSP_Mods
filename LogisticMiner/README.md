# LogisticMiner

#### Logistic Storages can mine all ores/water on current planet

## Usage

* Inspired
  by [PlanetMiner](https://dsp.thunderstore.io/package/blacksnipebiu/PlanetMiner)([github](https://github.com/blacksnipebiu/PlanetMiner))
  .

  But it is heavily optimized to resolve performance, accuracy and other issues in PlanetMiner.
* Only recalculate count of veins when vein chunks are changed (added/removed by foundations/Sandbox-Mode, or
  exhausted), so this removes Dictionary allocation on each planet for every frame.
* More accurate frame counting by use float number.
* Does not increase power consumptions on `Veins Utilization` upgrades.
* Separate power consumptions for veins, oil seeps and water.
* Power consumptions are counted by groups of veins and count of oil seeps, which is more sensible.
* Can burn fuels in certain slot when energy below half of max.
    * Sprayed fuels generates extra energy as normal.
* All used parameters are configurable:
    * Logistic Miner has the same speed as normal Mining Machine for normal ores by default.

      But you can set mining scale in configuration, which makes Logistic Miner working like Advance Mining Machines: power
      consumption increases by the square of the scale, and gradually decrease mining speed over half of the maximum
      count.

      This applies to all of veins, oils and water.

      Mining scale can be set to 0(by default), which means it is automatically set by tech unlocks, set to 300 when you
      reaches Advanced Mining Machine, otherwise 100.
    * 100/s for water by default.
    * Energy costs: 1MW/vein-group & 10MW/water-slot & 1.8MW/oil-seep(configurable), `Veins Utilization` upgrades
      does not increase power consumption(unlike PlanetMiner).
    * Fuels burning slot. Default: 4th for ILS, 3rd for PLS. Set to 0 to disable it.

## 使用说明

* 创意来自 [PlanetMiner](https://dsp.thunderstore.io/package/blacksnipebiu/PlanetMiner)([github](https://github.com/blacksnipebiu/PlanetMiner))

  对性能重度优化，并解决了PlanetMiner的精度等问题。
* (对PlanetMiner的优化) 仅当矿堆发生变化(填埋/恢复/采完)时重新计算矿堆数据，解决每行星每计算帧要重建字典的性能问题。
* (对PlanetMiner的优化) 用浮点数保证更精确的帧计算。
* (对PlanetMiner的优化) 升级`矿物利用`不会提升能耗。
* (对PlanetMiner的优化) 分开矿物，油井和水的采集能耗。
* (对PlanetMiner的优化) 采集能耗以矿物组，油井为单位，相对更加合理。
* 剩余电量少于一半时可以燃烧指定格子的燃料补充。
    * (对PlanetMiner的优化) 喷涂了增产剂的燃料按照正常的计算方式提供更多的能量(除了原本就不增加能量输出的反物质燃料棒)。
* 所有参数都可以在设置文件内配置:
    * 物流塔矿机和普通矿机采矿速度一样(等同于同时采集所有对应矿物)。

      你可以设置采矿倍率改变物流塔矿机采矿速度，和高级采矿机相同地，能耗和倍率的平方成正比，并且在存储矿物量多于一半时逐渐降低采矿倍率。

      此倍率对各种矿物，油井和水的采集都生效。

      倍率可以设置为0(默认)，此时倍率会随科技解锁而变化，默认是100%，解锁高级采矿机后变为300%。
    * 水的采集速度默认为100/s。
    * 能耗：每矿物组 1MW，单格水 10MW，每油井 1.8MW。
    * 燃料格位置。默认：星际物流塔第4格，行星内物流塔第3格。
