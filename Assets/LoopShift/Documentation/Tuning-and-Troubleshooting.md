# Tuning and troubleshooting

## Control work and memory

Start with preloadRadius = 1 and activeRadius = 1. Radius r describes a square of at most (2r + 1)^2 cells; unreachable cells may remain empty. Preload is clamped to the active radius. Radius zero keeps only the current cell and creates the next room on demand.

Increasing preload can hide instantiation work before a crossing but increases startup work and retained content. Increasing active radius without increasing preload keeps visited rooms longer. Test representative room prefabs: component count, physics, scripts and textures usually dominate memory.

maxPoolInstancesPerPrefab limits inactive instances per prefab; maxPooledInstances caps all inactive room instances. These limits do not cap active rooms. A zero pool limit destroys released rooms, reducing retained memory at the cost of future instantiation. Choose limits from the active window and the number of frequently revisited prefab types.

Graph routes compile once per run. Reusable traversal buffers and cached lifecycle handlers reduce repeated allocations after warmup. First instantiation, expanding buffers, user callbacks and Unity physics can still allocate. Measure a development player with the Unity Profiler after warmup; report CPU and allocations separately from GPU/rendering performance. An allocation-free route lookup is not a guarantee that an entire game frame allocates nothing.

## Geometry and origin

Use consistent dimensions and compatible opposing anchors for rooms intended to occupy a regular cardinal grid. Exit direction is logical; rotating a prefab does not rotate its direction enum. Unit-scale, unrotated room roots are the clearest authoring convention. Offset pivots and child geometry are supported.

Automatic renderer bounds transform geometry into room space. Collider world bounds are conservative, especially after rotation; inactive colliders have no useful world bounds. Set explicit manual bounds when a room must have a deliberate footprint or uses inactive collider-only geometry. Nested rooms own their own exits and geometry.

With recenterOnTransition, active rooms and registered targets shift so the current room stays at the runner origin. Register independent camera rigs and update independent world-space effects through WorldShifted. Child targets shift once. Logical CurrentCell continues to advance; recentering does not create an unlimited integer address space.

A graph can request a different room in an occupied cell. With preloading enabled, immediate routes from the current room replace conflicting or misaligned neighbors. More distant speculative routes retain existing occupants; an accepted crossing also resolves conflicts. This supports looping topology but cannot make every arbitrary graph geometrically consistent. Verify the authored routes visually.

## Reproducible choices

Enable useDeterministicSeed and set seed. Matching graph data, seed and traversal/preload order produce matching route choices. Changing preload radius can change random-consumption order. Stop and restart after editing graph configuration. Reproducibility does not include arbitrary random behavior inside room scripts.

## Diagnose a problem

| Symptom | Check |
| --- | --- |
| Runner does not start | Graph assignment, start ID, enabled state, and validation errors |
| Exit does nothing | Registered actor ancestry, enabled trigger, Rigidbody2D/layer collision configuration, and source room being current |
| Immediate return is blocked | Move clear of the arrival trigger; the overlap gate prevents an automatic bounce |
| Pool produces no room | Positive weights, opposing exits, target tags, and no-repeat excluding the only eligible candidate |
| A fixed route does not fall back | Invalid fixed targets are errors; they intentionally do not silently use a pool |
| Room or camera jumps incorrectly | Matching anchors, explicit dimensions, target registration, and independent world-space effects |
| Old enemy/timer state returns | Reset reusable state with ILevelZoneLifecycle instead of relying on Start |
| Excessive memory | Active radius, both inactive pool limits, prefab content and callback-owned objects |
| Same prefab has different progression | Use the graph's logical ZoneId from the event, not the prefab name |
| Graph edit has no effect while running | Stop and restart to compile the changed configuration |
| Cannot add an object to a graph | Save it as a prefab first; scene instances are rejected |
| Inactive objects change automatic bounds | Prefer manual bounds for the intended footprint |
| A callback aborts traversal | Inspect RuntimeError and the exception; catch expected errors in application subscribers |

Use the sample to separate a package setup issue from game-specific scripts. Include the Unity version, render pipeline/version, graph settings, relevant diagnostics and a small reproduction when contacting support at contact@estrixstudio.com.
