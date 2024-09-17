using MRK.Localization;
using MRK.Networking.Packets;
using UnityEngine;
using static MRK.Localization.LanguageManager;

namespace MRK.UI.MapInterface
{
    public partial class Navigation : Component
    {
        private Transform _navigationTransform;
        private Top _top;
        private Bottom _bottom;
        private AutoComplete _autoComplete;
        private NavInterface _navInterface;
        private bool _queryCancelled;
        private bool _isManualLocating;

        private static Navigation _instance;

        public override ComponentType ComponentType
        {
            get
            {
                return ComponentType.Navigation;
            }
        }

        public bool IsActive
        {
            get; private set;
        }

        private Vector2d? FromCoords
        {
            get; set;
        }

        private Vector2d? ToCoords
        {
            get; set;
        }

        private bool IsFromCurrentLocation
        {
            get; set;
        }

        private bool IsPreviewStartMode
        {
            get; set;
        }

        public NavInterface NavigationInterface
        {
            get
            {
                return _navInterface;
            }
        }

        public override void OnComponentInit(MapInterface mapInterface)
        {
            base.OnComponentInit(mapInterface);

            _instance = this;

            _navigationTransform = mapInterface.transform.Find("Navigation");
            _navigationTransform.gameObject.SetActive(false);

            _top = new Top((RectTransform)_navigationTransform.Find("Top"));
            _bottom = new Bottom((RectTransform)_navigationTransform.Find("Bot"));

            _autoComplete = new AutoComplete((RectTransform)_navigationTransform.Find("Top/AutoComplete"));
            _autoComplete.SetAutoCompleteState(false);

            _navInterface = new NavInterface((RectTransform)_navigationTransform.Find("NavInterface"));
            _navInterface.SetActive(false); //hide by def
        }

        public override void OnComponentUpdate()
        {
            _bottom.Update();

            if (_autoComplete.IsActive)
            {
                _autoComplete.Update();
            }
        }

        public void Show()
        {
            _navigationTransform.gameObject.SetActive(true);
            _top.Show();

            Client.Runnable.RunLater(_bottom.ShowBackButton, 0.5f);
            MapInterface.Components.MapButtons.RemoveAllButtons();

            IsActive = true;
            FromCoords = ToCoords = null;
            IsFromCurrentLocation = false;
        }

        public bool Hide()
        {
            _top.Hide();
            _bottom.Hide();
            _navInterface.SetActive(false);

            if (!_isManualLocating)
            {
                Client.Runnable.RunLater(() =>
                {
                    _navigationTransform.gameObject.SetActive(false);
                    _bottom.ClearDirections();

                    Client.NavigationManager.ExitNavigation();
                    Client.FlatCamera.ExitNavigation();

                    IsActive = false;
                }, 0.3f);

                MapInterface.RegenerateMapButtons();

                return true;
            }
            else
            {
                MapInterface.Components.LocationOverlay.Finish();
                return false;
            }
        }

        private bool CanQueryDirections()
        {
            return !string.IsNullOrEmpty(_top.From) && !string.IsNullOrWhiteSpace(_top.To)
                && (FromCoords.HasValue || IsFromCurrentLocation) && ToCoords.HasValue;
        }

        private void OnReceiveLocation(bool success, Vector2d? coords, float? bearing)
        {
            Client.Runnable.RunLater((System.Action)(() =>
            {
                MapInterface.MessageBox.HideScreen((System.Action)(() =>
                {
                    if (!success)
                    {
                        MapInterface.MessageBox.ShowPopup(
                            Localize(LanguageData.EGR),
                            Localize(LanguageData.CANNOT_OBTAIN_CURRENT_LOCATION),
                            (PopupCallback)null,
                            (Screen)MapInterface
                        );
                        return;
                    }

                    FromCoords = coords.Value;
                    QueryDirections(true);
                }), 1.1f);
            }), 0.4f);
        }

        private void QueryDirections(bool ignoreCurrentLocation = false)
        {
            if (!CanQueryDirections())
                return;

            if (!ignoreCurrentLocation && IsFromCurrentLocation)
            {
                //get cur loc
                MapInterface.MessageBox.ShowButton(false);
                MapInterface.MessageBox.ShowPopup(
                    Localize(LanguageData.EGR),
                    Localize(LanguageData.RETRIEVING_CURRENT_LOCATION___),
                    null,
                    MapInterface
                );

                Client.LocationService.GetCurrentLocation(OnReceiveLocation, true);
                return;
            }

            if (!Client.NetworkingClient.MainNetworkExternal.QueryDirections(FromCoords.Value, ToCoords.Value, _top.SelectedProfile, OnNetQueryDirections))
            {
                MapInterface.MessageBox.ShowPopup(
                    Localize(LanguageData.ERROR),
                    string.Format(Localize(LanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED),
                    null,
                    MapInterface
                );

                return;
            }

            _queryCancelled = false;

            MapInterface.MessageBox.SetOkButtonText(Localize(LanguageData.CANCEL));
            MapInterface.MessageBox.ShowPopup(
                Localize(LanguageData.NAVIGATION),
                Localize(LanguageData.FINDING_AVAILABLE_ROUTES),
                OnPopupCallback,
                MapInterface
            );
            MapInterface.MessageBox.SetResult(PopupResult.CANCEL);
        }

        private void OnPopupCallback(Popup popup, PopupResult result)
        {
            if (result == PopupResult.CANCEL)
            {
                _queryCancelled = true;
            }
        }

        private void OnNetQueryDirections(PacketInStandardJSONResponse response)
        {
            if (_queryCancelled)
                return;

            MapInterface.MessageBox.SetResult(PopupResult.OK);
            MapInterface.MessageBox.HideScreen();

            _bottom.SetStartText(IsFromCurrentLocation ? Localize(LanguageData.START) : Localize(LanguageData.PREVIEW));
            IsPreviewStartMode = !IsFromCurrentLocation;
            IsFromCurrentLocation = false;

            Client.NavigationManager.SetCurrentDirections(response.Response, () =>
            {
                _bottom.SetDirections(Client.NavigationManager.CurrentDirections.Value);
                _bottom.Show();
            });
        }

        private void ChooseLocationManually(int idx)
        {
            _isManualLocating = true;

            _top.Hide();
            _autoComplete.SetAutoCompleteState(false);
            _bottom.ShowBackButton();

            MapInterface.Components.LocationOverlay.ChooseLocationOnMap((geo) =>
            {
                _isManualLocating = false;

                if (idx == 0)
                    FromCoords = geo;
                else
                    ToCoords = geo;

                (idx == 0 ? _top.FromInput : _top.ToInput).SetTextWithoutNotify($"[{geo.y:F5}, {geo.x:F5}]");
                _top.SetValidationState(idx, true);
                _top.Show(false);

                if (!CanQueryDirections())
                {
                    _instance._top.SetInputActive(idx == 0 ? 1 : 0);
                }
                else
                {
                    _instance.QueryDirections();
                    _autoComplete.SetAutoCompleteState(false);
                }
            });
        }

        private void Start()
        {
            Client.NavigationManager.StartNavigation(IsPreviewStartMode);

            _top.Hide();
            _bottom.Hide();

            //show UI?
            _navInterface.SetActive(true);
        }
    }
}