using System.Collections.Generic;
using System.Linq;
using Gameplay.Data;
using UnityEngine;

namespace Gameplay.LevelScripts
{
	public class RoomVisistCondition : LevelCondition
	{
		[SerializeField] private List<CycleZoneID> zonesToVisit;

		private readonly Dictionary<CycleZoneID, bool> _zonesVisited = new();

		private void Start()
		{
			if (zonesToVisit != null)
			{
				foreach (var zone in zonesToVisit)
					_zonesVisited[zone] = false;
			}

			if (_zonesVisited.Count == 0) CompleteEvent();
		}

		protected override void InstanceOnOnZoneEntered(CycleZoneID obj)
		{
			if (_zonesVisited.ContainsKey(obj))
			{
				_zonesVisited[obj] = true;
				if (_zonesVisited.All(p => p.Value)) CompleteEvent();
			}
		}
	}
}
