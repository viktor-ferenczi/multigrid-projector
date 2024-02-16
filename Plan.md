## Problem

Multi-grid blueprints saved from vanilla SE have any toolbar slots with a block
from another subgrid broken due to inconsistent EntityId mapping, unless the
MGP plugin is used while producing the blueprint. In that case the EntityId
values are correct.

On welding the same terminal block (with slots or the ones associated to slots)
the second time from the same projection a new EntityId is assigned if the
previous one still exists in the world. The workaround is to power-cycle the
projector between welding attempts, but it still breaks in cases where there
are already built blocks to be consistent with. This affects mostly missiles
built from the repair projection of the whole ship, but can hit any player
if welding is interrupted and had to be restarted for some blocks.

### Solution

On loading the blueprint find all toolbar slots with assigned blocks and
store those associations based on their subgrid index and block location.
Build the mapping from the block to the toolbar slot.

After building a terminal block from projection find all terminal blocks
(maybe including itself) which has the newly built block in a toolbar slot
and fix those slots.

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
	MyTargetDummyBlock.cs  (1 usage found)
		79 public MyToolbar Toolbar { get; set; }
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

### Cleanup

Remove this hack in `MultigridProjection.cs`:

```cs
// Allow rebuilding the blueprint without EntityId collisions without power-cycling the projector,
// relies on the detection of cutting down the built grids by the lack of functional blocks, see
// where requestRemap is set to true
```

**Keep** the `EntityID` collision resolution:
```cs
// Make sure no EntityId collision will occur on re-welding a block on a previously disconnected (split)
// part of a built subgrid which has not been destroyed (or garbage collected) yet
if (blockBuilder.EntityId != 0 && MyEntityIdentifier.ExistsById(blockBuilder.EntityId))
{
    blockBuilder.EntityId = MyEntityIdentifier.AllocateId();
}
```

### Testing

- Expected to work in all game modes and with/without MGP installed on server side.
- Expected to work only with multigrid BPs saved with MGP active (requires consistent remapping of EntityId values).
