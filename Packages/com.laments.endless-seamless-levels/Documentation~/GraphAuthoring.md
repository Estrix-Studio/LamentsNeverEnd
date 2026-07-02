# Graph Authoring

`LevelGraphAsset` supports two connection styles.

## Fixed Connections

Use fixed connections when a specific zone should always follow another zone in a direction. Bidirectional connections automatically create the reverse link.

## Weighted Pools

Use pools for endless variation. Add zone entries with weights, then add pool rules that say which pool can be used when leaving a zone in a direction with an optional exit tag.

The runner uses a deterministic seed by default, so the same seed produces the same sequence.
