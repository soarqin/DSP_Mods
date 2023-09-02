# PoolOpt

#### Optimize memory pools on loading gamesaves
#### 加载游戏存档时优化内存池的使用

# Notes
* Does not optimize some rarely uses pools, mostly UI related.
* Does not optimize solar sails' pool, please use button on dyson sphere panel from original game to clean it up.

# Mechanism
* The game uses a lot of memory pools in array to store data, sizes and capacities of these pools are increased on demand but never decreased, even most data inside are not used due to dismantle objects. Unused slot IDs are save in recycle array for reuse.
* Some game functions loop through all allocated slots in the pool, so the size of the pool will affect the performance.
* This mod will optimize the memory pools when loading gamesaves, strip the unused slots from tail and reduce the size of the pool and recycle array, for better performance.

# TODO
* Remove the unused slots from middle of the pool, which leads to ID change for stored objects, needs more investigation.

# 说明
* 不会优化一些很少使用的内存池，主要是UI相关的。
* 不会优化太阳帆的内存池，请使用原版游戏中的戴森球面板上的按钮进行清理。

# 原理
* 游戏使用了大量的内存池数组来存储数据，这些内存池的大小和容量是只会随需求膨胀不会收缩的，即使由于拆除物体导致内部大多数数据不再使用。而未使用的槽位ID会被保存在回收数组中以供重复使用。
* 一些游戏功能会循环遍历内存池中的所有分配过的槽位，因此内存池的大小会影响性能。
* 本MOD会在加载游戏存档时优化内存池，去除尾部未使用的槽位，减少内存池数组和回收数组的大小，以获得更好的性能。

# TODO
* 去除内存池中间的未使用槽位，这会导致物件的存储ID发生变化，需要进一步调研。