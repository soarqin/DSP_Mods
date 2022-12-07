# Dustbin

#### Can turn Storages and Tanks into Dustbin(Destroy incoming items)
#### 储物仓和储液罐可以转变为垃圾桶(销毁送进的物品)

## Updates

* 1.2.1
  * Fix dynamic array bug in codes, which causes various bugs and errors.

* 1.2.0
  * Use [DSPModSave](https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/) to save dustbin specified data now, which fixes [#1](https://github.com/soarqin/DSP_Mods/issues/1).
  * Fix issue for storages on multiple planets.
  * Fix issue for multi-level tanks.
  * Add a note in README for known bug on tank.

* 1.1.0
  * Rewrite whole plugin, make a checkbox on UI so that you can turn storages into dustbin by just ticking it.
  * Can turn tank into dustbin now.

* 1.0.1
  * Remove a debug log

## Usage

* A checkbox is added to Storages and Tanks UI, which turns them into dustbins.
* Items sent into dustbins are removed immediately.
* Can get sands from destroyed items (with factors configurable):
  * Get 10/100 sands from each silicon/fractal silicon ore
  * Get 1 sand from any other normal item but fluid
* Known bug: Tank with some fluids inside cannot output to belts if turned into dusbin, you can put a dustbin upon a normal tank to resolve this problem. 

## 使用说明

* 在储物仓和储液罐上增加一个垃圾桶的勾选框。
* 送进垃圾桶的物品会立即被移除。
* 可以从移除的物品中获得沙子(可配置，下为默认值):
  * 从硅石和分形硅石中获得10/100个沙子。
  * 从普通物品中获得1个沙子，但液体不会给沙子。
* 已知问题：装有液体的储液罐变为垃圾桶后无法输出液体，可以在普通储液罐上面叠一个垃圾桶来解决这个问题。
