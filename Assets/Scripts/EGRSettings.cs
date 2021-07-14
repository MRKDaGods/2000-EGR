using UnityEngine;

namespace MRK {
    public enum EGRSettingsQuality {
        Low,
        Medium,
        High,
        Ultra
    }

    public enum EGRSettingsFPS {
        FPS30,
        FPS60,
        FPS90,
        FPS120,
        VSYNC
    }

    public enum EGRSettingsSensitivity {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum EGRSettingsResolution {
        RES100,
        RES90,
        RES80,
        RES75
    }

    public enum EGRSettingsMapStyle {
        EGR,
        Basic,
        Satellite
    }

    public enum EGRSettingsInputModel {
        Tween,
        MRK
    }

    public enum EGRSettingsSpaceFOV {
        Normal,
        Wide,
        Vast,
        Spacious
    }

    public class EGRSettings {
        static readonly int[] ms_EGRToUnityQualityMap = { 2, 2, 3, 3 };
        static readonly float[] ms_ResolutionMap = { 1f, 0.9f, 0.8f, 0.75f };
        static readonly float[] ms_SensitivityMap = { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };
        static readonly string[] ms_StyleMap = { "main", "basic", "satellite" };
        static int ms_Counter;
        static int ms_InitialWidth;
        static int ms_InitialHeight;

        public static EGRSettingsQuality Quality { get; set; }
        public static EGRSettingsFPS FPS { get; set; }
        public static EGRSettingsResolution Resolution { get; set; }
        public static EGRSettingsSensitivity GlobeSensitivity { get; set; }
        public static EGRSettingsSensitivity MapSensitivity { get; set; }
        public static EGRSettingsMapStyle MapStyle { get; set; }
        public static bool ShowTime { get; set; }
        public static bool ShowDistance { get; set; }
        public static EGRSettingsInputModel InputModel { get; set; }
        public static EGRSettingsSpaceFOV SpaceFOV { get; set; }

        public static void Load() {
            if (ms_InitialWidth == 0 || ms_InitialHeight == 0) {
                ms_InitialWidth = Screen.width;
                ms_InitialHeight = Screen.height;
            }

            Quality = (EGRSettingsQuality)MRKPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_QUALITY, 1);
            FPS = (EGRSettingsFPS)MRKPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_FPS, 1);
            Resolution = (EGRSettingsResolution)MRKPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_RESOLUTION, 0);
            GlobeSensitivity = (EGRSettingsSensitivity)MRKPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_GLOBE, 2);
            MapSensitivity = (EGRSettingsSensitivity)MRKPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_MAP, 2);
            MapStyle = (EGRSettingsMapStyle)MRKPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_FLAT_MAP_STYLE, 0);
            ShowTime = MRKPlayerPrefs.Get<bool>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_TIME, true);
            ShowDistance = MRKPlayerPrefs.Get<bool>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_DISTANCE, true);
            InputModel = (EGRSettingsInputModel)MRKPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_INPUT_MODEL, 0);
            SpaceFOV = (EGRSettingsSpaceFOV)MRKPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SPACE_FOV, 0);
        }

        public static void Save() {
            //write to player prefs
            MRKPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_QUALITY, (int)Quality);
            MRKPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_FPS, (int)FPS);
            MRKPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_RESOLUTION, (int)Resolution);
            MRKPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_GLOBE, (int)GlobeSensitivity);
            MRKPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_MAP, (int)MapSensitivity);
            MRKPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_FLAT_MAP_STYLE, (int)MapStyle);
            MRKPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_TIME, ShowTime);
            MRKPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_DISTANCE, ShowDistance);
            MRKPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_INPUT_MODEL, (int)InputModel);
            MRKPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SPACE_FOV, (int)SpaceFOV);

            MRKPlayerPrefs.Save();

            EGREventManager.Instance.BroadcastEvent<EGREventSettingsSaved>(new EGREventSettingsSaved());
        }

        public static void Apply() {
            QualitySettings.SetQualityLevel(ms_EGRToUnityQualityMap[(int)Quality]);
            if (FPS == EGRSettingsFPS.VSYNC)
                QualitySettings.vSyncCount = 1;
            else {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = ((int)FPS) * 30 + 30;
            }

            float resFactor = ms_ResolutionMap[(int)Resolution];
            Screen.SetResolution(Mathf.FloorToInt(ms_InitialWidth * resFactor), Mathf.FloorToInt(ms_InitialHeight * resFactor), false);

            Debug.Log($"RES={Screen.currentResolution}, W={Screen.width}, H={Screen.height}");

            EGREventManager.Instance.BroadcastEvent<EGREventGraphicsApplied>(new EGREventGraphicsApplied(Quality, FPS, ms_Counter == 0));
            ms_Counter++;
        }

        public static float GetGlobeSensitivity() {
            return ms_SensitivityMap[(int)GlobeSensitivity];
        }

        public static float GetMapSensitivity() {
            return ms_SensitivityMap[(int)MapSensitivity];
        }

        public static string GetCurrentTileset() {
            return ms_StyleMap[(int)MapStyle];
        }
    }
}
