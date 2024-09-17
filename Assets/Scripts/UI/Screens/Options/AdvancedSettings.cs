using TMPro;
using UnityEngine.UI;

namespace MRK.UI
{
    public class AdvancedSettings : AnimatedLayout, ISupportsBackKey
    {
        private MultiSelectorSettings _inputModelSelector;
        private TextMeshProUGUI _latitude;
        private TextMeshProUGUI _longitude;
        private TextMeshProUGUI _bearing;
        private TextMeshProUGUI _lastError;

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

            _inputModelSelector = GetElement<MultiSelectorSettings>("InputModelSelector");

            _latitude = GetElement<TextMeshProUGUI>("Layout/Latitude/Text");
            _longitude = GetElement<TextMeshProUGUI>("Layout/Longitude/Text");
            _bearing = GetElement<TextMeshProUGUI>("Layout/Bearing/Text");
            _lastError = GetElement<TextMeshProUGUI>("Layout/Error/Text");
            GetElement<Button>("Layout/Request").onClick.AddListener(OnRequestLocation);
        }

        protected override void OnScreenShow()
        {
            _inputModelSelector.SelectedIndex = (int)Settings.InputModel;
            _latitude.text = _longitude.text = _bearing.text = _lastError.text = "";
        }

        protected override void OnScreenHide()
        {
            Settings.InputModel = (SettingsInputModel)_inputModelSelector.SelectedIndex;
            Settings.Save();
        }

        private void OnRequestLocation()
        {
            Client.LocationService.GetCurrentLocation(OnReceiveLocation);
        }

        private void OnReceiveLocation(bool success, Vector2d? coord, float? bearing)
        {
            _latitude.text = success ? coord.Value.x.ToString() : "-";
            _longitude.text = success ? coord.Value.y.ToString() : "-";
            _bearing.text = success ? bearing.Value.ToString() : "-";
            _lastError.text = Client.LocationService.LastError.ToString();
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
