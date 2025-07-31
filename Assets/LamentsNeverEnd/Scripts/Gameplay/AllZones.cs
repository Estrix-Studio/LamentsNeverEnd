using System.Collections.Generic;

namespace LamentsNeverEnd.Scripts.Gameplay
{
    public static class AllZones
    {
        public List<ZoneInfo> AllZones = new List<ZoneInfo>()
        {
            new ZoneInfo(CycleZoneID.Penek)
            {
                Left = CycleZoneID.Zone1,
                Right =  CycleZoneID.Zone2,
            }, 
            new ZoneInfo(CycleZoneID.Zone1)
            {
                Up = CycleZoneID.Penek,
                Down = CycleZoneID.Zone2,
            }
        };
    }
}