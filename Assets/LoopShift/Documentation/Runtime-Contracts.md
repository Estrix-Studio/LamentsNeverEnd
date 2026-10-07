# Runtime graph contracts

Status: development build. Graph compilation, streaming, lifecycle and visual authoring are implemented; release verification remains in progress.

## Identity

A graph entry has a logical ZoneId: its override if provided, otherwise the prefab's ID. Routing uses that logical ID, not the instantiated prefab name. The same prefab can be used under multiple logical IDs. One logical ID cannot refer to different prefabs.

Graph entries and pool entries share the same ID registry. Pool-only entries can therefore be start zones or fixed-connection targets. Repeating an ID for the same prefab across different pools is valid; duplicate entries inside one pool are invalid, since weights express frequency.

Zone IDs are ordinal and case-sensitive. Exit tags are case-insensitive. An empty required tag is a wildcard. Connection keys are structured values rather than concatenated strings, so delimiters in user IDs cannot collide.

## Resolution priority

1. Fixed graph connection matching logical source ID, direction, and exact exit tag.
2. Fixed graph connection with an empty source tag (wildcard).
3. The exit component's explicit fixed target, if set.
4. Pool rules in authored order, selecting the first rule with eligible candidates.

A declared fixed target that cannot resolve is an error; it does not silently fall through to a pool.

Bidirectional graph edges generate the corresponding reverse edge. Ambiguous edges, including conflicts with generated reverse edges, are validation errors.

## Weighted pools

The compiler filters each rule's candidates by opposite exit direction and required target tag. Weight must be positive. Accumulation is 64-bit, so large integer weights do not overflow a 32-bit sum.

When immediate repeats are disabled, every candidate with the current logical zone ID is excluded. If no eligible candidates remain, that rule cannot resolve; it does not silently repeat a singleton. A later authored rule may supply a fallback.

Randomness uses the runner's seeded System.Random instance. Reproducibility requires identical graph data, seed, and traversal/preload order. Runtime edits require restarting to compile the changed graph. Identity and routing configuration are snapshotted, but Unity prefab content is still referenced; modifying live prefab objects while running is not supported.

## Runtime window

Preload uses a square (Chebyshev-distance) window around the current grid cell, expanding each reachable cell at most once. Radius zero loads only the current cell. Effective preload radius is clamped to the active retention radius. Unreachable cells need not be populated.

Queue, visited-cell and release buffers are reused. Cells outside retention are released before new neighbors are loaded, allowing pooled objects to be reused sooner.

ActiveZones is a live read-only dictionary view, not a new snapshot. Callers that require an immutable historical snapshot must copy it themselves. Do not enumerate it while a callback may mutate runner state.

## Verification boundaries

Package EditMode tests cover aliases, pool-only entries, malformed graphs, ambiguous reverse routes, tag matching, incompatible candidates, weights, deterministic sequences, and no-repeat behavior. They also cover bounded preload, pooled restart, and read-only live views.

A zero-allocation routing assertion covers warmed-up cache resolution only. It does not prove allocation-free prefab instantiation, user callbacks, physics, or entire transitions. PlayMode, stress, domain-reload-disabled, visual editor, clean-project, and player-build validation remain required before release.
## Origin and ownership

With recenterOnTransition enabled, the current room moves to the runner origin on each crossing. All active rooms and registered targets receive the same offset. Nested targets and actors parented inside a room move once. WorldShifted reports the offset after physics transforms synchronize; use it for independent world-space effects. Unregistered objects outside rooms are not moved.

CurrentCell remains a logical traversal coordinate; recentering bounds world positions, not the integer coordinate range.

Default active-room and pool containers are separately owned scene roots. They are not children of the runner GameObject, allowing shutdown to restore actors and release rooms without reparenting inside a hierarchy Unity is deactivating. The runner destroys its instances and containers. runtimeParent overrides active-room placement; keep externally supplied parents available during cleanup.

Callback stop requests complete cleanup before the operation returns. Callback starts are deferred; thrown subscriber exceptions abort the run. Reset pooled behaviour state with ILevelZoneLifecycle.
