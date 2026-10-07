using System;
using System.Collections.Generic;
using System.Linq;
using LoopShift.Runtime;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace LoopShift.Tests
{
	public class GraphRoutingTests
	{
		private readonly List<Object> _owned = new();
		private LevelGraphAsset _graph;
		private LevelZone _a;
		private LevelZone _b;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<LevelGraphAsset>();
			_owned.Add(_graph);
			_a = Zone("A", LevelDirection.East, LevelDirection.West);
			_b = Zone("B", LevelDirection.East, LevelDirection.West);
			Set(_graph, "zones", new List<LevelZoneEntry> { Entry(_a), Entry(_b) });
			_graph.SetStartZone("A");
		}

		[TearDown]
		public void TearDown()
		{
			for (var i = _owned.Count - 1; i >= 0; i--)
				Object.DestroyImmediate(_owned[i]);
			_owned.Clear();
		}

		[Test]
		public void ValidFixedGraphHasNoErrors()
		{
			Connect("A", "B");
			Assert.That(Errors(), Is.Empty);
		}

		[Test]
		public void LogicalIdOverridesAreUsedInBothDirections()
		{
			Set(_graph, "zones", new List<LevelZoneEntry> { Entry(_a, "alias-A"), Entry(_b, "alias-B") });
			_graph.SetStartZone("alias-A");
			Connect("alias-A", "alias-B");
			Assert.That(Errors(), Is.Empty);
			var cache = new LevelGraphRuntimeCache(_graph);
			Assert.That(Resolve(cache, "alias-A", _a, LevelDirection.East).ZoneId, Is.EqualTo("alias-B"));
			Assert.That(Resolve(cache, "alias-B", _b, LevelDirection.West).ZoneId, Is.EqualTo("alias-A"));
		}

		[Test]
		public void RouteKeysDoNotCollideOnDelimitersAndTagsIgnoreCase()
		{
			Assert.That(new LevelRouteKey("A|East", LevelDirection.East, "x"),
				Is.Not.EqualTo(new LevelRouteKey("A", LevelDirection.East, "East|x")));
			Assert.That(new LevelRouteKey("A", LevelDirection.East, "DOOR"),
				Is.EqualTo(new LevelRouteKey("A", LevelDirection.East, "door")));
			Assert.That(new LevelRouteKey("A", LevelDirection.East, null),
				Is.EqualTo(new LevelRouteKey("A", LevelDirection.East, " ")));
			Assert.That(new LevelRouteKey("A", LevelDirection.East, "door"),
				Is.Not.EqualTo(new LevelRouteKey("a", LevelDirection.East, "door")));
		}

		[Test]
		public void ExactTaggedRouteWinsOverWildcard()
		{
			var c = Zone("C", LevelDirection.West);
			Set(_graph, "zones", new List<LevelZoneEntry> { Entry(_a), Entry(_b), Entry(c) });
			Set(_graph, "fixedConnections", new List<LevelConnection>
			{
				new() { fromZoneId = "A", toZoneId = "B", direction = LevelDirection.East, bidirectional = false },
				new() { fromZoneId = "A", toZoneId = "C", direction = LevelDirection.East,
					fromExitTag = "dEfAuLt", bidirectional = false }
			});
			Assert.That(Errors(), Is.Empty);
			Assert.That(Resolve(new LevelGraphRuntimeCache(_graph), "A", _a, LevelDirection.East).ZoneId, Is.EqualTo("C"));
		}

		[Test]
		public void PoolOnlyZonesCanStartAndReceiveFixedConnections()
		{
			Set(_graph, "zones", new List<LevelZoneEntry> { Entry(_a) });
			Pool(true, Entry(_b));
			_graph.SetStartZone("B");
			Connect("A", "B");
			Assert.That(Errors(), Is.Empty);
			Assert.That(new LevelGraphRuntimeCache(_graph).TryGetZone("B", out _), Is.True);
		}

		[Test]
		public void PoolFiltersIncompatibleExitDirectionsAndTags()
		{
			var wrongDirection = Zone("Wrong", LevelDirection.North);
			var wrongTag = Zone("Tag", LevelDirection.West);
			Set(wrongTag.Exits[0], "exitTag", "Other");
			Pool(true, Entry(wrongDirection, weight: int.MaxValue), Entry(wrongTag, weight: int.MaxValue), Entry(_b));
			Rule("Default");
			Assert.That(Errors(), Is.Empty);
			var cache = new LevelGraphRuntimeCache(_graph);
			var random = new Random(10);
			for (var i = 0; i < 100; i++)
				Assert.That(Resolve(cache, "A", _a, LevelDirection.East, random).ZoneId, Is.EqualTo("B"));
		}

		[Test]
		public void NoRepeatNeverFallsBackToTheExcludedSingleton()
		{
			Pool(false, Entry(_a));
			Rule();
			var cache = new LevelGraphRuntimeCache(_graph);
			Assert.That(Resolve(cache, "A", _a, LevelDirection.East), Is.Null);
		}

		[Test]
		public void NoRepeatExcludesCurrentLogicalId()
		{
			Pool(false, Entry(_a, weight: int.MaxValue), Entry(_b));
			Rule();
			var cache = new LevelGraphRuntimeCache(_graph);
			for (var i = 0; i < 50; i++)
				Assert.That(Resolve(cache, "A", _a, LevelDirection.East).ZoneId, Is.EqualTo("B"));
		}

		[Test]
		public void LargeWeightsDoNotOverflowAndSeedIsReproducible()
		{
			Pool(true, Entry(_a, weight: int.MaxValue), Entry(_b, weight: int.MaxValue));
			Rule();
			var first = new LevelGraphRuntimeCache(_graph);
			var second = new LevelGraphRuntimeCache(_graph);
			var r1 = new Random(1234);
			var r2 = new Random(1234);
			var seen = new HashSet<string>();
			for (var i = 0; i < 100; i++)
			{
				var id = Resolve(first, "A", _a, LevelDirection.East, r1).ZoneId;
				Assert.That(Resolve(second, "A", _a, LevelDirection.East, r2).ZoneId, Is.EqualTo(id));
				seen.Add(id);
			}
			Assert.That(seen.Count, Is.EqualTo(2));
		}

		[Test]
		public void RepeatedResolutionAllocatesNoManagedMemoryAfterWarmup()
		{
			Pool(true, Entry(_a), Entry(_b));
			Rule();
			var cache = new LevelGraphRuntimeCache(_graph);
			var random = new Random(4);
			var exit = _a.Exits[0];
			for (var i = 0; i < 32; i++)
				cache.TryResolve("A", exit, random, "A", out _, out _);
			var before = GC.GetAllocatedBytesForCurrentThread();
			for (var i = 0; i < 10000; i++)
				cache.TryResolve("A", exit, random, "A", out _, out _);
			var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			Assert.That(allocated, Is.Zero, "Compiled routing should allocate only during construction.");
		}

		[Test]
		public void CompiledIdentityDoesNotChangeWhenAuthoringEntryChanges()
		{
			Connect("A", "B");
			var cache = new LevelGraphRuntimeCache(_graph);
			_graph.Zones[1].zoneIdOverride = "Changed";
			Assert.That(Resolve(cache, "A", _a, LevelDirection.East).ZoneId, Is.EqualTo("B"));
		}

		[Test]
		public void NullConnectionIdsAreDiagnosticsInsteadOfExceptions()
		{
			Set(_graph, "fixedConnections", new List<LevelConnection> { new() });
			Assert.That(Errors(), Is.Not.Empty);
		}

		[Test]
		public void NullCollectionsDoNotCrashValidation()
		{
			Set(_graph, "zones", null);
			Set(_graph, "fixedConnections", null);
			Set(_graph, "pools", null);
			Set(_graph, "poolRules", null);
			Assert.That(Errors().Any(message => message.Contains("Start zone")), Is.True);
		}

		[Test]
		public void ReverseConnectionConflictsAreRejected()
		{
			Connect("A", "B");
			((List<LevelConnection>)_graph.FixedConnections).Add(new LevelConnection
			{
				fromZoneId = "B", toZoneId = "A", direction = LevelDirection.West, bidirectional = false
			});
			Assert.That(Errors().Any(message => message.Contains("Ambiguous")), Is.True);
		}

		[Test]
		public void DifferentPrefabsCannotShareAnId()
		{
			Pool(true, Entry(_b, "A"));
			Assert.That(Errors().Any(message => message.Contains("different prefabs")), Is.True);
		}

		[Test]
		public void PoolWithoutCompatibleCandidatesIsInvalid()
		{
			Pool(true, Entry(Zone("NorthOnly", LevelDirection.North)));
			Rule();
			Assert.That(Errors().Any(message => message.Contains("no opposing exit")), Is.True);
		}

		[TestCase(0)]
		[TestCase(-3)]
		public void NonPositivePoolWeightsAreInvalid(int weight)
		{
			Pool(true, Entry(_b, weight: weight));
			Assert.That(Errors().Any(message => message.Contains("positive weight")), Is.True);
		}

		[Test]
		public void NullAndDuplicatePoolEntriesAreInvalid()
		{
			Pool(true, Entry(_b), Entry(_b), null);
			var errors = Errors();
			Assert.That(errors.Any(message => message.Contains("duplicate zone")), Is.True);
			Assert.That(errors.Any(message => message.Contains("empty zone entry")), Is.True);
		}

		[Test]
		public void MissingOpposingExitIsInvalid()
		{
			Set(_b.Exits[1], "direction", LevelDirection.North);
			Connect("A", "B");
			Assert.That(Errors().Any(message => message.Contains("no opposing exit")), Is.True);
		}


		[TestCase(0, 0, 1)]
		[TestCase(1, 1, 9)]
		[TestCase(2, 2, 25)]
		[TestCase(4, 1, 9)]
		[TestCase(-1, 1, 1)]
		public void PreloadStaysInsideConfiguredWindow(int preload, int active, int expected)
		{
			var runner = GridRunner(preload, active);
			runner.StartRunner();
			Assert.That(runner.ActiveZones.Count, Is.EqualTo(expected));
			var radius = Math.Min(Math.Max(0, preload), Math.Max(0, active));
			Assert.That(runner.ActiveZones.Keys.All(cell => Math.Abs(cell.x) <= radius && Math.Abs(cell.y) <= radius),
				Is.True);
		}

		[Test]
		public void ActiveViewIsStableReadOnlyAndLiveAcrossRestart()
		{
			var runner = GridRunner(1, 1);
			var view = runner.ActiveZones;
			runner.StartRunner();
			Assert.That(runner.ActiveZones, Is.SameAs(view));
			Assert.That(view.Count, Is.EqualTo(9));
			Assert.Throws<NotSupportedException>(() => ((IDictionary<Vector2Int, LevelZone>)view).Clear());
			runner.StopRunner();
			Assert.That(view, Is.Empty);
			runner.StartRunner();
			Assert.That(view.Count, Is.EqualTo(9));
		}

		[Test]
		public void ReadingActiveViewDoesNotAllocateAfterWarmup()
		{
			var runner = GridRunner(0, 0);
			runner.StartRunner();
			var count = runner.ActiveZones.Count;
			var before = GC.GetAllocatedBytesForCurrentThread();
			for (var i = 0; i < 10000; i++)
				count += runner.ActiveZones.Count;
			var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			Assert.That(count, Is.EqualTo(10001));
			Assert.That(allocated, Is.Zero);
		}

		private EndlessLevelRunner GridRunner(int preload, int active)
		{
			var directions = new[] { LevelDirection.North, LevelDirection.East, LevelDirection.South, LevelDirection.West };
			var grid = Zone("Grid", directions);
			Set(_graph, "zones", new List<LevelZoneEntry> { Entry(grid) });
			_graph.SetStartZone("Grid");
			Pool(true, Entry(grid));
			Set(_graph, "poolRules", directions.Select(direction => new LevelPoolRule
				{ poolId = "Pool", direction = direction }).ToList());
			var root = new GameObject("Runner");
			_owned.Add(root);
			var runner = root.AddComponent<EndlessLevelRunner>();
			Set(runner, "playOnAwake", false);
			Set(runner, "graph", _graph);
			runner.SpawnSettings.preloadRadius = preload;
			runner.SpawnSettings.activeRadius = active;
			runner.SpawnSettings.maxPoolInstancesPerPrefab = 128;
			return runner;
		}
		[Test]
		public void NullExitListProducesConnectionDiagnosticsAndCanBeRefreshed()
		{
			Set(_a, "exits", null);
			Connect("A", "B");
			Assert.That(Errors().Any(message => message.Contains("no East exit")), Is.True);
			_a.RefreshExits();
			Assert.That(Errors(), Is.Empty);
		}

		private LevelZone Zone(string id, params LevelDirection[] directions)
		{
			var zone = LevelTestFactory.CreateZone(id, directions);
			_owned.Add(zone.gameObject);
			return zone;
		}

		private static LevelZoneEntry Entry(LevelZone zone, string id = null, int weight = 1) =>
			new() { prefab = zone, zoneIdOverride = id, weight = weight };

		private void Connect(string source, string target) =>
			Set(_graph, "fixedConnections", new List<LevelConnection>
			{
				new() { fromZoneId = source, toZoneId = target, direction = LevelDirection.East }
			});

		private void Pool(bool repeat, params LevelZoneEntry[] entries) =>
			Set(_graph, "pools", new List<LevelZonePool>
			{
				new() { poolId = "Pool", allowImmediateRepeat = repeat, entries = new List<LevelZoneEntry>(entries) }
			});

		private void Rule(string targetTag = null) =>
			Set(_graph, "poolRules", new List<LevelPoolRule>
			{
				new() { poolId = "Pool", direction = LevelDirection.East, requiredTargetExitTag = targetTag }
			});

		private List<string> Errors() => _graph.ValidateGraph()
			.Where(message => message.Severity == LevelValidationSeverity.Error).Select(message => message.Message).ToList();

		private static LevelZoneEntry Resolve(LevelGraphRuntimeCache cache, string id, LevelZone zone,
			LevelDirection direction, Random random = null)
		{
			zone.TryGetExit(direction, null, out var exit);
			return cache.TryResolve(id, exit, random ?? new Random(1), id, out var entry, out _) ? entry : null;
		}

		private static void Set(object target, string field, object value) =>
			LevelTestFactory.Set(target, field, value);
	}
}