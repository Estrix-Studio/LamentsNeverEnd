# LoopShift delivery plan

Goal: rework and optimize the whole endless-level mechanic, provide an easy visual authoring editor, and deliver a separate Unity package ready for Asset Store submission. This is the acceptance ledger, not a completion claim.

## Initial evidence (2026-10-01; superseded by validation entries below)

- Source: Assets/LoopShift, an embedded Git repository, initially clean. Parent game has pre-existing changes.
- Existing runtime: 2D cardinal cell streamer, fixed graph connections, weighted pools, pooled zone prefabs.
- Existing editor: list inspectors and asset creation wizard only; no visual graph editor.
- Manifest: com.estrixstudio.loopshift 0.1.0, claims Unity 2022.3, sample path is missing.
- Available editor: 6000.7.0b1 only. Minimum-version and stable-LTS compatibility are unverified.
- Initial defects: allocating connection keys/snapshots, preload radius off-by-one, repeated full-grid scans, pool selection ignores compatible return exits, ID overrides not used during resolution, validator misses null/ambiguous connections and invalid pool data, runtime target ownership can be lost on shutdown.
- The legacy Gameplay.LevelTraversal is still used by the game. Reworking only the reusable module without a demonstrated game migration will not complete the objective.

## Design

Reuse the package's existing serialized zone/exit/graph types and GUIDs. Keep runtime free of editor and game dependencies. Compile graph configuration once at startup into typed routing keys and filtered weighted candidates. Separate graph authoring data from instantiated runtime state. Keep authoring changes undoable and deterministic runtime choices reproducible.

Use stable Unity Editor APIs for the visual canvas (avoid requiring an experimental graph package). Provide pan/zoom, draggable zone/pool nodes, exit connections, selection/details, validation, start-zone assignment, prefab drag/drop, persistence and Undo/Redo. Reuse the same validator and serialized editing operations in inspector, canvas and wizard.

## Ordered work and acceptance evidence

1. Runtime graph contracts and tests
   - Typed connection keys; canonical IDs including overrides and pool entries.
   - Deterministic weighted selection; filter incompatible exits; strict no-repeat behavior.
   - Actionable diagnostics for malformed, ambiguous, missing, duplicate and overflow-prone data.
   - EditMode tests covering malformed and valid graphs and repeated allocation-free resolution.
2. Streaming rework and optimization
   - Correct bounded preload/active radii, bounded queues and pool memory.
   - Cached read-only active view, reusable work buffers, no recurring transition allocations after warmup where feasible.
   - Correct target filtering/ownership, cooldown or entry gating, reentrant callbacks, lifecycle cleanup, disable/re-enable and domain-reload-disabled operation.
   - Correct alignment, pivot/scale handling, revisits, arbitrary graph cycles and fixed/pool conflicts.
   - PlayMode traversal, stress, determinism, pool reset and allocation/time measurements.
3. Visual authoring editor
   - Complete canvas workflows described above, no Inspector-only substitute.
   - Scene tools for bounds/exits; clear diagnostics and quick fixes.
   - Editor interaction/persistence tests and captured visual verification.
4. Separate distributable package
   - Coherent runtime/editor/test assemblies and stable package layout.
   - Self-contained sample scenes and content without game assets or undeclared dependencies.
   - UPM archive plus conventional .unitypackage export, reproducible build scripts.
   - Clean-project import, sample run, player build and minimum supported editor testing.
5. Integrate current game
   - Adapter/migration for current rooms, event IDs, progression and player/camera targets.
   - Preserve serialized assets or provide reversible migration.
   - Verify menu, looping traversal, dialogue, loss, restart and victory using the new runner.
6. Documentation and submission readiness
   - Quick start, visual editor guide, API/lifecycle, tuning, troubleshooting, migration, changelog and license/notices.
   - Store description, accurate compatibility/dependency disclosures, original screenshots/media.
   - Asset Store validation and current submission-guideline audit.
   - Record any publisher/account decisions needed; do not invent business details or submit publicly without authorization.
7. Final audit
   - Inspect actual artifacts/results against every item above. No completion until all are proven.

## Publication references

Checked 2026-10-01:
- https://assetstore.unity.com/publishing/submission-guidelines
- https://docs.unity.com/en-us/engine/6000.0/manual/packages-list/cus-pkg-development/custom-packages
- https://unity.com/core-standards

Recheck before delivery. In particular, current submission guidance includes domain-reload-disabled support for Unity 6.6+; UPM publication availability and signing workflows depend on publisher access. Preparing submission is distinct from Unity approving it.

## Validation log

2026-10-01: 28/28 package EditMode tests passed in Unity 6000.7.0b1 (Windows), exit code 0. Evidence: Docs/Evidence/LoopShift-GraphAndPreload.xml and matching source hashes in LoopShift-GraphAndPreload.json.

Implemented: typed route keys, logical IDs and pool-only registry, compatible weighted candidate compilation, no-repeat semantics, 64-bit weights, malformed/ambiguous graph diagnostics, missing assembly-reference fix, null exit-list recovery, bounded breadth-first preload with reusable buffers, release-buffer reuse, and stable live read-only ActiveZones view. Warmed 10,000-iteration checks measured zero managed bytes for route resolution and active-view reads.

Phase 1 foundation is tested. Phase 2 is partially implemented. Still unresolved: transition reentrancy, target ownership/cleanup, trigger bounce, graph cycles conflicting with occupied grid cells, geometry alignment/pivot/scale, memory bounds across many prefabs, pooled object lifecycle/reset, and full-transition profiling.

Next action: finish streaming lifecycle/topology with PlayMode tests, then visual authoring. Visual editor, independent distribution, samples, legacy-game migration and publication artifacts remain incomplete. No Asset Store readiness claim.

The host project's pre-existing malformed tile metadata warning is outside this package. A clean-project import remains required so host content cannot hide package problems.
## Standalone distribution progress (2026-10-01)

The preceding goal turn made verified progress (editor completion, fixes and passing tests). This continuation adds a self-contained sample and proves a first standalone consumer path.

Implemented:
- Demo/MinimalTopDown contains three original room prefabs, a fixed-plus-weighted graph, a physics actor, camera, dependency-free controls, simple original sprite art and an unlit shader.
- Tools/Build-LoopShift.ps1 builds the sample and both UPM and .unitypackage archives. The UPM export puts optional sample content under Samples~; the .unitypackage includes the runnable sample directly. Generated staging directories remain under ignored Build/.
- The source manifest now points to the existing sample and declares the built-in 2D physics and IMGUI dependencies.
- Tools/Validate-LoopShiftPackage.ps1 creates a fresh consumer project, installs the tarball, imports the sample, runs tests and builds a Windows player. Helper sources are under Tools/LoopShiftValidation and are not shipped.
- Root package README and changelog describe setup, editor workflow, runtime contracts, limitations and development compatibility status.

Verified against the actual exported UPM archive:
- Fresh project installed the tarball and compiled package runtime/editor without host-game packages.
- Sample imported by copying its packaged Samples~ content into Assets (the equivalent file-copy operation, not a visually verified Package Manager UI workflow).
- 33/33 EditMode tests passed.
- 19/19 PlayMode tests passed, including 15 actual physics-triggered sample room crossings, bounded active/pool counts and restart.
- Windows x64 development player build succeeded (188,935,458 bytes reported by BuildPipeline). This proves build compatibility in the tested editor; the player has not yet been launched or visually verified.
- .unitypackage contents audited: 46 entries, all under Assets/LoopShift. A clean import of this second format still needs verification.
- Evidence: Docs/Evidence/LoopShift-Consumer-EditMode.xml, LoopShift-Consumer-PlayMode.xml and LoopShift-Consumer.json.

Artifacts (ignored local output):
- Build/LoopShift/com.estrixstudio.loopshift-0.1.0.tgz
- Build/LoopShift/LoopShift-0.1.0.unitypackage
- Build/LoopShift/Consumer-UPM/Build/LoopShiftDemo.exe (requires adjacent player data)
- Build/LoopShift/checksums.json

Remaining acceptance work includes game migration, visual/editor interaction verification, stable/minimum Unity version testing, URP/HDRP rendering checks, geometry/long-run/domain-reload stress, scene authoring tools, wizard consistency, lifecycle/API/documentation review, clean .unitypackage import, player execution, store media and Asset Store validation. The 2022.3 manifest minimum remains an unverified target. Neither archive is being claimed as publication-ready.


## Installed game migration and origin/cleanup fixes (2026-10-02)

The previous continuation made concrete progress: optional recentering and its tests passed, and a migration process was launched. This continuation verified that process, installed the migration in the actual game, and repaired issues exposed by real scene changes.

Current state:
- Gameplay.unity now uses EndlessLevelRunner + GameLevelRunner and GameRooms.asset (15 rooms, 43 one-way routes). Original scene GUID/build index remain unchanged. Original Resources/Zones content and legacy scripts remain for comparison; they no longer drive current Gameplay.
- GameLevelRunner maps logical room entry to GameData once per accepted crossing and registers player/camera targets.
- recenterOnTransition keeps active world positions near the runner origin; WorldShifted exposes the offset. Nested targets move once.
- Separately owned active/pool scene roots allow runner GameObject deactivation without invalid hierarchy reparenting. Pool release never recreates a root destroyed during scene unload.
- Pooled enemies reset spawn pose and chase state. Completed fading objects do not leave paused fades or start coroutines in inactive rooms.
- The pre-existing terminal-room solid collider was corrected to a trigger in both source and migrated prefabs. ProjectChecks now verifies this property.
- Installation tooling reopens the saved Gameplay scene to verify its graph reference persists.

Evidence:
- 25/25 package PlayMode tests pass, including recentering, 100 repeated transitions, whole-GameObject disable/restart, destroyed-pool cleanup and scene unload.
- 100/100 project data/content checks pass.
- 107 game integration assertions pass in Unity 6000.7.0b1 with graphics enabled: real MainMenu callback loads the original Gameplay build index; a 14-transition tour checks destinations, exactly-once progression and room limits; pooled-state checks pass; the real victory collider loads WinScreen; loss and both restart cycles restore a fresh run.
- Route-tour movement is programmatic and deliberately disables the actor collider; it proves routing/progression contracts, not navigability of every authored doorway. The final victory check uses an enabled actor collider and physics. Loss uses GameData.RestartGame, not a physical enemy collision.
- Docs/Evidence/LoopShift-Ownership.xml and LoopShift-GameIntegration.json preserve test/source identities. Tools/Validate-LoopShiftGame.ps1 reproduces the game checks.

Still required: visual editor/gameplay inspection, dialogue/torch/enemy-collision tests, physical navigation through authored rooms, non-default lifecycle modes, minimum/stable editor and render-pipeline compatibility, further geometry/performance tests, and Asset Store validation/media. Distribution archives built on 2026-10-01 predate these runtime fixes and must be rebuilt and revalidated before release. Full goal remains active.


## Current distribution verification (2026-10-06)

Progress: authoring and transformed-bounds fixes passed 48 host EditMode tests, 25 runtime tests, 100 project checks and 107 game integration assertions. Both archives were rebuilt. The unitypackage export now includes the detailed guides; canonical source is Documentation and UPM staging uses Documentation~. Eighty-two runtime/editor/test/documentation files matched the staging snapshot by hash.

Fresh UPM consumer: 48 package EditMode tests, 26 PlayMode tests (25 runtime plus the physical sample test), Windows player build and execution passed. Fresh unitypackage consumer: sample import and documentation presence, one physical sample test, Windows build and player execution passed. Both actual players completed 15 physical crossings and stop/restart checks. Consumer-only probes are not exported.

Each archive also passed two reload-mode tests. Each test performs two consecutive Play sessions, with domain reload disabled and either scene reload enabled or disabled. Checks cover startup, four physical crossings per session, instance limits and no generated rooms after exit. Corrected consumer probes use FindAnyObjectByType to avoid deprecated API warnings.

Evidence is in Docs/Evidence/LoopShift-20261006-Distributions.json, matching XML files and player logs. Current archives remain Build/LoopShift/com.estrixstudio.loopshift-0.1.0.tgz and Build/LoopShift/LoopShift-0.1.0.unitypackage. Validation is reproducible with Tools/Validate-LoopShiftPackage.ps1 -Format UPM or UnityPackage.

The support email is contact@estrixstudio.com, supplied by the user. Website remains unspecified. Publication readiness still requires supported-editor and rendering verification, visual inspection/media, game interaction/navigation checks, broader performance/geometry work, remaining documentation/notices, Asset Store validator results and final listing details. The full goal remains active.

## Export stability and stress coverage (2026-10-07)

All 26 host PlayMode tests passed, including 768 square-route transitions across preload radii zero through two. The stress test checks active/pool limits, recentering, logical round trips and cleanup; it is not a universal performance guarantee.

Packaging now validates and exports the existing saved sample. Regeneration is an explicit Tools > LoopShift > Rebuild distribution sample operation; ordinary exports no longer rewrite sample object IDs. A before/after hash check verified all existing sample files were preserved during export (Docs/Evidence/LoopShift-PreservingExport.json). The newly rebuilt archive includes the stress test and corrected test instructions; clean minimum-editor verification runs in Build/S22.

The current game passed 123 integration assertions after fixing the starting dialogue prefab's completion event. Dialogue was advanced through the UI Next callback, torch state was checked and authored enemy contact triggered loss. Physical doorway navigation and native input/visual checks remain open.