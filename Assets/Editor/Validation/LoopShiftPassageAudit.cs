using System;
using System.Collections.Generic;
using System.IO;
using Gameplay;
using LoopShift.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace LamentsNeverEnd.Editor
{
    public static class LoopShiftPassageAudit
    {
        public static void Run() => Audit(false);
        public static void ApplyAnchors() => Audit(true);

        private static void Audit(bool apply)
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run in a disposable batch scene.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rows = new List<string> { "room,direction,clearSamples,minOffset,maxOffset" };
            foreach (var path in Directory.GetFiles(LoopShiftGameMigration.Root, "*.prefab"))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
                var anchors = new Dictionary<LevelDirection, Vector2>();
                var instance = Object.Instantiate(prefab);
                try
                {
                    instance.transform.position = Vector3.zero;
                    foreach (var collider in instance.GetComponentsInChildren<TilemapCollider2D>()) collider.ProcessTilemapChanges();
                    foreach (var collider in instance.GetComponentsInChildren<CompositeCollider2D>()) collider.GenerateGeometry();
                    Physics2D.SyncTransforms();
                    var zone = instance.GetComponent<LevelZone>();
                    foreach (var exit in zone.Exits)
                    {
                        var direction = (Vector2)LevelDirectionUtility.ToCellOffset(exit.Direction);
                        var tangent = new Vector2(-direction.y, direction.x);
                        var clear = new List<float>();
                        for (var index = -36; index <= 36; index++)
                        {
                            var lateral = index * 0.25f;
                            var start = direction * 8 + tangent * lateral;
                            var blocked = false;
                            foreach (var hit in Physics2D.BoxCastAll(start, new Vector2(0.6f, 0.8f), 0, direction, 2))
                            {
                                if (hit.collider.isTrigger || hit.collider.GetComponentInParent<Enemy>() != null) continue;
                                blocked = true;
                                break;
                            }
                            if (!blocked) clear.Add(lateral);
                        }
                        if (clear.Count > 0)
                        {
                            var bestStart = 0;
                            var bestEnd = 0;
                            var startIndex = 0;
                            for (var i = 1; i <= clear.Count; i++)
                            {
                                if (i < clear.Count && clear[i] - clear[i - 1] < 0.3f) continue;
                                if (i - 1 - startIndex > bestEnd - bestStart) { bestStart = startIndex; bestEnd = i - 1; }
                                startIndex = i;
                            }
                            anchors[exit.Direction] = direction * 10 + tangent * ((clear[bestStart] + clear[bestEnd]) * 0.5f);
                        }
                        rows.Add(zone.ZoneId + "," + exit.Direction + "," + clear.Count + "," +
                            (clear.Count == 0 ? "NA,NA" : clear[0].ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + clear[clear.Count - 1].ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    }
                }
                finally { Object.DestroyImmediate(instance); Physics2D.SyncTransforms(); }
                if (!apply) continue;
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var exit in contents.GetComponent<LevelZone>().Exits)
                    {
                        if (!anchors.TryGetValue(exit.Direction, out var position)) continue;
                        exit.transform.localPosition = position;
                        var trigger = exit.GetComponent<BoxCollider2D>();
                        trigger.size = exit.Direction == LevelDirection.North || exit.Direction == LevelDirection.South
                            ? new Vector2(1, 0.2f) : new Vector2(0.2f, 1);
                    }
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/loopshift-passages.csv", rows);
            Debug.Log("LOOPSHIFT PASSAGE AUDIT COMPLETE: " + (rows.Count - 1));
        }
    }
}