using System.Collections.Generic;

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

        private readonly Dictionary<string, bool> _completedEvents = new Dictionary<string, bool>();
        public CycleZoneID FirstZone => ZoneConfig.FirstZone;

        public void Reset()
        {
            CurrentConnections.Clear();
            foreach (var zone in ZoneConfig.DefaultZones)
            {
                CurrentConnections[zone.Zone] = zone;
            }
            _completedEvents.Clear();
        }

        public bool IsEventCompleted(string eventName)
        {
            return _completedEvents.GetValueOrDefault(eventName, false);
        }
    }
}