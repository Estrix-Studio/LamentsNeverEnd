using System.Collections.Generic;

namespace LamentsNeverEnd.Scripts.Gameplay
{
    public static class AllZones
    {
        public static List<ZoneInfo> allZones { get; } = new List<ZoneInfo>()
        {
            new ZoneInfo(CycleZoneID.Penyok)
            {
                Left = CycleZoneID.Zone1,
                Right = CycleZoneID.Zone2,
            },
            new ZoneInfo(CycleZoneID.Zone1)
            {
                Up = CycleZoneID.Penyok,
                Down = CycleZoneID.Zone2,
            }
        };
    }
}