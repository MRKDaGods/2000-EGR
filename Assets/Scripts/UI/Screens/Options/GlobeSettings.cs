using UnityEngine.UI;

namespace MRK.UI
{
    public class GlobeSettings : AnimatedLayout, ISupportsBackKey
    {
        private MultiSelectorSettings _sensitivitySelector;
        private MultiSelectorSettings _distanceSelector;
        private MultiSelectorSettings _timeSelector;

        public override bool CanChangeBar
        {
            get
            {
                return true;
            }
        }

        public override uint BarColor
        {
            get
            {
                return 0xFF000000;
            }
        }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);

            _sensitivitySelector = GetElement<MultiSelectorSettings>("SensitivitySelector");
            _distanceSelector = GetElement<MultiSelectorSettings>("DistanceSelector");
            _timeSelector = GetElement<MultiSelectorSettings>("TimeSelector");
        }

        protected override void OnScreenShow()
        {
            _sensitivitySelector.SelectedIndex = (int)Settings.GlobeSensitivity;
            _distanceSelector.SelectedIndex = Settings.ShowDistance ? 0 : 1;
            _timeSelector.SelectedIndex = Settings.ShowTime ? 0 : 1;
        }

        protected override void OnScreenHide()
        {
            Settings.GlobeSensitivity = (SettingsSensitivity)_sensitivitySelector.SelectedIndex;
            Settings.ShowDistance = _distanceSelector.SelectedIndex == 0;
            Settings.ShowTime = _timeSelector.SelectedIndex == 0;
            Settings.Save();

            if (Client.ActiveEGRCamera.InterfaceActive)
            {
                Client.ActiveEGRCamera.ResetStates();
            }
        }

        private void OnBackClick()
        {
            HideScreen();
        }

        public void OnBackKeyDown()
        {
            OnBackClick();
        }
    }
}
