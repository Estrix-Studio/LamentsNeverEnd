# Tile Size And Bounds

The system does not assume a fixed tile size, square room, or fixed 20x20 layout.

Zone placement is based on:

- Cached auto bounds from renderers and colliders.
- Manual size override when needed.
- Exit anchor alignment when matching exits exist.
- Optional bounds padding from `LevelSpawnSettings`.

For seamless art, place exit anchors at the exact point where connected zones should meet.
