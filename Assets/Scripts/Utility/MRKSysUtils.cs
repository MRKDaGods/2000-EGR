using UnityEngine;

namespace MRK {
    public class MRKSysUtils {
        public static string DeviceUniqueIdentifier { get; private set; }

        /// <summary>
        /// CALL FROM MAIN THREAD
        /// </summary>
        public static void Initialize() {
            DeviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
        }
    }
}
