## LogisticMiner

### Logistic Storages can mine all ores/water on current planet

* Inspired
  by [PlanetMiner](https://dsp.thunderstore.io/package/blacksnipebiu/PlanetMiner)([github](https://github.com/blacksnipebiu/PlanetMiner))
  .

  But it is heavily optimized to resolve performance, accuracy and other issues in PlanetMiner.
* Only recalculate count of veins when vein chunks are changed (added/removed by foundations/Sandbox-Mode, or
  exhausted), this removes Dictionary allocation on each planet for every frame which may impact performance.
* More accurate frame counting by use float number.
* Does not increase power consumptions on `Veins Utilization` upgrades.
* Separate power consumptions for veins, oil seeps and water.
* Power consumptions are counted by groups of veins and count of oil seeps, which is more sensible.
* Can burn fuels in certain slot when energy below half of max.
    * Sprayed fuels generates extra energy as normal.
* All used parameters are configurable:
    * ILS has the same speed as normal Mining Machine for normal ores by default.

      But you can set mining scale in configuration, which makes ILS working like Advance Mining Machines: power
      consumption increases by the square of the scale, and gradually decrease mining speed over half of the maximum
      count.

      This applies to all of veins, oils and water.

      Mining scale can be set to 0(by default), which means it is automatically set by tech unlocks, set to 300 when you
      reaches Advanced Mining Machine, otherwise 100.
    * 100/s for water by default.
    * Energy costs: 1MW/vein-group & 10MW/water-slot & 1.8MW/oil-seep(configurable), `Veins Utilization` upgrades
      does not increase power consumption(unlike PlanetMiner).
    * Fuels burning slot. Default: 4th for ILS, 3rd for PLS. Set to 0 to disable it.
