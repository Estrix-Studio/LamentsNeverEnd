using System;
using UnityEngine;

namespace LoopShift.Runtime
{
	[Serializable]
	public class LevelSpawnSettings
	{
		[Min(0)] public int preloadRadius = 1;
		[Min(0)] public int activeRadius = 1;
		[Min(0)] public int maxPoolInstancesPerPrefab = 4;
		[Min(0)] public int maxPooledInstances = 64;
		public bool parentTargetsToCurrentZone;
        [Tooltip("Keep the current room at the runner origin and shift registered actors with it.")]
        public bool recenterOnTransition;
		public bool useDeterministicSeed = true;
		public int seed = 12345;
		public Vector2 boundsPadding;
		public Transform runtimeParent;
	}
}
