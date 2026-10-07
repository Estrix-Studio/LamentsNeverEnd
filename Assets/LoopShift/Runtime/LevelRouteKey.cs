using System;

namespace LoopShift.Runtime
{
	/// <summary>Allocation-free route identity; zone IDs are ordinal and exit tags ignore case.</summary>
	internal readonly struct LevelRouteKey : IEquatable<LevelRouteKey>
	{
		private readonly string _zoneId;
		private readonly LevelDirection _direction;
		private readonly string _tag;

		public LevelRouteKey(string zoneId, LevelDirection direction, string tag)
		{
			_zoneId = zoneId ?? string.Empty;
			_direction = direction;
			_tag = string.IsNullOrWhiteSpace(tag) ? string.Empty : tag;
		}

		public bool Equals(LevelRouteKey other)
		{
			return _direction == other._direction &&
			       string.Equals(_zoneId, other._zoneId, StringComparison.Ordinal) &&
			       string.Equals(_tag, other._tag, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj) => obj is LevelRouteKey other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = StringComparer.Ordinal.GetHashCode(_zoneId);
				hash = hash * 397 ^ (int)_direction;
				return hash * 397 ^ StringComparer.OrdinalIgnoreCase.GetHashCode(_tag);
			}
		}
	}
}