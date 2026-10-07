using System;
using UnityEditor;
using UnityEngine;

namespace LoopShift.Editor
{
    /// <summary>Undoable room geometry edits shared by inspector and scene handles.</summary>
    public static class LevelZoneAuthoring
    {
        public static void SetManualBounds(Runtime.LevelZone zone, Vector2 center, Vector2 size)
        {
            if (zone == null || !Finite(center) || !Finite(size) || size.x <= 0 || size.y <= 0)
                throw new ArgumentException("Room bounds need a finite center and positive finite size.");
            using var data = new SerializedObject(zone);
            data.FindProperty("boundsMode").intValue = (int)Runtime.LevelBoundsMode.Manual;
            data.FindProperty("pivotOffset").vector2Value = center;
            data.FindProperty("manualSize").vector2Value = size;
            data.ApplyModifiedProperties();
        }

        public static Runtime.LevelExit AddExit(Runtime.LevelZone zone, Runtime.LevelDirection direction)
        {
            if (zone == null || !Runtime.LevelDirectionUtility.IsCardinal(direction))
                throw new ArgumentException("Choose a room and a cardinal exit direction.");
            if (EditorUtility.IsPersistent(zone))
                throw new ArgumentException("Open the prefab in Prefab Mode before adding exits.");
            foreach (var existing in zone.GetComponentsInChildren<Runtime.LevelExit>(true))
                if (existing.GetComponentInParent<Runtime.LevelZone>(true) == zone &&
                    existing.Direction == direction && string.Equals(existing.ExitTag, "Default", StringComparison.OrdinalIgnoreCase))
                    return existing;

            Undo.RecordObject(zone, "Add room exit");
            var item = new GameObject(direction + " Exit");
            Undo.RegisterCreatedObjectUndo(item, "Add room exit");
            Undo.SetTransformParent(item.transform, zone.transform, "Parent room exit");
            var bounds = zone.LocalBounds;
            var offset = (Vector2)Runtime.LevelDirectionUtility.ToCellOffset(direction);
            item.transform.localPosition = bounds.center + new Vector3(offset.x * bounds.extents.x, offset.y * bounds.extents.y, 0);
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
            var collider = Undo.AddComponent<BoxCollider2D>(item);
            collider.isTrigger = true;
            collider.size = offset.x == 0 ? new Vector2(Mathf.Min(2, bounds.size.x), 0.25f) : new Vector2(0.25f, Mathf.Min(2, bounds.size.y));
            var exit = Undo.AddComponent<Runtime.LevelExit>(item);
            using (var data = new SerializedObject(exit))
            {
                data.FindProperty("direction").intValue = (int)direction;
                data.FindProperty("exitTag").stringValue = "Default";
                data.FindProperty("spawnAnchor").objectReferenceValue = item.transform;
                data.ApplyModifiedProperties();
            }
            zone.RefreshExits();
            EditorUtility.SetDirty(zone);
            PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
            return exit;
        }

        public static void AddMissingCardinalExits(Runtime.LevelZone zone)
        {
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            try
            {
                AddExit(zone, Runtime.LevelDirection.North);
                AddExit(zone, Runtime.LevelDirection.East);
                AddExit(zone, Runtime.LevelDirection.South);
                AddExit(zone, Runtime.LevelDirection.West);
            }
            finally { Undo.CollapseUndoOperations(group); }
        }

        public static void SetExitPosition(Runtime.LevelZone zone, Runtime.LevelExit exit, Vector3 worldPosition)
        {
            if (zone == null || exit == null || exit.GetComponentInParent<Runtime.LevelZone>(true) != zone ||
                !exit.SpawnAnchor.IsChildOf(zone.transform) || !Finite(worldPosition))
                throw new ArgumentException("The exit and its anchor must belong to this room and have a finite position.");
            var anchor = exit.SpawnAnchor;
            Undo.RecordObject(anchor, "Move room exit anchor");
            anchor.position = worldPosition;
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);
            EditorUtility.SetDirty(anchor);
        }

        private static bool Finite(Vector2 point) => !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
            !float.IsNaN(point.y) && !float.IsInfinity(point.y);
        private static bool Finite(Vector3 point) => Finite((Vector2)point) && !float.IsNaN(point.z) && !float.IsInfinity(point.z);
    }
}
