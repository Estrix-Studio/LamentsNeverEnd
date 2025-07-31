using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class CyclingZone : MonoBehaviour
    {
        [SerializeField] private List<TriggerZone> zones;
        [SerializeField] public string zoneName;
    
        public string ZoneName => zoneName;
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
            Debug.Log($"Location {ZoneName} entered form side:  {e}");
            OnZoneEntered?.Invoke(this, e);
        }
    }
}
