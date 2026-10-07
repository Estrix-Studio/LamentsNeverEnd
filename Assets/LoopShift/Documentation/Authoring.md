# Authoring rooms and graphs

## Create a room

1. Add LevelZone to the root of the room prefab and open it in Prefab Mode.
2. Use manual bounds for a deliberate room footprint. The center is relative to the room root.
3. Enable the bounds handle in the inspector to adjust the footprint in Scene view.
4. Choose Add Missing Cardinal Exits to create trigger exits at the four edges. Repeating this action preserves existing default exits.
5. Enable exit handles to adjust arrival positions. Keep paired exits facing opposite directions.
6. Save the prefab.

Automatic bounds use renderer geometry transformed into room space and conservative collider world bounds. Mirrored or rotated room transforms are supported by the bounds calculation. Disabled colliders contribute no bounds; use manual bounds for collider-only inactive content. Nested LevelZone objects own their own geometry and exits. Bounds calculation support does not change the runner's cardinal grid model.

Room changes and exit creation support Undo/Redo. Edit prefab contents in Prefab Mode rather than modifying a persistent prefab asset directly.

## Create and connect a graph

Select saved room prefabs, then open Window > LoopShift > Create Graph Wizard. Choose an existing folder below Assets and a file name. The wizard and the canvas use the same creation service and reject scene objects before creating assets.

Open Window > LoopShift > Level Graph. Drag saved room prefabs onto the canvas, or use Add selected. Repeated use of a prefab produces distinct graph aliases without changing the prefab's own ID.

- Drag node headers to arrange rooms; Undo restores positions.
- Connect a source exit to an opposing exit on another room.
- Use the bidirectional option for a return route.
- Use + Pool for weighted alternatives. Pool rules apply globally to matching exits; fixed routes take priority.
- Use the Details panel to maintain identifiers and routes. Renaming through the editor updates references.
- Scroll to zoom, middle-drag or Alt-drag to pan, and choose Frame all to fit the graph.
- Save the graph and resolve its validation messages before running.

The game-specific graph is Assets/Prefabs/LoopShiftGame/GameRooms.asset. Game progression remains in GameData and its condition scripts; the reusable graph contains routing data only.
