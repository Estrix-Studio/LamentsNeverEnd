# Zone Prefab Setup

Each zone prefab needs:

- A root `LevelZone` component.
- One or more child `LevelExit` components.
- Trigger `Collider2D` components on exits.
- A stable zone ID.

Auto bounds are calculated from child `Renderer` and `Collider2D` components. This supports Tilemaps, SpriteRenderers, and mixed prefab content. Manual bounds are available for exact control.
