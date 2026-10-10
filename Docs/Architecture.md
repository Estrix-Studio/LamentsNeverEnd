# Current architecture

## Runtime composition

The enabled scene order is MainMenu, Gameplay, Options, Credits, LossScreen, WinScreen. MainMenu resets GameData and loads Gameplay through the Scene enum's build index. Enemy collision resets progression and loads LossScreen; WinCollider loads WinScreen. PauseMenu exists as a scene but is not in the enabled build list.

Scenes and prefabs compose MonoBehaviours through serialized references. There is no dependency-injection container, persistent save system, or service bootstrap layer.

~~~mermaid
flowchart TD
    Menu[MainMenu] --> State[GameData: run state and events]
    Menu --> Gameplay[Gameplay scene]
    Gameplay --> Player
    Gameplay --> Traversal[GameLevelRunner + EndlessLevelRunner]
    Traversal --> Config[GameRooms graph]
    Traversal --> Rooms[LoopShiftGame prefabs]
    Rooms --> Triggers[LevelExit]
    Triggers --> Traversal
    Traversal --> State
    Player --> Objects[InteractableObject / Krabik]
    Objects --> Dialog[DialogController]
    Dialog --> Player
    Objects --> State
    State --> Conditions[LevelCondition subclasses]
    Conditions --> State
    State --> Reactions[RemoveObjectEventCompleted]
    Player --> Torch[TorchController / LightController]
~~~

## State and progression

GameData is a process-local singleton containing completed event IDs, registered condition IDs, mutable room connections, and event notifications. Reset clears progression and creates independent copies of ZoneConfig records so runtime rewiring cannot corrupt future runs.

TryRegisterCondition grants one owner per configured event for a run. A condition's first instance subscribes to zone/object notifications and detaches from its room so room destruction does not discard progress. Duplicate or completed conditions are discarded. Completion is idempotent, including reentrant event callbacks; EventName.None is not a real completion.

RemoveObjectEventCompleted reacts to a configured event by removing, enabling, or completing referenced objects. On reload it checks whether the event already happened. Subscribers must unsubscribe when destroyed.

OnObjectInteracted remains an available hook. The current InteractableObject completes its configured event directly; it does not publish this string-based notification.

GameData is not a disk save. Starting through MainMenu is the normal reset boundary. Disabling Unity domain reload requires particular care with static state when testing directly in Gameplay.

## Room traversal

Gameplay now uses LoopShift.Runtime.EndlessLevelRunner with the game-specific GameLevelRunner adapter. Its original scene GUID and build index are retained, so MainMenu loads the same scene.

Assets/Prefabs/LoopShiftGame/GameRooms.asset contains 15 logical rooms and 43 one-way routes migrated from ZoneConfig. The copied prefabs retain game artwork, interactions, conditions and victory content, and add cardinal exits on a 20-unit room grid. Exit anchors are offset along each edge to match measured unlocked passages; the migration uses LoopShiftPassageAudit to apply those anchors. Legacy CyclingZone/TriggerZone callbacks are removed from these copies, making the runner the source of room-entry notifications.

GameLevelRunner registers the player and camera, starts the graph, and translates logical IDs into GameData progression notifications. The initial notification waits for room conditions to initialize. The adapter contains no streaming algorithm.

Immediate neighbors are aligned to the current room, replacing stale conflicting speculative instances when necessary. More distant speculative paths retain occupied cells. The runner retains a radius-one room window, bounds pooled instances, and recenters the current room at its origin. Registered actors and camera receive the same offset. Enemy lifecycle hooks restore spawn positions and clear chase state. Completed disappearing objects are removed on room disable; inactive event completion does not start a coroutine.

Original Resources/Zones prefabs and LevelTraversal remain for migration comparison and compatibility; current Gameplay does not load them. GameData.currentConnections remains a legacy compatibility surface. The graph is the current gameplay routing source.

GameplayLoopShift.unity is the migration preview, not an additional build entry. Room3_5 remains terminal; its corrected trigger collider opens WinScreen.

## Player, dialogue and lighting

Player owns generated InputSystem_Actions, enables/disables them with its component lifecycle, and disposes them on destruction. Movement uses Rigidbody2D in FixedUpdate. Dialogue start/end notifications lock/unlock movement and interactions. An interaction pass stops when an object starts dialogue.

DialogController supports legacy phrase lists and Yarn scripts supplied by DialogInfo. Yarn is compiled at runtime. Invalid compilation or node selection falls back to legacy phrases. Node selection occurs before announcing a dialogue session, so fallback does not falsely finish the interacting object's event. Closing or replacing dialogue cancels its typing coroutine; destruction removes the button listener and singleton reference.

Yarn options currently select the first available choice automatically, and commands are logged rather than executed. This is the current implementation, not a full branching conversation UI.

TorchController resolves references and supplies TorchData in Awake, before LightController.Start consumes it. Toggling controls the lights and animator. Enemies follow their target while its torch is lit.

## Editor and rendering boundary

Level.ShadowCaster remains a runtime component so serialized scene references are valid in a player build. Its inspector and generation code live in Assets/Editor/Level. The inspector writes URP 17.7's serialized shape data; URP owns mesh generation. Removal targets generated shadow children and supports Undo instead of deleting arbitrary children.

The shadow authoring adapter depends on the installed URP serialized property names. Revalidate it when upgrading URP. Rendering assets and imported art remain content configuration rather than runtime service code.
## LoopShift implementation boundaries

The module has three distinct layers:

- Runtime data: LevelGraphAsset stores room aliases, fixed connections, weighted pools, and global pool rules. LevelGraphValidator checks authored data; LevelGraphRuntimeCache compiles routes for a run.
- Runtime execution: EndlessLevelRunner coordinates traversal and bounded room activation. LevelInstancePool owns and reuses instances. LevelTargetRegistry manages actor registration and restores original parenting. ILevelZoneLifecycle provides spawn/despawn hooks for reusable room state.
- Editor authoring: LevelGraphAuthoring centralizes serialized, undoable graph mutations. LevelGraphWindow renders the canvas and its Details partial renders selection controls and validation. The graph inspector opens the same window. LevelZoneAuthoring centralizes undoable room bounds and exit edits; its inspector provides Scene view handles. Both the graph wizard and canvas use the same graph factory, which validates output paths and saved prefab inputs before creating an asset.

Dependencies point from editor tooling and tests to runtime. LoopShift runtime does not depend on game progression, dialogue, or UI. Gameplay uses the reusable runner through GameLevelRunner; game progression remains outside the package.

Graph node positions are serialized with rooms and pools. Renaming a room through the authoring service updates graph connections, pool membership aliases, and the start ID. Pool rules are global matching rules, not per-room connections. Fixed routes take priority.

## Extension decisions

Reuse GameData for game progression and LevelGraphAuthoring for graph edits. Avoid adding another state singleton or duplicating routing rules in editor code. Keep new reusable streaming logic inside LoopShift and game-specific adapters inside Scripts/Gameplay. Preserve prefab GUIDs and serialized identifiers when reorganizing assets.


## Dependency and change boundaries

| Area | Owns | Depends on | Must not own |
| --- | --- | --- | --- |
| LoopShift runtime | Graph data, routing, room instances, registered targets | Unity runtime | Game events, dialogue, menu state |
| GameLevelRunner | Translating room entry into game progression | LoopShift runtime, GameData | Pooling or route selection |
| Gameplay | Player, enemies, interactions, run progression | Unity, input, game data | Editor APIs |
| UI | Menus, dialogue presentation | Gameplay notifications and dialogue data | Room streaming |
| LoopShift editor | Graph canvas, room handles, shared authoring operations | LoopShift runtime, UnityEditor | A separate routing implementation |
| Project editor | Content validation and game migration tooling | Game runtime and editor APIs | Shipped player behavior |

Serialized scenes, prefab references, and asset GUIDs are the composition boundary. Keep behavior changes in the owning layer and expose a small callback or adapter where another layer needs to react. Tests mirror these boundaries: graph and authoring tests in EditMode, streaming/lifecycle tests in PlayMode, and project checks for game-specific configuration.

## Current constraints

- GameData lasts for the process and has no disk persistence. MainMenu is the supported fresh-run entry point.
- Legacy room assets remain available for comparison. Changes to live game routing belong in GameRooms.asset.
- The project currently targets the checked-in Unity beta version. Automated results on that editor do not establish compatibility with other Unity versions.
- Automated editor events test behavior, not visual appearance. Physical doorway navigation, dialogue presentation, lighting, and player controls still need the manual checklist in Validation.md.
## Verified content and navigation boundaries

The final-stage graph enters Room3_3 through its open north passage from Room3_2's south exit; returning north leads back to Room3_2. ZoneConfig retains the same routes for legacy comparison. Interaction event assignments in Room2_1, Room2_3, Room3_1 and Room3_3 match their authored gate consumers. Krabik completes Dialog_1Completed when its conversation finishes.

LoopShiftPassageAudit measures temporary prefab instances with event-controlled gate colliders disabled, identifies contiguous open spans, and saves only exit positions and trigger dimensions through prefab editing. This measures unlocked geometry; it does not remove gameplay gates. The runtime graph does not model whether an interaction gate is currently open.

The game integration probe crosses 17 local doorways with physical movement and authored interaction callbacks, and separately checks enemy loss, victory and restart. It positions the player near each doorway, so it does not establish complete room-interior navigation or native input usability. See Docs/Evidence/LoopShift-PhysicalProgression.json for the measured scope.