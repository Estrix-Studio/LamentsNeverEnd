using System;
using System.Collections.Generic;
using Gameplay.Data;
using UnityEngine;

namespace Gameplay
{
	public class LevelTraversal : MonoBehaviour
	{
		[SerializeField] private Transform centerPosition;
		[SerializeField] private Transform leftPosition;
		[SerializeField] private Transform rightPosition;
		[SerializeField] private Transform topPosition;
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
			foreach (var zone in zones) _resources[zone.zoneID] = zone;
			_gameData = GameData.instance;
			_neighbourZones[ZoneSide.Center] = null;
			_neighbourZones[ZoneSide.Left] = null;
			_neighbourZones[ZoneSide.Right] = null;
			_neighbourZones[ZoneSide.Top] = null;
			_neighbourZones[ZoneSide.Bottom] = null;

			if (_player == null)
				_player = FindFirstObjectByType<Player>();

			if (_camera == null)
				_camera = FindFirstObjectByType<CameraController>();

			if (_currentZone == null) SpawnZone(_gameData.firstZone, ZoneSide.Center);
		}

		private void Update()
		{
			if (_isChangingZonesCooldown > 0)
				_isChangingZonesCooldown--;
		}

		private void SpawnZone(CycleZoneID zoneToSpawn, ZoneSide side)
		{
			Debug.Log($"Spawning Zone: {zoneToSpawn}");
			var targetZone = _resources[zoneToSpawn];
			Vector3 pos;
			switch (side)
			{
				case ZoneSide.Center:
					pos = centerPosition.position;
					break;
				case ZoneSide.Top:
					pos = topPosition.position;
					break;
				case ZoneSide.Right:
					pos = rightPosition.position;
					break;
				case ZoneSide.Bottom:
					pos = bottomPosition.position;
					break;
				case ZoneSide.Left:
					pos = leftPosition.position;
					break;
				case ZoneSide.None:
				default:
					Debug.LogError("Should not call this method with None");
					throw new ArgumentOutOfRangeException(nameof(side), side, null);
			}

			var newZone = Instantiate(targetZone, pos, Quaternion.identity);
			if (_neighbourZones[side] != null)
				ClearZone(side);
			_neighbourZones[side] = newZone;

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

			_player.transform.SetParent(enteredZone.transform);
			_camera.transform.SetParent(enteredZone.transform);
			DespawnZones();

			enteredZone.transform.position = centerPosition.position;
			_neighbourZones[ZoneSide.Center] = enteredZone;

			var zoneInfo = _gameData.currentConnections[enteredZone.zoneID];
			if (zoneInfo.Left != CycleZoneID.None) SpawnZone(zoneInfo.Left, ZoneSide.Left);
			if (zoneInfo.Right != CycleZoneID.None) SpawnZone(zoneInfo.Right, ZoneSide.Right);
			if (zoneInfo.Top != CycleZoneID.None) SpawnZone(zoneInfo.Top, ZoneSide.Top);
			if (zoneInfo.Bottom != CycleZoneID.None) SpawnZone(zoneInfo.Bottom, ZoneSide.Bottom);
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
			if (_neighbourZones[side] != null && _neighbourZones[side] != _currentZone)
			{
				_neighbourZones[side].OnZoneEntered -= NewZoneOnOnZoneEntered;
				// Debug.Log($"Zone destroyed side: {side}, {_neighbourZones[side].ZoneID}");
				Destroy(_neighbourZones[side].gameObject);
			}

			_neighbourZones[side] = null;
		}
	}
}