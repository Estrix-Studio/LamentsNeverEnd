using EndlessSeamlessLevels;
using UnityEditor;
using UnityEngine;

namespace EndlessSeamlessLevels.Editor
{
	[CustomEditor(typeof(LevelZone))]
	public class LevelZoneEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var zone = (LevelZone)target;
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Detected Runtime Data", EditorStyles.boldLabel);
			EditorGUILayout.Vector2Field("Effective Size", zone.Size);
			EditorGUILayout.Vector2Field("Pivot Offset", zone.PivotOffset);
			EditorGUILayout.LabelField("Exit Count", zone.Exits.Count.ToString());

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Refresh Bounds And Exits"))
				{
					Undo.RecordObject(zone, "Refresh Level Zone");
					zone.RefreshCachedData();
					EditorUtility.SetDirty(zone);
				}

				if (GUILayout.Button("Select Exits"))
				{
					var exits = new Object[zone.Exits.Count];
					for (var i = 0; i < zone.Exits.Count; i++)
						exits[i] = zone.Exits[i];
					Selection.objects = exits;
				}
			}
		}
	}
}
