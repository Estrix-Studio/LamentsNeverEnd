namespace Gameplay
{
    public class ZoneInfo
    {
        /// <summary>
        /// Zone info all zones are containded inside one static file
        /// Better to use some kind of conf file but we dont have time for parsing system
        /// Meeting some conditions should change the zones connections in the class.
        /// 
        /// </summary>
        
        public CycleZoneID Zone;
        
        public CycleZoneID Left;
        public CycleZoneID Right;
        public CycleZoneID Top;
        public CycleZoneID Bottom;

        public ZoneInfo(CycleZoneID zone)
        {
            Zone = zone;
        }
    }
}