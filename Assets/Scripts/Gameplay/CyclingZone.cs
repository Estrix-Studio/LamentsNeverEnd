using System;
using System.Collections.Generic;
using Gameplay.Data;
using UnityEngine;

namespace Gameplay
{
    public class CyclingZone : MonoBehaviour
    {
        public CycleZoneID ZoneID => ThisZoneID;
        
        [SerializeField] private CycleZoneID ThisZoneID;
        
        [SerializeField] private List<TriggerZone> zones;
    
        public event EventHandler<ZoneSide> OnZoneEntered;
        private void Awake()
        {
            foreach (var zone in  zones)
            {
                zone.OnZoneEnter += OnZoneEnter;
            }
        }

        private void OnZoneEnter(object sender, ZoneSide e)
        {
            // Debug.Log($"Location {ThisZoneID} entered form side:  {e}");
            OnZoneEntered?.Invoke(this, e);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag(("Player")))
            {
                GameData.Instance.TriggerOnZoneEntered(ZoneID);
            } 
        }
    }
}
