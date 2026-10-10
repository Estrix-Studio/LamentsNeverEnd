# Native review and product capture checklist

Status: not executed. Automated tests and camera renders do not satisfy these checks. The computer-use runtime fails at sandbox initialization, before a window can be inspected. Run this review when native UI access is restored, or record a human review. Keep the generated cover unchanged, as requested by the publisher.

## Review setup

Use a disposable Unity 2022.3.62f3 or 6000.3.25f1 project with the final LoopShift archive. Import the Minimal Top Down sample. Keep the Console visible and clear it before each scenario. Record the editor version, pipeline, archive SHA-256, observations and real screenshots with every result. A checkbox below is a task, not evidence of success.

## Authoring workflow

- [ ] Follow README's installation and sample instructions without repository-only files. Check labels and menu paths against the guide.
- [ ] Open Window > LoopShift > Level Graph and the sample graph. At the minimum supported window size and a normal desktop size, verify toolbar, canvas, Details and validation text remain readable and reachable.
- [ ] Create a separate graph from saved sample room prefabs. Drag a prefab onto the canvas, move nodes, pan, zoom and Frame all. Check selection and ports after panning/zooming.
- [ ] Connect opposing exits, inspect the created fixed route, enable a return route, and undo/redo. Check the data and visible connection agree.
- [ ] Add a weighted pool, change weights and a pool rule, rename a room and select the start room. Verify referenced identifiers update and the graph remains valid.
- [ ] Make one deliberately invalid connection or missing assignment. Confirm the error is actionable, then correct it. Save, close and reopen the graph to verify persistence.
- [ ] In Prefab Mode, inspect room bounds and exit handles. Move an exit, undo/redo, save and reopen. Check placement visually against the actual doorway.

## Runtime workflow

- [ ] In the sample, use WASD/arrows and on-screen controls through all cardinal exits. Verify actor movement, door alignment, camera behavior and absence of visible room gaps.
- [ ] Enable automatic traversal, then restart. Verify controls stay usable and the start room/run reset is visible.
- [ ] Repeat Play/Stop with domain reload disabled and both scene reload settings. Confirm the scene returns cleanly, with no leftover runtime roots or missing actor references.
- [ ] In the host game, navigate room interiors with actual player input, complete dialogue/interactions, and reach the terminal room. Do not teleport to doorway approaches for this review. Exercise gates, backtracking, torch, enemy loss and restart.

## Real product captures still needed

| Capture | Show | Acceptance |
| --- | --- | --- |
| Graph overview | Sample rooms, routes and pool in the real graph editor | Legible labels; no errors, clipped controls or unrelated project content |
| Authoring detail | A selected route or weighted pool and its actual Details controls | Values agree with graph data and listing claims |
| Room setup | Bounds and opposing exit handles in Prefab Mode | Room footprint and doorway positions are clear |
| Runtime sample | Included sample crossing a doorway | Actual product output; no substituted or generated screenshot |
| Short demonstration | Create a connection, adjust a pool, Play, cross a doorway, restart | Real interaction, readable pacing and no unsupported feature claims |

Concept art is not evidence for any capture or usability check above. Use original product output for screenshots and recordings. Review final captures for private desktop information before using them in the listing.

## Marketing artwork

Media/LoopShift-Cover-Concept.png is the original 1536x1024 image from the built-in image-generation tool. The publisher explicitly chose to retain the original concept only on 2026-10-10. No resizing, cropping, icon or card variants were performed. The full prompt and SHA-256 are adjacent to the file.

The concept depicts connected 2D rooms and contains the intended product title, subtitle and publisher name. It is a conceptual illustration, not the editor UI or the included sample art. It must not imply that the illustrated room artwork ships in the package.

Unity's [publisher image template](https://assetstore.unity.com/publishing/image-template), checked on 2026-10-10, specifies a 1950x1300 cover, 420x280 card and 160x160 icon. The retained original is not a size-qualified cover upload. Final artwork exports and publisher approval remain open.