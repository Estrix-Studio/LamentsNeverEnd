# Project structure

This structure describes the existing project and establishes where additions belong. Existing asset GUIDs, namespaces, Resources paths, and serialized fields are retained.

~~~text
Assets/
  Scripts/                         Game runtime (Assembly-CSharp)
    Enums.cs                       Shared scene, side, and progression identifiers
    Gameplay/
      Data/                        Zone IDs and default connection configuration
      LevelScripts/                Conditions and event-driven object reactions
      ...                          Player, enemy, traversal, interactions, session state
    UI/                            Menus and dialogue presentation
    Utility/                       Dialogue assets, torch configuration and lighting
    Level/                         Tilemap visuals and shadow-authoring component
  Editor/                          Project tooling (Assembly-CSharp-Editor)
    Level/                         Shadow authoring inspector and Undo support
    Validation/                    Batch/menu regression and content checks
  Scenes/                          MainMenu, Gameplay, Options, Credits, results, PauseMenu
  Prefabs/
    Dialogs/                       Dialogue data and Yarn text assets
    Lights/                        Light prefabs
    Resources/Zones/               Legacy room sources retained for comparison
    LoopShiftGame/                 Migrated game rooms and GameRooms graph
  LoopShift/                       Reusable module (also contains embedded Git metadata)
    Runtime/                       LoopShift.Runtime assembly
    Editor/                        Editor assembly, inspectors and Graph/ canvas authoring
    Tests/                         Editor, PlayMode and shared test assemblies
    Documentation/                Module contracts and usage documentation
    Demo/                          Module demonstration content
  [ART]/                           Imported art and tile assets
  Materials/                       Shared materials
  RenderPipeline/, Settings/       Rendering and quality configuration
  TextMesh Pro/                    Text rendering resources
  InputSystem_Actions.*            Input asset and generated C# wrapper
Packages/                          Package manifest, lock and embedded packages
ProjectSettings/                   Unity project and build configuration
Docs/                              Architecture and verification guidance
  Evidence/                        Retained test results and integration reports
Tools/                             Repository validation entry points
~~~

## Ownership rules

- Runtime behavior belongs in Scripts. Editor dependencies belong in Editor; runtime assemblies must compile without UnityEditor.
- Keep condition/event semantics in GameData and Gameplay/LevelScripts. Keep menu and dialogue view behavior in UI.
- Keep LoopShift independent of Gameplay and UI. The parent repository currently tracks module source too; inspect both repository states before committing.
- Keep generated input C# alongside its input asset. Edit the input asset and regenerate the wrapper through Unity.
- Move Unity assets together with their .meta files, preferably through Unity. Never recreate a GUID merely to rename a folder.
- Preserve public methods referenced by UI buttons and serialized field names, or provide an explicit migration.
- Preserve Resources/Zones for legacy traversal and migration checks; current Gameplay references LoopShiftGame directly.
- Keep new documentation outside Assets unless Unity needs to import it.
- Library, Logs, obj, Build, UserSettings, and generated IDE projects are local output.

## Deliberate current boundaries

Game code remains in Unity's predefined Assembly-CSharp. Adding assembly definitions would require moving the generated input wrapper and declaring package assembly dependencies; that migration is not necessary for this state.

Existing serialized names such as RoomVisistCondition and the ZoneConfing filename remain compatible with existing content. Avoid cosmetic renames during behavior fixes.
## Where to start

- Runtime flow and dependency rules: [Architecture.md](Architecture.md).
- Reproducible checks and remaining manual scenarios: [Validation.md](Validation.md).
- Room and graph editing: [LoopShift authoring guide](../Assets/LoopShift/Documentation/Authoring.md).
- Current game routing: Assets/Prefabs/LoopShiftGame/GameRooms.asset.
- Main entry scene: Assets/Scenes/MainMenu.unity.

The current structure intentionally keeps serialized content in place. New features should use the ownership rules above; reorganizing assets is a separate migration that must preserve references and metadata.