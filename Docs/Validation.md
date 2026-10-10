# Validation

## Automated checks

Run Tools > Laments Never End > Run Project Checks in Unity outside Play mode, or use Tools/Validate-Project.ps1 with the editor executable that matches ProjectSettings/ProjectVersion.txt.

The batch entry point is LamentsNeverEnd.Editor.ProjectChecks.Run. It uses a separate GameData instance and does not reset the active singleton or save scenes/assets.

Checks cover:

- Runtime room data cannot mutate default configuration; reset restores independent records.
- First condition registration succeeds; duplicate, completed, and None conditions are rejected.
- Completion fires once, even when a subscriber recursively completes the same event.
- Reset clears progression and permits another run.
- Every connection target exists; the victory room has no outgoing neighbor and retains its win trigger.
- Every configured room has exactly one Resources/Zones prefab.
- Required scenes are enabled, exist, and Gameplay matches its enum index.
- Shadow generation preserves local alignment and removal retains unrelated children.

The batch log is Logs/project-checks.log (ignored by Git). A successful run contains PROJECT CHECKS PASSED and exits with code zero. A C# build of Unity-generated projects is an additional compiler check, but cannot establish scene, physics, or player-build correctness.

## Gameplay verification

1. Start at MainMenu, enter Gameplay, move in each direction, and toggle the torch.
2. Enter neighboring rooms and cross back. Verify camera/player positions and that previous rooms disappear.
3. Start a dialogue near overlapping interactables. Verify one conversation starts and movement locks.
4. Advance through the last line while text is typing. Verify the hidden panel stays cleared and movement resumes.
5. Complete an event, leave its room, and return. Verify completed content does not reset or fire twice.
6. Lose to an enemy, return to MainMenu, and start again. Verify room connections and progression reset.
7. Reach Finish and verify WinScreen opens.
8. Disable/re-enable the Player component and verify disabled input no longer triggers interactions.
9. On a disposable scene copy, create and remove shadows on a tilemap with unrelated children. Check local alignment, Undo, and preservation of unrelated children.
10. Make a development player build and repeat menu, dialogue, torch, loss and win paths.

Visual rendering, coroutine timing, UI input routing, and physics require these in-editor/player checks; passing data checks alone does not establish them.
## Earlier baseline (2026-10-01)

Validation ran in Unity 6000.7.0b1:

| Suite | Result |
| --- | --- |
| Project compilation and content/regression checks | 99 passed |
| LoopShift EditMode | 33 passed, 0 failed |
| LoopShift PlayMode | 18 passed, 0 failed |

EditMode includes graph routing, malformed graph data, authoring reference maintenance, scene-object rejection, duplicate routes and Undo/Redo. PlayMode covers traversal, pooling, lifecycle callbacks, actor parenting, trigger behavior and a bounded allocation benchmark.

The visual editor opens through Window > LoopShift > Level Graph or the graph asset inspector's Open Visual Editor button. Its canvas code compiled successfully; interactive visual inspection and a standalone player build were not performed in this pass.

Test runner XML is in Logs/loopshift-editmode.xml and Logs/loopshift-playmode.xml. Re-run the assemblies LoopShift.Tests.Editor (EditMode) and LoopShift.Tests.PlayMode (PlayMode) in Unity's Test Runner after changes. Gameplay now uses the LoopShift runner through GameLevelRunner. Module tests do not replace the gameplay checklist above.


## Game migration verification (2026-10-02)

25 package PlayMode tests, 100 project checks and 107 game integration assertions passed. Run Tools/Validate-LoopShiftGame.ps1 with -UnityPath to reproduce menu entry, programmatic route/progression checks, pooled-state checks, physical victory, loss reset and restart checks. The route tour bypasses navigation obstacles; visual doorway navigation, dialogue and physical enemy collision still require separate verification.

## Historical verification (2026-10-06)

All runs completed successfully in the installed Unity 6000.7.0b1 editor.

| Suite | Passing checks |
| --- | ---: |
| Project compilation and content/regression checks | 100 |
| LoopShift EditMode | 48 |
| LoopShift PlayMode | 25 |
| Game integration assertions | 107 |

The editor suite now covers graph factory input validation, exit creation and Undo/Redo, nested room ownership, transformed automatic bounds, and graph-window drag/Undo/port connections. The window test accounts for Unity's content offset when sending mouse events.

The bounds correction transforms renderer geometry into room space, avoiding inflated bounds on mirrored rooms and accounting for rotation. Collider world bounds remain conservative; disabled colliders are excluded. Manual bounds are the appropriate choice when inactive collider-only content must define a room footprint.

Retained results are in [LoopShift-Authoring.xml](Evidence/LoopShift-Authoring.xml), [LoopShift-Runtime-Current.xml](Evidence/LoopShift-Runtime-Current.xml), and [Project-Current.json](Evidence/Project-Current.json). The JSON records log hashes and relevant source hashes. The runtime suite deliberately exercises a throwing subscriber; its expected exception is part of a passing error-handling test.

To reproduce: run the project validation script above, run LoopShift.Tests.Editor in EditMode and LoopShift.Tests.PlayMode in PlayMode, then run Tools/Validate-LoopShiftGame.ps1 with the same UnityPath. Keep graphics enabled for tests that open the graph window.

The game route tour invokes traversal programmatically. Physical victory, menu entry, reset and restart are exercised, but visual navigation, dialogue, lighting and physical enemy collision remain manual checks. No other Unity editor version or new distribution archive was validated in this pass.
## Current verification (2026-10-09)

The host has passed 100 project checks, 50 package editor tests, 27 package runtime tests and 198 game integration assertions. The latest game probe replaces the older programmatic route tour with 17 local physical doorway crossings. Authored interaction and dialogue callbacks, torch state, victory, a separate physical enemy collision, and restart are covered. It still does not verify full room-interior navigation, keyboard/controller input, or visual quality.

The current UPM archive passed clean Unity 2022.3.62f3 qualification: 50 editor tests, 28 runtime tests (including the consumer sample), two reload-mode tests and a Windows player smoke run. The exact archive identity and results are in [LoopShift-PreloadFix-Unity2022.json](Evidence/LoopShift-PreloadFix-Unity2022.json).

The Unity 6/URP retry crashed during import with an out-of-memory exception. The first current-archive Asset Store validator attempt failed during editor startup; an isolated Unity 2022.3 retry subsequently passed all 36 checks without warnings. The Unity 6 crash remains an incomplete qualification. Follow [publication readiness](Publication/Readiness.md) for archive-specific qualification and remaining release work; historical successes do not qualify changed archives.
The current native Unity package also passed clean Unity 2022.3.62f3 import, one consumer sample traversal test, two reload-mode tests, Windows build and actual player smoke. See [LoopShift-PreloadFix-NativeUnity2022.json](Evidence/LoopShift-PreloadFix-NativeUnity2022.json). Unlike UPM, this distribution excludes package test assemblies, so its test count is intentionally smaller.
## Unity 6 recovery and final package audit (2026-10-09)

The interrupted Unity 6000.3.25f1 / URP 17.3.0 consumer was recovered. It passed 50 editor tests, 28 runtime tests, two reload-mode tests and Built-in/URP camera rendering. A URP texture left with a failed import was explicitly reimported; a subsequent Windows build and actual player smoke run passed without that import error. [LoopShift-PreloadFix-Unity6URP.json](Evidence/LoopShift-PreloadFix-Unity6URP.json) records the recovery and evidence identity.

The final publisher/documentation rebuild contains identical implementation, tests, sample content and Unity .meta files. Only README.md, Authoring.md and UPM publisher contact metadata changed. [The complete archive comparison](Evidence/LoopShift-PublisherMetadata-ArchiveAudit.json) records both base and final hashes. See [readiness](Publication/Readiness.md) for final validator status and remaining native visual/publisher gates.