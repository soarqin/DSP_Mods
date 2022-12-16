# OrbitalCollectorBatchBuild

#### Batch build Orbital Collectors
#### 轨道采集器快速批量建造

## Updates
### 1.2.0
* Support for mods that change default radius of Gas Planets, e.g. `GalacticScale`.
* Remove maximum build count limit in config.

### 1.1.0
* Add `InstantBuild` to config

## Usage
* Build any orbital collector on a Gas Giant to trigger building all placable orbital collectors.
* Can set maximum orbital collectors to build once in config.
* Note: Collectors are placed as prebuilt status, you still need to fly around the Gas Giant to complete building. This is designed not to break much game logic.  
  You can set `InstantBuild` to `true` in config to make them built instantly.

## 使用说明
* 在气态星球上建造任何一个轨道采集器触发所有可建造采集器的放置。
* 可以在设置文件中配置一次批量建造的最大轨道采集器数量。
* 提示：轨道采集器会处于待建造状态，你仍然需要绕行气态行星一圈以完成建造。这个机制是为了尽可能减少对原有游戏逻辑的破坏。  
  在配置文件里设置`InstantBuild`为`true`可以使采集器的建造立即完成。