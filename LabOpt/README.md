# LabOpt

#### Performance optimizations for Matrix Labs
#### 优化研究站性能

## Updates
* 0.3.3
  * Add a lock to `PlanetFactory.PickFrom()` to avoid thread-conflicts. 
* 0.3.2
  * Separate large block locks into small locks to improve performance.
* 0.3.1
  * Add some locks to avoid thread-conflicts.
* 0.3.0
  * Reverse changes of 0.2.0, and rewrite most of codes to make better performance with less patches.
* 0.2.0
  * New mechanism to update `LabComponent.needs()`.
* 0.1.0
  * Initial release.

## Features
* Greatly reduce CPU usage of Matrix Labs without changing gameplay logic.
* Discard some calls on Matrix Labs that are stacked on others.
  * Only keep calls of manufacturing and researching.
* All materials and products are stored in the base level of Matrix Labs.
  * Thus this MOD discards all item pulling up and down actions.
* Manufacturing and researching uses items from base level and output to base level directly.
* UI on Matrix Labs shows count of items in base level always, but displays working progress for current level.
* Insert into or pick from any level of Matrix Labs apply to base level actually.
* Increased capacity input and output of Matrix Labs to 15 and 30, to avoid lack of supply or output jam.

## Known issue
* In researching mode, you will find the progress circle runs faster on stacked Labs
  * This is normal due to mechanism of calculation, it does not change the real consumptions and output hashes.
  * Progress speed is multiplied by stacked levels indeed.

## 功能
* 在不改变游戏基础逻辑的前提下，大幅降低研究站的CPU消耗
* 去除非底层研究站的许多调用
  * 只保留制造和研发的函数调用
* 所有物品都存储在研究站的底层
  * 因此该MOD去除了所有的物品上下传输
* 制造和研发使用底层的物品并将产物直接送到底层
* 研究站的UI始终显示底层的物品数量，但显示当前层的工作进度
* 向任意层放入或取出物品实际上都是对底层的操作
* 增加研究站的输入和输出容量到15和30，以避免输入供应不足和输出堵塞

## 已知问题
* 在研发模式下，你会发现堆叠研究站的进度圈运行得更快
  * 这是正常的，因为计算机制的原因，它并不会改变真实的消耗和产出
  * 进度速度实际上是乘了堆叠层数
