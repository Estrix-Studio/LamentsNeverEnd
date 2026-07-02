using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessSeamlessLevels
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
		public IReadOnlyList<LevelExit> Exits => exits;

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
			exits.Clear();
			GetComponentsInChildren(true, exits);
		}

		public bool TryGetExit(LevelDirection direction, string requiredTag, out LevelExit exit)
		{
			exit = null;
			for (var i = 0; i < exits.Count; i++)
			{
				var candidate = exits[i];
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
			var worldBounds = new Bounds(transform.position, Vector3.zero);

			var renderers = GetComponentsInChildren<Renderer>(true);
			foreach (var rendererComponent in renderers)
			{
				if (rendererComponent == null)
					continue;
				if (!found)
				{
					worldBounds = rendererComponent.bounds;
					found = true;
				}
				else
				{
					worldBounds.Encapsulate(rendererComponent.bounds);
				}
			}

			var colliders = GetComponentsInChildren<Collider2D>(true);
			foreach (var colliderComponent in colliders)
			{
				if (colliderComponent == null)
					continue;
				if (colliderComponent.GetComponent<LevelExit>() != null)
					continue;
				if (!found)
				{
					worldBounds = colliderComponent.bounds;
					found = true;
				}
				else
				{
					worldBounds.Encapsulate(colliderComponent.bounds);
				}
			}

			if (!found)
			{
				cachedLocalBounds = new Bounds(Vector3.zero, new Vector3(manualSize.x, manualSize.y, 1f));
				return;
			}

			var localCenter = transform.InverseTransformPoint(worldBounds.center);
			var localSize = new Vector3(
				Mathf.Abs(worldBounds.size.x / Mathf.Max(0.0001f, transform.lossyScale.x)) + boundsPadding.x,
				Mathf.Abs(worldBounds.size.y / Mathf.Max(0.0001f, transform.lossyScale.y)) + boundsPadding.y,
				Mathf.Max(1f, worldBounds.size.z));
			cachedLocalBounds = new Bounds(localCenter, localSize);
		}
	}
}
