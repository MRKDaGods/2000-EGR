using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public class EGRScreenOptionsDisplaySettings : EGRScreenAnimatedLayout {
        EGRUIMultiSelectorSettings m_QualitySelector;
        EGRUIMultiSelectorSettings m_FPSSelector;
        EGRUIMultiSelectorSettings m_ResolutionSelector;
        bool m_GraphicsModified;

        protected override string m_LayoutPath => "Scroll View/Viewport/Content/Layout";

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);

            m_QualitySelector = GetElement<EGRUIMultiSelectorSettings>("QualitySelector");
            m_FPSSelector = GetElement<EGRUIMultiSelectorSettings>("FpsSelector");
            m_ResolutionSelector = GetElement<EGRUIMultiSelectorSettings>("ResolutionSelector");
        }

        protected override void OnScreenShow() {
            m_QualitySelector.SelectedIndex = (int)EGRSettings.Quality;
            m_FPSSelector.SelectedIndex = (int)EGRSettings.FPS;
            m_ResolutionSelector.SelectedIndex = (int)EGRSettings.Resolution;

            m_GraphicsModified = false;
        }

        protected override void OnScreenHide() {
            EGRSettings.Save();

            if (m_GraphicsModified)
                EGRSettings.Apply();
        }

        void OnBackClick() {
            if ((EGRSettingsQuality)m_QualitySelector.SelectedIndex != EGRSettings.Quality || (EGRSettingsFPS)m_FPSSelector.SelectedIndex != EGRSettings.FPS) {
                m_GraphicsModified = true;

                EGRPopupConfirmation popup = Manager.GetPopup<EGRPopupConfirmation>();
                popup.SetYesButtonText(Localize(EGRLanguageData.APPLY));
                popup.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
                popup.ShowPopup(Localize(EGRLanguageData.SETTINGS), Localize(EGRLanguageData.GRAPHIC_SETTINGS_WERE_MODIFIED_nWOULD_YOU_LIKE_TO_APPLY_THEM_), OnUnsavedClose, null);
                return;
            }

            HideScreen();
        }

        void OnUnsavedClose(EGRPopup popup, EGRPopupResult result) {
            if (result == EGRPopupResult.YES) {
                EGRSettings.Quality = (EGRSettingsQuality)m_QualitySelector.SelectedIndex;
                EGRSettings.FPS = (EGRSettingsFPS)m_FPSSelector.SelectedIndex;
            }

            HideScreen();
        }
    }
}
