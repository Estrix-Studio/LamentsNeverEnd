using System;
using System.Collections.Generic;
using LoopShift.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoopShift.Editor
{
    public class EndlessLevelWizard : EditorWindow
    {
        private string _graphName = "LevelGraph";
        private string _folder = "Assets";
        private string _error;

        [MenuItem("Tools/Endless Seamless Levels/Wizard")]
        [MenuItem("Window/LoopShift/Create Graph Wizard")]
        public static void Open() => GetWindow<EndlessLevelWizard>("Create LoopShift Graph");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create a level graph", EditorStyles.boldLabel);
            _graphName = EditorGUILayout.TextField("Graph name", _graphName);
            _folder = EditorGUILayout.TextField("Output folder", _folder);
            if (GUILayout.Button("Create empty graph")) CreateGraph(false);
            if (GUILayout.Button("Create from selected prefabs")) CreateGraph(true);
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
            EditorGUILayout.HelpBox("Select saved LevelZone prefabs in the Project window. Room aliases, start selection and layout use the same authoring rules as the visual editor. Source prefabs are not modified.", MessageType.Info);
        }

        private void CreateGraph(bool fromSelection)
        {
            try
            {
                var zones = new List<LevelZone>();
                if (fromSelection)
                {
                    foreach (var selected in Selection.objects)
                    {
                        var zone = selected is GameObject item ? item.GetComponent<LevelZone>() : selected as LevelZone;
                        if (zone != null && !zones.Contains(zone)) zones.Add(zone);
                    }
                    if (zones.Count == 0) throw new ArgumentException("Select at least one saved LevelZone prefab.");
                }
                var graph = LevelGraphAuthoring.CreateGraph(_folder, _graphName, zones);
                Selection.activeObject = graph;
                LevelGraphWindow.Open(graph);
                _error = null;
            }
            catch (ArgumentException exception) { _error = exception.Message; }
        }
    }
}
