using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessSeamlessLevels
{
	[CreateAssetMenu(fileName = "LevelGraph", menuName = "Endless Seamless Levels/Level Graph")]
	public class LevelGraphAsset : ScriptableObject
	{
		[SerializeField] private string startZoneId;
		[SerializeField] private List<LevelZoneEntry> zones = new();
		[SerializeField] private List<LevelConnection> fixedConnections = new();
		[SerializeField] private List<LevelZonePool> pools = new();
		[SerializeField] private List<LevelPoolRule> poolRules = new();

		public string StartZoneId => startZoneId;
		public IReadOnlyList<LevelZoneEntry> Zones => zones;
		public IReadOnlyList<LevelConnection> FixedConnections => fixedConnections;
		public IReadOnlyList<LevelZonePool> Pools => pools;
		public IReadOnlyList<LevelPoolRule> PoolRules => poolRules;

		public void SetStartZone(string zoneId)
		{
			startZoneId = zoneId;
		}

		public IReadOnlyList<LevelValidationMessage> ValidateGraph()
		{
			var validator = new LevelGraphValidator(this);
			return validator.Validate();
		}
	}

	[Serializable]
	public class LevelZoneEntry
	{
		public LevelZone prefab;
		public string zoneIdOverride;
		[Min(1)] public int weight = 1;
		public List<string> tags = new();

		public string ZoneId
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(zoneIdOverride))
					return zoneIdOverride;
				return prefab != null ? prefab.ZoneId : string.Empty;
			}
		}
	}

	[Serializable]
	public class LevelConnection
	{
		public string fromZoneId;
		public LevelDirection direction = LevelDirection.North;
		public string fromExitTag;
		public string toZoneId;
		public string toExitTag;
		public bool bidirectional = true;
	}

	[Serializable]
	public class LevelZonePool
	{
		public string poolId = "Default";
		public bool allowImmediateRepeat = true;
		public List<LevelZoneEntry> entries = new();
	}

	[Serializable]
	public class LevelPoolRule
	{
		public LevelDirection direction = LevelDirection.North;
		public string fromExitTag;
		public string poolId = "Default";
		public string requiredTargetExitTag;
	}
}
