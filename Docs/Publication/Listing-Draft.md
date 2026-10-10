# LoopShift — Asset Store listing draft

Status: local review draft. Do not publish until the readiness checklist is complete and the publisher has reviewed the copy, compatibility fields, media and disclosure.

## Product name

LoopShift — Endless 2D Level Builder

## Short description

Build connected 2D worlds with a visual room graph, fixed routes and seeded weighted room pools. Stream nearby rooms, reuse instances and reset room behavior through lifecycle callbacks.

## Description

LoopShift connects your room prefabs into a traversable 2D cardinal grid. Use fixed connections for authored sequences, weighted pools for variation, or combine both in the same graph.

Create rooms with the LevelZone component and trigger-based exits, then arrange them in the visual graph editor. Connect exit ports, choose the starting room, organize pools and adjust weights. Validation identifies missing rooms, ambiguous routes and incompatible exits before you run the level.

The runtime loads a configurable neighborhood around the player and reuses rooms through bounded instance pools. Optional recentering shifts the world and registered targets to keep the current room near the origin. Lifecycle callbacks let your own scripts reset enemies, effects and other reusable room state.

### Included

- Visual graph canvas with room and pool nodes, prefab drag-and-drop, pan, zoom, framing and Undo/Redo.
- Fixed connections, exit tags, seeded weighted pools and no-repeat selection rules.
- Room bounds and exit authoring tools with shared graph validation.
- Configurable preload and retention radii, pool limits and optional origin recentering.
- Runtime events and lifecycle interfaces for integration with your game.
- A three-room top-down sample with original geometric visuals, movement controls, automatic eastward traversal and restart.
- Runtime and editor C# source, authoring and integration guides, lifecycle contracts and troubleshooting documentation.

### Requirements and scope

Designed for 2D cardinal room layouts. Compatible opposing exit anchors and consistent room geometry are required. This package does not generate arbitrary 3D terrain, supply navigation AI or implement a complete game progression system.

The sample shader supports the Built-in Render Pipeline and URP. An HDRP sample material is not included. Runtime physics uses Unity's Physics 2D module; editor and sample controls use IMGUI. No third-party runtime framework is required. Optional tests require Unity Test Framework.

Development verification has covered Unity 2022.3.62f3, Unity 6000.3.25f1 LTS and Unity 6000.7.0b1. URP sample rendering has been checked with URP 17.3.0 and 17.7.0. Windows standalone builds and execution have been tested. Do not select other tested platforms or editor versions without corresponding evidence. Final release compatibility must be tied to the submitted archive in Readiness.md.

Pooling bounds retained room instances. Actual CPU and memory use depends on room content, neighborhood size and user callbacks; no universal zero-allocation or frame-rate guarantee is made.

### Support

contact@estrixstudio.com

https://estrixstudio.com

## AI assistance disclosure draft

AI tools assisted development of the package's C# runtime and editor code, automated tests, documentation and the tooling that creates the geometric sample assets. The local conceptual marketing cover was generated with AI and does not depict bundled room artwork or the editor interface. The publisher must review the final functional content, disclosure wording and media before submission.

## Publisher decisions still required

- Price and final product name.
- Confirm the proposed Tools/Level Design category in the publisher portal.
- Approve the description, compatibility selections, disclosure and original media.

No claim of Unity endorsement or Asset Store approval is intended.