using System.IO;
using UnityEngine;

namespace MRK {
    public class MRKSysUtils {
        static string ms_PathSeperators;

        public static string DeviceUniqueIdentifier { get; private set; }

        static MRKSysUtils() {
            ms_PathSeperators = "\\/";
        }

        /// <summary>
        /// CALL FROM MAIN THREAD
        /// </summary>
        public static void Initialize() {
            DeviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
        }

        static int IndexOfPathSeperator(string path, int startIndex = 0) {
            int seperatorIndex;
            int local = 0;
            do {
                seperatorIndex = path.IndexOf(ms_PathSeperators[local++], startIndex);
            }
            while (seperatorIndex == -1 && local < ms_PathSeperators.Length);
            return seperatorIndex;
        }

        public static void CreateRecursiveDirectory(string dir) {
            int start = 0;
            while (start < dir.Length) {
                int sepIdx = IndexOfPathSeperator(dir, start);
                if (sepIdx == -1) {
                    sepIdx = dir.Length - 1;
                }

                string _dir = dir.Substring(0, sepIdx + 1);
                if (!Directory.Exists(_dir)) {
                    Directory.CreateDirectory(_dir);
                }

                start = sepIdx + 1;
            }
        }
    }
}
