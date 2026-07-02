# Performance Guide

The runtime is designed to stay lightweight:

- Graph lookup tables are built once at startup.
- Bounds are cached on `LevelZone` instead of scanned every frame.
- Zones are pooled and reused.
- Transitions are driven by trigger events.
- There is no runtime dependency on `Resources.LoadAll` or `FindObjectOfType`.

Keep `activeRadius` small for mobile and dense 2D scenes.
