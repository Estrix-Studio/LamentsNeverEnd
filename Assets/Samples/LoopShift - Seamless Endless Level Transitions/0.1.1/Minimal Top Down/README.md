# Minimal Top Down — guided sample

This sample teaches the smallest complete LoopShift setup. Open `Scenes/MinimalTopDown`, enter Play mode, and follow the panel in the upper-left corner. Move the yellow player with WASD or the arrow keys and cross either cyan exit strip. Press R to restart.

## What happens in Play mode

1. `EndlessLevelRunner` validates `MinimalTopDownGraph` and spawns its start zone.
2. `Preload Radius = 1` resolves the East and West pool rules and creates neighboring zones.
3. The player's `Collider2D` enters a cyan `LevelExit` trigger.
4. The runner changes its current grid cell, generates the next neighbor, and reclaims zones outside `Active Radius`.
5. The tutorial panel receives the runner events and displays its current zone, cell, and active instance count.

## Inspect these assets after Play mode

### 1. `Scenes/MinimalTopDown`

Select **LoopShift Runner** and inspect:

- `Graph`: the ScriptableObject that controls selection;
- `Targets`: only these transforms can trigger transitions;
- `Preload Radius` and `Active Radius`: generation and retention distances;
- `Use Deterministic Seed`: makes procedural choices repeatable.

The player demonstrates the required physics setup: a `Rigidbody2D`, a `Collider2D`, and any movement solution. `SampleCameraFollow` and `SampleTutorialUI` are sample-only helpers, not required by LoopShift.

### 2. `Prefabs/SampleZone`

The root has `LevelZone`. Its renderer supplies automatic bounds. The two descriptively named child objects each have:

- a trigger `BoxCollider2D`;
- a `LevelExit` pointing East or West;
- the `Default` exit tag; and
- a transform used as the alignment anchor.

The cyan children are only visual markers. Real game doors can use any art or remain invisible.

### 3. `MinimalTopDownGraph`

The graph contains:

- one registered zone and its start ID;
- one weighted pool containing `SampleZone`; and
- East and West pool rules requiring matching `Default` exits.

Because immediate repeats are allowed, the same prefab can produce an endless corridor. Add more entries with different weights to see procedural selection.

## Adapt it for your game

1. Duplicate `SampleZone` into your project's `Assets` folder.
2. Replace the visuals while keeping the root `LevelZone` and exit children.
3. Place exit anchors on matching seams and click **Refresh Bounds And Exits**.
4. Add the new prefab to a graph pool or fixed connection.
5. Assign that graph and your player to an `EndlessLevelRunner`.
6. Validate the graph, then test every exit in Play mode.
