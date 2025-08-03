using System.Collections.Generic;

namespace Gameplay.Data
{
    public static class ZoneConfig
    {
        public static CycleZoneID FirstZone => CycleZoneID.Penyok1;

        public static IReadOnlyCollection<ZoneInfo> DefaultZones = new List<ZoneInfo>()
        {
            new ZoneInfo(CycleZoneID.Penyok1)
            {
                Left = CycleZoneID.None,
                Right = CycleZoneID.Room1_3,
                Bottom = CycleZoneID.Room1_1,
                Top = CycleZoneID.Penyok1
            },
            new ZoneInfo(CycleZoneID.Room1_1)
            {
                Left = CycleZoneID.Room1_2,
                Top = CycleZoneID.Room1_2,
                Bottom = CycleZoneID.Room1_1,
                Right = CycleZoneID.None,
            },
            new ZoneInfo(CycleZoneID.Room1_2)
            {
                Left =  CycleZoneID.Room1_2,
                Top = CycleZoneID.Penyok2,
                Right = CycleZoneID.Room1_1,
                Bottom = CycleZoneID.Room1_2,
                
            },
            new ZoneInfo(CycleZoneID.Room1_3)
            {
                Left = CycleZoneID.Room1_3,
                Top = CycleZoneID.Room1_3,
                Right = CycleZoneID.Room1_2,
                Bottom = CycleZoneID.None,
            },
            new ZoneInfo(CycleZoneID.Penyok2)
            {
                Left = CycleZoneID.Penyok2,
                Top = CycleZoneID.Room2_1,
                Right = CycleZoneID.Room2_2,
                Bottom = CycleZoneID.Room2_3,
            },
            new ZoneInfo(CycleZoneID.Room2_1)
            {
                Left = CycleZoneID.Room2_2,
                Top = CycleZoneID.Room2_3,
                Right = CycleZoneID.Room2_1,
                Bottom = CycleZoneID.Penyok2
            },
            new ZoneInfo(CycleZoneID.Room2_2)
            {
                Left = CycleZoneID.Room2_1,
                Top = CycleZoneID.Room2_4,
                Right = CycleZoneID.Room2_2,
                Bottom = CycleZoneID.Room2_2,
            },
            new ZoneInfo(CycleZoneID.Room2_3)
            {
                Left = CycleZoneID.None,
                Top = CycleZoneID.Room2_3,
                Right = CycleZoneID.Room2_2,
                Bottom = CycleZoneID.Room2_2,
            },
            new ZoneInfo(CycleZoneID.Room2_4)
            {
                Left = CycleZoneID.None,
                Top = CycleZoneID.None,
                Right = CycleZoneID.None,
                Bottom = CycleZoneID.Penyok3,
            },
            new ZoneInfo(CycleZoneID.Penyok3)
            {
                Left = CycleZoneID.Penyok3,
                Top = CycleZoneID.Penyok3,
                Right = CycleZoneID.Room3_1,
                Bottom = CycleZoneID.Room3_1,
            },
            new ZoneInfo(CycleZoneID.Room3_1)
            {
                Left = CycleZoneID.Penyok3,
                Top = CycleZoneID.Penyok3,
                Right = CycleZoneID.Room3_2,
                Bottom = CycleZoneID.Room3_1,
            },
            new ZoneInfo(CycleZoneID.Room3_2)
            {
                Left = CycleZoneID.Room3_4,
                Top = CycleZoneID.Penyok3,
                Right = CycleZoneID.Room3_3,
                Bottom = CycleZoneID.Room3_2,
            },
            new ZoneInfo(CycleZoneID.Room3_3)
            {
                Left = CycleZoneID.None,
                Top = CycleZoneID.Room3_3,
                Right = CycleZoneID.None,
                Bottom = CycleZoneID.None,
            },
            new ZoneInfo(CycleZoneID.Room3_4)
            {
                Left = CycleZoneID.None,
                Top = CycleZoneID.Room3_5,
                Right = CycleZoneID.None,
                Bottom = CycleZoneID.None,
            },
            new ZoneInfo(CycleZoneID.Room3_5)
            {
                Left = CycleZoneID.None,
                Top = CycleZoneID.Finish,
                Right = CycleZoneID.None,
                Bottom = CycleZoneID.None,
            },
        };
    }
}