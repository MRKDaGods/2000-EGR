using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public class EGRSettings {
        static readonly int[] ms_EGRToUnityQualityMap = { 2, 2, 3, 3 };
        static int ms_Counter;

        public static EGRSettingsQuality Quality { get; set; }
        public static EGRSettingsFPS FPS { get; set; }
        public static float SensitivityGlobeX { get; set; }
        public static float SensitivityGlobeY { get; set; }
        public static float SensitivityGlobeZ { get; set; }
        public static float SensitivityMapX { get; set; }
        public static float SensitivityMapY { get; set; }
        public static float SensitivityMapZ { get; set; }
        public static bool ShowTime { get; set; }
        public static bool ShowDistance { get; set; }

        public static void Load() {
            Quality = (EGRSettingsQuality)PlayerPrefs.GetInt(EGRConstants.EGR_LOCALPREFS_SETTINGS_QUALITY, 1);
            FPS = (EGRSettingsFPS)PlayerPrefs.GetInt(EGRConstants.EGR_LOCALPREFS_SETTINGS_FPS, 1);

            SensitivityGlobeX = PlayerPrefs.GetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_GLOBE_X, 0.5f);
            SensitivityGlobeY = PlayerPrefs.GetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_GLOBE_Y, 0.5f);
            SensitivityGlobeZ = PlayerPrefs.GetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_GLOBE_Z, 0.5f);

            SensitivityMapX = PlayerPrefs.GetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_MAP_X, 0.5f);
            SensitivityMapY = PlayerPrefs.GetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_MAP_Y, 0.5f);
            SensitivityMapZ = PlayerPrefs.GetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_MAP_Z, 0.5f);

            ShowTime = PlayerPrefs.GetInt(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_TIME, 1).ToBool();
            ShowDistance = PlayerPrefs.GetInt(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_DISTANCE, 1).ToBool();
        }

        public static void Save() {
            //write to player prefs
            PlayerPrefs.SetInt(EGRConstants.EGR_LOCALPREFS_SETTINGS_QUALITY, (int)Quality);
            PlayerPrefs.SetInt(EGRConstants.EGR_LOCALPREFS_SETTINGS_FPS, (int)FPS);

            PlayerPrefs.SetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_GLOBE_X, SensitivityGlobeX);
            PlayerPrefs.SetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_GLOBE_Y, SensitivityGlobeY);
            PlayerPrefs.SetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_GLOBE_Z, SensitivityGlobeZ);

            PlayerPrefs.SetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_MAP_X, SensitivityMapX);
            PlayerPrefs.SetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_MAP_Y, SensitivityMapY);
            PlayerPrefs.SetFloat(EGRConstants.EGR_LOCALPREFS_SETTINGS_SENSITIVITY_MAP_Z, SensitivityMapZ);

            PlayerPrefs.SetInt(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_TIME, ShowTime ? 1 : 0);
            PlayerPrefs.SetInt(EGRConstants.EGR_LOCALPREFS_SETTINGS_SHOW_DISTANCE, ShowDistance ? 1 : 0);

            PlayerPrefs.Save();
        }

        public static void Apply() {
            QualitySettings.SetQualityLevel(ms_EGRToUnityQualityMap[(int)Quality]);
            if (FPS == EGRSettingsFPS.VSYNC)
                QualitySettings.vSyncCount = 1;
            else {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = ((int)FPS) * 30 + 30;
            }

            EGREventManager.Instance.BroadcastEvent<EGREventGraphicsApplied>(new EGREventGraphicsApplied(Quality, FPS, ms_Counter == 0));
            ms_Counter++;
        }
    }
}
