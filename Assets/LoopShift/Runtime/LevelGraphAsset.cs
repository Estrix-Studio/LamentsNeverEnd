using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoopShift.Runtime
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
		public IReadOnlyList<LevelZoneEntry> Zones => zones ?? (IReadOnlyList<LevelZoneEntry>)Array.Empty<LevelZoneEntry>();
		public IReadOnlyList<LevelConnection> FixedConnections => fixedConnections ?? (IReadOnlyList<LevelConnection>)Array.Empty<LevelConnection>();
		public IReadOnlyList<LevelZonePool> Pools => pools ?? (IReadOnlyList<LevelZonePool>)Array.Empty<LevelZonePool>();
		public IReadOnlyList<LevelPoolRule> PoolRules => poolRules ?? (IReadOnlyList<LevelPoolRule>)Array.Empty<LevelPoolRule>();

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
		[HideInInspector] public Vector2 editorPosition;
		[HideInInspector] public bool hasEditorPosition;
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
		[HideInInspector] public Vector2 editorPosition;
		[HideInInspector] public bool hasEditorPosition;
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
