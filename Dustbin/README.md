# Dustbin

#### Storages can destroy incoming items while capacity limited to zero
#### 空间限制为0的储物仓可以销毁送进来的物品

## Updates

* 1.0.1
    * Remove a debug log

## Usage

* Conditions to be dustbin: Storages with capacity limited to zero at top of stacks(or only one level), and empty in 1st cell.
* Items sent into dustbins are removed immediately.
* Can get sands from destroyed items (with factors configurable):
    * Get 10/100 sands from each silicon/fractal silicon ore
    * Get 1 sand from any other normal item but fluid
* Known bugs
    * Stack 1 more storage up on a zero limited one and remove it will cause dustbin stop working. Just put somethings
      in and take them out to make the dustbin working again.  
      This is caused by a logic bug in original code where faulty set `lastFullItem` field of `StorageComponent` for
      empty storages.

## 使用说明

* 垃圾桶条件：空间限制为0第一格为空并且放在堆叠顶部(或者只有一层)的储物仓。
* 送进垃圾桶的物品会立即被移除。
* 可以从移除的物品中获得沙子(可配置，下为默认值):
    * 从硅石和分形硅石中获得10/100个沙子。
    * 从普通物品中获得1个沙子，但液体不会给沙子。
* 已知Bug
    * 在空间限制为0的储物仓上面再叠一个储物仓后再移除，会导致垃圾箱功能失效，放一个物品进去再拿出来即可恢复正常。  
      这是原游戏的逻辑Bug错误设置了`StorageComponent`的`lastFullItem`字段导致。
