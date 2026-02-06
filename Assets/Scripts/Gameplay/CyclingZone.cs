using System;
using System.Collections.Generic;
using Gameplay.Data;
using UnityEngine;

namespace Gameplay
{
	public class CyclingZone : MonoBehaviour
	{
		[SerializeField] private CycleZoneID thisZoneID;

		[SerializeField] private List<TriggerZone> zones;
		public CycleZoneID zoneID => thisZoneID;

		private void Awake()
		{
			foreach (var zone in zones) zone.OnZoneEnter += OnZoneEnter;
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (other.gameObject.CompareTag("Player")) GameData.instance.TriggerOnZoneEntered(zoneID);
		}

		public event EventHandler<ZoneSide> OnZoneEntered;

		private void OnZoneEnter(object sender, ZoneSide e)
		{
			// Debug.Log($"Location {ThisZoneID} entered form side:  {e}");
			OnZoneEntered?.Invoke(this, e);
		}
	}
}