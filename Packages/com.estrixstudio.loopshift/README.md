# LoopShift

LoopShift is a lightweight Unity package for streaming connected 2D level zones. It combines authored graph connections with weighted procedural pools, aligns matching exits, and reuses inactive zone instances.

## Requirements

- Unity 2022.3 or newer
- Unity 2D Physics
- A `Collider2D` on the moving target; normal Unity trigger rules apply, so at least one participant should also have a `Rigidbody2D`

## Install

This repository uses LoopShift as an embedded package under `Packages/com.estrixstudio.loopshift`.

For another local project, open **Window > Package Manager**, choose **Add package from disk**, and select this package's `package.json`. A Git repository containing this folder can also be installed with Unity's **Add package from git URL** flow by using the appropriate package subdirectory query.

## Quick start

1. Build each reusable level section as a prefab and add `LevelZone` to its root.
2. Add child objects with trigger `Collider2D` and `LevelExit` components.
3. Assign each exit a cardinal direction. Use exit tags when only particular doorway types should connect.
4. Click **Refresh Bounds And Exits** on every zone prefab.
5. Select the zone prefabs and open **Tools > LoopShift > Level Graph Wizard**.
6. Create a graph from the selection, choose its start zone, and configure fixed connections and/or pools.
7. Add `EndlessLevelRunner` to a scene object, assign the graph and player transform, then enter Play mode.

Create a graph directly with **Assets > Create > LoopShift > Level Graph** when the wizard is not needed. Use the graph inspector's **Validate Graph** button before running.

## Graph resolution order

For a triggered exit, LoopShift tries:

1. A fixed graph connection matching zone ID, direction, and exit tag.
2. The `Fixed Target Zone Id` stored directly on the exit.
3. The first compatible pool rule, followed by weighted selection from its pool.

Fixed connections can be bidirectional. Pool entries support weights and optional immediate-repeat prevention.

## Runner settings

- `Preload Radius`: number of grid steps generated around the current zone.
- `Active Radius`: distance beyond which zones are returned to the instance pool.
- `Max Pool Instances Per Prefab`: inactive instances retained for each zone prefab.
- `Parent Targets To Current Zone`: keeps targets attached to the current zone while preserving and restoring their original parents.
- `Use Deterministic Seed`: produces repeatable weighted selections for a given graph and seed.
- `Bounds Padding`: additional spacing used before precise exit-anchor alignment.
- `Runtime Parent`: optional hierarchy parent for spawned zones.

## Runtime API

```csharp
using LoopShift.Runtime;

public sealed class LevelEvents : UnityEngine.MonoBehaviour
{
    [UnityEngine.SerializeField] private EndlessLevelRunner runner;

    private void OnEnable()
    {
        runner.ZoneEntered += OnZoneEntered;
        runner.RuntimeError += OnRuntimeError;
    }

    private void OnDisable()
    {
        runner.ZoneEntered -= OnZoneEntered;
        runner.RuntimeError -= OnRuntimeError;
    }

    private static void OnZoneEntered(LevelZoneEvent evt) { }
    private static void OnRuntimeError(LevelRuntimeMessage message) { }
}
```

The runner also exposes `ZoneSpawned`, `ZoneBecameCenter`, and `ZoneDespawned`, plus `StartRunner()`, `StopRunner()`, `AddTarget()`, and `RemoveTarget()`.

## Sample

Import **Minimal Top Down** from the Package Manager's Samples tab. Open `Scenes/MinimalTopDown`, enter Play mode, and move with WASD or the arrow keys.

See [Documentation~/index.md](Documentation~/index.md) for authoring details and troubleshooting.
