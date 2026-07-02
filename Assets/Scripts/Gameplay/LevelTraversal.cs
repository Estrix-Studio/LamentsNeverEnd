using System;
using System.Collections.Generic;
using Gameplay.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay
{
	public class LevelTraversal : MonoBehaviour
	{
		private static readonly Dictionary<ZoneSide, Vector3> DefaultOffsets = new()
		{
			{ ZoneSide.Center, Vector3.zero },
			{ ZoneSide.Left, new Vector3(-20f, 0f, 0f) },
			{ ZoneSide.Right, new Vector3(20f, 0f, 0f) },
			{ ZoneSide.Top, new Vector3(0f, 20f, 0f) },
			{ ZoneSide.Bottom, new Vector3(0f, -20f, 0f) }
		};

		[FormerlySerializedAs("CenterPosition")]
		[SerializeField] private Transform centerPosition;
		[FormerlySerializedAs("LeftPosition")]
		[SerializeField] private Transform leftPosition;
		[FormerlySerializedAs("RightPosition")]
		[SerializeField] private Transform rightPosition;
		[FormerlySerializedAs("TopPosition")]
		[SerializeField] private Transform topPosition;
		[FormerlySerializedAs("BottomPosition")]
		[SerializeField] private Transform bottomPosition;
		private readonly Dictionary<ZoneSide, CyclingZone> _neighbourZones = new();
		private readonly Dictionary<CycleZoneID, CyclingZone> _resources = new();
		private CameraController _camera;
		private CyclingZone _currentZone;

		private GameData _gameData;

		private int _isChangingZonesCooldown;
		private Player _player;

		private void Awake()
		{
			var zones = Resources.LoadAll<CyclingZone>("Zones");
			foreach (var zone in zones)
			{
				if (zone == null || zone.ZoneID == CycleZoneID.None)
					continue;

				if (_resources.ContainsKey(zone.ZoneID))
					Debug.LogWarning($"Duplicate zone prefab ID {zone.ZoneID} found. Keeping the last loaded prefab.", zone);

				_resources[zone.ZoneID] = zone;
			}

			_gameData = GameData.instance;
			_neighbourZones[ZoneSide.Center] = null;
			_neighbourZones[ZoneSide.Left] = null;
			_neighbourZones[ZoneSide.Right] = null;
			_neighbourZones[ZoneSide.Top] = null;
			_neighbourZones[ZoneSide.Bottom] = null;

			if (_player == null)
				_player = FindAnyObjectByType<Player>();

			if (_camera == null)
				_camera = FindAnyObjectByType<CameraController>();

			if (_currentZone == null) SpawnZone(_gameData.firstZone, ZoneSide.Center);
		}

		private void Update()
		{
			if (_isChangingZonesCooldown > 0)
				_isChangingZonesCooldown--;
		}

		private void SpawnZone(CycleZoneID zoneToSpawn, ZoneSide side)
		{
			if (zoneToSpawn == CycleZoneID.None || side == ZoneSide.None)
				return;

			Debug.Log($"Spawning Zone: {zoneToSpawn}");
			if (!_resources.TryGetValue(zoneToSpawn, out var targetZone))
			{
				Debug.LogError($"Cannot spawn zone {zoneToSpawn}: no CyclingZone prefab with this ZoneID was found in Resources/Zones.");
				return;
			}

			var newZone = Instantiate(targetZone, GetSpawnPosition(side), Quaternion.identity);
			if (_neighbourZones.TryGetValue(side, out var existingZone) && existingZone != null)
				ClearZone(side);
			_neighbourZones[side] = newZone;
			if (side == ZoneSide.Center)
				_currentZone = newZone;

			newZone.OnZoneEntered += NewZoneOnOnZoneEntered;
		}

		private void NewZoneOnOnZoneEntered(object sender, ZoneSide e)
		{
			if (_isChangingZonesCooldown > 0)
				return;

			_isChangingZonesCooldown = 10;
			var enteredZone = sender as CyclingZone;
			if (enteredZone == null)
				return;

			_currentZone = enteredZone;

			if (_player != null)
				_player.transform.SetParent(enteredZone.transform);
			else
				Debug.LogWarning("LevelTraversal cannot parent the player because no Player was found.", this);

			if (_camera != null)
				_camera.transform.SetParent(enteredZone.transform);
			else
				Debug.LogWarning("LevelTraversal cannot parent the camera because no CameraController was found.", this);

			DespawnZones();

			enteredZone.transform.position = GetSpawnPosition(ZoneSide.Center);
			_neighbourZones[ZoneSide.Center] = enteredZone;

			if (!_gameData.currentConnections.TryGetValue(enteredZone.ZoneID, out var zoneInfo))
			{
				Debug.LogError($"Cannot load neighbours for {enteredZone.ZoneID}: GameData has no connection data for this zone.", enteredZone);
				return;
			}

			if (zoneInfo.Left != CycleZoneID.None) SpawnZone(zoneInfo.Left, ZoneSide.Left);
			if (zoneInfo.Right != CycleZoneID.None) SpawnZone(zoneInfo.Right, ZoneSide.Right);
			if (zoneInfo.Top != CycleZoneID.None) SpawnZone(zoneInfo.Top, ZoneSide.Top);
			if (zoneInfo.Bottom != CycleZoneID.None) SpawnZone(zoneInfo.Bottom, ZoneSide.Bottom);
		}

		private void OnDestroy()
		{
			foreach (var zone in _neighbourZones.Values)
				if (zone != null)
					zone.OnZoneEntered -= NewZoneOnOnZoneEntered;
		}

		private void DespawnZones()
		{
			ClearZone(ZoneSide.Center);
			ClearZone(ZoneSide.Left);
			ClearZone(ZoneSide.Right);
			ClearZone(ZoneSide.Top);
			ClearZone(ZoneSide.Bottom);
		}

		private void ClearZone(ZoneSide side)
		{
			if (!_neighbourZones.TryGetValue(side, out var zone))
				return;

			if (zone != null && zone != _currentZone)
			{
				zone.OnZoneEntered -= NewZoneOnOnZoneEntered;
				// Debug.Log($"Zone destroyed side: {side}, {zone.ZoneID}");
				Destroy(zone.gameObject);
			}

			_neighbourZones[side] = null;
		}

		private Vector3 GetSpawnPosition(ZoneSide side)
		{
			var position = side switch
			{
				ZoneSide.Center => centerPosition,
				ZoneSide.Left => leftPosition,
				ZoneSide.Right => rightPosition,
				ZoneSide.Top => topPosition,
				ZoneSide.Bottom => bottomPosition,
				_ => null
			};

			if (position != null)
				return position.position;

			if (DefaultOffsets.TryGetValue(side, out var offset))
				return transform.position + offset;

			Debug.LogError("Should not call this method with None");
			throw new ArgumentOutOfRangeException(nameof(side), side, null);
		}
	}
}
