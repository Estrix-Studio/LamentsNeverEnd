using System.Collections.Generic;

namespace Gameplay
{
    public static class ZoneConfig
    {
        public static CycleZoneID FirstZone => CycleZoneID.Penyok;

        public static IReadOnlyCollection<ZoneInfo> DefaultZones = new List<ZoneInfo>()
        {
            new ZoneInfo(CycleZoneID.Penyok)
            {
                Left = CycleZoneID.Zone2,
                Top = CycleZoneID.Zone1,
            },
            new ZoneInfo(CycleZoneID.Zone1)
            {
                Bottom =  CycleZoneID.Penyok,
                Left = CycleZoneID.Zone2,
            },
            new ZoneInfo(CycleZoneID.Zone2)
            {
                Bottom =  CycleZoneID.Penyok,
                Left = CycleZoneID.Zone1,
                Right = CycleZoneID.Zone1,
            }
        };
    }
}