using System;
using System.Linq;
using Level;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LamentsNeverEnd.Editor
{
	[CustomEditor(typeof(ShadowCaster))]
	public class ShadowCasterEditor : UnityEditor.Editor
	{
		private const string GeneratedPrefix = "shadow_caster_";

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Create"))
					Create((ShadowCaster)target);
				if (GUILayout.Button("Remove Shadows"))
					RemoveGenerated((ShadowCaster)target);
			}
		}

		public static void Create(ShadowCaster creator)
		{
			var collider = creator.GetComponent<CompositeCollider2D>();
			RemoveGenerated(creator);
			for (var i = 0; i < collider.pathCount; i++)
			{
				var points = new Vector2[collider.GetPathPointCount(i)];
				collider.GetPath(i, points);
				if (points.Length < 3)
					continue;

				var child = new GameObject(GeneratedPrefix + i);
				Undo.RegisterCreatedObjectUndo(child, "Create tilemap shadows");
				child.transform.SetParent(creator.transform, false);
				var caster = Undo.AddComponent<ShadowCaster2D>(child);
				caster.selfShadows = creator.SelfShadows;

				// URP 17.7 rebuilds its own mesh from the serialized shape and hash.
				// The old m_Mesh field and two-argument GenerateShadowMesh API no longer exist.
				var serialized = new SerializedObject(caster);
				var path = serialized.FindProperty("m_ShapePath");
				var hash = serialized.FindProperty("m_ShapePathHash");
				var source = serialized.FindProperty("m_ShadowCastingSource");
				if (path == null || hash == null || source == null)
				{
					Undo.DestroyObjectImmediate(child);
					throw new InvalidOperationException("Installed URP shadow shape serialization is unsupported.");
				}

				path.arraySize = points.Length;
				for (var j = 0; j < points.Length; j++)
					path.GetArrayElementAtIndex(j).vector3Value = points[j];
				hash.intValue = Guid.NewGuid().GetHashCode();
				source.intValue = 1; // ShadowCastingSources.ShapeEditor in the installed URP.
				serialized.ApplyModifiedProperties();
			}
		}

		public static void RemoveGenerated(ShadowCaster creator)
		{
			foreach (var child in creator.transform.Cast<Transform>().ToArray())
				if (child.name.StartsWith(GeneratedPrefix, StringComparison.Ordinal) &&
				    child.GetComponent<ShadowCaster2D>() != null)
					Undo.DestroyObjectImmediate(child.gameObject);
		}
	}
}