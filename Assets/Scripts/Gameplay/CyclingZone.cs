using System;
using System.Collections.Generic;
using Gameplay.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay
{
	public class CyclingZone : MonoBehaviour
	{
		[FormerlySerializedAs("ThisZoneID")]
		[SerializeField] private CycleZoneID thisZoneID;

		[SerializeField] private List<TriggerZone> zones;
		public CycleZoneID ZoneID => thisZoneID;

		private void Awake()
		{
			if (zones == null)
				return;

			foreach (var zone in zones)
			{
				if (zone == null)
				{
					Debug.LogWarning($"{name} has an empty trigger zone reference.", this);
					continue;
				}

				zone.OnZoneEnter += OnZoneEnter;
			}
		}

		private void OnDestroy()
		{
			if (zones == null)
				return;

			foreach (var zone in zones)
				if (zone != null)
					zone.OnZoneEnter -= OnZoneEnter;
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (other.gameObject.CompareTag("Player")) GameData.instance.TriggerOnZoneEntered(ZoneID);
		}

		public event EventHandler<ZoneSide> OnZoneEntered;

		private void OnZoneEnter(object sender, ZoneSide e)
		{
			// Debug.Log($"Location {ThisZoneID} entered form side:  {e}");
			OnZoneEntered?.Invoke(this, e);
		}
	}
}
