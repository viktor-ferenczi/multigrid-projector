# Shared Harmony Patches

These Harmony patches live under `Shared/Patches/` and are compiled into **both** the client plugin (loaded by Pulsar) and the server plugin (loaded by Magnetar). They intercept game methods that govern projection placement checks, mechanical block construction, and grid hierarchy management — the three areas where vanilla Space Engineers falls short for multi-subgrid blueprints. Most patches delegate non-trivial decisions to the projection engine described in [Core-Projection-Engine.md](./Core-Projection-Engine.md); low-level IL manipulation relies on helpers from [Shared-Utilities.md](./Shared-Utilities.md) (`TranspilerHelpers`, `EnsureOriginal`).

Patch applicability is controlled by placement attributes (`[Everywhere]`, `[ServerOnly]`, `[Server]`): placement-check patches run on both sides, while construction and hierarchy patches run only on the server.

---

## Files

| File | Lines | Purpose |
|------|-------|---------|
| [`MyBlueprintIdTrackerPatches.cs`](../../Shared/Patches/MyBlueprintIdTrackerPatches.cs) | 30 | Suppresses all `MyBlueprintIdTracker` callbacks to prevent spurious ID-remapping during projection |
| [`MyCubeGrid_TestPlacementAreaCube.cs`](../../Shared/Patches/MyCubeGrid_TestPlacementAreaCube.cs) | 83 | Postfix on the placement-area test to approve projected blocks that belong to a known multigrid projection |
| [`MyGridPhysicalHierarchy_BreakLink.cs`](../../Shared/Patches/MyGridPhysicalHierarchy_BreakLink.cs) | 54 | Server-only Prefix crash-guard that skips `BreakLink` when either grid node is `null` |
| [`MyMechanicalConnectionBlockBase_CreateTopPart.cs`](../../Shared/Patches/MyMechanicalConnectionBlockBase_CreateTopPart.cs) | 111 | Server-only Transpiler that extends the `topSize` enum logic so MGP can force same-size or opposite-size top parts |
| [`MyMechanicalConnectionBlockBase_OnBuildSuccess.cs`](../../Shared/Patches/MyMechanicalConnectionBlockBase_OnBuildSuccess.cs) | 41 | Server-only Transpiler that skips automatic top-part creation when the base block is being built from a projection |
| [`MyShipToolBase_UpdateAfterSimulation10.cs`](../../Shared/Patches/MyShipToolBase_UpdateAfterSimulation10.cs) | 23 | Server-only Prefix that prevents projected welders and grinders from functioning |

---

## MyBlueprintIdTrackerPatches

*`public static class MyBlueprintIdTrackerPatches` — `[HarmonyPatch(typeof(MyBlueprintIdTracker))]`*

**Patched target:** `MyBlueprintIdTracker.OnRemap`, `.OnAdded`, `.OnRemove`

**Patch kinds:** three independent Prefixes, each returning `false` to suppress the original method entirely.

`MyBlueprintIdTracker` tracks entity IDs in projected blueprints and reacts to remapping events. During multigrid projection the tracker fires on every subgrid entity and can corrupt or unnecessarily reassign IDs. All three callbacks are blanket-suppressed; none delegate to the projection engine.

| Member | Kind | Description |
|--------|------|-------------|
| `OnRemapPrefix` | Prefix | Returns `false`, preventing `MyBlueprintIdTracker.OnRemap` from executing |
| `OnAddedPrefix` | Prefix | Returns `false`, preventing `MyBlueprintIdTracker.OnAdded` from executing |
| `OnRemovePrefix` | Prefix | Returns `false`, preventing `MyBlueprintIdTracker.OnRemove` from executing |

---

## MyCubeGrid_TestPlacementAreaCube

*`public static class MyCubeGrid_TestPlacementAreaCube` — `[HarmonyPatch(typeof(MyCubeGrid), "TestPlacementAreaCube", ...)]` · `[EnsureOriginal("7eec76c6")]`*

**Patched target:** `MyCubeGrid.TestPlacementAreaCube` (static; 13-parameter overload including `ref MyGridPlacementSettings`, `out MyCubeGrid touchingGrid`, and the `wheelsAsCylinders` parameter introduced in 1.207.020)

**Patch kinds:** Postfix (`[Everywhere]` — runs on both client and server).

Vanilla `TestPlacementAreaCube` returns `false` for many projected block positions that are geometrically valid for multigrid blueprints (e.g. positions that would be occupied by a top part on a different subgrid). The Postfix intercepts a `false` result for projected blocks (`isProjected == true`) and asks the projection engine whether the position is actually acceptable via `MultigridProjection.TestPlacementAreaCube`. The engine lookup uses `MultigridProjection.TryFindProjectionByBuiltGrid` to resolve the relevant `MultigridProjection` and subgrid; see [Core-Projection-Engine.md](./Core-Projection-Engine.md). In release builds errors are caught and logged via `PluginLog.Error`; in debug builds they propagate.

The patch uses the `[EnsureOriginal]` attribute (from [Shared-Utilities.md](./Shared-Utilities.md)) with checksum `7eec76c6` to detect game updates that change the patched method's IL.

| Member | Kind | Description |
|--------|------|-------------|
| `Postfix` | Postfix | If `__result` is `false` and `isProjected` is `true`, delegates to `MultigridProjection.TestPlacementAreaCube` to potentially flip the result to `true` |

---

## MyGridPhysicalHierarchy_BreakLink

*`public static class MyGridPhysicalHierarchy_BreakLink` — `[HarmonyPatch(typeof(MyGridPhysicalHierarchy), "BreakLink")]` · `[EnsureOriginal("dc95de5b")]`*

**Patched target:** `MyGridPhysicalHierarchy.BreakLink`

**Patch kinds:** Prefix (`[ServerOnly]`).

This is a **crash-guard** for a known vanilla server defect ([Keen issue #1.105.024](https://support.keenswh.com/spaceengineers/pc/topic/1-105-024-ds-crash-keynotfoundexception-detatching-mechanical-connection)): when a mechanically-connected group of grids is deleted, `BreakLink` can be called with a `null` `childNode`, causing a hard server crash. The Prefix intercepts both the `childNode == null` and the `parentNode == null` cases, logs a warning via `PluginLog.Warn`, and returns `false` to skip the original (leaving potential stale state, but avoiding the crash). When both nodes are non-null, the Prefix returns `true` and the original runs normally.

An inline comment notes this fix may no longer be needed and suggests removal if the warning never appears in logs.

| Member | Kind | Description |
|--------|------|-------------|
| `Prefix` | Prefix | Guards against `null` grid nodes in `BreakLink`; skips original when either is `null` and logs a warning |

---

## MyMechanicalConnectionBlockBase_CreateTopPart

*`public static class MyMechanicalConnectionBlockBase_CreateTopPart` — `[HarmonyPatch(typeof(MyMechanicalConnectionBlockBase), "CreateTopPart")]` · `[EnsureOriginal("dc43452a")]`*

**Patched target:** `MyMechanicalConnectionBlockBase.CreateTopPart`

**Patch kinds:** Transpiler (`[ServerOnly]`).

The `topSize` parameter of `CreateTopPart` is a `MyTopBlockSize` enum. Vanilla handles values `Small (2)`, `Medium (1)`, and `Large (0)`, using the first two to force a small top block. MGP needs to express two additional intents: *same size as base* (encoded as `10`) and *opposite size to base* (encoded as `11`). The transpiler inserts IL **before** the existing enum-check sequence:

- If `topSize == 10` → branch directly to the exit label (`L2`/`L3`), preserving the current `myCubeSize` unchanged (same-size behavior).
- If `topSize == 11` → XOR `myCubeSize` with `1` to flip between `Small` and `Large`, then branch to exit (opposite-size behavior).

Because values `10` and `11` are never passed by vanilla code, the original behavior is fully preserved for all normal calls. The inserted IL uses `RecordOriginalCode`/`RecordPatchedCode` helpers from [Shared-Utilities.md](./Shared-Utilities.md) for diagnostic recording.

| Member | Kind | Description |
|--------|------|-------------|
| `Transpiler` | Transpiler | Inserts IL branches for `topSize == 10` (keep size) and `topSize == 11` (flip size) before the vanilla size-selection logic |

---

## MyMechanicalConnectionBlockBase_OnBuildSuccess

*`public static class MyMechanicalConnectionBlockBase_OnBuildSuccess` — `[HarmonyPatch(typeof(MyMechanicalConnectionBlockBase), nameof(MyMechanicalConnectionBlockBase.OnBuildSuccess))]` · `[EnsureOriginal("9d1cc43c")]`*

**Patched target:** `MyMechanicalConnectionBlockBase.OnBuildSuccess`

**Patch kinds:** Transpiler (`[ServerOnly]`).

When a mechanical base block (rotor, hinge, piston) is successfully built, `OnBuildSuccess` automatically spawns a matching top part. In multigrid projection the top part is a distinct subgrid that MGP manages itself; allowing vanilla auto-spawn would create a duplicate (and incorrectly sized/typed) top block. The transpiler locates the first `Brfalse_S` instruction in the method body — part of the existing early-return guard — and inserts immediately after it a call to `MultigridProjection.ShouldAllowBuildingDefaultTopBlock(__instance)`. If that method returns `false` (i.e. the base was built from a projection that already defines a top-part subgrid), the injected `Brfalse` branches to the same early-return target, skipping top-part creation. The engine method is described in [Core-Projection-Engine.md](./Core-Projection-Engine.md).

| Member | Kind | Description |
|--------|------|-------------|
| `Transpiler` | Transpiler | Inserts a call to `MultigridProjection.ShouldAllowBuildingDefaultTopBlock` after the first `Brfalse_S`; skips vanilla top-part spawn when the projection defines its own top subgrid |

---

## MyShipToolBase_UpdateAfterSimulation10

*`public class MyShipToolBase_UpdateAfterSimulation10` — `[HarmonyPatch(typeof(MyShipToolBase))]`*

**Patched target:** `MyShipToolBase.UpdateAfterSimulation10`

**Patch kinds:** Prefix (`[Server]`).

Ship tools (welders and grinders) that exist on a projected (preview) grid have no physics object (`CubeGrid.Physics == null`). The Prefix guards against projected tools activating by short-circuiting the update tick: if `__instance.CubeGrid.Physics` is `null`, the method returns `false`, preventing the tool from operating. Real (built) tools pass through normally.

| Member | Kind | Description |
|--------|------|-------------|
| `UpdateAfterSimulation10Prefix` | Prefix | Returns `false` (skip original) when the tool's grid has no physics, disabling projected welders/grinders |

---

```json
{"module":"shared-patches","page":"Shared-Patches.md","overview":"Six Harmony patches compiled into both the client and server plugins intercept game methods for placement validation, mechanical block construction, grid hierarchy management, and ship-tool activation. They bridge the gap between Space Engineers' single-grid assumptions and the multigrid projection engine, either delegating to MultigridProjection for decisions or acting as targeted crash-guards and suppression filters. Transpiler patches on MyMechanicalConnectionBlockBase extend the topSize enum with two MGP-specific sentinel values and suppress automatic top-part spawning for projection-sourced base blocks. All IL-modifying patches are version-guarded with EnsureOriginal checksums.","files":[{"path":"Shared/Patches/MyBlueprintIdTrackerPatches.cs","summary":"Suppresses all three MyBlueprintIdTracker event callbacks (OnRemap, OnAdded, OnRemove) to prevent ID corruption during multigrid projection."},{"path":"Shared/Patches/MyCubeGrid_TestPlacementAreaCube.cs","summary":"Postfix on the 13-parameter TestPlacementAreaCube static method; overrides a false result for projected blocks by consulting MultigridProjection.TestPlacementAreaCube."},{"path":"Shared/Patches/MyGridPhysicalHierarchy_BreakLink.cs","summary":"Server-only Prefix crash-guard that skips BreakLink when either the parent or child grid node is null, preventing a known vanilla server crash on mechanical group deletion."},{"path":"Shared/Patches/MyMechanicalConnectionBlockBase_CreateTopPart.cs","summary":"Server-only Transpiler that inserts IL to handle topSize sentinel values 10 (same size as base) and 11 (opposite size), enabling correct top-part sizing for arbitrary blueprint combinations."},{"path":"Shared/Patches/MyMechanicalConnectionBlockBase_OnBuildSuccess.cs","summary":"Server-only Transpiler that inserts a call to MultigridProjection.ShouldAllowBuildingDefaultTopBlock to suppress vanilla automatic top-part spawning when the base block belongs to a multigrid projection."},{"path":"Shared/Patches/MyShipToolBase_UpdateAfterSimulation10.cs","summary":"Server-only Prefix that prevents projected (physics-less) welders and grinders from activating during their simulation update tick."}],"key_types":["MyBlueprintIdTrackerPatches — patches MyBlueprintIdTracker.OnRemap/OnAdded/OnRemove — suppresses all three callbacks","MyCubeGrid_TestPlacementAreaCube — patches MyCubeGrid.TestPlacementAreaCube — overrides placement rejection for projected multigrid blocks","MyGridPhysicalHierarchy_BreakLink — patches MyGridPhysicalHierarchy.BreakLink — null-guard crash fix for mechanical group deletion","MyMechanicalConnectionBlockBase_CreateTopPart — patches MyMechanicalConnectionBlockBase.CreateTopPart — extends topSize enum with MGP sentinel values 10 and 11","MyMechanicalConnectionBlockBase_OnBuildSuccess — patches MyMechanicalConnectionBlockBase.OnBuildSuccess — suppresses auto top-part spawn for projection-built bases","MyShipToolBase_UpdateAfterSimulation10 — patches MyShipToolBase.UpdateAfterSimulation10 — prevents projected tools from activating"],"depends_on":["shared-logic","shared-utilities"],"used_by":["client-core","server-core"],"cross_refs":["Core-Projection-Engine.md — MultigridProjection.TestPlacementAreaCube, TryFindProjectionByBuiltGrid, ShouldAllowBuildingDefaultTopBlock","Shared-Utilities.md — EnsureOriginal attribute, TranspilerHelpers (RecordOriginalCode, RecordPatchedCode), PluginLog"],"notes":"Placement check patch (MyCubeGrid_TestPlacementAreaCube) runs on both client and server via [Everywhere]; all mechanical-block and tool patches are [ServerOnly]/[Server]. The CreateTopPart transpiler uses out-of-range enum sentinel values (10, 11) that vanilla never passes, so the original behavior is fully preserved for non-MGP calls. The BreakLink crash-guard may be removable in future versions — see inline FIXME comment."}
```
