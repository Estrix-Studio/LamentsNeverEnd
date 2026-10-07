using System.Collections;
using LoopShift.Runtime;
using LoopShift.Samples;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

public class LoopShiftSampleSmokeTests
{
#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator ExportedSampleTraversesMultipleRoomsAndRestarts()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            (AssetDatabase.IsValidFolder("Assets/LoopShiftSample") ? "Assets/LoopShiftSample" : "Assets/LoopShift/Demo/MinimalTopDown") + "/MinimalTopDown.unity", new LoadSceneParameters(LoadSceneMode.Single));
        var demo = Object.FindAnyObjectByType<TopDownDemo>();
        var runner = Object.FindAnyObjectByType<EndlessLevelRunner>();
        Assert.That(demo, Is.Not.Null);
        Assert.That(runner, Is.Not.Null);
        Assert.That(runner.IsRunning, Is.True);
        var demoType = typeof(TopDownDemo);
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        demoType.GetField("_autoWalk", flags).SetValue(demo, true);
        demoType.GetField("speed", flags).SetValue(demo, 20f);
        var deadline = Time.realtimeSinceStartup + 15;
        while (demo.Visits < 15 && Time.realtimeSinceStartup < deadline)
        {
            Assert.That(runner.IsRunning, Is.True);
            Assert.That(runner.ActiveZones.Count, Is.LessThanOrEqualTo(9));
            Assert.That(runner.PooledZoneCount, Is.LessThanOrEqualTo(runner.SpawnSettings.maxPooledInstances));
            yield return null;
        }
        Assert.That(demo.Visits, Is.GreaterThanOrEqualTo(15), "Actor stopped crossing real sample doorways.");
        runner.StopRunner();
        Assert.That(runner.ActiveZones, Is.Empty);
        var body = demo.GetComponent<Rigidbody2D>();
        body.position = Vector2.zero;
        demo.transform.position = Vector3.zero;
        runner.StartRunner();
        Assert.That(runner.IsRunning, Is.True);
        Assert.That(runner.CurrentCell, Is.EqualTo(Vector2Int.zero));
        runner.StopRunner();
    }
#endif
}
