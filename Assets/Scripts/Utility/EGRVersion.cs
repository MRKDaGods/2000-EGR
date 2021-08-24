using UnityEngine;

namespace MRK {
    public class EGRVersion {
        public enum Staging : byte {
            Development = (byte)'d',
            Alpha = (byte)'a',
            Beta = (byte)'b',
            Release = (byte)'\0'
        }

        static bool ms_LoadedBuild;
        static int ms_Build;

        public static int Major = 0;
        public static int Minor = 4;
        public static int Revision = 2;

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
                    PlayerPrefs.Save();
                    ms_Dirty = true;
                }

                return build;
#else
                if (!ms_LoadedBuild) {
                    ms_LoadedBuild = true;
                    ms_Build = int.Parse(Resources.Load<TextAsset>("BuildInfo/Build").text);
                }

                return ms_Build;
#endif
            }
        }

        public static Staging Stage = Staging.Beta;

        public static string VersionString() {
            return $"{Major}.{Minor}.{Revision}{(char)Stage}.{Build}";
        }

        public static string VersionSignature() {
            return ((Major ^ 397) * (Minor ^ 397) * Revision * 10000 + Build).ToString();
        }
    }
}