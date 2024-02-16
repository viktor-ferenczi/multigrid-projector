## Restoring slots

There is a well known issue with welding blueprints, that blocks are not restored
into slots (cockpit, etc) on welding. This happens because the `EntityID` values
are remapped when the blueprint is loaded into the projector to avoid collisions.
While it works for first welding on the main grid, it breaks on subgrids and on
second and further welding of the same block.

This issue forces players to use named groups for all blocks wired to slots in those
cases. It affects re-weldable missiles and bombs with functional blocks on them the 
worst, making them impossible to weld reliably without reloading the projection.

### Solution

Save the names of blocks in each slot into the `CustomData` of blocks with slots. 

### Affected blocks with slots

- `Timer`
- `Sensor`
- `*Cockpit`
- `*Controller`
- `*Button*`

### When to save/load

- Append the names of blocks in slots to `CustomData` on saving BPs.
- If the `CustomData` already has contents, then append a fancy separator and the slot data after it.
- On welding a functional block with slots use the `CustomData` to wire up blocks into slots instead of `EntityID` which is always wrong due to remapping.
- On welding any functional block search for slots it may belong to and fill them in.
- Searching of the `BuiltBlock` to wire up by name.
- Make this feature configurable, enable by default.

#### Resolving ambiguity

- Search by name on the same subgrid first. If multiple blocks are found pick the one nearest to the block with the slots.
- Walk the subgrids in breadth-first order until a block is found. If multiple blocks are found pick the one nearest to the root block of that subgrid. 

### Data structures

- Mapping from preview blocks with slots to the preview blocks of functional blocks in each used slot based on CustomData.
- Fill this mapping only once when the BP is loaded, it will not change.
- Create an inverse map to speed up the search from the functional block to the block with the slots.

### Server or client

- The configuration is client side only, since blueprints are saved there
- Welding does not change, the slot setting happens after welding
- Setting of the slots can happen on client side, so no server side change is required

### Testing

- It must work in all game modes and with/without MGP installed on server side.
- Expected to work only with BPs saved with MGP active.

### Document

- Players must make a new BP with MGP loaded of any critical BP with the affected block for this to work.
- Document the exact block selection rules, however they should work intuitively most of the time.
