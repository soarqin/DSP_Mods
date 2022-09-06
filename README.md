# DSP Mods by Soar Qin

## [CheatEnabler](CheatEnabler)

### Enable cheat functions as below

* Disable abnormal determinants (Disable all sanity checks, and can get achievements on using Console and DevShortcuts).
* Shift+F4 to switch Developer Mode on/off (no message, tip or sounds on switch).
    * Numpad 1: Gets all items and extends bag.
    * Numpad 2: Boosts walk speed, gathering speed and mecha energy restoration.
    * Numpad 3: Fills planet with foundations and bury all veins.
    * Numpad 4: +1 construction drone.
    * Numpad 5: Upgrades drone engine tech to full.
    * Numpad 6: Unlocks researching tech.
    * Numpad 7: Unlocks Drive Engine 1.
    * Numpad 8: Unlocks Drive Engine 2 and maximize energy.
    * Numpad 9: Unlocks ability to warp.
    * Numpad 0: No costs for Logistic Storages' output.
    * LCtrl + T: Unlocks all techs (not upgrades).
    * LCtrl + A: Resets all local achievements.
    * LCtrl + Q: Adds 10000 to every metadata.
    * LCtrl + W: Enters Sandbox Mode.
    * Numpad *: Proliferates items on hand.
    * Numpad /: Removes proliferations from items on hand.
    * PageDown: Remembers Pose of game camera.
    * PageUp: Locks game camera using remembered Pose.
* Always infinite resource.
* Each function can be enabled individually in config file.

## [LogisticMiner](LogisticMiner)

### Logistic Storages can mine all ores/water on current planet

* Inspired
  by [PlanetMiner](https://dsp.thunderstore.io/package/blacksnipebiu/PlanetMiner)([github](https://github.com/blacksnipebiu/PlanetMiner))
  .
  But it is heavily optimized to resolve performance, accuracy and other issues in PlanetMiner:
    * Only recalculate count of veins when vein chunks are changed (added/removed by foundations/Sandbox-Mode, or
      exhausted), so this removes Dictionary allocation on each planet for every frame.
    * More accurate frame counting by use float number.
    * Does not increase power consumptions on `Veins Utilization` upgrades.
    * Separate power consumptions for veins, oil seeps and water.
    * Power consumptions are counted by groups of veins and count of oil seeps, which is more sensible.
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

## [HideTips](HideTips)

### Hide/Disable various in-game tips/messages

* Tips/messages that can be hidden: random-reminder, tutorial, achievement/milestone card.
* Each type of tips/messages can be configurable individually.
* For sanity check warning messages, please check [CheatEnabler](CheatEnabler)
