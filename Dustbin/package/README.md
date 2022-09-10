## Dustbin

### Storages with capacity limited to zero act like dustbins(abandon incoming items)

* Conditions to become dustbin: Storages with capacity limited to zero at top of storage stacks with nothing in 1st cell.
* Items sent into dustbin are removed immediately.
* Can get sands from abandoned items (with factors configurable):
    * Get 100 sands from each fractal silicon ore
    * Get 10 sands from each silicon ore
    * Get nothing from fluids
    * Get 1 sand from any other normal item
* Known bug: stack 1 more storage up on a zero limited one and remove it will cause dustbin stop working. Just put somethings in and take them out to make the dustbin working again.