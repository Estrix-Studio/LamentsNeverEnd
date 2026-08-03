using UnityEngine;

namespace LoopShift.Runtime
{
	public readonly struct LevelZoneEvent
	{
		public LevelZoneEvent(LevelZone zone, string zoneId, Vector2Int cell)
		{
			Zone = zone;
			ZoneId = zoneId;
			Cell = cell;
		}

		public LevelZone Zone { get; }
		public string ZoneId { get; }
		public Vector2Int Cell { get; }
	}

	public readonly struct LevelRuntimeMessage
	{
		public LevelRuntimeMessage(string message, Object context)
		{
			Message = message;
			Context = context;
		}

		public string Message { get; }
		public Object Context { get; }
	}
}
