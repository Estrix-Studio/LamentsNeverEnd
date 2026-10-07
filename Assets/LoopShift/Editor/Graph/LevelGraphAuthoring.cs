using System;
using System.Collections.Generic;
using LoopShift.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoopShift.Editor
{
	/// <summary>Shared, undoable authoring operations for the canvas, inspector and wizard.</summary>
	public static class LevelGraphAuthoring
	{

        public static LevelGraphAsset CreateGraph(string folder, string name, IReadOnlyList<LevelZone> prefabs)
        {
            folder = (folder ?? "").Replace('\\', '/').TrimEnd('/');
            name = (name ?? "").Trim();
            if ((folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal)) ||
                folder.Contains("/../") || folder.EndsWith("/..", StringComparison.Ordinal) || !AssetDatabase.IsValidFolder(folder))
                throw new ArgumentException("Choose an existing output folder inside Assets.");
            if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." ||
                name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0 || name.Contains("/") || name.Contains("\\"))
                throw new ArgumentException("Choose a valid graph file name without directory separators.");
            if (prefabs == null) throw new ArgumentNullException(nameof(prefabs));
            for (var i = 0; i < prefabs.Count; i++)
                if (prefabs[i] == null || !PrefabUtility.IsPartOfPrefabAsset(prefabs[i]))
                    throw new ArgumentException("All rooms must be saved LevelZone prefab assets.");

            var graph = ScriptableObject.CreateInstance<LevelGraphAsset>();
            var path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".asset");
            AssetDatabase.CreateAsset(graph, path);
            try
            {
                for (var i = 0; i < prefabs.Count; i++)
                    AddZone(graph, prefabs[i], new Vector2(i % 3 * 290, i / 3 * 225));
                AssetDatabase.SaveAssetIfDirty(graph);
                return graph;
            }
            catch
            {
                AssetDatabase.DeleteAsset(path);
                throw;
            }
        }

        public static LevelZoneEntry AddZone(LevelGraphAsset graph, LevelZone prefab, Vector2 position)
		{
			if (graph == null || prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
				throw new ArgumentException("Choose a saved prefab with a LevelZone component.");
			var id = UniqueId(graph, prefab.ZoneId);
			using var serialized = Edit(graph, "Add room");
			var array = serialized.FindProperty("zones");
			var entry = Append(array);
			WriteEntry(entry, prefab, id, position);
			if (string.IsNullOrWhiteSpace(graph.StartZoneId))
				serialized.FindProperty("startZoneId").stringValue = id;
			serialized.ApplyModifiedProperties();
			return graph.Zones[graph.Zones.Count - 1];
		}

		public static void RenameZone(LevelGraphAsset graph, string oldId, string newId)
		{
			if (string.IsNullOrWhiteSpace(newId))
				throw new ArgumentException("A room ID cannot be empty.");
			if (oldId == newId)
				return;
			if (FindZone(graph, newId) != null)
				throw new ArgumentException("That room ID already exists.");
			using var serialized = Edit(graph, "Rename room");
			VisitEntries(serialized, entry =>
			{
				if (EntryId(entry) == oldId)
					entry.FindPropertyRelative("zoneIdOverride").stringValue = newId;
			});
			var connections = serialized.FindProperty("fixedConnections");
			for (var i = 0; i < connections.arraySize; i++)
			{
				var connection = connections.GetArrayElementAtIndex(i);
				ReplaceId(connection.FindPropertyRelative("fromZoneId"), oldId, newId);
				ReplaceId(connection.FindPropertyRelative("toZoneId"), oldId, newId);
			}
			ReplaceId(serialized.FindProperty("startZoneId"), oldId, newId);
			serialized.ApplyModifiedProperties();
		}

		public static void RemoveZone(LevelGraphAsset graph, string id)
		{
			using var serialized = Edit(graph, "Remove room");
			RemoveEntries(serialized.FindProperty("zones"), id);
			var pools = serialized.FindProperty("pools");
			for (var i = 0; i < pools.arraySize; i++)
				RemoveEntries(pools.GetArrayElementAtIndex(i).FindPropertyRelative("entries"), id);
			var connections = serialized.FindProperty("fixedConnections");
			for (var i = connections.arraySize - 1; i >= 0; i--)
			{
				var connection = connections.GetArrayElementAtIndex(i);
				if (connection.FindPropertyRelative("fromZoneId").stringValue == id ||
				    connection.FindPropertyRelative("toZoneId").stringValue == id)
					connections.DeleteArrayElementAtIndex(i);
			}
			if (graph.StartZoneId == id)
				serialized.FindProperty("startZoneId").stringValue = string.Empty;
			serialized.ApplyModifiedProperties();
		}

		public static void SetStart(LevelGraphAsset graph, string id)
		{
			if (FindZone(graph, id) == null)
				throw new ArgumentException("Choose a room in this graph.");
			using var serialized = Edit(graph, "Set start room");
			serialized.FindProperty("startZoneId").stringValue = id;
			serialized.ApplyModifiedProperties();
		}

		public static void Connect(LevelGraphAsset graph, string sourceId, LevelDirection direction, string sourceTag,
			string targetId, string targetTag, bool bidirectional)
		{
			var source = FindZone(graph, sourceId);
			var target = FindZone(graph, targetId);
			if (source?.prefab == null || target?.prefab == null ||
			    !source.prefab.TryGetExit(direction, sourceTag, out _) ||
			    !target.prefab.TryGetExit(LevelDirectionUtility.Opposite(direction), targetTag, out _))
				throw new ArgumentException("Connect an exit to an opposing exit on another room (or the same room).");
			if (HasRoute(graph, sourceId, direction, sourceTag) ||
			    bidirectional && HasRoute(graph, targetId, LevelDirectionUtility.Opposite(direction), targetTag))
				throw new ArgumentException("That exit already has a connection. Remove it before connecting again.");

			using var serialized = Edit(graph, "Connect rooms");
			var connection = Append(serialized.FindProperty("fixedConnections"));
			connection.FindPropertyRelative("fromZoneId").stringValue = sourceId;
			connection.FindPropertyRelative("direction").intValue = (int)direction;
			connection.FindPropertyRelative("fromExitTag").stringValue = sourceTag ?? string.Empty;
			connection.FindPropertyRelative("toZoneId").stringValue = targetId;
			connection.FindPropertyRelative("toExitTag").stringValue = targetTag ?? string.Empty;
			connection.FindPropertyRelative("bidirectional").boolValue = bidirectional;
			serialized.ApplyModifiedProperties();
		}

		public static void RemoveConnection(LevelGraphAsset graph, int index)
		{
			using var serialized = Edit(graph, "Remove connection");
			var array = serialized.FindProperty("fixedConnections");
			if (index < 0 || index >= array.arraySize)
				return;
			array.DeleteArrayElementAtIndex(index);
			serialized.ApplyModifiedProperties();
		}

		public static LevelZonePool AddPool(LevelGraphAsset graph, Vector2 position)
		{
			var id = "Pool";
			for (var suffix = 2; FindPool(graph, id) != null; suffix++)
				id = "Pool " + suffix;
			using var serialized = Edit(graph, "Add pool");
			var pool = Append(serialized.FindProperty("pools"));
			pool.FindPropertyRelative("poolId").stringValue = id;
			pool.FindPropertyRelative("allowImmediateRepeat").boolValue = true;
			pool.FindPropertyRelative("entries").arraySize = 0;
			SetPosition(pool, position);
			serialized.ApplyModifiedProperties();
			return graph.Pools[graph.Pools.Count - 1];
		}

		public static void RenamePool(LevelGraphAsset graph, string oldId, string newId)
		{
			if (string.IsNullOrWhiteSpace(newId) || oldId != newId && FindPool(graph, newId) != null)
				throw new ArgumentException("Pool IDs must be nonempty and unique.");
			using var serialized = Edit(graph, "Rename pool");
			var pools = serialized.FindProperty("pools");
			for (var i = 0; i < pools.arraySize; i++)
				ReplaceId(pools.GetArrayElementAtIndex(i).FindPropertyRelative("poolId"), oldId, newId);
			var rules = serialized.FindProperty("poolRules");
			for (var i = 0; i < rules.arraySize; i++)
				ReplaceId(rules.GetArrayElementAtIndex(i).FindPropertyRelative("poolId"), oldId, newId);
			serialized.ApplyModifiedProperties();
		}

		public static void RemovePool(LevelGraphAsset graph, string id)
		{
			using var serialized = Edit(graph, "Remove pool");
			RemoveById(serialized.FindProperty("pools"), "poolId", id);
			RemoveById(serialized.FindProperty("poolRules"), "poolId", id);
			serialized.ApplyModifiedProperties();
		}

		public static void AddToPool(LevelGraphAsset graph, string poolId, string zoneId)
		{
			var zone = FindZone(graph, zoneId);
			if (zone?.prefab == null)
				throw new ArgumentException("Choose a valid room.");
			using var serialized = Edit(graph, "Add room to pool");
			var pools = serialized.FindProperty("pools");
			for (var i = 0; i < pools.arraySize; i++)
			{
				var pool = pools.GetArrayElementAtIndex(i);
				if (pool.FindPropertyRelative("poolId").stringValue != poolId)
					continue;
				var entries = pool.FindPropertyRelative("entries");
				for (var j = 0; j < entries.arraySize; j++)
					if (EntryId(entries.GetArrayElementAtIndex(j)) == zoneId)
						return;
				WriteEntry(Append(entries), zone.prefab, zoneId, zone.editorPosition);
				serialized.ApplyModifiedProperties();
				return;
			}
			throw new ArgumentException("Choose a pool in this graph.");
		}

		public static void AddPoolRule(LevelGraphAsset graph, string poolId, LevelDirection direction, string sourceTag)
		{
			if (FindPool(graph, poolId) == null)
				throw new ArgumentException("Choose a pool in this graph.");
			foreach (var rule in graph.PoolRules)
				if (rule != null && rule.poolId == poolId && rule.direction == direction &&
				    string.Equals(rule.fromExitTag, sourceTag, StringComparison.OrdinalIgnoreCase))
					return;
			using var serialized = Edit(graph, "Add global pool rule");
			var property = Append(serialized.FindProperty("poolRules"));
			property.FindPropertyRelative("poolId").stringValue = poolId;
			property.FindPropertyRelative("direction").intValue = (int)direction;
			property.FindPropertyRelative("fromExitTag").stringValue = sourceTag ?? string.Empty;
			property.FindPropertyRelative("requiredTargetExitTag").stringValue = string.Empty;
			serialized.ApplyModifiedProperties();
		}

		public static LevelZoneEntry FindZone(LevelGraphAsset graph, string id)
		{
			foreach (var zone in graph.Zones)
				if (zone != null && zone.ZoneId == id)
					return zone;
			foreach (var pool in graph.Pools)
			{
				if (pool?.entries == null)
					continue;
				foreach (var zone in pool.entries)
					if (zone != null && zone.ZoneId == id)
						return zone;
			}
			return null;
		}

		public static LevelZonePool FindPool(LevelGraphAsset graph, string id)
		{
			foreach (var pool in graph.Pools)
				if (pool != null && pool.poolId == id)
					return pool;
			return null;
		}

		private static bool HasRoute(LevelGraphAsset graph, string id, LevelDirection direction, string tag)
		{
			foreach (var edge in graph.FixedConnections)
			{
				if (edge == null)
					continue;
				if (edge.fromZoneId == id && edge.direction == direction && SameTag(edge.fromExitTag, tag))
					return true;
				if (edge.bidirectional && edge.toZoneId == id &&
				    LevelDirectionUtility.Opposite(edge.direction) == direction && SameTag(edge.toExitTag, tag))
					return true;
			}
			return false;
		}

		private static bool SameTag(string a, string b) =>
			string.Equals(string.IsNullOrWhiteSpace(a) ? "" : a, string.IsNullOrWhiteSpace(b) ? "" : b,
				StringComparison.OrdinalIgnoreCase);

		private static string UniqueId(LevelGraphAsset graph, string preferred)
		{
			var id = preferred;
			for (var i = 2; FindZone(graph, id) != null; i++)
				id = preferred + "_" + i;
			return id;
		}

		private static SerializedObject Edit(LevelGraphAsset graph, string label)
		{
			if (graph == null)
				throw new ArgumentNullException(nameof(graph));
			Undo.SetCurrentGroupName(label);
			return new SerializedObject(graph);
		}

		private static SerializedProperty Append(SerializedProperty array)
		{
			var index = array.arraySize;
			array.InsertArrayElementAtIndex(index);
			return array.GetArrayElementAtIndex(index);
		}

		private static void WriteEntry(SerializedProperty property, LevelZone prefab, string id, Vector2 position)
		{
			property.FindPropertyRelative("prefab").objectReferenceValue = prefab;
			property.FindPropertyRelative("zoneIdOverride").stringValue = id;
			property.FindPropertyRelative("weight").intValue = 1;
			property.FindPropertyRelative("tags").arraySize = 0;
			SetPosition(property, position);
		}

		private static void SetPosition(SerializedProperty property, Vector2 position)
		{
			property.FindPropertyRelative("editorPosition").vector2Value = position;
			property.FindPropertyRelative("hasEditorPosition").boolValue = true;
		}

		private static string EntryId(SerializedProperty property)
		{
			var id = property.FindPropertyRelative("zoneIdOverride").stringValue;
			var prefab = property.FindPropertyRelative("prefab").objectReferenceValue as LevelZone;
			return string.IsNullOrWhiteSpace(id) ? prefab != null ? prefab.ZoneId : string.Empty : id;
		}

		private static void ReplaceId(SerializedProperty property, string oldId, string newId)
		{
			if (property.stringValue == oldId)
				property.stringValue = newId;
		}

		private static void RemoveEntries(SerializedProperty array, string id)
		{
			for (var i = array.arraySize - 1; i >= 0; i--)
				if (EntryId(array.GetArrayElementAtIndex(i)) == id)
					array.DeleteArrayElementAtIndex(i);
		}

		private static void RemoveById(SerializedProperty array, string property, string id)
		{
			for (var i = array.arraySize - 1; i >= 0; i--)
				if (array.GetArrayElementAtIndex(i).FindPropertyRelative(property).stringValue == id)
					array.DeleteArrayElementAtIndex(i);
		}

		private static void VisitEntries(SerializedObject graph, Action<SerializedProperty> visit)
		{
			var zones = graph.FindProperty("zones");
			for (var i = 0; i < zones.arraySize; i++)
				visit(zones.GetArrayElementAtIndex(i));
			var pools = graph.FindProperty("pools");
			for (var i = 0; i < pools.arraySize; i++)
			{
				var entries = pools.GetArrayElementAtIndex(i).FindPropertyRelative("entries");
				for (var j = 0; j < entries.arraySize; j++)
					visit(entries.GetArrayElementAtIndex(j));
			}
		}
	}
}