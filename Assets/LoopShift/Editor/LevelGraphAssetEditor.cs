using LoopShift.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoopShift.Editor
{
	[CustomEditor(typeof(LevelGraphAsset))]
	public class LevelGraphAssetEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var graph = (LevelGraphAsset)target;
			EditorGUILayout.Space();
			if (GUILayout.Button("Validate Graph"))
				PrintValidation(graph);
		}

		private static void PrintValidation(LevelGraphAsset graph)
		{
			var messages = graph.ValidateGraph();
			if (messages.Count == 0)
			{
				Debug.Log($"Level graph '{graph.name}' is valid.", graph);
				return;
			}

			foreach (var message in messages)
			{
				switch (message.Severity)
				{
					case LevelValidationSeverity.Error:
						Debug.LogError(message.Message, message.Context);
						break;
					case LevelValidationSeverity.Warning:
						Debug.LogWarning(message.Message, message.Context);
						break;
					default:
						Debug.Log(message.Message, message.Context);
						break;
				}
			}
		}
	}
}
