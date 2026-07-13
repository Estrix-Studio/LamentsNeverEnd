using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoopShift.Runtime
{
	[DisallowMultipleComponent]
	public class EndlessLevelRunner : MonoBehaviour
	{
		[SerializeField] private LevelGraphAsset graph;
		[SerializeField] private LevelSpawnSettings spawnSettings = new();
		[SerializeField] private List<Transform> targets = new();
		[SerializeField] private bool playOnAwake = true;

		private readonly Dictionary<Vector2Int, ActiveZone> _activeZones = new();
		private readonly Dictionary<LevelZone, Stack<LevelZone>> _pool = new();
		private LevelGraphRuntimeCache _cache;
		private System.Random _random;
		private Vector2Int _currentCell;
		private string _lastGeneratedZoneId;
		private bool _isRunning;

		public event Action<LevelZoneEvent> ZoneSpawned;
		public event Action<LevelZoneEvent> ZoneEntered;
		public event Action<LevelZoneEvent> ZoneBecameCenter;
		public event Action<LevelZoneEvent> ZoneDespawned;
		public event Action<LevelRuntimeMessage> RuntimeError;

		public LevelGraphAsset Graph => graph;
		public LevelSpawnSettings SpawnSettings => spawnSettings;
		public IReadOnlyDictionary<Vector2Int, LevelZone> ActiveZones => BuildActiveZoneSnapshot();

		private void Awake()
		{
			if (playOnAwake)
				StartRunner();
		}

		private void OnDestroy()
		{
			StopRunner();
		}

		public void StartRunner()
		{
			StopRunner();
			if (graph == null)
			{
				ReportError("Cannot start endless level runner: graph is not assigned.", this);
				return;
			}

			var validation = graph.ValidateGraph();
			foreach (var message in validation)
			{
				if (message.Severity == LevelValidationSeverity.Error)
				{
					ReportError(message.Message, message.Context);
					return;
				}
			}

			_cache = new LevelGraphRuntimeCache(graph);
			_random = new System.Random(spawnSettings.useDeterministicSeed ? spawnSettings.seed : Environment.TickCount);
			_currentCell = Vector2Int.zero;
			_isRunning = true;

			if (!_cache.TryGetZone(graph.StartZoneId, out var startEntry))
			{
				ReportError($"Start zone '{graph.StartZoneId}' could not be resolved.", graph);
				return;
			}

			var startZone = SpawnZone(startEntry, _currentCell, transform.position, null, null);
			if (startZone == null)
				return;

			MakeCenter(_currentCell);
			PreloadAround(_currentCell);
		}

		public void StopRunner()
		{
			foreach (var active in _activeZones.Values)
				Release(active);
			_activeZones.Clear();
			_isRunning = false;
		}

		public void AddTarget(Transform target)
		{
			if (target != null && !targets.Contains(target))
				targets.Add(target);
		}

		public void RemoveTarget(Transform target)
		{
			targets.Remove(target);
		}

		private void OnExitTriggered(LevelExit exit, Collider2D other)
		{
			if (!_isRunning || exit == null || !IsTargetCollider(other))
				return;

			var sourceZone = exit.GetComponentInParent<LevelZone>();
			if (sourceZone == null || !_activeZones.TryGetValue(_currentCell, out var current) || current.Zone != sourceZone)
				return;

			var nextCell = _currentCell + LevelDirectionUtility.ToCellOffset(exit.Direction);
			if (!_activeZones.ContainsKey(nextCell))
			{
				if (!_cache.TryResolve(sourceZone, exit, _random, _lastGeneratedZoneId, out var nextEntry, out var requiredTargetExitTag))
				{
					ReportError($"No zone could be resolved from '{sourceZone.ZoneId}' through {exit.Direction}.", sourceZone);
					return;
				}

				SpawnZone(nextEntry, nextCell, EstimatePosition(sourceZone, nextEntry.prefab, exit.Direction), exit, requiredTargetExitTag);
			}

			if (_activeZones.TryGetValue(nextCell, out var entered))
			{
				_currentCell = nextCell;
				ZoneEntered?.Invoke(new LevelZoneEvent(entered.Zone, entered.ZoneId, nextCell));
				MakeCenter(nextCell);
				PreloadAround(nextCell);
				DespawnOutsideRadius(nextCell);
			}
		}

		private LevelZone SpawnZone(LevelZoneEntry entry, Vector2Int cell, Vector3 position, LevelExit sourceExit, string requiredTargetExitTag)
		{
			if (entry?.prefab == null)
			{
				ReportError("Cannot spawn an empty zone entry.", graph);
				return null;
			}

			var zone = GetFromPool(entry.prefab);
			zone.transform.SetParent(spawnSettings.runtimeParent != null ? spawnSettings.runtimeParent : transform, false);
			zone.transform.position = position;
			zone.transform.rotation = Quaternion.identity;
			zone.gameObject.SetActive(true);

			if (sourceExit != null)
				AlignToExit(zone, sourceExit, requiredTargetExitTag);

			Subscribe(zone);
			var active = new ActiveZone(entry.prefab, zone, cell, entry.ZoneId);
			_activeZones[cell] = active;
			_lastGeneratedZoneId = entry.ZoneId;
			ZoneSpawned?.Invoke(new LevelZoneEvent(zone, entry.ZoneId, cell));
			return zone;
		}

		private LevelZone GetFromPool(LevelZone prefab)
		{
			if (_pool.TryGetValue(prefab, out var stack) && stack.Count > 0)
				return stack.Pop();
			return Instantiate(prefab);
		}

		private void Release(ActiveZone active)
		{
			if (active.Zone == null)
				return;

			Unsubscribe(active.Zone);
			active.Zone.gameObject.SetActive(false);
			if (!_pool.TryGetValue(active.Prefab, out var stack))
			{
				stack = new Stack<LevelZone>();
				_pool[active.Prefab] = stack;
			}

			if (stack.Count < spawnSettings.maxPoolInstancesPerPrefab)
				stack.Push(active.Zone);
			else
				Destroy(active.Zone.gameObject);

			ZoneDespawned?.Invoke(new LevelZoneEvent(active.Zone, active.ZoneId, active.Cell));
		}

		private void Subscribe(LevelZone zone)
		{
			foreach (var exit in zone.Exits)
				if (exit != null)
					exit.Triggered += OnExitTriggered;
		}

		private void Unsubscribe(LevelZone zone)
		{
			foreach (var exit in zone.Exits)
				if (exit != null)
					exit.Triggered -= OnExitTriggered;
		}

		private void MakeCenter(Vector2Int cell)
		{
			if (!_activeZones.TryGetValue(cell, out var active))
				return;

			if (spawnSettings.parentTargetsToCurrentZone)
			{
				foreach (var target in targets)
					if (target != null)
						target.SetParent(active.Zone.transform, true);
			}

			ZoneBecameCenter?.Invoke(new LevelZoneEvent(active.Zone, active.ZoneId, cell));
		}

		private void PreloadAround(Vector2Int centerCell)
		{
			var radius = Mathf.Max(0, spawnSettings.preloadRadius);
			for (var depth = 0; depth <= radius; depth++)
			{
				var cells = new List<Vector2Int>(_activeZones.Keys);
				foreach (var cell in cells)
				{
					var distance = Mathf.Max(Mathf.Abs(cell.x - centerCell.x), Mathf.Abs(cell.y - centerCell.y));
					if (distance <= depth)
						SpawnNeighbours(cell);
				}
			}
		}

		private void SpawnNeighbours(Vector2Int centerCell)
		{
			if (!_activeZones.TryGetValue(centerCell, out var center))
				return;

			foreach (var exit in center.Zone.Exits)
			{
				if (exit == null || exit.Direction is LevelDirection.None or LevelDirection.Center)
					continue;

				var targetCell = centerCell + LevelDirectionUtility.ToCellOffset(exit.Direction);
				if (_activeZones.ContainsKey(targetCell))
					continue;

				if (!_cache.TryResolve(center.Zone, exit, _random, _lastGeneratedZoneId, out var nextEntry, out var requiredTargetExitTag))
					continue;

				SpawnZone(nextEntry, targetCell, EstimatePosition(center.Zone, nextEntry.prefab, exit.Direction), exit, requiredTargetExitTag);
			}
		}

		private void DespawnOutsideRadius(Vector2Int centerCell)
		{
			var toRelease = new List<Vector2Int>();
			foreach (var pair in _activeZones)
			{
				var distance = Mathf.Max(Mathf.Abs(pair.Key.x - centerCell.x), Mathf.Abs(pair.Key.y - centerCell.y));
				if (distance > Mathf.Max(0, spawnSettings.activeRadius))
					toRelease.Add(pair.Key);
			}

			foreach (var cell in toRelease)
			{
				var active = _activeZones[cell];
				_activeZones.Remove(cell);
				Release(active);
			}
		}

		private void AlignToExit(LevelZone zone, LevelExit sourceExit, string requiredTargetExitTag)
		{
			var opposite = LevelDirectionUtility.Opposite(sourceExit.Direction);
			if (!zone.TryGetExit(opposite, requiredTargetExitTag, out var targetExit))
				return;

			var delta = sourceExit.SpawnAnchor.position - targetExit.SpawnAnchor.position;
			zone.transform.position += delta;
		}

		private Vector3 EstimatePosition(LevelZone source, LevelZone target, LevelDirection direction)
		{
			var sourceSize = source.Size + spawnSettings.boundsPadding;
			var targetSize = target.Size + spawnSettings.boundsPadding;
			var offset = direction switch
			{
				LevelDirection.North => new Vector3(0f, (sourceSize.y + targetSize.y) * 0.5f, 0f),
				LevelDirection.East => new Vector3((sourceSize.x + targetSize.x) * 0.5f, 0f, 0f),
				LevelDirection.South => new Vector3(0f, -(sourceSize.y + targetSize.y) * 0.5f, 0f),
				LevelDirection.West => new Vector3(-(sourceSize.x + targetSize.x) * 0.5f, 0f, 0f),
				_ => Vector3.zero
			};
			return source.transform.position + offset;
		}

		private bool IsTargetCollider(Collider2D other)
		{
			if (other == null)
				return false;
			if (targets.Count == 0)
				return true;

			foreach (var target in targets)
			{
				if (target == null)
					continue;
				if (other.transform == target || other.transform.IsChildOf(target) || target.IsChildOf(other.transform))
					return true;
			}

			return false;
		}

		private IReadOnlyDictionary<Vector2Int, LevelZone> BuildActiveZoneSnapshot()
		{
			var snapshot = new Dictionary<Vector2Int, LevelZone>();
			foreach (var pair in _activeZones)
				snapshot[pair.Key] = pair.Value.Zone;
			return snapshot;
		}

		private void ReportError(string message, UnityEngine.Object context)
		{
			Debug.LogError(message, context);
			RuntimeError?.Invoke(new LevelRuntimeMessage(message, context));
		}

		private readonly struct ActiveZone
		{
			public ActiveZone(LevelZone prefab, LevelZone zone, Vector2Int cell, string zoneId)
			{
				Prefab = prefab;
				Zone = zone;
				Cell = cell;
				ZoneId = zoneId;
			}

			public LevelZone Prefab { get; }
			public LevelZone Zone { get; }
			public Vector2Int Cell { get; }
			public string ZoneId { get; }
		}
	}
}
