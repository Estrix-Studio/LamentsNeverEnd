using System;
using System.IO;
using System.Linq;
using LoopShift.Runtime;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LoopShiftConsumerVerification
{
    public static string Sample => AssetDatabase.IsValidFolder("Assets/LoopShiftSample") ? "Assets/LoopShiftSample" : "Assets/LoopShift/Demo/MinimalTopDown";
    public static string Scene => Sample + "/MinimalTopDown.unity";
    public static void Prepare()
    {
        var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(EndlessLevelRunner).Assembly);
        if (info == null)
        {
            if (!File.Exists("Assets/LoopShift/Documentation/Authoring.md") ||
                !File.Exists("Assets/LoopShift/Documentation/Runtime-Contracts.md") ||
                !File.Exists(Scene))
                throw new InvalidOperationException("Unitypackage is missing its sample or documentation.");
            Debug.Log("LOOPSHIFT CLEAN UNITYPACKAGE IMPORTED");
            return;
        }
        if (!File.Exists(Path.Combine(info.resolvedPath, "Documentation~/Authoring.md")) ||
            !File.Exists(Path.Combine(info.resolvedPath, "Documentation~/Runtime-Contracts.md")))
            throw new InvalidOperationException("UPM package is missing its documentation.");
        if (!File.Exists(Path.Combine(info.resolvedPath, "package.json.meta")))
            throw new InvalidOperationException("UPM manifest metadata is missing.");
        var source = Path.Combine(info.resolvedPath, "Samples~/MinimalTopDown");
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
        FileUtil.CopyFileOrDirectory(source, "Assets/LoopShiftSample");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("LOOPSHIFT CLEAN SAMPLE IMPORTED from " + info.resolvedPath);
    }
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(Scene);
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentsInChildren<Component>(true).Any(component => component == null))
                throw new InvalidOperationException("Missing script in sample: " + root.name);
        var graph = AssetDatabase.LoadAssetAtPath<LevelGraphAsset>(Sample + "/EndlessRooms.asset");
        if (graph == null || graph.ValidateGraph().Any(message => message.Severity == LevelValidationSeverity.Error))
            throw new InvalidOperationException("Sample graph is invalid.");
        foreach (var dependency in AssetDatabase.GetDependencies(Scene, true))
            if (dependency.StartsWith("Assets/") && !dependency.StartsWith(Sample + "/") && !dependency.StartsWith("Assets/LoopShift/Runtime/"))
                throw new InvalidOperationException("Sample references external asset: " + dependency);
        Directory.CreateDirectory("Build");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = "Build/LoopShiftDemo.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Sample player build failed: " + report.summary.result);
        Debug.Log("LOOPSHIFT CLEAN PLAYER BUILT: " + report.summary.totalSize + " bytes");
    }
}
