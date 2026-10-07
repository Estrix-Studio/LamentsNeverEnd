namespace LoopShift.Runtime
{
	/// <summary>Optional component hooks for resetting gameplay state when a room is reused.</summary>
	public interface ILevelZoneLifecycle
	{
		/// <summary>Called after activation at the final position, before ZoneSpawned.</summary>
		void OnZoneSpawned(LevelZoneEvent context);

		/// <summary>Called before deactivation, after the room leaves the active registry.</summary>
		void OnZoneDespawned(LevelZoneEvent context);
	}
}