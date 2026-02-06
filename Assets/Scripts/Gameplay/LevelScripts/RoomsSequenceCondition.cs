using System.Collections.Generic;
using Gameplay.Data;
using UnityEngine;

namespace Gameplay.LevelScripts
{
	public class RoomsSequenceCondition : LevelCondition
	{
		[SerializeField] private List<CycleZoneID> zonesToVisit;

		private int _nextIndexToVisit;

		private void Start()
		{
			if (zonesToVisit.Count == 0) CompleteEvent();
		}

		protected override void InstanceOnOnZoneEntered(CycleZoneID obj)
		{
			if (zonesToVisit[_nextIndexToVisit] == obj)
			{
				_nextIndexToVisit++;
				if (_nextIndexToVisit == zonesToVisit.Count) CompleteEvent();
			}
			else
			{
				_nextIndexToVisit = 0;
			}
		}
	}
}