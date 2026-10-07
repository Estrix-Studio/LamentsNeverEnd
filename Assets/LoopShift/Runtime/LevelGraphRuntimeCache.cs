using System;
using System.Collections.Generic;

namespace LoopShift.Runtime
{
	/// <summary>Compiles validated authoring data once. Resolution does not allocate.</summary>
	internal sealed class LevelGraphRuntimeCache
	{
		private readonly Dictionary<string, LevelZoneEntry> _zonesById = new(StringComparer.Ordinal);
		private readonly Dictionary<LevelRouteKey, Route> _connections = new();
		private readonly List<PoolRule> _poolRules = new();

		public LevelGraphRuntimeCache(LevelGraphAsset graph)
		{
			if (graph == null)
				throw new ArgumentNullException(nameof(graph));

			foreach (var entry in graph.Zones)
				RegisterZone(entry);
			foreach (var pool in graph.Pools)
			{
				if (pool?.entries == null)
					continue;
				foreach (var entry in pool.entries)
					RegisterZone(entry);
			}

			foreach (var connection in graph.FixedConnections)
			{
				if (connection == null)
					continue;
				AddRoute(connection.fromZoneId, connection.direction, connection.fromExitTag,
					connection.toZoneId, connection.toExitTag);
				if (connection.bidirectional)
					AddRoute(connection.toZoneId, LevelDirectionUtility.Opposite(connection.direction),
						connection.toExitTag, connection.fromZoneId, connection.fromExitTag);
			}

			var pools = new Dictionary<string, LevelZonePool>(StringComparer.Ordinal);
			foreach (var pool in graph.Pools)
				if (pool != null && !string.IsNullOrWhiteSpace(pool.poolId))
					pools[pool.poolId] = pool;

			foreach (var rule in graph.PoolRules)
			{
				if (rule == null || string.IsNullOrWhiteSpace(rule.poolId) ||
				    !pools.TryGetValue(rule.poolId, out var pool) || pool.entries == null)
					continue;
				var candidates = new List<Candidate>(pool.entries.Count);
				foreach (var entry in pool.entries)
				{
					if (entry?.prefab == null || !entry.prefab.TryGetExit(
						    LevelDirectionUtility.Opposite(rule.direction), rule.requiredTargetExitTag, out _))
						continue;
					candidates.Add(new Candidate(_zonesById[entry.ZoneId], Math.Max(1, entry.weight)));
				}
				_poolRules.Add(new PoolRule(rule, pool.allowImmediateRepeat, candidates.ToArray()));
			}
		}

		public bool TryGetZone(string zoneId, out LevelZoneEntry entry)
		{
			entry = null;
			return !string.IsNullOrWhiteSpace(zoneId) && _zonesById.TryGetValue(zoneId, out entry);
		}

		// The caller must use the active entry's logical ID, which may override its prefab ID.
		public bool TryResolve(string currentZoneId, LevelExit exit, Random random, string previousZoneId,
			out LevelZoneEntry nextEntry, out string requiredTargetExitTag)
		{
			nextEntry = null;
			requiredTargetExitTag = string.Empty;
			if (string.IsNullOrWhiteSpace(currentZoneId) || exit == null || random == null)
				return false;

			if (_connections.TryGetValue(new LevelRouteKey(currentZoneId, exit.Direction, exit.ExitTag), out var route) ||
			    _connections.TryGetValue(new LevelRouteKey(currentZoneId, exit.Direction, null), out route))
			{
				requiredTargetExitTag = route.TargetTag;
				return TryGetZone(route.TargetId, out nextEntry);
			}

			if (!string.IsNullOrWhiteSpace(exit.FixedTargetZoneId))
				return TryGetZone(exit.FixedTargetZoneId, out nextEntry);

			foreach (var rule in _poolRules)
			{
				if (rule.Direction != exit.Direction || !LevelZone.TagsCompatible(exit.ExitTag, rule.SourceTag))
					continue;
				nextEntry = rule.Pick(random, previousZoneId);
				if (nextEntry == null)
					continue;
				requiredTargetExitTag = rule.TargetTag;
				return true;
			}
			return false;
		}

		private void RegisterZone(LevelZoneEntry entry)
		{
			if (entry?.prefab == null || string.IsNullOrWhiteSpace(entry.ZoneId) || _zonesById.ContainsKey(entry.ZoneId))
				return;
			// Snapshot identity so Inspector edits cannot mutate an already compiled route table.
			_zonesById.Add(entry.ZoneId, new LevelZoneEntry
			{
				prefab = entry.prefab,
				zoneIdOverride = entry.ZoneId,
				weight = entry.weight
			});
		}

		private void AddRoute(string source, LevelDirection direction, string sourceTag, string target, string targetTag)
		{
			_connections[new LevelRouteKey(source, direction, sourceTag)] = new Route(target, targetTag);
		}

		private readonly struct Route
		{
			public readonly string TargetId;
			public readonly string TargetTag;
			public Route(string targetId, string targetTag)
			{
				TargetId = targetId;
				TargetTag = targetTag;
			}
		}

		private readonly struct Candidate
		{
			public readonly LevelZoneEntry Entry;
			public readonly int Weight;
			public Candidate(LevelZoneEntry entry, int weight)
			{
				Entry = entry;
				Weight = weight;
			}
		}

		private sealed class PoolRule
		{
			public readonly LevelDirection Direction;
			public readonly string SourceTag;
			public readonly string TargetTag;
			private readonly bool _allowRepeat;
			private readonly Candidate[] _candidates;
			private readonly long _totalWeight;

			public PoolRule(LevelPoolRule rule, bool allowRepeat, Candidate[] candidates)
			{
				Direction = rule.direction;
				SourceTag = rule.fromExitTag;
				TargetTag = rule.requiredTargetExitTag;
				_allowRepeat = allowRepeat;
				_candidates = candidates;
				foreach (var candidate in candidates)
					_totalWeight += candidate.Weight;
			}

			public LevelZoneEntry Pick(Random random, string previousId)
			{
				var total = _totalWeight;
				if (!_allowRepeat)
					foreach (var candidate in _candidates)
						if (candidate.Entry.ZoneId == previousId)
							total -= candidate.Weight;
				if (total <= 0)
					return null;

				// A 64-bit accumulator prevents overflow from large user-authored weights.
				var roll = (long)(random.NextDouble() * total);
				foreach (var candidate in _candidates)
				{
					if (!_allowRepeat && candidate.Entry.ZoneId == previousId)
						continue;
					roll -= candidate.Weight;
					if (roll < 0)
						return candidate.Entry;
				}
				return null;
			}
		}
	}
}