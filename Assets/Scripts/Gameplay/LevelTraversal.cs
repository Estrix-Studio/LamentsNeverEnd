using System;
using System.Collections.Generic;
using Gameplay.Data;
using UnityEngine;

namespace Gameplay
{
    public class LevelTraversal : MonoBehaviour
    {
        private Player _player;
        private CyclingZone _currentZone;
        private readonly Dictionary<ZoneSide, CyclingZone> _neighbourZones = new Dictionary<ZoneSide, CyclingZone>();

        private GameData _gameData;

        [SerializeField] private Transform CenterPosition;
        [SerializeField] private Transform LeftPosition;
        [SerializeField] private Transform RightPosition;
        [SerializeField] private Transform TopPosition;
        [SerializeField] private Transform BottomPosition;
        
        private int _isChangingZonesCooldown = 0;
        private Dictionary<CycleZoneID, CyclingZone> _resources = new Dictionary<CycleZoneID, CyclingZone>();
        private CameraController _camera;
        private void Awake()
        {
             var zones = Resources.LoadAll<CyclingZone>("Zones");
             foreach (var zone in zones)
             {
                 _resources[zone.ZoneID] = zone;
             }
             _gameData = GameData.Instance;
             _neighbourZones[ZoneSide.Center] = null;
             _neighbourZones[ZoneSide.Left] = null;
             _neighbourZones[ZoneSide.Right] = null;
             _neighbourZones[ZoneSide.Top] = null;
             _neighbourZones[ZoneSide.Bottom] = null;
            
             if (_player == null)
                 _player = FindFirstObjectByType<Player>();

             if (_camera == null)
                 _camera = FindFirstObjectByType<CameraController>();

             if (_currentZone == null)
             {
                 SpawnZone(_gameData.FirstZone, ZoneSide.Center);
             }
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
                    pos = CenterPosition.position;
                    break;
                case ZoneSide.Top:
                    pos = TopPosition.position;
                    break;
                case ZoneSide.Right:
                    pos = RightPosition.position;
                    break;
                case ZoneSide.Bottom:
                    pos = BottomPosition.position;
                    break;
                case ZoneSide.Left:
                    pos = LeftPosition.position;
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
            
            enteredZone.transform.position = CenterPosition.position;
            _neighbourZones[ZoneSide.Center] = enteredZone;
            
            var zoneInfo = _gameData.CurrentConnections[enteredZone.ZoneID];
            if (zoneInfo.Left != CycleZoneID.None)
            {
                SpawnZone(zoneInfo.Left, ZoneSide.Left);
            }
            if (zoneInfo.Right != CycleZoneID.None)
            {
                SpawnZone(zoneInfo.Right, ZoneSide.Right);
            }
            if (zoneInfo.Top != CycleZoneID.None)
            {
                SpawnZone(zoneInfo.Top, ZoneSide.Top);
            }
            if (zoneInfo.Bottom != CycleZoneID.None)
            {
                SpawnZone(zoneInfo.Bottom, ZoneSide.Bottom);
            }
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