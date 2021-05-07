using UnityEngine;

namespace MRK {
    public class EGRVersion {
        public enum Staging : byte {
            Development = (byte)'d',
            Alpha = (byte)'a',
            Beta = (byte)'b',
            Release = (byte)'\0'
        }

        public static int Major = 0;
        public static int Minor = 1;
        public static int Revision = 1;

#if UNITY_EDITOR
        static bool ms_Dirty;
#endif

        public static int Build {
            get {
#if UNITY_EDITOR
                int build = PlayerPrefs.GetInt(EGRConstants.EGR_LOCALPREFS_BUILD, 0);

                if (!ms_Dirty) {
                    build++;
                    PlayerPrefs.SetInt(EGRConstants.EGR_LOCALPREFS_BUILD, build);
                    ms_Dirty = true;
                }

                return build;
#else
                return 1;
#endif
            }
        }

        public static Staging Stage = Staging.Development;

        public static string VersionString() {
            return $"{Major}.{Minor}.{Revision}{(char)Stage}.{Build}";
        }

        public static string VersionSignature() {
            return ((Major + 1) ^ Minor * Revision * 10000 + Build).ToString();
        }
    }
}