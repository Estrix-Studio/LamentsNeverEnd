using LoopShift.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LoopShift.Editor
{
    [CustomEditor(typeof(LevelZone))]
    public class LevelZoneEditor : UnityEditor.Editor
    {
        private readonly BoxBoundsHandle _boundsHandle = new();
        private bool _editBounds;
        private bool _editExits;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var zone = (LevelZone)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Effective bounds", zone.Size + " at " + zone.PivotOffset);
            EditorGUILayout.LabelField("Exit count", zone.Exits.Count.ToString());
            if (GUILayout.Button("Refresh bounds and exits"))
            {
                Undo.RecordObject(zone, "Refresh room data");
                zone.RefreshCachedData();
                PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
                EditorUtility.SetDirty(zone);
            }
            var persistent = EditorUtility.IsPersistent(zone);
            if (persistent && GUILayout.Button("Open prefab for scene editing"))
                AssetDatabase.OpenAsset(zone.gameObject);
            using (new EditorGUI.DisabledScope(persistent))
            {
                if (GUILayout.Button("Add missing cardinal exits"))
                    LevelZoneAuthoring.AddMissingCardinalExits(zone);
                EditorGUI.BeginChangeCheck();
                _editBounds = EditorGUILayout.Toggle("Edit bounds in scene", _editBounds);
                _editExits = EditorGUILayout.Toggle("Edit exit anchors in scene", _editExits);
                if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
            }
            if (_editBounds && zone.BoundsMode != LevelBoundsMode.Manual)
                EditorGUILayout.HelpBox("Dragging a bounds handle switches this room to manual bounds.", MessageType.Info);
            EditorGUILayout.HelpBox("Exits begin at the bounds edges. Place their trigger colliders in passable doorways; opposing spawn anchors align when rooms connect.", MessageType.None);

        }

        private void OnSceneGUI()
        {
            var zone = (LevelZone)target;
            if (zone == null || EditorUtility.IsPersistent(zone)) return;
            using (new Handles.DrawingScope(new Color(0.3f, 0.8f, 0.95f), zone.transform.localToWorldMatrix))
            {
                var bounds = zone.LocalBounds;
                Handles.DrawWireCube(bounds.center, bounds.size);
                if (_editBounds)
                {
                    _boundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
                    _boundsHandle.center = bounds.center;
                    _boundsHandle.size = bounds.size;
                    EditorGUI.BeginChangeCheck();
                    _boundsHandle.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        var size = (Vector2)_boundsHandle.size;
                        size.x = Mathf.Max(0.01f, size.x);
                        size.y = Mathf.Max(0.01f, size.y);
                        LevelZoneAuthoring.SetManualBounds(zone, _boundsHandle.center, size);
                    }
                }
            }
            foreach (var exit in zone.Exits)
            {
                if (exit == null) continue;
                var anchor = exit.SpawnAnchor;
                Handles.Label(anchor.position, exit.Direction + " / " + exit.ExitTag);
                if (!_editExits || !anchor.IsChildOf(zone.transform)) continue;
                EditorGUI.BeginChangeCheck();
                var position = Handles.PositionHandle(anchor.position, zone.transform.rotation);
                if (EditorGUI.EndChangeCheck()) LevelZoneAuthoring.SetExitPosition(zone, exit, position);
            }
        }
    }
}
