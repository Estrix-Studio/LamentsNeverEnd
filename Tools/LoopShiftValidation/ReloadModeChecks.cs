using System.Collections;
using System.Reflection;
using LoopShift.Runtime;
using LoopShift.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace LoopShift.ConsumerTests
{
    public class ReloadModeChecks
    {
        private bool _previousEnabled;
        private EnterPlayModeOptions _previousOptions;

        [SetUp]
        public void SaveSettings()
        {
            _previousEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            _previousOptions = EditorSettings.enterPlayModeOptions;
        }

        [UnityTearDown]
        public IEnumerator RestoreSettings()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            EditorSettings.enterPlayModeOptionsEnabled = _previousEnabled;
            EditorSettings.enterPlayModeOptions = _previousOptions;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        [UnityTest]
        public IEnumerator DomainReloadDisabledSupportsConsecutiveRuns() => VerifyRuns(false);

        [UnityTest]
        public IEnumerator DomainAndSceneReloadDisabledSupportConsecutiveRuns() => VerifyRuns(true);

        private static IEnumerator VerifyRuns(bool disableSceneReload)
        {
            var folder = AssetDatabase.IsValidFolder("Assets/LoopShiftSample")
                ? "Assets/LoopShiftSample" : "Assets/LoopShift/Demo/MinimalTopDown";
            EditorSceneManager.OpenScene(folder + "/MinimalTopDown.unity");
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload |
                (disableSceneReload ? EnterPlayModeOptions.DisableSceneReload : EnterPlayModeOptions.None);
            for (var run = 0; run < 2; run++)
            {
                yield return new EnterPlayMode(false);
                yield return null;
                var runner = Object.FindAnyObjectByType<EndlessLevelRunner>();
                var demo = Object.FindAnyObjectByType<TopDownDemo>();
                Assert.That(runner, Is.Not.Null);
                Assert.That(demo, Is.Not.Null);
                Assert.That(runner.IsRunning, Is.True, "Fresh Play session did not start.");
                Assert.That(runner.CurrentCell, Is.EqualTo(Vector2Int.zero));
                var initialVisits = demo.Visits;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(TopDownDemo).GetField("_autoWalk", flags).SetValue(demo, true);
                typeof(TopDownDemo).GetField("speed", flags).SetValue(demo, 20f);
                var deadline = Time.realtimeSinceStartup + 10;
                while (demo.Visits < initialVisits + 4 && Time.realtimeSinceStartup < deadline)
                {
                    Assert.That(runner.ActiveZones.Count, Is.LessThanOrEqualTo(9));
                    Assert.That(runner.PooledZoneCount, Is.LessThanOrEqualTo(runner.SpawnSettings.maxPooledInstances));
                    yield return null;
                }
                Assert.That(demo.Visits, Is.GreaterThanOrEqualTo(initialVisits + 4));
                yield return new ExitPlayMode();
                Assert.That(Object.FindObjectsByType<LevelZone>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                    Is.Empty, "Exiting Play mode leaked generated rooms.");
            }
        }
    }
}
