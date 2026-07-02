# Zone Prefab Setup

Each zone prefab needs:

- A root `LevelZone` component.
- One or more child `LevelExit` components.
- Trigger `Collider2D` components on exits.
- A stable zone ID. The component generates one automatically, but readable IDs are easier to debug.

## Bounds

Auto bounds are calculated from child `Renderer` and `Collider2D` components. This supports Tilemaps, SpriteRenderers, and mixed prefab content. Manual bounds are available for exact control.

## Exits

Set each exit direction to the side it leaves from: North, East, South, or West. The target zone should have an opposite exit. Exit tags allow you to restrict compatibility, for example a `Forest` exit only connects to another `Forest` exit.
