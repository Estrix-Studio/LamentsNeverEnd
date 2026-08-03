using System;
using System.Collections.Generic;
using LoopShift.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LoopShift.Tests
{
	public sealed class LevelGraphTests
	{
		private readonly List<UnityEngine.Object> _objects = new();

		[TearDown]
		public void TearDown()
		{
			foreach (var value in _objects)
				if (value != null)
					UnityEngine.Object.DestroyImmediate(value);
			_objects.Clear();
		}

		[Test]
		public void ValidSingleZonePool_HasNoValidationMessages()
		{
			var zone = CreateZone("zone-a");
			var graph = CreateGraph((zone, string.Empty));
			var serializedGraph = new SerializedObject(graph);
			serializedGraph.FindProperty("startZoneId").stringValue = "zone-a";
			var pools = serializedGraph.FindProperty("pools");
			pools.arraySize = 1;
			var pool = pools.GetArrayElementAtIndex(0);
			pool.FindPropertyRelative("poolId").stringValue = "Default";
			var entries = pool.FindPropertyRelative("entries");
			entries.arraySize = 1;
			SetEntry(entries.GetArrayElementAtIndex(0), zone, string.Empty);
			serializedGraph.ApplyModifiedPropertiesWithoutUndo();

			Assert.That(graph.ValidateGraph(), Is.Empty);
		}

		[Test]
		public void DuplicateEffectiveZoneId_IsRejected()
		{
			var first = CreateZone("first");
			var second = CreateZone("second");
			var graph = CreateGraph((first, "shared"), (second, "shared"));
			graph.SetStartZone("shared");

			Assert.That(graph.ValidateGraph(), Has.Some.Matches<LevelValidationMessage>(
				message => message.Severity == LevelValidationSeverity.Error && message.Message.Contains("Duplicate zone ID")));
		}

		[Test]
		public void FixedConnection_UsesZoneIdOverrideAtRuntime()
		{
			var source = CreateZone("source-prefab-id");
			var target = CreateZone("target-prefab-id");
			var exitObject = new GameObject("East Exit");
			_objects.Add(exitObject);
			exitObject.AddComponent<BoxCollider2D>().isTrigger = true;
			var exit = exitObject.AddComponent<LevelExit>();
			var serializedExit = new SerializedObject(exit);
			serializedExit.FindProperty("direction").enumValueIndex = (int)LevelDirection.East;
			serializedExit.FindProperty("exitTag").stringValue = "Default";
			serializedExit.ApplyModifiedPropertiesWithoutUndo();

			var graph = CreateGraph((source, "source"), (target, "target"));
			graph.SetStartZone("source");
			var serializedGraph = new SerializedObject(graph);
			var connections = serializedGraph.FindProperty("fixedConnections");
			connections.arraySize = 1;
			var connection = connections.GetArrayElementAtIndex(0);
			connection.FindPropertyRelative("fromZoneId").stringValue = "source";
			connection.FindPropertyRelative("direction").enumValueIndex = (int)LevelDirection.East;
			connection.FindPropertyRelative("fromExitTag").stringValue = "Default";
			connection.FindPropertyRelative("toZoneId").stringValue = "target";
			connection.FindPropertyRelative("toExitTag").stringValue = "Default";
			connection.FindPropertyRelative("bidirectional").boolValue = false;
			serializedGraph.ApplyModifiedPropertiesWithoutUndo();

			var cache = new LevelGraphRuntimeCache(graph);
			var resolved = cache.TryResolve("source", exit, new System.Random(1), null, out var entry, out var requiredTag);

			Assert.That(resolved, Is.True);
			Assert.That(entry.ZoneId, Is.EqualTo("target"));
			Assert.That(requiredTag, Is.EqualTo("Default"));
		}

		private LevelGraphAsset CreateGraph(params (LevelZone zone, string idOverride)[] zones)
		{
			var graph = ScriptableObject.CreateInstance<LevelGraphAsset>();
			_objects.Add(graph);
			var serializedGraph = new SerializedObject(graph);
			var entries = serializedGraph.FindProperty("zones");
			entries.arraySize = zones.Length;
			for (var index = 0; index < zones.Length; index++)
				SetEntry(entries.GetArrayElementAtIndex(index), zones[index].zone, zones[index].idOverride);
			serializedGraph.ApplyModifiedPropertiesWithoutUndo();
			return graph;
		}

		private LevelZone CreateZone(string id)
		{
			var gameObject = new GameObject(id);
			_objects.Add(gameObject);
			var zone = gameObject.AddComponent<LevelZone>();
			zone.SetZoneId(id);
			return zone;
		}

		private static void SetEntry(SerializedProperty entry, LevelZone zone, string idOverride)
		{
			entry.FindPropertyRelative("prefab").objectReferenceValue = zone;
			entry.FindPropertyRelative("zoneIdOverride").stringValue = idOverride;
			entry.FindPropertyRelative("weight").intValue = 1;
		}
	}
}
