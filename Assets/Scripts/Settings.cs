using UnityEngine;

namespace MRK
{
    public enum SettingsQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    public enum SettingsFPS
    {
        FPS30,
        FPS60,
        FPS90,
        FPS120,
        VSYNC
    }

    public enum SettingsSensitivity
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum SettingsResolution
    {
        RES100,
        RES90,
        RES80,
        RES75
    }

    public enum SettingsMapStyle
    {
        EGR,
        Basic,
        Satellite
    }

    public enum SettingsInputModel
    {
        Tween,
        MRK
    }

    public enum SettingsSpaceFOV
    {
        Normal,
        Wide,
        Vast,
        Spacious
    }

    public enum SettingsMapViewingAngle
    {
        Flat,
        Spherical,
        Globe
    }

    public class Settings
    {
        private static readonly int[] _egrToUnityQualityMap = { 2, 2, 3, 3 };
        private static readonly float[] _sensitivityMap = { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };
        private static readonly string[] _styleMap = { "main", "basic", "satellite" };
        private static readonly float[] _viewingAngleMap = { 0f, 25f, -50f };
        private static int _counter;
        private static int _initialWidth;
        private static int _initialHeight;

        public static SettingsQuality Quality
        {
            get; set;
        }

        public static SettingsFPS FPS
        {
            get; set;
        }

        public static SettingsResolution Resolution
        {
            get; set;
        }

        public static SettingsSensitivity GlobeSensitivity
        {
            get; set;
        }

        public static SettingsSensitivity MapSensitivity
        {
            get; set;
        }

        public static SettingsMapStyle MapStyle
        {
            get; set;
        }

        public static bool ShowTime
        {
            get; set;
        }

        public static bool ShowDistance
        {
            get; set;
        }

        public static SettingsInputModel InputModel
        {
            get; set;
        }

        public static SettingsSpaceFOV SpaceFOV
        {
            get; set;
        }

        public static SettingsMapViewingAngle MapViewingAngle
        {
            get; set;
        }

        public static void Load()
        {
            if (_initialWidth == 0 || _initialHeight == 0)
            {
                _initialWidth = Screen.width;
                _initialHeight = Screen.height;
            }

            Quality = (SettingsQuality)CryptoPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_QUALITY, 1);
            FPS = (SettingsFPS)CryptoPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_FPS, 1);
            Resolution = (SettingsResolution)CryptoPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_RESOLUTION, 0);
            GlobeSensitivity = (SettingsSensitivity)CryptoPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_GLOBE, 2);
            MapSensitivity = (SettingsSensitivity)CryptoPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_MAP, 2);
            MapStyle = (SettingsMapStyle)CryptoPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_FLAT_MAP_STYLE, 0);
            ShowTime = CryptoPlayerPrefs.Get<bool>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_TIME, true);
            ShowDistance = CryptoPlayerPrefs.Get<bool>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_DISTANCE, true);
            InputModel = (SettingsInputModel)CryptoPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_INPUT_MODEL, 0);
            SpaceFOV = (SettingsSpaceFOV)CryptoPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SPACE_FOV, 0);
            MapViewingAngle = (SettingsMapViewingAngle)CryptoPlayerPrefs.Get<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_FLAT_MAP_VIEWING_ANGLE, 1);
        }

        public static void Save()
        {
            //write to player prefs
            CryptoPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_QUALITY, (int)Quality);
            CryptoPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_FPS, (int)FPS);
            CryptoPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_RESOLUTION, (int)Resolution);
            CryptoPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_GLOBE, (int)GlobeSensitivity);
            CryptoPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_MAP, (int)MapSensitivity);
            CryptoPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_FLAT_MAP_STYLE, (int)MapStyle);
            CryptoPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_TIME, ShowTime);
            CryptoPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_DISTANCE, ShowDistance);
            CryptoPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_INPUT_MODEL, (int)InputModel);
            CryptoPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_SPACE_FOV, (int)SpaceFOV);
            CryptoPlayerPrefs.Set<int>(EGRConstants.EGR_LOCALPREFS_SETTINGS_FLAT_MAP_VIEWING_ANGLE, (int)MapViewingAngle);

            CryptoPlayerPrefs.Save();

            EGREventManager.Instance.BroadcastEvent<SettingsSaved>(new SettingsSaved());
        }

        public static void Apply()
        {
            QualitySettings.SetQualityLevel(_egrToUnityQualityMap[(int)Quality]);
            if (FPS == SettingsFPS.VSYNC)
            {
                QualitySettings.vSyncCount = 1;
            }
            else
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = (((int)FPS) * 30) + 30;
            }

            EGREventManager.Instance.BroadcastEvent(new EGREventGraphicsApplied(Quality, FPS, _counter == 0));
            _counter++;
        }

        public static float GetGlobeSensitivity()
        {
            return _sensitivityMap[(int)GlobeSensitivity];
        }

        public static float GetMapSensitivity()
        {
            return _sensitivityMap[(int)MapSensitivity];
        }

        public static string GetCurrentTileset()
        {
            return _styleMap[(int)MapStyle];
        }

        public static float GetCurrentMapViewingAngle()
        {
            return _viewingAngleMap[(int)MapViewingAngle];
        }
    }
}
