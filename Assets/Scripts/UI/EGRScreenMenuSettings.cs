using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using static MRK.EGRLanguageManager;
using static MRK.UI.EGRUI_Main.EGRScreen_MenuSettings;

namespace MRK.UI {
    public class EGRScreenMenuSettings : EGRScreen {
        EGRUIMultiSelector m_Quality;
        EGRUIMultiSelector m_FPS;
        Slider m_GlobeX;
        Slider m_GlobeY;
        Slider m_GlobeZ;
        Slider m_MapX;
        Slider m_MapY;
        Slider m_MapZ;
        SegmentedControl m_ShowTime;
        SegmentedControl m_ShowDist;
        bool m_GraphicsModified;

        protected override void OnScreenInit() {
            GetElement<Button>(Buttons.Back).onClick.AddListener(OnBackClick);

            //we gotta do it manually *shrug*
            m_Quality = GetElement<EGRUIMultiSelector>("LAYOUT/Quality/Custom");
            m_FPS = GetElement<EGRUIMultiSelector>("LAYOUT/FPS/Custom");
            m_GlobeX = GetElement<Slider>("LAYOUT/GlobeX/Slider");
            m_GlobeY = GetElement<Slider>("LAYOUT/GlobeY/Slider");
            m_GlobeZ = GetElement<Slider>("LAYOUT/GlobeZ/Slider");
            m_MapX = GetElement<Slider>("LAYOUT/MapX/Slider");
            m_MapY = GetElement<Slider>("LAYOUT/MapY/Slider");
            m_MapZ = GetElement<Slider>("LAYOUT/MapZ/Slider");
            m_ShowTime = GetElement<SegmentedControl>("LAYOUT/Time/Segmented");
            m_ShowDist = GetElement<SegmentedControl>("LAYOUT/Distance/Segmented");
        }

        protected override void OnScreenShow() {
            //set values from settings
            m_Quality.SelectedIndex = (int)EGRSettings.Quality;
            m_FPS.SelectedIndex = (int)EGRSettings.FPS;
            //m_GlobeX.DOValue(EGRSettings.SensitivityGlobeX, 0.4f).ChangeStartValue(0f);
            //m_GlobeY.DOValue(EGRSettings.SensitivityGlobeY, 0.4f).ChangeStartValue(0f);
            //m_GlobeZ.DOValue(EGRSettings.SensitivityGlobeZ, 0.4f).ChangeStartValue(0f);
            //m_MapX.DOValue(EGRSettings.SensitivityMapX, 0.4f).ChangeStartValue(0f);
            //m_MapY.DOValue(EGRSettings.SensitivityMapY, 0.4f).ChangeStartValue(0f);
            //m_MapZ.DOValue(EGRSettings.SensitivityMapZ, 0.4f).ChangeStartValue(0f);
            m_ShowTime.selectedSegmentIndex = EGRSettings.ShowTime ? 0 : 1; //SHOW = 0, HIDE = 1 based on hierarchy
            m_ShowDist.selectedSegmentIndex = EGRSettings.ShowDistance ? 0 : 1;
        }

        void OnBackClick() {
            if ((EGRSettingsQuality)m_Quality.SelectedIndex != EGRSettings.Quality || (EGRSettingsFPS)m_FPS.SelectedIndex != EGRSettings.FPS) {
                m_GraphicsModified = true;

                EGRPopupConfirmation popup = Manager.GetPopup<EGRPopupConfirmation>();
                popup.SetYesButtonText(Localize(EGRLanguageData.APPLY));
                popup.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
                popup.ShowPopup(Localize(EGRLanguageData.SETTINGS), Localize(EGRLanguageData.GRAPHIC_SETTINGS_WERE_MODIFIED_nWOULD_YOU_LIKE_TO_APPLY_THEM_), OnUnsavedClose, null);
                return;
            }

            //EGRSettings.SensitivityGlobeX = m_GlobeX.value;
            //EGRSettings.SensitivityGlobeY = m_GlobeY.value;
            //EGRSettings.SensitivityGlobeZ = m_GlobeZ.value;
            //EGRSettings.SensitivityMapX = m_MapX.value;
            //EGRSettings.SensitivityMapY = m_MapY.value;
            //EGRSettings.SensitivityMapZ = m_MapZ.value;
            //EGRSettings.ShowTime = m_ShowTime.selectedSegmentIndex == 0;
            //EGRSettings.ShowDistance = m_ShowDist.selectedSegmentIndex == 0;

            HideScreen();
        }

        void OnUnsavedClose(EGRPopup popup, EGRPopupResult result) {
            if (result == EGRPopupResult.YES) {
                EGRSettings.Quality = (EGRSettingsQuality)m_Quality.SelectedIndex;
                EGRSettings.FPS = (EGRSettingsFPS)m_FPS.SelectedIndex;
            }

            HideScreen();
        }

        protected override void OnScreenHide() {
            EGRSettings.Save();

            if (m_GraphicsModified)
                EGRSettings.Apply();
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            PushGfxState(EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.1f + i * 0.03f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.05f + i * 0.03f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }
    }
}
