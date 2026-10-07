using LoopShift.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoopShift.Editor
{
    public partial class LevelGraphWindow
    {
        private void DrawDetails(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll);
            EditorGUILayout.LabelField("Graph details", EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(_notice))
                EditorGUILayout.HelpBox(_notice, MessageType.Info);
            if (graph != null)
                DrawGraphDetails();
            else
                EditorGUILayout.HelpBox("Choose a graph in the toolbar to begin.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGraphDetails()
        {
            bidirectional = EditorGUILayout.Toggle("New wires bidirectional", bidirectional);
            EditorGUILayout.HelpBox("Drag prefabs onto the canvas. Connect opposing exit ports. Middle-drag to pan, scroll to zoom, F to frame. Right-click a room to add it to a pool.", MessageType.None);
            if (_selected != null)
                DrawNodeDetails();
            if (_selectedEdge >= 0 && _selectedEdge < graph.FixedConnections.Count)
                DrawConnectionDetails();

            using (var serialized = new SerializedObject(graph))
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serialized.FindProperty("poolRules"), new GUIContent("Global pool rules"), true);
                if (serialized.ApplyModifiedProperties()) Refresh();
            }
            EditorGUILayout.HelpBox("Pool rules apply to every matching exit. Fixed connections take priority.", MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            if (_validation.Count == 0)
                EditorGUILayout.LabelField("No graph issues found.");
            foreach (var message in _validation)
            {
                var type = message.Severity == LevelValidationSeverity.Error ? MessageType.Error :
                    message.Severity == LevelValidationSeverity.Warning ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.HelpBox(message.Message, type);
            }
        }

        private void DrawNodeDetails()
        {
            var node = _selected;
            var oldId = node.Id;
            var newId = EditorGUILayout.DelayedTextField("ID", oldId);
            if (newId != oldId)
            {
                Perform(() =>
                {
                    if (node.Pool != null) LevelGraphAuthoring.RenamePool(graph, oldId, newId);
                    else LevelGraphAuthoring.RenameZone(graph, oldId, newId);
                });
                return;
            }
            if (node.Zone != null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Prefab", node.Zone.prefab, typeof(LevelZone), false);
                if (GUILayout.Button("Open prefab") && node.Zone.prefab != null)
                    AssetDatabase.OpenAsset(node.Zone.prefab.gameObject);
                if (GUILayout.Button("Set as start"))
                    Perform(() => LevelGraphAuthoring.SetStart(graph, oldId));
            }
            else
                DrawPoolDetails(oldId);
            if (GUILayout.Button("Delete selected node")) DeleteSelection();
        }

        private void DrawPoolDetails(string id)
        {
            using var serialized = new SerializedObject(graph);
            var pools = serialized.FindProperty("pools");
            for (var i = 0; i < pools.arraySize; i++)
            {
                var pool = pools.GetArrayElementAtIndex(i);
                if (pool.FindPropertyRelative("poolId").stringValue != id) continue;
                EditorGUILayout.PropertyField(pool.FindPropertyRelative("allowImmediateRepeat"));
                EditorGUILayout.PropertyField(pool.FindPropertyRelative("entries"), true);
                if (serialized.ApplyModifiedProperties()) Refresh();
                return;
            }
        }

        private void DrawConnectionDetails()
        {
            using var serialized = new SerializedObject(graph);
            var edge = serialized.FindProperty("fixedConnections").GetArrayElementAtIndex(_selectedEdge);
            EditorGUILayout.PropertyField(edge, new GUIContent("Selected connection"), true);
            if (serialized.ApplyModifiedProperties()) Refresh();
            if (GUILayout.Button("Delete connection")) DeleteSelection();
        }
    }
}
