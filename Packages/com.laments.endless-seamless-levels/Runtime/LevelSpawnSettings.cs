using System;
using UnityEngine;

namespace EndlessSeamlessLevels
{
	[Serializable]
	public class LevelSpawnSettings
	{
		[Min(0)] public int preloadRadius = 1;
		[Min(0)] public int activeRadius = 1;
		[Min(0)] public int maxPoolInstancesPerPrefab = 4;
		public bool parentTargetsToCurrentZone;
		public bool useDeterministicSeed = true;
		public int seed = 12345;
		public Vector2 boundsPadding;
		public Transform runtimeParent;
	}
}
