using System;
using System.Collections.Generic;
using System.Linq;
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
        
        private void Awake()
        {
            _gameData = GameData.Instance;
            _neighbourZones[ZoneSide.Center] = null;
            _neighbourZones[ZoneSide.Left] = null;
            _neighbourZones[ZoneSide.Right] = null;
            _neighbourZones[ZoneSide.Top] = null;
            _neighbourZones[ZoneSide.Bottom] = null;
            
            if (_player == null)
                _player = FindFirstObjectByType<Player>();
            
            if (_currentZone == null)
            {
                SpawnZone(_gameData.FirstZone, ZoneSide.Center);
            }
        }

        private void SpawnZone(CycleZoneID zoneToSpawn, ZoneSide side)
        {
            var zones = Resources.LoadAll<CyclingZone>("Zones");
            var targetZone = zones.FirstOrDefault(z => z.ZoneID == zoneToSpawn);
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
            var enteredZone = sender as CyclingZone;
            if (enteredZone == null)
                return;
            var zoneInfo = _gameData.CurrentConnections[enteredZone.ZoneID];

            _player.transform.SetParent(enteredZone.transform);
            enteredZone.transform.position = CenterPosition.position;
            _neighbourZones[ZoneSide.Center] = enteredZone;
            
            _currentZone = enteredZone;

            DespawnZones();
            
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
            ClearZone(ZoneSide.Left);
            ClearZone(ZoneSide.Right);
            ClearZone(ZoneSide.Top);
            ClearZone(ZoneSide.Bottom);
        }

        private void ClearZone(ZoneSide side)
        {
            if (side == ZoneSide.Center) return;
            if (_neighbourZones[side] != null && _neighbourZones[side] != _currentZone)
            {
                _neighbourZones[side].OnZoneEntered -= NewZoneOnOnZoneEntered;
                Destroy(_neighbourZones[side]);
                Debug.Log($"Zone destroyed side: {side}, {_neighbourZones[side].ZoneID}");
            }
            _neighbourZones[side] = null;
        }
        
        private void Start()
        {
        }
    }
}