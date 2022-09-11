## Dustbin

### Storages can abandon incoming items while capacity limited to zero

* Conditions to be dustbin: Storages with capacity limited to zero at top of stacks, and empty in 1st cell.
* Items sent into dustbins are removed immediately.
* Can get sands from abandoned items (with factors configurable):
    * Get 10/100 sands from each silicon/fractal silicon ore
    * Get 1 sand from any other normal item but fluid
* Known bugs
    * Stack 1 more storage up on a zero limited one and remove it will cause dustbin stop working. Just put somethings
      in and take them out to make the dustbin working again.

      This is caused by a logic bug in original code where faulty set `lastFullItem` field of `StorageComponent` for
      empty storages.