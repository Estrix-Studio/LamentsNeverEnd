# Runtime integration and migration

## Start a run

Assign a LevelGraphAsset to EndlessLevelRunner in the Inspector. Its Graph property is read-only at runtime. Disable Play On Awake if an adapter must register targets or subscribe before startup. Call AddTarget for the actor root and any independent camera or effects that must move during recentering, then call StartRunner. The runner must be active and enabled.

A collider on the registered actor or its descendants can trigger an exit. The actor needs the normal Unity 2D physics configuration for trigger callbacks, including an appropriate Rigidbody2D and layer collision settings. Registering the runner or one of its ancestors as an actor is ignored.

StartRunner begins a fresh seeded run; it is not a resume operation. StopRunner releases owned rooms and restores actor parenting. Disable/destroy also stops the runner. RemoveTarget unregisters an actor and restores parenting that the runner owns.

## Observe progression

| API | Meaning |
| --- | --- |
| IsRunning | A graph has started and has not been stopped or aborted |
| CurrentCell | Logical integer coordinate of the current room |
| ActiveZones | Live read-only cell-to-instance dictionary |
| PooledZoneCount | Inactive instances retained for reuse |
| OwnedZoneCount | All currently tracked active and pooled instances |
| ZoneSpawned | A room has activated at its final position |
| ZoneBecameCenter | Initial center selection or a subsequent center change |
| ZoneEntered | An accepted crossing; startup does not emit this event |
| ZoneDespawned | A room leaves the active registry before deactivation |
| WorldShifted | World-space offset applied during recentering |
| RuntimeError | Diagnostic message and associated Unity object |

LevelZoneEvent contains Zone, logical ZoneId, and Cell. Use ZoneId for progression; a prefab can appear under several graph aliases. ZoneBecameCenter precedes ZoneEntered on a crossing. Subscribe and unsubscribe with matching component lifetimes. Avoid subscribing to both for the same progression action.

TryTraverse(exit, actorCollider) is available for custom trigger adapters. It validates ownership, actor registration, the current room, arrival overlap, and the one-crossing-per-frame gate. A false result means no successful traversal was completed; do not advance progression independently.

Callbacks may stop the runner. A start requested from a callback is deferred. An unhandled subscriber exception aborts the run and cleans up; catch expected application errors in your adapter. Do not enumerate the live ActiveZones view across calls that may change the runner.

## Reset reusable room content

Implement ILevelZoneLifecycle on room components:

- OnZoneSpawned(LevelZoneEvent context) runs after activation and final placement, before ZoneSpawned.
- OnZoneDespawned(LevelZoneEvent context) runs after removal from the active registry and before deactivation.

Use these hooks to reset chase state, timers, velocities, local poses and effects. Awake runs only when an instance is first initialized; Start is not a per-visit hook. Hook discovery is cached when the instance is created. Dynamically adding new lifecycle components to already pooled rooms is unsupported.

Save persistent quest or pickup state outside reusable room instances. Restore the appropriate visual state on activation. Objects deliberately destroyed by game logic remain destroyed within that pooled instance; reset rules belong to the application.

## Migrate an existing room system

1. Keep a source-control copy of the original scenes/prefabs and preserve their metadata.
2. Add LevelZone and compatible opposing LevelExit anchors to saved room prefabs, preferably copies during migration.
3. Use logical aliases matching existing progression IDs. Create the graph through the wizard or canvas.
4. Translate fixed connections explicitly. Mark an edge bidirectional only when the original design permits a return route.
5. Replace old streaming triggers in the migrated copies. Running both systems can double-fire progression or instantiate overlapping rooms.
6. Add one game adapter to register targets and translate room callbacks into existing progression events.
7. Verify initial entry separately from transitions; room conditions may need to initialize before receiving the initial notification.
8. Test return routes, terminal rooms, pooled content, loss/reset/restart and scene unloading before switching the main scene.

LoopShift does not include saved-game persistence, networking, navigation generation or game-specific progression. It exposes runtime boundaries for those systems.
