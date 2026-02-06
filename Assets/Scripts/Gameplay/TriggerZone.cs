using System;
using UnityEngine;

namespace Gameplay
{
	public class TriggerZone : MonoBehaviour
	{
		[SerializeField] private ZoneSide direction;

		public EventHandler<ZoneSide> OnZoneEnter;

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (!other.TryGetComponent<Player>(out var player)) return;
			OnZoneEnter?.Invoke(this, direction);
		}
	}
}