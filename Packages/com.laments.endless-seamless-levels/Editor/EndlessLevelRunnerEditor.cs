using EndlessSeamlessLevels;
using UnityEditor;
using UnityEngine;

namespace EndlessSeamlessLevels.Editor
{
	[CustomEditor(typeof(EndlessLevelRunner))]
	public class EndlessLevelRunnerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var runner = (EndlessLevelRunner)target;
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Active Zones", runner.ActiveZones.Count.ToString());

			if (runner.Graph != null && GUILayout.Button("Validate Assigned Graph"))
			{
				foreach (var message in runner.Graph.ValidateGraph())
				{
					if (message.Severity == LevelValidationSeverity.Error)
						Debug.LogError(message.Message, message.Context);
					else if (message.Severity == LevelValidationSeverity.Warning)
						Debug.LogWarning(message.Message, message.Context);
					else
						Debug.Log(message.Message, message.Context);
				}
			}

			using (new EditorGUI.DisabledScope(!Application.isPlaying))
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Start Runner"))
					runner.StartRunner();
				if (GUILayout.Button("Stop Runner"))
					runner.StopRunner();
			}
		}
	}
}
