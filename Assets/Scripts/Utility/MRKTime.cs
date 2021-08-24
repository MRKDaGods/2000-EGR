using System;

namespace MRK {
    public class MRKTime {
        static DateTime ms_StartTime;

        public static float Time => (float)(DateTime.Now - ms_StartTime).TotalSeconds;

        public static void Initialize() {
            ms_StartTime = DateTime.Now;
        }
    }
}
