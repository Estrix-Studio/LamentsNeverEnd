namespace LamentsNeverEnd.Scripts.Gameplay
{
    public class ZoneInfo
    {
        public CycleZoneID Zone;
        
        public CycleZoneID Left;
        public CycleZoneID Right;
        public CycleZoneID Up;
        public CycleZoneID Down;

        public ZoneInfo(CycleZoneID zone)
        {
            Zone = zone;
        }
    }
}