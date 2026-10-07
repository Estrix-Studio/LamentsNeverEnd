using System.Collections.Generic;
using UnityEngine;

namespace LoopShift.Runtime
{
	/// <summary>Owns all instantiated rooms, including rooms parented outside the runner.</summary>
	internal sealed class LevelInstancePool
	{
		private readonly Transform _owner;
		private readonly Dictionary<LevelZone, Stack<LevelZone>> _pools = new();
		private readonly Dictionary<LevelZone, ILevelZoneLifecycle[]> _handlers = new();
		private readonly HashSet<LevelZone> _owned = new();
		private Transform _inactiveRoot;
        private Transform _activeRoot;

		public int PooledCount { get; private set; }
		public int OwnedCount => _owned.Count;

		public LevelInstancePool(Transform owner) => _owner = owner;

		public LevelZone Acquire(LevelZone prefab)
		{
			if (_pools.TryGetValue(prefab, out var stack))
			{
				while (stack.Count > 0)
				{
					var cached = stack.Pop();
					PooledCount--;
					if (cached != null)
						return cached;
					_owned.Remove(cached);
					_handlers.Remove(cached);
				}
			}

			var zone = Object.Instantiate(prefab, InactiveRoot);
			zone.gameObject.SetActive(false);
			_owned.Add(zone);
			var hooks = new List<ILevelZoneLifecycle>();
			foreach (var component in zone.GetComponentsInChildren<MonoBehaviour>(true))
				if (component is ILevelZoneLifecycle hook)
					hooks.Add(hook);
			_handlers.Add(zone, hooks.ToArray());
			return zone;
		}

		public ILevelZoneLifecycle[] Handlers(LevelZone zone) => _handlers[zone];

		public void Release(LevelZone prefab, LevelZone zone, int perPrefabLimit, int totalLimit)
		{
			if (zone == null)
				return;
			zone.gameObject.SetActive(false);
			if (!_pools.TryGetValue(prefab, out var stack))
			{
				stack = new Stack<LevelZone>();
				_pools.Add(prefab, stack);
			}
			// Scene unload may destroy the pool root before the runner. Release must never recreate it.
            if (_inactiveRoot == null || stack.Count >= Mathf.Max(0, perPrefabLimit) || PooledCount >= Mathf.Max(0, totalLimit))
			{
				_owned.Remove(zone);
				_handlers.Remove(zone);
				DestroyObject(zone.gameObject);
				return;
			}
			zone.transform.SetParent(_inactiveRoot, false);
			stack.Push(zone);
			PooledCount++;
		}

		public void Dispose()
		{
			foreach (var zone in _owned)
				if (zone != null)
					DestroyObject(zone.gameObject);
			_owned.Clear();
			_handlers.Clear();
			_pools.Clear();
			PooledCount = 0;
			if (_inactiveRoot != null)
				DestroyObject(_inactiveRoot.gameObject);
			_inactiveRoot = null;
            if (_activeRoot != null) DestroyObject(_activeRoot.gameObject);
            _activeRoot = null;
		}

		private Transform InactiveRoot
		{
			get
			{
				if (_inactiveRoot != null)
					return _inactiveRoot;
				_inactiveRoot = CreateRoot("LoopShift Pool", false);
                return _inactiveRoot;
			}
		}


        private Transform CreateRoot(string name, bool active)
        {
            var root = new GameObject(name);
            root.SetActive(active);
            var scene = _owner.gameObject.scene;
            if (scene.name == "DontDestroyOnLoad")
                Object.DontDestroyOnLoad(root);
            else if (scene.IsValid() && scene.isLoaded)
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            return root.transform;
        }

        public Transform ActiveRoot => _activeRoot != null ? _activeRoot : _activeRoot = CreateRoot("LoopShift Rooms", true);

        private static void DestroyObject(GameObject target)
		{
			if (Application.isPlaying)
				Object.Destroy(target);
			else
				Object.DestroyImmediate(target);
		}
	}
}