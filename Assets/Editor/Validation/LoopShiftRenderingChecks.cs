using System;
using System.IO;
using LoopShift.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace LamentsNeverEnd.Editor
{
    /// <summary>Repository-only rendering probe; no changes are saved to project settings.</summary>
    public static class LoopShiftRenderingChecks
    {
        public static void ConfigureConsumer()
        {
            if (!Application.isBatchMode || !Directory.Exists("Assets/LoopShiftSample"))
                throw new InvalidOperationException("Only configure a disposable imported-sample consumer.");
            const string folder = "Assets/LoopShiftVerification";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "LoopShiftVerification");
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, folder + "/Renderer.asset");
            var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipeline, folder + "/Pipeline.asset");
            QualitySettings.renderPipeline = null;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log("LOOPSHIFT CONSUMER CONFIGURED FOR URP");
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this probe in its own batch editor.");
            var previousPipeline = GraphicsSettings.defaultRenderPipeline;
            var previousQuality = QualitySettings.renderPipeline;
            UniversalRenderPipelineAsset pipeline = null;
            UniversalRendererData rendererData = null;
            EndlessLevelRunner runner = null;
            try
            {
                EditorSceneManager.OpenScene((AssetDatabase.IsValidFolder("Assets/LoopShiftSample") ? "Assets/LoopShiftSample" : "Assets/LoopShift/Demo/MinimalTopDown") + "/MinimalTopDown.unity");
                runner = Object.FindAnyObjectByType<EndlessLevelRunner>();
                var camera = Object.FindAnyObjectByType<Camera>();
                if (runner == null || camera == null) throw new InvalidOperationException("Sample setup is missing.");
                runner.StartRunner();
                if (!runner.IsRunning) throw new InvalidOperationException("Sample runner did not start.");
                Directory.CreateDirectory("Build/LoopShift/Rendering");
                QualitySettings.renderPipeline = null;
                GraphicsSettings.defaultRenderPipeline = null;
                Capture(camera, "BuiltIn", false);
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                GraphicsSettings.defaultRenderPipeline = pipeline;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                Capture(camera, "URP", true);
                Debug.Log("LOOPSHIFT RENDERING CHECKS PASSED: Built-in and URP sample pixels.");
            }
            finally
            {
                if (runner != null) runner.StopRunner();
                QualitySettings.renderPipeline = previousQuality;
                GraphicsSettings.defaultRenderPipeline = previousPipeline;
                if (pipeline != null) Object.DestroyImmediate(pipeline);
                if (rendererData != null) Object.DestroyImmediate(rendererData);
            }
        }

        private static void Capture(Camera camera, string name, bool useSrp)
        {
            var texture = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(640, 360, TextureFormat.RGBA32, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                texture.Create();
                camera.aspect = 640f / 360;
                if (useSrp)
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = texture });
                else
                {
                    camera.targetTexture = texture;
                    camera.Render();
                }
                if (useSrp && !(RenderPipelineManager.currentPipeline is UniversalRenderPipeline))
                    throw new InvalidOperationException("URP render request did not use UniversalRenderPipeline.");
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
                image.Apply();
                var colored = 0;
                var magenta = 0;
                var white = 0;
                foreach (var pixel in image.GetPixels32())
                {
                    if (pixel.r > 200 && pixel.b > 200 && pixel.g < 60) magenta++;
                    if (pixel.r > 220 && pixel.g > 220 && pixel.b > 220) white++;
                    if (pixel.g > 30 && pixel.b > 40 && pixel.g > pixel.r * 1.3f && pixel.b > pixel.r * 1.3f) colored++;
                }
                File.WriteAllBytes("Build/LoopShift/Rendering/" + name + ".png", image.EncodeToPNG());
                File.WriteAllText("Build/LoopShift/Rendering/" + name + ".txt",
                    $"Colored pixels: {colored}; white actor pixels: {white}; magenta error pixels: {magenta}");
                if (colored < 4000 || white < 10 || magenta > 0)
                    throw new InvalidOperationException($"{name} sample render failed: colored={colored}, actor={white}, magenta={magenta}");
                Debug.Log($"LOOPSHIFT {name} RENDER: colored={colored}, actor={white}, magenta={magenta}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                texture.Release();
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(image);
            }
        }
    }
}
