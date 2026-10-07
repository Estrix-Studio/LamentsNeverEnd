using System;
using System.Collections.Generic;
using Gameplay.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay
{
	public class GameData
	{
		private static GameData _instance;

		private readonly HashSet<EventName> _completedEvents = new();

		private GameData()
		{
			Reset();
		}

		public static GameData instance
		{
			get
			{
				_instance ??= new GameData();
				return _instance;
			}
		}

		public Dictionary<CycleZoneID, ZoneInfo> currentConnections { get; } = new();

		public HashSet<EventName> spawnedConditions { get; } = new();

		public CycleZoneID firstZone => ZoneConfig.firstZone;

		public event Action<CycleZoneID> OnZoneEntered;
		public event Action<string> OnObjectInteracted;
		public event Action<EventName> OnEventCompleted;

		public void Reset()
		{
			currentConnections.Clear();
			foreach (var zone in ZoneConfig.DefaultZones)
				currentConnections[zone.Zone] = new ZoneInfo(zone.Zone)
				{
					Left = zone.Left, Right = zone.Right, Top = zone.Top, Bottom = zone.Bottom
				};
			_completedEvents.Clear();
			spawnedConditions.Clear();
		}

		public bool TryRegisterCondition(EventName eventName)
		{
			return eventName != EventName.None && !IsEventCompleted(eventName) &&
			       spawnedConditions.Add(eventName);
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
			if (eventName != EventName.None && _completedEvents.Add(eventName))
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