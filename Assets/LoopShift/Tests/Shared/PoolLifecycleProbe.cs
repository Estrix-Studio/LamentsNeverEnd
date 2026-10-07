using LoopShift.Runtime;
using UnityEngine;

namespace LoopShift.Tests
{
	public sealed class PoolLifecycleProbe : MonoBehaviour, ILevelZoneLifecycle
	{
		public int SpawnCount;
		public int DespawnCount;
		public int State;
		public bool AwakeCalled;
		public bool SpawnSawAwake;
		public Vector3 EnablePosition;

		private void Awake() => AwakeCalled = true;
		private void OnEnable() => EnablePosition = transform.position;

		public void OnZoneSpawned(LevelZoneEvent context)
		{
			SpawnCount++;
			SpawnSawAwake = AwakeCalled;
		}

		public void OnZoneDespawned(LevelZoneEvent context)
		{
			DespawnCount++;
			State = 0;
		}
	}
}