using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenOptionsMapSettings : EGRScreenAnimatedLayout {
        EGRUIMultiSelectorSettings m_SensitivitySelector;
        EGRUIMultiSelectorSettings m_StyleSelector;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(() => HideScreen());
            GetElement<Button>("Layout/Preview").onClick.AddListener(OnPreviewClick);

            m_SensitivitySelector = GetElement<EGRUIMultiSelectorSettings>("SensitivitySelector");
            m_StyleSelector = GetElement<EGRUIMultiSelectorSettings>("StyleSelector");
        }

        protected override void OnScreenShow() {
            m_SensitivitySelector.SelectedIndex = (int)EGRSettings.MapSensitivity;
            m_StyleSelector.SelectedIndex = (int)EGRSettings.MapStyle;
        }

        protected override void OnScreenHide() {
            EGRSettings.MapSensitivity = (EGRSettingsSensitivity)m_SensitivitySelector.SelectedIndex;
            EGRSettings.MapStyle = (EGRSettingsMapStyle)m_StyleSelector.SelectedIndex;
            EGRSettings.Save();
        }

        void OnPreviewClick() {
            EGRScreenMapChooser mapChooser = Manager.GetScreen<EGRScreenMapChooser>();
            mapChooser.MapStyleCallback = OnMapStyleChosen;
            mapChooser.ShowScreen();
        }

        void OnMapStyleChosen(int style) {
            m_StyleSelector.SelectedIndex = style;
        }
    }
}
