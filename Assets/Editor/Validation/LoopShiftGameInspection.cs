using System.Linq;
using Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LamentsNeverEnd.Editor
{
    public static class LoopShiftGameInspection
    {
        public static void Run()
        {
            foreach (var zone in Resources.LoadAll<CyclingZone>("Zones").OrderBy(z => z.ZoneID))
            {
                var triggers = zone.GetComponentsInChildren<TriggerZone>(true).Select(trigger =>
                {
                    using var data = new SerializedObject(trigger);
                    var collider = trigger.GetComponent<Collider2D>();
                    return data.FindProperty("direction").intValue + ":" + zone.transform.InverseTransformPoint(trigger.transform.position) + " collider=" + (collider != null ? collider.GetType().Name + " offset=" + collider.offset + (collider is BoxCollider2D box ? " size=" + box.size : "") : "none");
                });
                Debug.Log("GAME ROOM " + zone.ZoneID + " root=" + zone.transform.position + " triggers=" + string.Join("; ", triggers));
                var rootCollider = zone.GetComponent<Collider2D>();
                Debug.Log("GAME ROOT COLLIDER " + zone.ZoneID + " " + (rootCollider != null ? rootCollider.bounds.ToString() : "none"));
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            var traversal = Object.FindFirstObjectByType<LevelTraversal>();
            using var serialized = new SerializedObject(traversal);
            foreach (var name in new[] { "centerPosition", "leftPosition", "rightPosition", "topPosition", "bottomPosition" })
            {
                var anchor = serialized.FindProperty(name).objectReferenceValue as Transform;
                Debug.Log("GAME ANCHOR " + name + " " + (anchor != null ? anchor.position.ToString() : "none"));
            }
        }
    }
}
