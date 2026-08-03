# LoopShift authoring guide

## Zones and bounds

Place `LevelZone` on the prefab root. In Auto mode, **Refresh Bounds And Exits** calculates local bounds from child renderers and non-exit 2D colliders. Use Manual mode for procedural visuals or geometry that should not determine spacing.

Zone IDs must be unique within a graph. A graph entry's `zoneIdOverride` becomes that entry's effective runtime ID and may be used by fixed connections.

## Exits

Every `LevelExit` requires a trigger `Collider2D`. Place its transform, or its optional spawn anchor, exactly where the next zone's opposite exit should meet. East connects to West and North connects to South.

Tags are case-insensitive. A blank required tag accepts any candidate; otherwise the candidate exit tag must match.

## Fixed and procedural connections

Fixed connections are best for entrances, bosses, checkpoints, and other deliberate sequences. Select `Bidirectional` when the reverse transition should be generated automatically.

Pools are best for repeatable endless content. Give each pool a unique ID and add weighted zone entries. Pool rules select a pool by outgoing direction and tag and can require a particular tag on the destination's opposite exit.

Resolution stops at the first successful strategy: graph connection, exit-level fixed target, then compatible pool rule.

## Streaming behavior

`Preload Radius` controls generation. `Active Radius` controls retention. An active radius smaller than the preload radius is valid, but the outer preloaded zones will be reclaimed after the center changes.

Released instances are disabled and pooled by source prefab. Keep transient zone state in components that reset themselves when enabled, or subscribe to the runner's zone events from a central system.

## Troubleshooting

- **Runner reports no graph:** assign a `LevelGraphAsset` before Play mode.
- **Graph validation fails:** fix every error printed by **Validate Graph**; runtime startup stops on validation errors.
- **An exit does nothing:** verify the moving collider belongs to a configured target, at least one object has a `Rigidbody2D`, and the exit has a resolvable connection or pool rule.
- **Zones overlap or leave gaps:** refresh zone bounds and place opposite spawn anchors at matching seam positions.
- **A pool never selects a zone:** confirm its rule direction/tag, pool ID, entries, and destination exit tag.
