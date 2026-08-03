using System;
using UnityEngine;

namespace LoopShift.Runtime
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Collider2D))]
	public class LevelExit : MonoBehaviour
	{
		[SerializeField] private LevelDirection direction = LevelDirection.North;
		[SerializeField] private string exitTag = "Default";
		[SerializeField] private Transform spawnAnchor;
		[SerializeField] private string fixedTargetZoneId;

		public LevelDirection Direction => direction;
		public string ExitTag => string.IsNullOrWhiteSpace(exitTag) ? "Default" : exitTag;
		public Transform SpawnAnchor => spawnAnchor != null ? spawnAnchor : transform;
		public string FixedTargetZoneId => fixedTargetZoneId;

		public event Action<LevelExit, Collider2D> Triggered;

		private void Reset()
		{
			var trigger = GetComponent<Collider2D>();
			if (trigger != null)
				trigger.isTrigger = true;
			spawnAnchor = transform;
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			Triggered?.Invoke(this, other);
		}
	}
}
