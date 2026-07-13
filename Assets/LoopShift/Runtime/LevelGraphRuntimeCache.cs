using System.Collections.Generic;
using UnityEngine;

namespace LoopShift.Runtime
{
	internal sealed class LevelGraphRuntimeCache
	{
		private readonly Dictionary<string, LevelZoneEntry> _zonesById = new();
		private readonly Dictionary<string, LevelConnection> _connectionsByKey = new();
		private readonly Dictionary<string, LevelZonePool> _poolsById = new();
		private readonly List<LevelPoolRule> _poolRules = new();

		public LevelGraphRuntimeCache(LevelGraphAsset graph)
		{
			foreach (var zone in graph.Zones)
			{
				if (zone?.prefab == null || string.IsNullOrWhiteSpace(zone.ZoneId))
					continue;
				_zonesById[zone.ZoneId] = zone;
			}

			foreach (var connection in graph.FixedConnections)
			{
				if (connection == null)
					continue;
				_connectionsByKey[ConnectionKey(connection.fromZoneId, connection.direction, connection.fromExitTag)] = connection;
				if (connection.bidirectional)
				{
					var reverse = new LevelConnection
					{
						fromZoneId = connection.toZoneId,
						direction = LevelDirectionUtility.Opposite(connection.direction),
						fromExitTag = connection.toExitTag,
						toZoneId = connection.fromZoneId,
						toExitTag = connection.fromExitTag,
						bidirectional = false
					};
					_connectionsByKey[ConnectionKey(reverse.fromZoneId, reverse.direction, reverse.fromExitTag)] = reverse;
				}
			}

			foreach (var pool in graph.Pools)
			{
				if (pool == null || string.IsNullOrWhiteSpace(pool.poolId))
					continue;
				_poolsById[pool.poolId] = pool;
			}

			foreach (var rule in graph.PoolRules)
				if (rule != null)
					_poolRules.Add(rule);
		}

		public bool TryGetZone(string zoneId, out LevelZoneEntry entry)
		{
			return _zonesById.TryGetValue(zoneId, out entry);
		}

		public bool TryResolve(LevelZone currentZone, LevelExit exit, System.Random random, string lastZoneId, out LevelZoneEntry nextEntry, out string requiredTargetExitTag)
		{
			nextEntry = null;
			requiredTargetExitTag = string.Empty;
			if (currentZone == null || exit == null)
				return false;

			var key = ConnectionKey(currentZone.ZoneId, exit.Direction, exit.ExitTag);
			if (_connectionsByKey.TryGetValue(key, out var connection) ||
			    _connectionsByKey.TryGetValue(ConnectionKey(currentZone.ZoneId, exit.Direction, string.Empty), out connection))
			{
				requiredTargetExitTag = connection.toExitTag;
				return TryGetZone(connection.toZoneId, out nextEntry);
			}

			if (!string.IsNullOrWhiteSpace(exit.FixedTargetZoneId) && TryGetZone(exit.FixedTargetZoneId, out nextEntry))
				return true;

			foreach (var rule in _poolRules)
			{
				if (rule.direction != exit.Direction)
					continue;
				if (!LevelZone.TagsCompatible(exit.ExitTag, rule.fromExitTag))
					continue;
				if (!_poolsById.TryGetValue(rule.poolId, out var pool))
					continue;

				requiredTargetExitTag = rule.requiredTargetExitTag;
				nextEntry = PickFromPool(pool, random, lastZoneId);
				return nextEntry != null;
			}

			return false;
		}

		private static string ConnectionKey(string zoneId, LevelDirection direction, string exitTag)
		{
			return $"{zoneId}|{direction}|{exitTag}";
		}

		private static LevelZoneEntry PickFromPool(LevelZonePool pool, System.Random random, string lastZoneId)
		{
			if (pool.entries == null || pool.entries.Count == 0)
				return null;

			var totalWeight = 0;
			foreach (var entry in pool.entries)
			{
				if (entry?.prefab == null)
					continue;
				if (!pool.allowImmediateRepeat && entry.ZoneId == lastZoneId && pool.entries.Count > 1)
					continue;
				totalWeight += Mathf.Max(1, entry.weight);
			}

			if (totalWeight <= 0)
				return null;

			var roll = random.Next(0, totalWeight);
			foreach (var entry in pool.entries)
			{
				if (entry?.prefab == null)
					continue;
				if (!pool.allowImmediateRepeat && entry.ZoneId == lastZoneId && pool.entries.Count > 1)
					continue;

				roll -= Mathf.Max(1, entry.weight);
				if (roll < 0)
					return entry;
			}

			return null;
		}
	}
}
