using Gameplay.Data;

namespace Gameplay
{
	public class ZoneInfo
	{
		public CycleZoneID Bottom;

		public CycleZoneID Left;
		public CycleZoneID Right;
		public CycleZoneID Top;

        /// <summary>
        ///     Zone info all zones are containded inside one static file
        ///     Better to use some kind of conf file but we dont have time for parsing system
        ///     Meeting some conditions should change the zones connections in the class.
        /// </summary>
        public CycleZoneID Zone;

		public ZoneInfo(CycleZoneID zone)
		{
			Zone = zone;
		}
	}
}