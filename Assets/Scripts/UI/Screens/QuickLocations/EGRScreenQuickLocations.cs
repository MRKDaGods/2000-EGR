using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public partial class EGRScreenQuickLocations : EGRScreen, IEGRScreenSupportsBackKey {
        RectTransform m_TopTransform;
        Button m_DragButton;
        Vector2 m_InitialOffsetMin;
        bool m_Expanded;
        float m_ExpansionProgress;
        RectTransform m_BodyTransform;
        LocationList m_LocationList;
        DetailedView m_DetailedView;
        TextMeshProUGUI m_NoLocationsLabel;
        Button m_FinishButton;
        bool m_IsChoosingLocation;
        int m_OldMapButtonsMask;

        static EGRScreenQuickLocations ms_Instance;
        static bool ms_HasImportedLocalLocations;
        static int ms_DesiredMapButtonMask;

        static EGRScreenQuickLocations() {
            ms_DesiredMapButtonMask = (1 << EGRScreenMapInterface.MapButtonIDs.CURRENT_LOCATION)
                | (1 << EGRScreenMapInterface.MapButtonIDs.SETTINGS);
        }

        protected override void OnScreenInit() {
            ms_Instance = this;

            m_TopTransform = (RectTransform)GetTransform("Top");

            m_DragButton = m_TopTransform.GetElement<Button>("Layout/Drag");
            m_DragButton.onClick.AddListener(OnDragClick);

            m_InitialOffsetMin = m_TopTransform.offsetMin;

            m_BodyTransform = (RectTransform)GetTransform("Body");

            m_LocationList = new LocationList((RectTransform)m_BodyTransform.Find("LocationList"));
            m_DetailedView = new DetailedView((RectTransform)m_BodyTransform.Find("DetailedView"));

            m_NoLocationsLabel = m_BodyTransform.GetElement<TextMeshProUGUI>("LocationList/Viewport/Content/NoLocs");

            m_LocationList.Other.GetElement<Button>("CurLoc").onClick.AddListener(AddLocationFromCurrentLocation);
            m_LocationList.Other.GetElement<Button>("Custom").onClick.AddListener(AddLocationFromCustom);

            m_FinishButton = GetElement<Button>("FinishButton");
            m_FinishButton.onClick.AddListener(OnFinishClick);
        }

        protected override void OnScreenShow() {
            Client.GlobeCamera.SetDistanceEased(5000f);

            Client.Runnable.RunLater(() => Client.GlobeCamera.SwitchToFlatMapExternal(() => {
                Client.FlatCamera.SetRotation(new Vector3(35f, 0f, 0f));
            }), 0.4f);

            EventManager.Register<EGREventScreenHideRequest>(OnScreenHideRequest);

            UpdateBodyVisibility();
            m_DetailedView.SetActive(false); //hide initially

            UpdateNoLocationLabelVisibility();
            UpdateFinishButtonVisibility();

            if (!ms_HasImportedLocalLocations) {
                ms_HasImportedLocalLocations = true;
                EGRQuickLocation.ImportLocalLocations(() => {
                    //called from a thread pool
                    Client.Runnable.RunOnMainThread(UpdateLocationListFromLocal);
                });
            }
            else
                UpdateLocationListFromLocal();

            ScreenManager.MapInterface.MapButtonsMask = ms_DesiredMapButtonMask;
            ScreenManager.MapInterface.ShowBackButton(false);
        }

        protected override void OnScreenHide() {
            Client.FlatCamera.SetRotation(Vector3.zero);
            EventManager.Unregister<EGREventScreenHideRequest>(OnScreenHideRequest);

            ScreenManager.MapInterface.ShowBackButton(true);
        }

        void OnScreenHideRequest(EGREventScreenHideRequest evt) {
            if (evt.Screen == ScreenManager.MapInterface) {
                HideScreen();
            }
        }

        void OnDragClick() {
            if (m_IsChoosingLocation)
                return;

            m_Expanded = !m_Expanded;
            UpdateMainView();
        }

        void UpdateMainView(bool? forcedState = null) {
            if (forcedState.HasValue) {
                m_Expanded = forcedState.Value;
            }

            EGRScreenMapInterface mapInterface = ScreenManager.MapInterface;
            if (m_Expanded) {
                if (!m_IsChoosingLocation)
                    m_OldMapButtonsMask = mapInterface.MapButtonsInteractivityMask;
                mapInterface.MapButtonsInteractivityMask = 0; //none?
            }
            else if (!m_IsChoosingLocation) {
                mapInterface.MapButtonsInteractivityMask = m_OldMapButtonsMask;
            }

            float targetProgress = m_Expanded ? 1f : 0f;
            Vector3 rotVec = new Vector3(0f, 0f, 180f);
            DOTween.To(() => m_ExpansionProgress, x => m_ExpansionProgress = x, targetProgress, 0.3f)
                .SetEase(Ease.OutSine)
                .OnUpdate(() => {
                    m_TopTransform.offsetMin = Vector2.Lerp(m_InitialOffsetMin, Vector2.zero, m_ExpansionProgress);
                    m_DragButton.transform.eulerAngles = Vector3.Lerp(Vector3.zero, rotVec, m_ExpansionProgress);
                }
            ).OnComplete(m_LocationList.UpdateOtherPosition);

            UpdateBodyVisibility();
        }

        void UpdateBodyVisibility() {
            m_BodyTransform.gameObject.SetActive(m_Expanded);
        }

        void OpenDetailedView(EGRQuickLocation location) {
            m_DetailedView.SetLocation(location);
            m_DetailedView.SetActive(true);
            m_DetailedView.AnimateIn();

            m_LocationList.SetActive(false);
        }

        void CloseDetailedView() {
            m_DetailedView.AnimateOut(() => m_DetailedView.SetActive(false));
            m_LocationList.SetActive(true);
        }

        void UpdateNoLocationLabelVisibility() {
            m_NoLocationsLabel.gameObject.SetActive(m_LocationList.ItemCount == 0);
        }

        void AddLocation(Vector2d coords) {
            EGRPopupConfirmation conf = ScreenManager.GetPopup<EGRPopupConfirmation>();
            conf.SetYesButtonText(Localize(EGRLanguageData.ADD));
            conf.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
            conf.ShowPopup(
                Localize(EGRLanguageData.QUICK_LOCATIONS),
                string.Format(Localize(EGRLanguageData.ADD_CURRENT_LOCATION____0__), coords),
                (_, res) => {
                    if (res == EGRPopupResult.YES) {
                        EGRPopupInputText input = ScreenManager.GetPopup<EGRPopupInputText>();
                        input.ShowPopup(
                            Localize(EGRLanguageData.QUICK_LOCATIONS),
                            Localize(EGRLanguageData.ENTER_LOCATION_NAME),
                            (_, _res) => {
                                EGRQuickLocation.Add(input.Input, coords);
                                UpdateLocationListFromLocal();
                            },
                            this
                        );
                    }
                },
                this
            );
        }

        void AddLocationFromCurrentLocation() {
            Client.LocationService.GetCurrentLocation((success, coords, bearing) => {
                if (success) {
                    AddLocation(coords.Value);
                }
            });
        }

        void AddLocationFromCustom() {
            m_IsChoosingLocation = true;
            UpdateFinishButtonVisibility();

            UpdateMainView(false);

            ScreenManager.MapInterface.Components.LocationOverlay.ChooseLocationOnMap((coords) => {
                AddLocation(coords);
            });
        }

        public void UpdateLocationListFromLocal() {
            m_LocationList.SetLocations(EGRQuickLocation.Locations);
            UpdateNoLocationLabelVisibility();
        }

        void OnFinishClick() {
            if (!m_IsChoosingLocation)
                return;

            UpdateMainView(true);

            m_IsChoosingLocation = false;
            UpdateFinishButtonVisibility();

            ScreenManager.MapInterface.Components.LocationOverlay.Finish();
        }

        void UpdateFinishButtonVisibility() {
            m_FinishButton.gameObject.SetActive(m_IsChoosingLocation);
        }

        public void OnBackKeyDown() {
            if (m_DetailedView.IsActive) {
                m_DetailedView.Close();
                return;
            }

            if (m_Expanded) {
                UpdateMainView(false);
                return;
            }

            //TODO: hide map interface as well?
            HideScreen();
        }
    }
}

