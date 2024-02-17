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

Save the names of blocks in each slot into a new entry of the 
`MyObjectBuilder_ModStorageComponent` of the block with the slots.
Use a new hardcoded GUID as Mod ID.

### Affected blocks with slots

- `Timer`
- `Sensor`
- `*Cockpit`
- `*Controller`
- `*Button*`

These are the block classes with a `Toolbar` property, but there is no shared interface for this purpose:
```cs
public MyToolbar Toolbar { get; set; }
```

Source search results:
```
Sandbox.Game\Sandbox\Game\Entities\Blocks  (2 usages found)
	MySensorBlock.cs  (1 usage found)
		77 public MyToolbar Toolbar { get; set; }
SpaceEngineers.Game\SpaceEngineers\Game\Entities\Blocks  (4 usages found)
	MyButtonPanel.cs  (1 usage found)
		63 public MyToolbar Toolbar { get; set; }
	MyEventControllerBlock.cs  (1 usage found)
		78 public MyToolbar Toolbar { get; set; }
	MyFlightMovementBlock.cs  (1 usage found)
		130 public MyToolbar Toolbar { get; set; }
	MyTimerBlock.cs  (1 usage found)
		57 public MyToolbar Toolbar { get; set; }
```

The object builder classes have this member:
```cs
public MyObjectBuilder_Toolbar Toolbar;
```

Source search results:
```
SpaceEngineers.ObjectBuilders\Sandbox\Common\ObjectBuilders  (7 usages found)
    MyObjectBuilder_ButtonPanel.cs  (1 usage found)
        23 public MyObjectBuilder_Toolbar Toolbar;
    MyObjectBuilder_DefensiveCombatBlock.cs  (1 usage found)
        49 public MyObjectBuilder_Toolbar Toolbar;
    MyObjectBuilder_EventControllerBlock.cs  (1 usage found)
        23 public MyObjectBuilder_Toolbar Toolbar;
    MyObjectBuilder_SensorBlock.cs  (1 usage found)
        26 public MyObjectBuilder_Toolbar Toolbar;
    MyObjectBuilder_ShipController.cs  (1 usage found)
        34 public MyObjectBuilder_Toolbar Toolbar;
    MyObjectBuilder_TimerBlock.cs  (1 usage found)
        23 public MyObjectBuilder_Toolbar Toolbar;
    MyObjectBuilder_TurretControlBlock.cs  (1 usage found)
        55 public MyObjectBuilder_Toolbar Toolbar;
```

### When to save/load

- Append the names of blocks in slots to `MyObjectBuilder_ModStorageComponent` on saving BPs:
  * `MultigridProjection.GetObjectBuilderOfProjector` for `Ctrl-B` (saving grid as blueprint)
  * `RepairProjection.LoadMechanicalGroup` for the "Load Repair Projection" action on the projector.
- On welding a functional block with slots use the stored values from `MyObjectBuilder_ModStorageComponent`
  to wire up blocks into slots instead of `EntityID` which is always wrong due to remapping.
- On welding any functional block search for slots it may belong to and fill them in.
- Searching of the `BuiltBlock` to wire up by name.
- Make this feature configurable, enable by default.

#### Resolving ambiguity

- Search by name on the same subgrid first. If multiple blocks are found pick the one nearest to the block with the slots.
- Walk the subgrids in breadth-first order until a block is found. If multiple blocks are found pick the one nearest to the root block of that subgrid. 

### Data structures

- Mapping from preview blocks with slots to the preview blocks of functional blocks in each used slot.
- Fill this mapping only once when the BP is loaded, it will not change.
- Create an inverse map to speed up the search from the functional block to the block with the slots.

### Remove existing hack

There is a hack in `MultigridProjection.cs`:

```cs
// Allow rebuilding the blueprint without EntityId collisions without power-cycling the projector,
// relies on the detection of cutting down the built grids by the lack of functional blocks, see
// where requestRemap is set to true
```

The above hack can be removed in favor of this fix.

Any `EntityID` collision has already been resolved by these lines further below anyway:
```cs
// Make sure no EntityId collision will occur on re-welding a block on a previously disconnected (split)
// part of a built subgrid which has not been destroyed (or garbage collected) yet
if (blockBuilder.EntityId != 0 && MyEntityIdentifier.ExistsById(blockBuilder.EntityId))
{
    blockBuilder.EntityId = MyEntityIdentifier.AllocateId();
}
```

### Testing

- It must work in all game modes and with/without MGP installed on server side.
- Expected to work only with BPs saved with MGP active.

### Document

- Players must make a new BP with MGP loaded of any critical BP with the affected block for this to work.
- Document the exact block selection rules, however they should work intuitively most of the time.
