using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoopShift.Runtime
{
	[DisallowMultipleComponent]
	public class LevelZone : MonoBehaviour
	{
		[SerializeField] private string zoneId;
		[SerializeField] private string displayName;
		[SerializeField] private LevelBoundsMode boundsMode;
		[SerializeField] private Vector2 manualSize = new(20f, 20f);
		[SerializeField] private Vector2 pivotOffset;
		[SerializeField] private Vector2 boundsPadding;
		[SerializeField] private Bounds cachedLocalBounds = new(Vector3.zero, new Vector3(20f, 20f, 1f));
		[SerializeField] private List<LevelExit> exits = new();

		public string ZoneId => string.IsNullOrWhiteSpace(zoneId) ? name : zoneId;
		public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ZoneId : displayName;
		public LevelBoundsMode BoundsMode => boundsMode;
		public IReadOnlyList<LevelExit> Exits => exits ?? (IReadOnlyList<LevelExit>)Array.Empty<LevelExit>();

		public Bounds LocalBounds
		{
			get
			{
				if (boundsMode == LevelBoundsMode.Manual)
				{
					var size = new Vector3(Mathf.Max(0.01f, manualSize.x), Mathf.Max(0.01f, manualSize.y), 1f);
					return new Bounds(pivotOffset, size);
				}

				return cachedLocalBounds;
			}
		}

		public Vector2 Size => LocalBounds.size;
		public Vector2 PivotOffset => LocalBounds.center;

		public void SetZoneId(string value)
		{
			zoneId = value;
		}

		public void RefreshCachedData()
		{
			RefreshExits();
			RecalculateBounds();
		}

		public void RefreshExits()
		{
			exits ??= new List<LevelExit>();
			exits.Clear();
			GetComponentsInChildren(true, exits);
			for (var i = exits.Count - 1; i >= 0; i--)
				if (exits[i].GetComponentInParent<LevelZone>(true) != this) exits.RemoveAt(i);
		}

		public bool TryGetExit(LevelDirection direction, string requiredTag, out LevelExit exit)
		{
			exit = null;
			var candidates = Exits;
			for (var i = 0; i < candidates.Count; i++)
			{
				var candidate = candidates[i];
				if (candidate == null || candidate.Direction != direction)
					continue;
				if (!TagsCompatible(candidate.ExitTag, requiredTag))
					continue;

				exit = candidate;
				return true;
			}

			return false;
		}

		public static bool TagsCompatible(string candidate, string required)
		{
			return string.IsNullOrWhiteSpace(required) ||
			       string.Equals(candidate, required, StringComparison.OrdinalIgnoreCase);
		}

		private void Reset()
		{
			if (string.IsNullOrWhiteSpace(zoneId))
				zoneId = Guid.NewGuid().ToString("N");
			displayName = name;
			RefreshCachedData();
		}

		private void OnValidate()
		{
			if (string.IsNullOrWhiteSpace(zoneId))
				zoneId = Guid.NewGuid().ToString("N");
			if (manualSize.x < 0.01f)
				manualSize.x = 0.01f;
			if (manualSize.y < 0.01f)
				manualSize.y = 0.01f;
		}

		private void RecalculateBounds()
		{
			var found = false;
			var localBounds = new Bounds();
			var worldToLocal = transform.worldToLocalMatrix;
			foreach (var rendererComponent in GetComponentsInChildren<Renderer>(true))
			{
				if (rendererComponent.GetComponentInParent<LevelZone>(true) != this)
					continue;
				EncapsulateTransformedBounds(ref localBounds, ref found, rendererComponent.localBounds,
					worldToLocal * rendererComponent.transform.localToWorldMatrix);
			}

			foreach (var colliderComponent in GetComponentsInChildren<Collider2D>(true))
			{
				if (colliderComponent.GetComponentInParent<LevelZone>(true) != this ||
					colliderComponent.GetComponent<LevelExit>() != null)
					continue;
				// Disabled colliders have empty world bounds; do not include an artificial origin.
				var bounds = colliderComponent.bounds;
				if (bounds.size == Vector3.zero)
					continue;
				EncapsulateTransformedBounds(ref localBounds, ref found, bounds, worldToLocal);
			}

			if (!found)
			{
				cachedLocalBounds = new Bounds(pivotOffset,
					new Vector3(Mathf.Max(0.01f, manualSize.x), Mathf.Max(0.01f, manualSize.y), 1));
				return;
			}

			var size = localBounds.size;
			size.x = Mathf.Max(0.01f, size.x + boundsPadding.x);
			size.y = Mathf.Max(0.01f, size.y + boundsPadding.y);
			size.z = Mathf.Max(1, size.z);
			cachedLocalBounds = new Bounds(localBounds.center, size);
		}

		private static void EncapsulateTransformedBounds(ref Bounds result, ref bool found,
			Bounds source, Matrix4x4 matrix)
		{
			// Transform all corners; dividing world sizes by scale loses rotation and mirror signs.
			for (var i = 0; i < 8; i++)
			{
				var corner = source.center + Vector3.Scale(source.extents,
					new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
				var point = matrix.MultiplyPoint3x4(corner);
				if (found) result.Encapsulate(point);
				else { result = new Bounds(point, Vector3.zero); found = true; }
			}
		}
	}
}