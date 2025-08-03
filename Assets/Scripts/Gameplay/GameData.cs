using System;
using System.Collections.Generic;
using Gameplay.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay
{
    public class GameData
    {
        public static GameData Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameData();
                return _instance;
            }
        }
        private static GameData _instance;
        private GameData()
        {
            Reset();
        }

        private readonly Dictionary<CycleZoneID, ZoneInfo> _currentConnections = new ();
        public Dictionary<CycleZoneID, ZoneInfo> CurrentConnections => _currentConnections;

        private readonly HashSet<EventName> _completedEvents = new HashSet<EventName>();

        public HashSet<EventName> SpawnedConditions => _spawnedLevelConditions;
        private readonly HashSet<EventName> _spawnedLevelConditions = new HashSet<EventName>();
        
        public CycleZoneID FirstZone => ZoneConfig.FirstZone;

        public event Action<CycleZoneID> OnZoneEntered;
        public event Action<string> OnObjectInteracted;
        public event Action<EventName> OnEventCompleted;
        
        public void Reset()
        {
            CurrentConnections.Clear();
            foreach (var zone in ZoneConfig.DefaultZones)
            {
                CurrentConnections[zone.Zone] = zone;
            }
            _completedEvents.Clear();
            _spawnedLevelConditions.Clear();
        }

        public bool IsEventCompleted(EventName eventName)
        {
            return _completedEvents.Contains(eventName);
        }

        public void TriggerOnZoneEntered(CycleZoneID zone)
        {
            OnZoneEntered?.Invoke(zone);
        }

        public void TriggerOnObjectInteracted(string objectName)
        {
            OnObjectInteracted?.Invoke(objectName);
        }

        public void CompleteEvent(EventName eventName)
        {
            _completedEvents.Add(eventName);
            OnEventCompleted?.Invoke(eventName);
        }

        public void RestartGame()
        {
            Debug.Log("RESTARTING GAME HERE!");
            Reset();
            SceneManager.LoadScene("LossScreen");
        }
    }
}