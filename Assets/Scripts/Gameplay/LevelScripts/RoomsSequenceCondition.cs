using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LevelScripts
{
    public class RoomsSequenceCondition : LevelCondition
    {
        [SerializeField] private List<CycleZoneID> zonesToVisit;

        private int nextIndexToVisit = 0;

        private void Start()
        {
            if (zonesToVisit.Count == 0)
            {
                CompleteEvent();
            }
        }

        protected override void InstanceOnOnZoneEntered(CycleZoneID obj)
        {
            if (zonesToVisit[nextIndexToVisit] == obj)
            {
                nextIndexToVisit++;
                if (nextIndexToVisit == zonesToVisit.Count)
                {
                    CompleteEvent();
                }
            }
            else
            {
                nextIndexToVisit = 0;
            }
        }
    }
}