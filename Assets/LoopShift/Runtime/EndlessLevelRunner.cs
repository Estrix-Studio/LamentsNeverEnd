using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		private readonly Dictionary<Vector2Int, LevelZone> _publicZones = new();
		private readonly Dictionary<LevelExit, Vector2Int> _exitOwners = new();
		private readonly Dictionary<LevelExit, ResolvedRoute> _routes = new();
		private readonly List<Vector2Int> _preloadQueue = new();
		private readonly HashSet<Vector2Int> _queuedCells = new();
		private readonly List<Vector2Int> _releaseCells = new();
		private readonly List<ActiveZone> _stopBuffer = new();
		private readonly List<Collider2D> _arrivalColliders = new();
		private ReadOnlyDictionary<Vector2Int, LevelZone> _activeZoneView;
		private LevelGraphRuntimeCache _cache;
		private LevelInstancePool _pool;
		private LevelTargetRegistry _targetRegistry;
		private System.Random _random;
		private Vector2Int _currentCell;
		private LevelExit _arrivalExit;
		private Collider2D _arrivalTrigger;
		private bool _isRunning;
		private bool _isMutating;
		private bool _isDestroying;
		private Command _pendingCommand;
		private int _lastTransitionFrame = -1;

		private enum Command { None, Start, Stop }

		public event Action<LevelZoneEvent> ZoneSpawned;
		public event Action<LevelZoneEvent> ZoneEntered;
		public event Action<LevelZoneEvent> ZoneBecameCenter;
		public event Action<LevelZoneEvent> ZoneDespawned;
		public event Action<LevelRuntimeMessage> RuntimeError;
        /// <summary>World-space offset applied to active rooms and registered targets during recentering.</summary>
        public event Action<Vector3> WorldShifted;

		public LevelGraphAsset Graph => graph;
		public LevelSpawnSettings SpawnSettings => spawnSettings ??= new LevelSpawnSettings();
		public bool IsRunning => _isRunning;
		public Vector2Int CurrentCell => _currentCell;
		public int PooledZoneCount => _pool?.PooledCount ?? 0;
		public int OwnedZoneCount => _pool?.OwnedCount ?? 0;

		/// <summary>Live read-only view; copy explicitly if a historical snapshot is needed.</summary>
		public IReadOnlyDictionary<Vector2Int, LevelZone> ActiveZones =>
			_activeZoneView ??= new ReadOnlyDictionary<Vector2Int, LevelZone>(_publicZones);

		private void OnEnable()
		{
			if (Application.isPlaying && playOnAwake)
				StartRunner();
		}

		private void OnDisable() => StopRunner();

		private void OnDestroy()
		{
			_isDestroying = true;
			StopRunner();
		}

		private void LateUpdate()
		{
			// Defer callback-requested restarts rather than recursing into user callbacks.
			if (_pendingCommand != Command.Start || _isMutating || _isDestroying)
				return;
			_pendingCommand = Command.None;
			StartRunner();
		}

		private void FixedUpdate()
		{
			if (_arrivalExit == null || _arrivalTrigger == null)
				return;
			for (var i = 0; i < _arrivalColliders.Count; i++)
			{
				var collider = _arrivalColliders[i];
				if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy &&
				    _arrivalTrigger.Distance(collider).isOverlapped)
					return;
			}
			ClearArrivalGate();
		}

		/// <summary>Starts a fresh seeded run. Calls made from callbacks are deferred.</summary>
		public void StartRunner()
		{
			if (_isDestroying || !isActiveAndEnabled)
				return;
			if (_isMutating)
			{
				_pendingCommand = Command.Start;
				_isRunning = false;
				return;
			}
			_isMutating = true;
			_pendingCommand = Command.None;
			try
			{
				EnsureRuntime();
				StopCore();
				if (_pendingCommand != Command.None || _isDestroying)
					return;
				if (graph == null)
				{
					ReportError("Cannot start endless level runner: graph is not assigned.", this);
					return;
				}
				foreach (var message in graph.ValidateGraph())
				{
					if (message.Severity != LevelValidationSeverity.Error)
						continue;
					ReportError(message.Message, message.Context);
					return;
				}

				_cache = new LevelGraphRuntimeCache(graph);
				_random = new System.Random(SpawnSettings.useDeterministicSeed ? SpawnSettings.seed : Environment.TickCount);
				_currentCell = Vector2Int.zero;
				_lastTransitionFrame = -1;
				if (!_cache.TryGetZone(graph.StartZoneId, out var entry))
					return;
				_isRunning = true;
				if (SpawnZone(entry, _currentCell, transform.position, null, null) == null || !CanContinue)
					return;
				MakeCenter();
				if (CanContinue)
					PreloadAround(_currentCell);
			}
			catch (Exception exception) { Abort(exception); }
			finally { EndMutation(); }
		}

		public void StopRunner()
		{
			_isRunning = false;
			if (_isMutating)
			{
				_pendingCommand = Command.Stop;
				return;
			}
			_isMutating = true;
			_pendingCommand = Command.None;
			try { StopCore(); }
			finally { EndMutation(); }
		}

		public void AddTarget(Transform target)
		{
			if (target == null || transform.IsChildOf(target))
				return;
			EnsureRuntime();
			if (!targets.Contains(target))
				targets.Add(target);
		}

		public void RemoveTarget(Transform target)
		{
			EnsureRuntime();
			_targetRegistry.Remove(target);
		}

		/// <summary>Attempts a registered actor's exit crossing. At most one crossing is accepted per frame.</summary>
		public bool TryTraverse(LevelExit exit, Collider2D actor)
		{
			if (!CanContinue || _isMutating || exit == null || exit == _arrivalExit ||
			    _lastTransitionFrame == Time.frameCount || !LevelDirectionUtility.IsCardinal(exit.Direction) ||
			    !_targetRegistry.TryGetActor(actor, out var actorRoot) ||
			    !_exitOwners.TryGetValue(exit, out var sourceCell) || sourceCell != _currentCell)
				return false;

			_isMutating = true;
			try
			{
				var current = _activeZones[_currentCell];
				if (!TryRoute(current, exit, out var route))
					return false;
				var nextCell = _currentCell + LevelDirectionUtility.ToCellOffset(exit.Direction);
				if (!EnsureNeighbour(current, exit, route, nextCell, true) || !CanContinue)
					return false;
				var entered = _activeZones[nextCell];
				_currentCell = nextCell;
				_lastTransitionFrame = Time.frameCount;
				entered.Zone.TryGetExit(LevelDirectionUtility.Opposite(exit.Direction), route.TargetTag, out _arrivalExit);
				_arrivalTrigger = _arrivalExit != null ? _arrivalExit.GetComponent<Collider2D>() : null;
				_arrivalColliders.Clear();
				actorRoot.GetComponentsInChildren(false, _arrivalColliders);
				MakeCenter();
				if (!CanContinue)
					return false;
				Publish(ZoneEntered, entered);
				if (!CanContinue)
					return false;
				DespawnOutsideRadius(nextCell);
				if (CanContinue)
					PreloadAround(nextCell);
				return CanContinue;
			}
			catch (Exception exception)
			{
				Abort(exception);
				return false;
			}
			finally { EndMutation(); }
		}

		private bool CanContinue => _isRunning && !_isDestroying && _pendingCommand == Command.None;

		private void EnsureRuntime()
		{
			targets ??= new List<Transform>();
			_targetRegistry ??= new LevelTargetRegistry(targets);
			_pool ??= new LevelInstancePool(transform);
		}

		private void EndMutation()
		{
			// Stop requests take effect before returning to the caller; restart waits for LateUpdate.
			if (_pendingCommand == Command.Stop || _isDestroying)
			{
				_pendingCommand = Command.None;
				StopCore();
			}
			if (_isDestroying)
			{
				_pool?.Dispose();
				_pendingCommand = Command.None;
			}
			_isMutating = false;
		}

		private void StopCore()
		{
			_isRunning = false;
			ClearArrivalGate();
			_targetRegistry?.RestoreAll();
			_stopBuffer.Clear();
			foreach (var active in _activeZones.Values)
				_stopBuffer.Add(active);
			_activeZones.Clear();
			_publicZones.Clear();
			foreach (var active in _stopBuffer)
				Release(active);
			_stopBuffer.Clear();
			_exitOwners.Clear();
			_routes.Clear();
		}

		private void ClearArrivalGate()
		{
			_arrivalExit = null;
			_arrivalTrigger = null;
			_arrivalColliders.Clear();
		}

		private void OnExitTriggered(LevelExit exit, Collider2D actor) => TryTraverse(exit, actor);

		private LevelZone SpawnZone(LevelZoneEntry entry, Vector2Int cell, Vector3 position,
			LevelExit sourceExit, string targetTag)
		{
			if (!CanContinue || entry?.prefab == null)
				return null;
			var zone = _pool.Acquire(entry.prefab);
			zone.transform.SetParent(SpawnSettings.runtimeParent != null ? SpawnSettings.runtimeParent : _pool.ActiveRoot, false);
			zone.transform.SetPositionAndRotation(position, Quaternion.identity);
			zone.transform.localScale = entry.prefab.transform.localScale;
			if (sourceExit != null)
				AlignToExit(zone, sourceExit, targetTag);

			var active = new ActiveZone(entry.prefab, zone, cell, entry.ZoneId);
			_activeZones.Add(cell, active);
			_publicZones.Add(cell, zone);
			var exits = zone.Exits;
			for (var i = 0; i < exits.Count; i++)
			{
				var exit = exits[i];
				if (exit == null)
					continue;
				_exitOwners[exit] = cell;
				exit.Triggered += OnExitTriggered;
			}
			// Awake/OnEnable see the correct placement and an already registered instance.
			zone.gameObject.SetActive(true);
			if (!CanContinue)
				return zone;
			NotifyLifecycle(active, true);
			if (CanContinue)
				Publish(ZoneSpawned, active);
			return zone;
		}

		private void Release(ActiveZone active)
		{
			if (active.Zone == null)
				return;
			var exits = active.Zone.Exits;
			for (var i = 0; i < exits.Count; i++)
			{
				var exit = exits[i];
				if (exit == null)
					continue;
				exit.Triggered -= OnExitTriggered;
				_exitOwners.Remove(exit);
				_routes.Remove(exit);
			}
			NotifyLifecycle(active, false);
			_pool.Release(active.Prefab, active.Zone, SpawnSettings.maxPoolInstancesPerPrefab, SpawnSettings.maxPooledInstances);
			Publish(ZoneDespawned, active);
		}

		private void DespawnCell(Vector2Int cell)
		{
			if (!_activeZones.TryGetValue(cell, out var active))
				return;
			_activeZones.Remove(cell);
			_publicZones.Remove(cell);
			Release(active);
		}

		private void MakeCenter()
		{
			if (!CanContinue || !_activeZones.TryGetValue(_currentCell, out var active))
				return;
			if (SpawnSettings.parentTargetsToCurrentZone)
				_targetRegistry.ParentTo(active.Zone.transform);
			RecenterWorld(active);
            if (CanContinue) Publish(ZoneBecameCenter, active);
		}


        private void RecenterWorld(ActiveZone center)
        {
            if (!SpawnSettings.recenterOnTransition)
                return;
            var offset = transform.position - center.Zone.transform.position;
            if (offset.sqrMagnitude < 0.00000001f)
                return;
            // Shift actors outside rooms first; nested targets and parented actors move once.
            _targetRegistry.ShiftUnparentedTargets(offset, _publicZones);
            foreach (var room in _publicZones.Values)
                if (room != null)
                    room.transform.position += offset;
            Physics2D.SyncTransforms();
            WorldShifted?.Invoke(offset);
        }

        private bool TryRoute(ActiveZone source, LevelExit exit, out ResolvedRoute route)
		{
			if (_routes.TryGetValue(exit, out route))
				return true;
			if (!_cache.TryResolve(source.ZoneId, exit, _random, source.ZoneId, out var entry, out var tag))
				return false;
			route = new ResolvedRoute(entry, tag);
			_routes.Add(exit, route);
			return true;
		}

		private bool EnsureNeighbour(ActiveZone source, LevelExit exit, ResolvedRoute route, Vector2Int cell, bool replaceConflict)
		{
			if (_activeZones.TryGetValue(cell, out var existing))
			{
				if (existing.Zone != null && existing.ZoneId == route.Entry.ZoneId &&
				    IsAligned(existing.Zone, exit, route.TargetTag))
					return true;
				// Speculative paths cannot evict the current room or its immediate neighborhood.
				if (!replaceConflict || cell == _currentCell)
					return false;
				DespawnCell(cell);
				if (!CanContinue)
					return false;
			}
			return SpawnZone(route.Entry, cell, source.Zone.transform.position, exit, route.TargetTag) != null;
		}

		private void PreloadAround(Vector2Int center)
		{
			var radius = Mathf.Min(Mathf.Max(0, SpawnSettings.preloadRadius), Mathf.Max(0, SpawnSettings.activeRadius));
			if (radius == 0)
				return;
			_preloadQueue.Clear();
			_queuedCells.Clear();
			_preloadQueue.Add(center);
			_queuedCells.Add(center);
			for (var i = 0; i < _preloadQueue.Count && CanContinue; i++)
				SpawnNeighbours(_preloadQueue[i], center, radius);
		}

		private void SpawnNeighbours(Vector2Int cell, Vector2Int centerCell, int radius)
		{
			if (!_activeZones.TryGetValue(cell, out var source) || source.Zone == null)
				return;
			var exits = source.Zone.Exits;
			for (var i = 0; i < exits.Count; i++)
			{
				var exit = exits[i];
				if (!CanContinue)
					return;
				if (exit == null || !LevelDirectionUtility.IsCardinal(exit.Direction))
					continue;
				var next = cell + LevelDirectionUtility.ToCellOffset(exit.Direction);
				if (!IsWithinRadius(next, centerCell, radius) || !TryRoute(source, exit, out var route) ||
				    !EnsureNeighbour(source, exit, route, next, cell == centerCell))
					continue;
				if (_queuedCells.Add(next))
					_preloadQueue.Add(next);
			}
		}

		private void DespawnOutsideRadius(Vector2Int center)
		{
			_releaseCells.Clear();
			foreach (var pair in _activeZones)
				if (!IsWithinRadius(pair.Key, center, Mathf.Max(0, SpawnSettings.activeRadius)))
					_releaseCells.Add(pair.Key);
			for (var i = 0; i < _releaseCells.Count && CanContinue; i++)
				DespawnCell(_releaseCells[i]);
		}

		private static bool IsWithinRadius(Vector2Int cell, Vector2Int center, int radius) =>
			Math.Abs((long)cell.x - center.x) <= radius && Math.Abs((long)cell.y - center.y) <= radius;

		private static bool IsAligned(LevelZone zone, LevelExit source, string targetTag) =>
			zone.TryGetExit(LevelDirectionUtility.Opposite(source.Direction), targetTag, out var target) &&
			(source.SpawnAnchor.position - target.SpawnAnchor.position).sqrMagnitude < 0.000001f;

		private static void AlignToExit(LevelZone zone, LevelExit source, string targetTag)
		{
			if (!zone.TryGetExit(LevelDirectionUtility.Opposite(source.Direction), targetTag, out var target))
				throw new InvalidOperationException("Validated target room no longer has its required exit.");
			zone.transform.position += source.SpawnAnchor.position - target.SpawnAnchor.position;
		}

		private void NotifyLifecycle(ActiveZone active, bool spawning)
		{
			var context = new LevelZoneEvent(active.Zone, active.ZoneId, active.Cell);
			foreach (var hook in _pool.Handlers(active.Zone))
			{
				if (hook is UnityEngine.Object component && component == null)
					continue;
				try
				{
					if (spawning) hook.OnZoneSpawned(context);
					else hook.OnZoneDespawned(context);
				}
				catch (Exception exception) { Abort(exception); }
				if (spawning && !CanContinue)
					return;
			}
		}

		private void Publish(Action<LevelZoneEvent> callback, ActiveZone active)
		{
			try { callback?.Invoke(new LevelZoneEvent(active.Zone, active.ZoneId, active.Cell)); }
			catch (Exception exception) { Abort(exception); }
		}

		private void Abort(Exception exception)
		{
			_isRunning = false;
			_pendingCommand = Command.Stop;
			Debug.LogException(exception, this);
		}

		private void ReportError(string message, UnityEngine.Object context)
		{
			Debug.LogError(message, context);
			try { RuntimeError?.Invoke(new LevelRuntimeMessage(message, context)); }
			catch (Exception exception) { Abort(exception); }
		}

		private readonly struct ResolvedRoute
		{
			public readonly LevelZoneEntry Entry;
			public readonly string TargetTag;
			public ResolvedRoute(LevelZoneEntry entry, string targetTag) { Entry = entry; TargetTag = targetTag; }
		}

		private readonly struct ActiveZone
		{
			public readonly LevelZone Prefab;
			public readonly LevelZone Zone;
			public readonly Vector2Int Cell;
			public readonly string ZoneId;
			public ActiveZone(LevelZone prefab, LevelZone zone, Vector2Int cell, string zoneId)
			{
				Prefab = prefab; Zone = zone; Cell = cell; ZoneId = zoneId;
			}
		}
	}
}