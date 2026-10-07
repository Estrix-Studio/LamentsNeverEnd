# LoopShift tests

Open the Unity Test Runner, select EditMode, and run the LoopShift.Tests.Editor assembly.

For batch validation (replace the editor and project paths):

~~~powershell
& 'C:/path/to/Unity.exe' -batchmode -projectPath 'C:/path/to/project' -runTests -testPlatform EditMode -assemblyNames LoopShift.Tests.Editor -testResults 'C:/path/to/results.xml' -logFile 'C:/path/to/test.log'
~~~

Do not add -quit: the Unity Test Runner exits when it finishes. Close the same project in the editor before launching batch validation.

Keep graphics enabled: the editor tests exercise an EditorWindow. To run runtime coverage, select PlayMode in the Test Runner, or replace the batch arguments with -testPlatform PlayMode -assemblyNames LoopShift.Tests.PlayMode.

The test assemblies depend on Unity Test Framework. Runtime distribution has no NUnit dependency. When installed through UPM, add com.estrixstudio.loopshift to the host manifest's testables list to enable package tests; the host project must install the test framework explicitly.

Current coverage:

- Fixed-route priority, bidirectional connections and logical ID overrides.
- Case-insensitive exit tags with case-sensitive logical IDs.
- Pool-only entries, compatible target filtering and no-repeat rules.
- Seeded reproducibility and large weighted totals without integer overflow.
- Null collections, missing/ambiguous routes, duplicate entries and invalid weights.
- Null exit-list diagnostics and authoring repair.
- Bounded preload windows, retention clamping, zero/negative radius.
- Reuse across stop/start and live read-only active-zone views.
- Managed allocations for 10,000 warmed-up resolutions and active-view reads.

These EditMode tests are not a substitute for PlayMode physics/lifecycle tests, full-transition profiling, visual editor interaction testing, supported-editor compatibility, or clean package installation.
PlayMode coverage includes physics-triggered traversal, compound actor gating, pooled lifecycle callbacks, callback reentrancy, scene cleanup, recentering and managed allocations during warmed transitions. A square-route stress test performs 256 transitions at each preload radius from zero through two (768 total), checking active and pooled room limits, origin position, return to the starting cell and stop cleanup. These bounded checks do not establish performance for every authored graph or prefab.
