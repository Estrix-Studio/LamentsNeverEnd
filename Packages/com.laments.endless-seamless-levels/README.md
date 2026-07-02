# Endless Seamless Level System

Lightweight Unity 2D level streaming for authored and endless maps. The runtime is independent from project-specific player, camera, dialogue, input, render pipeline, and gameplay code.

## Quick Start

1. Add `LevelZone` to each room or zone prefab.
2. Add child objects with `LevelExit` trigger colliders for doorways or screen edges.
3. Select the prefabs and open `Tools > Endless Seamless Levels > Wizard`.
4. Create a graph from selected zones.
5. Add `EndlessLevelRunner` to an empty scene object.
6. Assign the graph and the player/camera transforms as targets.
7. Press Play.

## Main Components

- `LevelGraphAsset`: start zone, fixed connections, weighted pools, and pool rules.
- `LevelZone`: prefab component with stable ID, cached bounds, manual size override, and exits.
- `LevelExit`: trigger that marks a north/east/south/west connection point.
- `EndlessLevelRunner`: runtime streamer that spawns, aligns, pools, and despawns zones.

## Compatibility

Designed for Unity 2022 LTS and newer. Runtime has no dependency on URP, Yarn Spinner, Input System, or project-specific scripts.
