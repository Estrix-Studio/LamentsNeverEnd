using System.Collections.Generic;
using UnityEngine;

namespace LoopShift.Runtime
{
	/// <summary>Preserves target hierarchies while temporarily parenting root targets to rooms.</summary>
	internal sealed class LevelTargetRegistry
	{
		private readonly List<Transform> _targets;
		private readonly Dictionary<Transform, Transform> _originalParents = new();
		private Transform _assignedParent;

		public LevelTargetRegistry(List<Transform> targets) => _targets = targets;

		public bool TryGetActor(Collider2D collider, out Transform actor)
		{
			actor = null;
			if (collider == null)
				return false;
			for (var i = 0; i < _targets.Count; i++)
			{
				var target = _targets[i];
				if (target == null || !collider.transform.IsChildOf(target))
					continue;
				actor = target;
				return true;
			}
			return false;
		}

		public void ParentTo(Transform room)
		{
			// Capture before any reparenting so a registered camera under a player stays nested.
			for (var i = 0; i < _targets.Count; i++)
			{
				var target = _targets[i];
				if (target == null || IsNestedTarget(target) || _originalParents.ContainsKey(target))
					continue;
				_originalParents.Add(target, target.parent);
			}
			foreach (var pair in _originalParents)
				if (pair.Key != null)
					pair.Key.SetParent(room, true);
			_assignedParent = room;
		}

		public void RestoreAll()
		{
			foreach (var pair in _originalParents)
				Restore(pair.Key, pair.Value);
			_originalParents.Clear();
			_assignedParent = null;
		}

		public void Remove(Transform target)
		{
			if (target == null)
				return;
			if (_originalParents.TryGetValue(target, out var parent))
			{
				Restore(target, parent);
				_originalParents.Remove(target);
			}
			_targets.Remove(target);
		}

		private void Restore(Transform target, Transform parent)
		{
			// Respect deliberate reparenting performed by the user's own gameplay code.
			if (target != null && target.parent == _assignedParent)
				target.SetParent(parent, true);
		}


        public void ShiftUnparentedTargets(Vector3 offset, Dictionary<Vector2Int, LevelZone> rooms)
        {
            for (var i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];
                if (target == null || IsNestedTarget(target) || IsInsideRoom(target, rooms))
                    continue;
                target.position += offset;
                if (target.TryGetComponent<Rigidbody2D>(out var body))
                    body.position = target.position;
            }
        }

        private static bool IsInsideRoom(Transform target, Dictionary<Vector2Int, LevelZone> rooms)
        {
            foreach (var room in rooms.Values)
                if (room != null && target.IsChildOf(room.transform))
                    return true;
            return false;
        }

        private bool IsNestedTarget(Transform target)
		{
			for (var i = 0; i < _targets.Count; i++)
				if (_targets[i] != null && _targets[i] != target && target.IsChildOf(_targets[i]))
					return true;
			return false;
		}
	}
}