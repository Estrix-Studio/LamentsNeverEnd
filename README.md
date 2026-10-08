# Laments Never End

A Unity 2D exploration game with looping rooms, torch-driven enemy pursuit, and dialogue/event progression.

## Open and run

1. Install the editor version in `ProjectSettings/ProjectVersion.txt` (currently **6000.7.0b1**).
2. Open this repository root in Unity Hub. Let Unity restore the packages in `Packages/manifest.json`.
3. Open `Assets/Scenes/MainMenu.unity` and enter Play mode. Starting a game resets progression.
4. Use the enabled scenes in Build Profiles when building the player.

The checked-in package lock records resolved dependencies. Yarn Spinner currently tracks its upstream `current` Git reference, so deliberately review package-lock changes when updating dependencies.

## Project guide

- [Architecture and runtime flow](Docs/Architecture.md)
- [Folder structure and ownership](Docs/ProjectStructure.md)
- [Validation and gameplay checks](Docs/Validation.md)
- [Room and graph authoring](Assets/LoopShift/Documentation/Authoring.md)

Game code lives in `Assets/Scripts`. Project-specific editor tools and automated checks live in `Assets/Editor`. `Assets/LoopShift` is a separate reusable room-streaming module; the current Gameplay scene uses its runner through Gameplay.GameLevelRunner and the GameRooms graph.

## Validate

In Unity, select **Tools > Laments Never End > Run Project Checks** outside Play mode.

For repeatable batch validation, close the project in Unity and run:

~~~powershell
./Tools/Validate-Project.ps1 -UnityPath 'C:/path/to/Editor/Unity.exe'
~~~

This compiles the imported project and checks progression reset, event completion, condition ownership, room connections, resource prefabs, and enabled scenes. It does not replace gameplay testing.

Unity-generated solution/project files are local IDE output. Regenerate them through Unity after adding scripts; do not maintain them by hand.