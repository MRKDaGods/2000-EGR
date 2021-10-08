using System;

namespace MRK {
    public class MRKTime {
        static DateTime ms_StartTime;

        public static TimeSpan RelativeTime => DateTime.Now - ms_StartTime;
        public static float Time => (float)RelativeTime.TotalSeconds;

        static MRKTime() {
            ms_StartTime = DateTime.Now;
        }
    }
}
