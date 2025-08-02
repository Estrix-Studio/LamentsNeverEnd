using System;
using UnityEngine;

namespace Utility
{
    [Serializable]
    public struct IntensityData
    {
        [Range(0.5f, 1f)]
        public float minIntensity;
        
        [Range(1f, 1.5f)]
        public float maxIntensity;

        public float flickTime;
        public float flickBackTime;

        public Vector2 flickCooldown;
    }
}