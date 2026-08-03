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

		[MenuItem("Tools/LoopShift/Level Graph Wizard")]
		public static void Open()
		{
			GetWindow<EndlessLevelWizard>("Endless Levels");
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("LoopShift Level Graph", EditorStyles.boldLabel);
			_graphName = EditorGUILayout.TextField("Graph Name", _graphName);
			_folder = EditorGUILayout.TextField("Output Folder", _folder);

			EditorGUILayout.Space();
			if (GUILayout.Button("Create Empty Graph"))
				CreateGraph(false);

			if (GUILayout.Button("Create Graph From Selected Zones"))
				CreateGraph(true);

			EditorGUILayout.HelpBox("Select prefab assets or scene objects with LevelZone components, then create a graph. The wizard refreshes zone bounds and exits, adds entries, and sets the first zone as the start zone.", MessageType.Info);
		}

		private void CreateGraph(bool fromSelection)
		{
			if (!AssetDatabase.IsValidFolder(_folder))
			{
				Debug.LogError($"Output folder does not exist: {_folder}");
				return;
			}

			var graph = CreateInstance<LevelGraphAsset>();
			var path = AssetDatabase.GenerateUniqueAssetPath($"{_folder}/{_graphName}.asset");
			AssetDatabase.CreateAsset(graph, path);

			if (fromSelection)
				PopulateFromSelection(graph);

			EditorUtility.SetDirty(graph);
			AssetDatabase.SaveAssets();
			Selection.activeObject = graph;
			Debug.Log($"Created level graph at {path}", graph);
		}

		private static void PopulateFromSelection(LevelGraphAsset graph)
		{
			var zones = GetSelectedZones();
			var serializedGraph = new SerializedObject(graph);
			var zonesProperty = serializedGraph.FindProperty("zones");
			var startZoneIdProperty = serializedGraph.FindProperty("startZoneId");

			foreach (var zone in zones)
			{
				Undo.RecordObject(zone, "Refresh Level Zone");
				zone.RefreshCachedData();
				EditorUtility.SetDirty(zone);

				var index = zonesProperty.arraySize;
				zonesProperty.InsertArrayElementAtIndex(index);
				var entry = zonesProperty.GetArrayElementAtIndex(index);
				entry.FindPropertyRelative("prefab").objectReferenceValue = zone;
				entry.FindPropertyRelative("zoneIdOverride").stringValue = string.Empty;
				entry.FindPropertyRelative("weight").intValue = 1;

				if (string.IsNullOrWhiteSpace(startZoneIdProperty.stringValue))
					startZoneIdProperty.stringValue = zone.ZoneId;
			}

			serializedGraph.ApplyModifiedProperties();
		}

		private static List<LevelZone> GetSelectedZones()
		{
			var zones = new List<LevelZone>();
			foreach (var selectedObject in Selection.objects)
			{
				if (selectedObject is GameObject gameObject)
				{
					var zone = gameObject.GetComponent<LevelZone>();
					if (zone != null && !zones.Contains(zone))
						zones.Add(zone);
				}
			}

			return zones;
		}
	}
}
