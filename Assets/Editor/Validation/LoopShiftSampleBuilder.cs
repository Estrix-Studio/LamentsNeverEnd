using System;
using System.IO;
using System.Linq;
using LoopShift.Editor;
using LoopShift.Runtime;
using LoopShift.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LamentsNeverEnd.Editor
{
    /// <summary>Repository build tooling. Not part of the distributed package.</summary>
    public static class LoopShiftSampleBuilder
    {
        public const string Root = "Assets/LoopShift/Demo/MinimalTopDown";
        public const string ScenePath = Root + "/MinimalTopDown.unity";

        [MenuItem("Tools/LoopShift/Rebuild distribution sample")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root + "/Art");
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var sprite = CreateSprite();
                var materialPath = Root + "/Art/Unlit.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("LoopShift/Samples/Unlit Sprite"));
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                var blue = CreateRoom("Atrium", new Color(0.08f, 0.23f, 0.32f), new Color(0.23f, 0.77f, 0.85f), sprite, material);
                var amber = CreateRoom("Gallery", new Color(0.29f, 0.19f, 0.1f), new Color(0.95f, 0.65f, 0.24f), sprite, material);
                var violet = CreateRoom("Garden", new Color(0.2f, 0.14f, 0.29f), new Color(0.7f, 0.48f, 0.87f), sprite, material);
                var graphPath = Root + "/EndlessRooms.asset";
                var graph = AssetDatabase.LoadAssetAtPath<LevelGraphAsset>(graphPath);
                if (graph == null) { graph = ScriptableObject.CreateInstance<LevelGraphAsset>(); AssetDatabase.CreateAsset(graph, graphPath); }
                using (var data = new SerializedObject(graph))
                {
                    foreach (var name in new[] { "zones", "fixedConnections", "pools", "poolRules" }) data.FindProperty(name).arraySize = 0;
                    data.FindProperty("startZoneId").stringValue = "";
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                LevelGraphAuthoring.AddZone(graph, blue, new Vector2(0, 0));
                LevelGraphAuthoring.AddZone(graph, amber, new Vector2(330, 0));
                LevelGraphAuthoring.AddZone(graph, violet, new Vector2(660, 0));
                LevelGraphAuthoring.Connect(graph, "Atrium", LevelDirection.East, "Default", "Gallery", "Default", true);
                var pool = LevelGraphAuthoring.AddPool(graph, new Vector2(330, 290));
                LevelGraphAuthoring.RenamePool(graph, pool.poolId, "Endless");
                foreach (var id in new[] { "Atrium", "Gallery", "Garden" }) LevelGraphAuthoring.AddToPool(graph, "Endless", id);
                foreach (var direction in Directions) LevelGraphAuthoring.AddPoolRule(graph, "Endless", direction, "");
                using (var data = new SerializedObject(graph))
                {
                    data.FindProperty("pools").GetArrayElementAtIndex(0).FindPropertyRelative("entries").GetArrayElementAtIndex(2).FindPropertyRelative("weight").intValue = 2;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                var errors = graph.ValidateGraph().Where(m => m.Severity == LevelValidationSeverity.Error).ToArray();
                if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n", errors.Select(m => m.Message)));

                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.orthographic = true;
                camera.orthographicSize = 7;
                camera.backgroundColor = new Color(0.025f, 0.04f, 0.06f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.transform.position = new Vector3(0, 0, -10);
                var actor = Visual("Explorer", null, Vector2.zero, new Vector2(0.5f, 0.5f), Color.white, sprite, material, 10);
                actor.AddComponent<CircleCollider2D>().radius = 0.45f;
                var body = actor.AddComponent<Rigidbody2D>();
                body.gravityScale = 0;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var runner = new GameObject("Endless Level Runner").AddComponent<EndlessLevelRunner>();
                using (var data = new SerializedObject(runner))
                {
                    data.FindProperty("graph").objectReferenceValue = graph;
                    var targets = data.FindProperty("targets");
                    targets.arraySize = 1;
                    targets.GetArrayElementAtIndex(0).objectReferenceValue = actor.transform;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                var demo = actor.AddComponent<TopDownDemo>();
                using (var data = new SerializedObject(demo))
                {
                    data.FindProperty("runner").objectReferenceValue = runner;
                    data.FindProperty("followCamera").objectReferenceValue = camera;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("LOOPSHIFT SAMPLE GENERATED: " + ScenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static readonly LevelDirection[] Directions = { LevelDirection.North, LevelDirection.East, LevelDirection.South, LevelDirection.West };

        private static Sprite CreateSprite()
        {
            var path = Root + "/Art/White.png";
            var texture = new Texture2D(4, 4);
            texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 4;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static LevelZone CreateRoom(string id, Color floor, Color accent, Sprite sprite, Material material)
        {
            var root = new GameObject(id);
            try
            {
                var zone = root.AddComponent<LevelZone>();
                zone.SetZoneId(id);
                using (var data = new SerializedObject(zone))
                {
                    data.FindProperty("boundsMode").intValue = (int)LevelBoundsMode.Manual;
                    data.FindProperty("manualSize").vector2Value = new Vector2(10, 8);
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                Visual("Floor", root.transform, Vector2.zero, new Vector2(10, 8), floor, sprite, material, 0);
                for (var x = -4; x <= 4; x++)
                    Visual("Floor seam", root.transform, new Vector2(x, 0), new Vector2(0.025f, 8), floor * 1.15f, sprite, material, 1);
                foreach (var direction in Directions)
                {
                    var offset = (Vector2)LevelDirectionUtility.ToCellOffset(direction);
                    var vertical = offset.x == 0;
                    var point = new Vector2(offset.x * 5, offset.y * 4);
                    var exitObject = new GameObject(direction + " exit");
                    exitObject.transform.SetParent(root.transform, false);
                    exitObject.transform.localPosition = point;
                    var trigger = exitObject.AddComponent<BoxCollider2D>();
                    trigger.isTrigger = true;
                    trigger.size = vertical ? new Vector2(2, 0.25f) : new Vector2(0.25f, 2);
                    var exit = exitObject.AddComponent<LevelExit>();
                    using (var data = new SerializedObject(exit))
                    {
                        data.FindProperty("direction").intValue = (int)direction;
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }
                    Visual("Doorway", root.transform, point - offset * 0.16f, vertical ? new Vector2(1.8f, 0.18f) : new Vector2(0.18f, 1.8f), accent, sprite, material, 2);
                    for (var sign = -1; sign <= 1; sign += 2)
                    {
                        var wallPoint = point + (vertical ? Vector2.right * (3 * sign) : Vector2.up * (2.5f * sign));
                        var wallSize = vertical ? new Vector2(4, 0.18f) : new Vector2(0.18f, 3);
                        var wall = Visual("Wall", root.transform, wallPoint, wallSize, accent * 0.75f, sprite, material, 3);
                        wall.AddComponent<BoxCollider2D>();
                    }
                }
                zone.RefreshExits();
                return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + id + ".prefab").GetComponent<LevelZone>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject Visual(string name, Transform parent, Vector2 position, Vector2 size, Color color, Sprite sprite, Material material, int order)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = color;
            renderer.sortingOrder = order;
            return item;
        }

        public static void BuildDistribution()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Distribution sample is missing. Run Tools/LoopShift/Rebuild distribution sample before exporting.");
            var graph = AssetDatabase.LoadAssetAtPath<LevelGraphAsset>(Root + "/EndlessRooms.asset");
            if (graph == null)
                throw new InvalidOperationException("Distribution sample graph is missing.");
            var errors = graph.ValidateGraph().Where(message => message.Severity == LevelValidationSeverity.Error).ToArray();
            if (errors.Length != 0)
                throw new InvalidOperationException(string.Join("\n", errors.Select(message => message.Message)));
            ExportUnityPackage();
        }

        public static void ExportUnityPackage()
        {
            Directory.CreateDirectory("Build/LoopShift");
            var paths = new[] { "Assets/LoopShift/Runtime", "Assets/LoopShift/Editor", "Assets/LoopShift/Documentation", Root, "Assets/LoopShift/LICENSE", "Assets/LoopShift/Third-Party Notices.txt", "Assets/LoopShift/README.md", "Assets/LoopShift/CHANGELOG.md" };
            AssetDatabase.ExportPackage(paths, "Build/LoopShift/LoopShift-0.1.0.unitypackage", ExportPackageOptions.Recurse);
            Debug.Log("LOOPSHIFT UNITYPACKAGE EXPORTED");
        }
    }
}
