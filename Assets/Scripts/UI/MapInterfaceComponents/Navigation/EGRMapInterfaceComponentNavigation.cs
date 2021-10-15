using MRK.Networking.Packets;
using UnityEngine;
using static MRK.EGRLanguageManager;

namespace MRK.UI.MapInterface {
    public partial class EGRMapInterfaceComponentNavigation : EGRMapInterfaceComponent {
        Transform m_NavigationTransform;
        Top m_Top;
        Bottom m_Bottom;
        AutoComplete m_AutoComplete;
        NavInterface m_NavInterface;
        bool m_QueryCancelled;
        bool m_IsManualLocating;

        static EGRMapInterfaceComponentNavigation ms_Instance;

        public override EGRMapInterfaceComponentType ComponentType => EGRMapInterfaceComponentType.Navigation;
        public bool IsActive { get; private set; }
        Vector2d? FromCoords { get; set; }
        Vector2d? ToCoords { get; set; }
        bool IsFromCurrentLocation { get; set; }
        bool IsPreviewStartMode { get; set; }
        public NavInterface NavigationInterface => m_NavInterface;

        public override void OnComponentInit(EGRScreenMapInterface mapInterface) {
            base.OnComponentInit(mapInterface);

            ms_Instance = this;

            m_NavigationTransform = mapInterface.transform.Find("Navigation");
            m_NavigationTransform.gameObject.SetActive(false);

            m_Top = new Top((RectTransform)m_NavigationTransform.Find("Top"));
            m_Bottom = new Bottom((RectTransform)m_NavigationTransform.Find("Bot"));

            m_AutoComplete = new AutoComplete((RectTransform)m_NavigationTransform.Find("Top/AutoComplete"));
            m_AutoComplete.SetAutoCompleteState(false);

            m_NavInterface = new NavInterface((RectTransform)m_NavigationTransform.Find("NavInterface"));
            m_NavInterface.SetActive(false); //hide by def
        }

        public override void OnComponentUpdate() {
            m_Bottom.Update();

            if (m_AutoComplete.IsActive) {
                m_AutoComplete.Update();
            }
        }

        public void Show() {
            m_NavigationTransform.gameObject.SetActive(true);
            m_Top.Show();

            Client.Runnable.RunLater(m_Bottom.ShowBackButton, 0.5f);
            MapInterface.Components.MapButtons.RemoveAllButtons();

            IsActive = true;
            FromCoords = ToCoords = null;
            IsFromCurrentLocation = false;
        }

        public bool Hide() {
            m_Top.Hide();
            m_Bottom.Hide();
            m_NavInterface.SetActive(false);

            if (!m_IsManualLocating) {
                Client.Runnable.RunLater(() => {
                    m_NavigationTransform.gameObject.SetActive(false);
                    m_Bottom.ClearDirections();

                    Client.NavigationManager.ExitNavigation();
                    Client.FlatCamera.ExitNavigation();

                    IsActive = false;
                }, 0.3f);

                MapInterface.RegenerateMapButtons();

                return true;
            }
            else {
                MapInterface.Components.LocationOverlay.Finish();
                return false;
            }
        }

        bool CanQueryDirections() {
            return !string.IsNullOrEmpty(m_Top.From) && !string.IsNullOrWhiteSpace(m_Top.To)
                && (FromCoords.HasValue || IsFromCurrentLocation) && ToCoords.HasValue;
        }

        void OnReceiveLocation(bool success, Vector2d? coords, float? bearing) {
            Client.Runnable.RunLater(() => {
                MapInterface.MessageBox.HideScreen(() => {
                    if (!success) {
                        MapInterface.MessageBox.ShowPopup(
                            Localize(EGRLanguageData.EGR),
                            Localize(EGRLanguageData.CANNOT_OBTAIN_CURRENT_LOCATION),
                            null,
                            MapInterface
                        );
                        return;
                    }

                    FromCoords = coords.Value;
                    QueryDirections(true);
                }, 1.1f);
            }, 0.4f);
        }

        void QueryDirections(bool ignoreCurrentLocation = false) {
            if (!CanQueryDirections())
                return;

            if (!ignoreCurrentLocation && IsFromCurrentLocation) {
                //get cur loc
                MapInterface.MessageBox.ShowButton(false);
                MapInterface.MessageBox.ShowPopup(
                    Localize(EGRLanguageData.EGR),
                    Localize(EGRLanguageData.RETRIEVING_CURRENT_LOCATION___),
                    null,
                    MapInterface
                );

                Client.LocationService.GetCurrentLocation(OnReceiveLocation, true);
                return;
            }

            if (!Client.NetworkingClient.MainNetworkExternal.QueryDirections(FromCoords.Value, ToCoords.Value, m_Top.SelectedProfile, OnNetQueryDirections)) {
                MapInterface.MessageBox.ShowPopup(
                    Localize(EGRLanguageData.ERROR),
                    string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED),
                    null,
                    MapInterface
                );

                return;
            }

            m_QueryCancelled = false;

            MapInterface.MessageBox.SetOkButtonText(Localize(EGRLanguageData.CANCEL));
            MapInterface.MessageBox.ShowPopup(
                Localize(EGRLanguageData.NAVIGATION),
                Localize(EGRLanguageData.FINDING_AVAILABLE_ROUTES),
                OnPopupCallback,
                MapInterface
            );
            MapInterface.MessageBox.SetResult(EGRPopupResult.CANCEL);
        }

        void OnPopupCallback(EGRPopup popup, EGRPopupResult result) {
            if (result == EGRPopupResult.CANCEL) {
                m_QueryCancelled = true;
            }
        }

        void OnNetQueryDirections(PacketInStandardJSONResponse response) {
            if (m_QueryCancelled)
                return;

            MapInterface.MessageBox.SetResult(EGRPopupResult.OK);
            MapInterface.MessageBox.HideScreen();

            m_Bottom.SetStartText(IsFromCurrentLocation ? Localize(EGRLanguageData.START) : Localize(EGRLanguageData.PREVIEW));
            IsPreviewStartMode = !IsFromCurrentLocation;
            IsFromCurrentLocation = false;

            Client.NavigationManager.SetCurrentDirections(response.Response, () => {
                m_Bottom.SetDirections(Client.NavigationManager.CurrentDirections.Value);
                m_Bottom.Show();
            });
        }

        void ChooseLocationManually(int idx) {
            m_IsManualLocating = true;

            m_Top.Hide();
            m_AutoComplete.SetAutoCompleteState(false);
            m_Bottom.ShowBackButton();

            MapInterface.Components.LocationOverlay.ChooseLocationOnMap((geo) => {
                m_IsManualLocating = false;

                if (idx == 0)
                    FromCoords = geo;
                else
                    ToCoords = geo;

                (idx == 0 ? m_Top.FromInput : m_Top.ToInput).SetTextWithoutNotify($"[{geo.y:F5}, {geo.x:F5}]");
                m_Top.SetValidationState(idx, true);
                m_Top.Show(false);

                if (!CanQueryDirections()) {
                    ms_Instance.m_Top.SetInputActive(idx == 0 ? 1 : 0);
                }
                else {
                    ms_Instance.QueryDirections();
                    m_AutoComplete.SetAutoCompleteState(false);
                }
            });
        }

        void Start() {
            Client.NavigationManager.StartNavigation(IsPreviewStartMode);

            m_Top.Hide();
            m_Bottom.Hide();

            //show UI?
            m_NavInterface.SetActive(true);
        }
    }
}