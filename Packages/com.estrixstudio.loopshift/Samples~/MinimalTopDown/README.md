# Minimal Top Down

Open `Scenes/MinimalTopDown` and enter Play mode. Move the yellow player with WASD or the arrow keys. Crossing either end of a zone makes the adjacent pooled zone current and streams another zone ahead.

The sample demonstrates:

- a `LevelZone` prefab with East and West tagged exits;
- automatic renderer bounds;
- a graph containing one weighted pool and two directional pool rules;
- deterministic preloading and instance reuse; and
- a target with `Rigidbody2D`, `Collider2D`, and a small controller compatible with either Unity input backend.
