using System.Collections.Generic;
using System.Reflection;
using LoopShift.Runtime;
using UnityEngine;

namespace LoopShift.Tests
{
	public static class LevelTestFactory
	{
		public static void Set(object target, string field, object value) =>
			target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

		public static LevelZone CreateZone(string id, IEnumerable<LevelDirection> directions, float halfSize = 0)
		{
			var root = new GameObject(id);
			root.SetActive(false);
			var zone = root.AddComponent<LevelZone>();
			zone.SetZoneId(id);
			foreach (var direction in directions)
			{
				var child = new GameObject(direction.ToString());
				child.transform.SetParent(root.transform, false);
				child.transform.localPosition = (Vector2)LevelDirectionUtility.ToCellOffset(direction) * halfSize;
				child.AddComponent<BoxCollider2D>().isTrigger = true;
				var exit = child.AddComponent<LevelExit>();
				Set(exit, "direction", direction);
			}
			zone.RefreshExits();
			return zone;
		}

		public static LevelGraphAsset CreateGraph(params LevelZone[] zones)
		{
			var graph = ScriptableObject.CreateInstance<LevelGraphAsset>();
			var entries = new List<LevelZoneEntry>();
			foreach (var zone in zones)
				entries.Add(new LevelZoneEntry { prefab = zone });
			Set(graph, "zones", entries);
			graph.SetStartZone(zones[0].ZoneId);
			return graph;
		}

		public static EndlessLevelRunner CreateRunner(LevelGraphAsset graph, int preload = 0, int active = 1)
		{
			var root = new GameObject("Runner");
			root.SetActive(false);
			var runner = root.AddComponent<EndlessLevelRunner>();
			Set(runner, "graph", graph);
			Set(runner, "playOnAwake", false);
			runner.SpawnSettings.preloadRadius = preload;
			runner.SpawnSettings.activeRadius = active;
			root.SetActive(true);
			return runner;
		}
	}
}