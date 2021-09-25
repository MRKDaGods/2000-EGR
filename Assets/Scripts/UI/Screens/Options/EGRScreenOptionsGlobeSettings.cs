using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenOptionsGlobeSettings : EGRScreenAnimatedLayout, IEGRScreenSupportsBackKey {
        EGRUIMultiSelectorSettings m_SensitivitySelector;
        EGRUIMultiSelectorSettings m_DistanceSelector;
        EGRUIMultiSelectorSettings m_TimeSelector;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0xFF000000;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);

            m_SensitivitySelector = GetElement<EGRUIMultiSelectorSettings>("SensitivitySelector");
            m_DistanceSelector = GetElement<EGRUIMultiSelectorSettings>("DistanceSelector");
            m_TimeSelector = GetElement<EGRUIMultiSelectorSettings>("TimeSelector");
        }

        protected override void OnScreenShow() {
            m_SensitivitySelector.SelectedIndex = (int)EGRSettings.GlobeSensitivity;
            m_DistanceSelector.SelectedIndex = EGRSettings.ShowDistance ? 0 : 1;
            m_TimeSelector.SelectedIndex = EGRSettings.ShowTime ? 0 : 1;
        }

        protected override void OnScreenHide() {
            EGRSettings.GlobeSensitivity = (EGRSettingsSensitivity)m_SensitivitySelector.SelectedIndex;
            EGRSettings.ShowDistance = m_DistanceSelector.SelectedIndex == 0;
            EGRSettings.ShowTime = m_TimeSelector.SelectedIndex == 0;
            EGRSettings.Save();

            if (Client.ActiveEGRCamera.InterfaceActive) {
                Client.ActiveEGRCamera.ResetStates();
            }
        }

        void OnBackClick() {
            HideScreen();
        }

        public void OnBackKeyDown() {
            OnBackClick();
        }
    }
}
