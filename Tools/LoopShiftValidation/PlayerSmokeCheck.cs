using System;
using System.Collections;
using System.Reflection;
using LoopShift.Runtime;
using LoopShift.Samples;
using UnityEngine;

/// <summary>Consumer-only player probe. This file is never exported in LoopShift.</summary>
public sealed class LoopShiftPlayerSmokeCheck : MonoBehaviour
{
    private bool _finished;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-loopshift-smoke") < 0) return;
        Application.runInBackground = true;
        new GameObject("LoopShift player verification").AddComponent<LoopShiftPlayerSmokeCheck>();
    }

    private void OnEnable() => Application.logMessageReceived += OnLog;
    private void OnDisable() => Application.logMessageReceived -= OnLog;
    private void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            Finish(false, message);
    }

    private IEnumerator Start()
    {
        yield return null;
        var runner = FindAnyObjectByType<EndlessLevelRunner>();
        var demo = FindAnyObjectByType<TopDownDemo>();
        if (runner == null || demo == null || !runner.IsRunning) { Finish(false, "Sample did not start."); yield break; }
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var autoWalk = typeof(TopDownDemo).GetField("_autoWalk", flags);
        autoWalk.SetValue(demo, true);
        typeof(TopDownDemo).GetField("speed", flags).SetValue(demo, 20f);
        var deadline = Time.realtimeSinceStartup + 30;
        while (demo.Visits < 15 && Time.realtimeSinceStartup < deadline && !_finished)
        {
            if (!runner.IsRunning || runner.ActiveZones.Count > 9 ||
                runner.PooledZoneCount > runner.SpawnSettings.maxPooledInstances)
            { Finish(false, "Streaming stopped or exceeded its configured bounds."); yield break; }
            yield return null;
        }
        if (_finished) yield break;
        if (demo.Visits < 15) { Finish(false, "Physics traversal timed out."); yield break; }
        autoWalk.SetValue(demo, false);
        runner.StopRunner();
        if (runner.ActiveZones.Count != 0) { Finish(false, "Stop retained active rooms."); yield break; }
        demo.GetComponent<Rigidbody2D>().position = Vector2.zero;
        demo.transform.position = Vector3.zero;
        runner.StartRunner();
        if (!runner.IsRunning || runner.CurrentCell != Vector2Int.zero)
        { Finish(false, "Restart did not restore the origin."); yield break; }
        yield return null;
        runner.StopRunner();
        Finish(true, "15 physical crossings, bounded room counts, stop and restart.");
    }

    private void Finish(bool success, string detail)
    {
        if (_finished) return;
        _finished = true;
        Debug.Log((success ? "LOOPSHIFT PLAYER SMOKE PASSED: " : "LOOPSHIFT PLAYER SMOKE FAILED: ") + detail);
        Application.Quit(success ? 0 : 1);
    }
}
