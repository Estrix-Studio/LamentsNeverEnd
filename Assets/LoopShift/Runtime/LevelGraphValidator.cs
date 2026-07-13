using System.Collections.Generic;

namespace LoopShift.Runtime
{
	public sealed class LevelGraphValidator
	{
		private readonly LevelGraphAsset _graph;
		private readonly List<LevelValidationMessage> _messages = new();
		private readonly Dictionary<string, LevelZoneEntry> _zones = new();

		public LevelGraphValidator(LevelGraphAsset graph)
		{
			_graph = graph;
		}

		public IReadOnlyList<LevelValidationMessage> Validate()
		{
			_messages.Clear();
			_zones.Clear();

			if (_graph == null)
			{
				_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "No graph asset assigned.", null));
				return _messages;
			}

			ValidateZones();
			ValidateStartZone();
			ValidateConnections();
			ValidatePools();
			return _messages;
		}

		private void ValidateZones()
		{
			foreach (var entry in _graph.Zones)
			{
				if (entry == null || entry.prefab == null)
				{
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "Graph contains an empty zone entry.", _graph));
					continue;
				}

				var zoneId = entry.ZoneId;
				if (string.IsNullOrWhiteSpace(zoneId))
				{
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"Zone prefab '{entry.prefab.name}' has no zone ID.", entry.prefab));
					continue;
				}

				if (_zones.ContainsKey(zoneId))
				{
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"Duplicate zone ID '{zoneId}'.", entry.prefab));
					continue;
				}

				if (entry.prefab.Size.x <= 0f || entry.prefab.Size.y <= 0f)
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"Zone '{zoneId}' has invalid bounds.", entry.prefab));

				_zones[zoneId] = entry;
			}
		}

		private void ValidateStartZone()
		{
			if (string.IsNullOrWhiteSpace(_graph.StartZoneId))
			{
				_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "Graph has no start zone ID.", _graph));
				return;
			}

			if (!_zones.ContainsKey(_graph.StartZoneId))
				_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"Start zone '{_graph.StartZoneId}' is not in the graph zone list.", _graph));
		}

		private void ValidateConnections()
		{
			foreach (var connection in _graph.FixedConnections)
			{
				if (connection == null)
					continue;
				if (connection.direction is LevelDirection.None or LevelDirection.Center)
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "A fixed connection uses an invalid direction.", _graph));
				if (!_zones.ContainsKey(connection.fromZoneId))
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"Connection source zone '{connection.fromZoneId}' is missing.", _graph));
				if (!_zones.ContainsKey(connection.toZoneId))
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"Connection target zone '{connection.toZoneId}' is missing.", _graph));
			}
		}

		private void ValidatePools()
		{
			var poolsById = new HashSet<string>();
			foreach (var pool in _graph.Pools)
			{
				if (pool == null || string.IsNullOrWhiteSpace(pool.poolId))
				{
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "Graph contains a pool without an ID.", _graph));
					continue;
				}

				if (!poolsById.Add(pool.poolId))
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"Duplicate pool ID '{pool.poolId}'.", _graph));

				if (pool.entries == null || pool.entries.Count == 0)
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Warning, $"Pool '{pool.poolId}' has no entries.", _graph));
			}

			foreach (var rule in _graph.PoolRules)
			{
				if (rule == null)
					continue;
				if (rule.direction is LevelDirection.None or LevelDirection.Center)
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "A pool rule uses an invalid direction.", _graph));
				if (!poolsById.Contains(rule.poolId))
					_messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"Pool rule references missing pool '{rule.poolId}'.", _graph));
			}
		}
	}
}
