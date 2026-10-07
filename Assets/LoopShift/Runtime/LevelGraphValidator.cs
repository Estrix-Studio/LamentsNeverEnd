using System;
using System.Collections.Generic;

namespace LoopShift.Runtime
{
	/// <summary>Shared by authoring tools and runtime startup; invalid content yields diagnostics, not exceptions.</summary>
	public sealed class LevelGraphValidator
	{
		private readonly LevelGraphAsset _graph;
		private readonly List<LevelValidationMessage> _messages = new();
		private readonly Dictionary<string, LevelZoneEntry> _zones = new(StringComparer.Ordinal);
		private readonly HashSet<string> _listedZones = new(StringComparer.Ordinal);
		private readonly Dictionary<string, LevelZonePool> _pools = new(StringComparer.Ordinal);
		private readonly HashSet<LevelRouteKey> _routes = new();

		public LevelGraphValidator(LevelGraphAsset graph) => _graph = graph;

		public IReadOnlyList<LevelValidationMessage> Validate()
		{
			_messages.Clear();
			_zones.Clear();
			_listedZones.Clear();
			_pools.Clear();
			_routes.Clear();
			if (_graph == null)
			{
				Error("No graph asset assigned.");
				return _messages;
			}

			foreach (var entry in _graph.Zones)
			{
				if (!RegisterZone(entry))
					continue;
				if (!_listedZones.Add(entry.ZoneId))
					Error($"Duplicate graph zone ID '{entry.ZoneId}'.");
			}
			ValidatePools();
			if (string.IsNullOrWhiteSpace(_graph.StartZoneId) || !_zones.ContainsKey(_graph.StartZoneId))
				Error($"Start zone '{_graph.StartZoneId}' is missing.");

			foreach (var entry in _zones.Values)
				ValidateExits(entry);
			ValidateConnections();
			ValidatePoolRules();
			return _messages;
		}

		private bool RegisterZone(LevelZoneEntry entry)
		{
			if (entry?.prefab == null)
			{
				Error("Graph or pool contains an empty zone entry.");
				return false;
			}
			var id = entry.ZoneId;
			if (string.IsNullOrWhiteSpace(id))
			{
				Error($"Zone prefab '{entry.prefab.name}' has no ID.");
				return false;
			}
			if (_zones.TryGetValue(id, out var existing))
			{
				if (existing.prefab == entry.prefab)
					return true;
				Error($"Zone ID '{id}' refers to different prefabs.");
				return false;
			}

			var size = entry.prefab.Size;
			if (!IsPositiveFinite(size.x) || !IsPositiveFinite(size.y))
				Error($"Zone '{id}' has invalid bounds.");
			_zones.Add(id, entry);
			return true;
		}

		private void ValidateExits(LevelZoneEntry entry)
		{
			var seen = new HashSet<LevelRouteKey>();
			foreach (var exit in entry.prefab.Exits)
			{
				if (exit == null)
				{
					Error($"Zone '{entry.ZoneId}' contains an empty exit reference. Refresh its exits.");
					continue;
				}
				if (!LevelDirectionUtility.IsCardinal(exit.Direction))
					Error($"Zone '{entry.ZoneId}' has an exit with an invalid direction.");
				if (!seen.Add(new LevelRouteKey(entry.ZoneId, exit.Direction, exit.ExitTag)))
					Error($"Zone '{entry.ZoneId}' has duplicate {exit.Direction} exits tagged '{exit.ExitTag}'.");
				if (exit.GetComponentInParent<LevelZone>(true) != entry.prefab)
					Error($"Zone '{entry.ZoneId}' lists an exit owned by another zone.");
				var collider = exit.GetComponent<UnityEngine.Collider2D>();
				if (collider == null || !collider.isTrigger)
					Error($"Zone '{entry.ZoneId}' exit '{exit.name}' requires a trigger collider.");

				if (string.IsNullOrWhiteSpace(exit.FixedTargetZoneId))
					continue;
				if (!_zones.TryGetValue(exit.FixedTargetZoneId, out var target))
				{
					Error($"Exit on '{entry.ZoneId}' references missing zone '{exit.FixedTargetZoneId}'.");
					continue;
				}
				if (!target.prefab.TryGetExit(LevelDirectionUtility.Opposite(exit.Direction), null, out _))
					Error($"Exit target '{target.ZoneId}' has no opposing exit for {exit.Direction}.");
			}
		}

		private void ValidateConnections()
		{
			foreach (var connection in _graph.FixedConnections)
			{
				if (connection == null)
				{
					Error("Graph contains an empty fixed connection.");
					continue;
				}
				ValidateRoute(connection.fromZoneId, connection.direction, connection.fromExitTag,
					connection.toZoneId, connection.toExitTag);
				if (connection.bidirectional)
					ValidateRoute(connection.toZoneId, LevelDirectionUtility.Opposite(connection.direction),
						connection.toExitTag, connection.fromZoneId, connection.fromExitTag);
			}
		}

		private void ValidateRoute(string sourceId, LevelDirection direction, string sourceTag, string targetId, string targetTag)
		{
			if (!LevelDirectionUtility.IsCardinal(direction))
			{
				Error("A fixed connection uses an invalid direction.");
				return;
			}
			if (!_routes.Add(new LevelRouteKey(sourceId, direction, sourceTag)))
				Error($"Ambiguous connection from '{sourceId}' through {direction} tagged '{sourceTag}'.");
			if (!TryZone(sourceId, out var source))
			{
				Error($"Connection source zone '{sourceId}' is missing.");
				return;
			}
			if (!TryZone(targetId, out var target))
			{
				Error($"Connection target zone '{targetId}' is missing.");
				return;
			}
			if (!source.prefab.TryGetExit(direction, sourceTag, out _))
				Error($"Source '{sourceId}' has no {direction} exit matching '{sourceTag}'.");
			if (!target.prefab.TryGetExit(LevelDirectionUtility.Opposite(direction), targetTag, out _))
				Error($"Target '{targetId}' has no opposing exit matching '{targetTag}'.");
		}

		private void ValidatePools()
		{
			foreach (var pool in _graph.Pools)
			{
				if (pool == null || string.IsNullOrWhiteSpace(pool.poolId))
				{
					Error("Graph contains a pool without an ID.");
					continue;
				}
				if (_pools.ContainsKey(pool.poolId))
					Error($"Duplicate pool ID '{pool.poolId}'.");
				else
					_pools.Add(pool.poolId, pool);
				if (pool.entries == null || pool.entries.Count == 0)
				{
					Error($"Pool '{pool.poolId}' has no entries.");
					continue;
				}
				var ids = new HashSet<string>(StringComparer.Ordinal);
				foreach (var entry in pool.entries)
				{
					if (!RegisterZone(entry))
						continue;
					if (!ids.Add(entry.ZoneId))
						Error($"Pool '{pool.poolId}' contains duplicate zone '{entry.ZoneId}'; use its weight instead.");
					if (entry.weight <= 0)
						Error($"Pool '{pool.poolId}' zone '{entry.ZoneId}' must have a positive weight.");
				}
				if (!pool.allowImmediateRepeat && ids.Count == 1)
					Warning($"Pool '{pool.poolId}' has one zone and disallows repeats; it cannot continue from that zone.");
			}
		}

		private void ValidatePoolRules()
		{
			foreach (var rule in _graph.PoolRules)
			{
				if (rule == null || !LevelDirectionUtility.IsCardinal(rule.direction))
				{
					Error("Graph contains an empty pool rule or an invalid rule direction.");
					continue;
				}
				if (string.IsNullOrWhiteSpace(rule.poolId) || !_pools.TryGetValue(rule.poolId, out var pool))
				{
					Error($"Pool rule references missing pool '{rule.poolId}'.");
					continue;
				}
				if (HasCompatibleEntry(pool, rule))
					continue;
				Error($"Pool '{pool.poolId}' has no opposing exit for {rule.direction} matching '{rule.requiredTargetExitTag}'.");
			}
		}

		private static bool HasCompatibleEntry(LevelZonePool pool, LevelPoolRule rule)
		{
			if (pool.entries == null)
				return false;
			foreach (var entry in pool.entries)
				if (entry?.prefab != null && entry.prefab.TryGetExit(
					    LevelDirectionUtility.Opposite(rule.direction), rule.requiredTargetExitTag, out _))
					return true;
			return false;
		}

		private bool TryZone(string id, out LevelZoneEntry entry)
		{
			entry = null;
			return !string.IsNullOrWhiteSpace(id) && _zones.TryGetValue(id, out entry);
		}

		private static bool IsPositiveFinite(float value) => value > 0 && !float.IsInfinity(value) && !float.IsNaN(value);
		private void Error(string message) => _messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, message, _graph));
		private void Warning(string message) => _messages.Add(new LevelValidationMessage(LevelValidationSeverity.Warning, message, _graph));
	}
}