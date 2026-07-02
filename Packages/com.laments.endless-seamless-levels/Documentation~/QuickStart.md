# Quick Start

## Create Zones

Place your map content under one prefab root and add `LevelZone` to that root. Add child objects with `LevelExit` and trigger `Collider2D` components for every doorway or screen edge where a new zone can appear.

`LevelZone` automatically detects bounds from child renderers and 2D colliders. Use Manual bounds when a zone has invisible gameplay space or unusual art layout.

## Create A Graph

Open `Tools > Endless Seamless Levels > Wizard`.

- `Create Empty Graph` creates a blank `LevelGraphAsset`.
- `Create Graph From Selected Zones` adds selected zone prefabs or scene objects to a new graph and sets the first selected zone as the start zone.

Use fixed connections for authored progression. Use pools and pool rules for endless weighted variation.

## Run

Add `EndlessLevelRunner` to a scene object, assign the graph, and add player/camera transforms to Targets. The runner follows plain transforms and does not require any specific controller script.
