using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gameplay;
using Gameplay.Data;
using LoopShift.Editor;
using LoopShift.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LamentsNeverEnd.Editor
{
    /// <summary>Creates independent migrated room assets and a reviewable Gameplay scene copy.</summary>
    public static class LoopShiftGameMigration
    {
        public const string Root = "Assets/Prefabs/LoopShiftGame";
        public const string GraphPath = Root + "/GameRooms.asset";
        public const string PreviewScene = "Assets/Scenes/GameplayLoopShift.unity";
        private static readonly LevelDirection[] Directions = { LevelDirection.North, LevelDirection.East, LevelDirection.South, LevelDirection.West };

        public static void Generate()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var entries = new Dictionary<CycleZoneID, LevelZone>();
            foreach (var original in Resources.LoadAll<CyclingZone>("Zones").OrderBy(zone => zone.ZoneID))
            {
                var id = original.ZoneID;
                if (id == CycleZoneID.None) continue;
                if (entries.ContainsKey(id)) throw new InvalidOperationException("Duplicate room: " + id);
                var path = Root + "/" + id + ".prefab";
                if (File.Exists(path)) throw new InvalidOperationException("Migration output already exists; preserve/review before rebuilding: " + path);
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), path))
                    throw new InvalidOperationException("Cannot copy room: " + id);
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    // Remove the old room-entry callbacks; LoopShift publishes one authoritative entry.
                    foreach (var trigger in contents.GetComponentsInChildren<TriggerZone>(true)) Object.DestroyImmediate(trigger);
                    foreach (var oldZone in contents.GetComponentsInChildren<CyclingZone>(true)) Object.DestroyImmediate(oldZone);
                    var zone = contents.AddComponent<LevelZone>();
                    zone.SetZoneId(id.ToString());
                    using (var data = new SerializedObject(zone))
                    {
                        data.FindProperty("boundsMode").intValue = (int)LevelBoundsMode.Manual;
                        data.FindProperty("manualSize").vector2Value = new Vector2(20, 20);
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }
                    foreach (var direction in Directions)
                    {
                        var offset = (Vector2)LevelDirectionUtility.ToCellOffset(direction);
                        var item = new GameObject("LoopShift " + direction);
                        item.transform.SetParent(contents.transform, false);
                        item.transform.localPosition = offset * 10;
                        var collider = item.AddComponent<BoxCollider2D>();
                        collider.isTrigger = true;
                        collider.size = offset.x == 0 ? new Vector2(19.5f, 0.2f) : new Vector2(0.2f, 19.5f);
                        var exit = item.AddComponent<LevelExit>();
                        using var data = new SerializedObject(exit);
                        data.FindProperty("direction").intValue = (int)direction;
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }
                    zone.RefreshExits();
                    var prefab = PrefabUtility.SaveAsPrefabAsset(contents, path);
                    entries.Add(id, prefab.GetComponent<LevelZone>());
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            var graph = ScriptableObject.CreateInstance<LevelGraphAsset>();
            AssetDatabase.CreateAsset(graph, GraphPath);
            var index = 0;
            foreach (var pair in entries)
            {
                LevelGraphAuthoring.AddZone(graph, pair.Value, new Vector2(index % 4 * 300, index / 4 * 220));
                index++;
            }
            LevelGraphAuthoring.SetStart(graph, ZoneConfig.firstZone.ToString());
            foreach (var room in ZoneConfig.DefaultZones)
            {
                Connect(graph, room.Zone, room.Top, LevelDirection.North);
                Connect(graph, room.Zone, room.Right, LevelDirection.East);
                Connect(graph, room.Zone, room.Bottom, LevelDirection.South);
                Connect(graph, room.Zone, room.Left, LevelDirection.West);
            }
            var errors = graph.ValidateGraph().Where(message => message.Severity == LevelValidationSeverity.Error).ToArray();
            if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n", errors.Select(error => error.Message)));
            if (File.Exists(PreviewScene)) throw new InvalidOperationException("Migration scene already exists: " + PreviewScene);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            graph = AssetDatabase.LoadAssetAtPath<LevelGraphAsset>(GraphPath);
            ConfigureCurrentScene(graph);
            EditorSceneManager.SaveScene(scene, PreviewScene, true);
            AssetDatabase.SaveAssets();
            Debug.Log("LOOPSHIFT GAME MIGRATION PREVIEW: " + entries.Count + " rooms, " + graph.FixedConnections.Count + " routes; " + PreviewScene);
        }


        public static void InstallInGameplay()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            var graph = AssetDatabase.LoadAssetAtPath<LevelGraphAsset>(GraphPath);
            if (graph == null) throw new InvalidOperationException("Generate and validate the migrated graph first.");
            ConfigureCurrentScene(graph);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            if (Object.FindFirstObjectByType<EndlessLevelRunner>().Graph == null) throw new InvalidOperationException("Saved graph reference was lost.");
            Debug.Log("LOOPSHIFT INSTALLED IN GAMEPLAY");
        }

        private static void ConfigureCurrentScene(LevelGraphAsset graph)
        {
            var legacy = Object.FindFirstObjectByType<LevelTraversal>();
            var runner = Object.FindFirstObjectByType<EndlessLevelRunner>();
            if (legacy != null)
            {
                var host = legacy.gameObject;
                Object.DestroyImmediate(legacy);
                runner = host.AddComponent<EndlessLevelRunner>();
            }
            if (runner == null || graph == null)
                throw new InvalidOperationException("Gameplay runner or graph is missing.");
            runner.SpawnSettings.recenterOnTransition = true;
            runner.SpawnSettings.preloadRadius = 1;
            runner.SpawnSettings.activeRadius = 1;
            runner.SpawnSettings.maxPoolInstancesPerPrefab = 2;
            runner.SpawnSettings.maxPooledInstances = 16;
            using (var data = new SerializedObject(runner))
            {
                data.FindProperty("graph").objectReferenceValue = graph;
                data.FindProperty("playOnAwake").boolValue = false;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            if (runner.GetComponent<GameLevelRunner>() == null) runner.gameObject.AddComponent<GameLevelRunner>();
        }

        private static void Connect(LevelGraphAsset graph, CycleZoneID source, CycleZoneID target, LevelDirection direction)
        {
            if (target == CycleZoneID.None) return;
            LevelGraphAuthoring.Connect(graph, source.ToString(), direction, "Default", target.ToString(), "Default", false);
        }
    }
}
