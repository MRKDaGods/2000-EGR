using DG.Tweening;
using MRK.Events;
using MRK.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public partial class QuickLocations : Screen, ISupportsBackKey
    {
        private RectTransform _topTransform;
        private Button _dragButton;
        private Vector2 _initialOffsetMin;
        private bool _expanded;
        private float _expansionProgress;
        private RectTransform _bodyTransform;
        private LocationList _locationList;
        private DetailedView _detailedView;
        private TextMeshProUGUI _noLocationsLabel;
        private Button _finishButton;
        private bool _isChoosingLocation;
        private int _oldMapButtonsMask;

        private static QuickLocations _instance;
        private static bool _hasImportedLocalLocations;
        private static int _desiredMapButtonMask;

        static QuickLocations()
        {
            _desiredMapButtonMask = (1 << HUD.MapButtonIDs.CURRENT_LOCATION)
                | (1 << HUD.MapButtonIDs.SETTINGS);
        }

        protected override void OnScreenInit()
        {
            _instance = this;

            _topTransform = (RectTransform)GetTransform("Top");

            _dragButton = _topTransform.GetElement<Button>("Layout/Drag");
            _dragButton.onClick.AddListener(OnDragClick);

            _initialOffsetMin = _topTransform.offsetMin;

            _bodyTransform = (RectTransform)GetTransform("Body");

            _locationList = new LocationList((RectTransform)_bodyTransform.Find("LocationList"));
            _detailedView = new DetailedView((RectTransform)_bodyTransform.Find("DetailedView"));

            _noLocationsLabel = _bodyTransform.GetElement<TextMeshProUGUI>("LocationList/Viewport/Content/NoLocs");

            _locationList.Other.GetElement<Button>("CurLoc").onClick.AddListener(AddLocationFromCurrentLocation);
            _locationList.Other.GetElement<Button>("Custom").onClick.AddListener(AddLocationFromCustom);

            _finishButton = GetElement<Button>("FinishButton");
            _finishButton.onClick.AddListener(OnFinishClick);
        }

        protected override void OnScreenShow()
        {
            Client.GlobeCamera.SetDistanceEased(5000f);

            Client.Runnable.RunLater(() => Client.GlobeCamera.SwitchToFlatMapExternal(() =>
            {
                Client.FlatCamera.SetRotation(new Vector3(35f, 0f, 0f));
            }), 0.4f);

            EventManager.Register<ScreenHideRequest>(OnScreenHideRequest);

            UpdateBodyVisibility();
            _detailedView.SetActive(false); //hide initially

            UpdateNoLocationLabelVisibility();
            UpdateFinishButtonVisibility();

            if (!_hasImportedLocalLocations)
            {
                _hasImportedLocalLocations = true;
                EGRQuickLocation.ImportLocalLocations(() =>
                {
                    //called from a thread pool
                    Client.Runnable.RunOnMainThread(UpdateLocationListFromLocal);
                });
            }
            else
            {
                UpdateLocationListFromLocal();
            }

            ScreenManager.MapInterface.MapButtonsMask = _desiredMapButtonMask;
            ScreenManager.MapInterface.ShowBackButton(false);
        }

        protected override void OnScreenHide()
        {
            Client.FlatCamera.SetRotation(Vector3.zero);
            EventManager.Unregister<ScreenHideRequest>(OnScreenHideRequest);

            ScreenManager.MapInterface.ShowBackButton(true);
        }

        private void OnScreenHideRequest(ScreenHideRequest evt)
        {
            if (evt.Screen == ScreenManager.MapInterface)
            {
                HideScreen();
            }
        }

        private void OnDragClick()
        {
            if (_isChoosingLocation)
                return;

            _expanded = !_expanded;
            UpdateMainView();
        }

        private void UpdateMainView(bool? forcedState = null)
        {
            if (forcedState.HasValue)
            {
                _expanded = forcedState.Value;
            }

            HUD mapInterface = ScreenManager.MapInterface;
            if (_expanded)
            {
                if (!_isChoosingLocation)
                    _oldMapButtonsMask = mapInterface.MapButtonsInteractivityMask;
                mapInterface.MapButtonsInteractivityMask = 0; //none?
            }
            else if (!_isChoosingLocation)
            {
                mapInterface.MapButtonsInteractivityMask = _oldMapButtonsMask;
            }

            float targetProgress = _expanded ? 1f : 0f;
            Vector3 rotVec = new Vector3(0f, 0f, 180f);
            DOTween.To(() => _expansionProgress, x => _expansionProgress = x, targetProgress, 0.3f)
                .SetEase(Ease.OutSine)
                .OnUpdate(() =>
                {
                    _topTransform.offsetMin = Vector2.Lerp(_initialOffsetMin, Vector2.zero, _expansionProgress);
                    _dragButton.transform.eulerAngles = Vector3.Lerp(Vector3.zero, rotVec, _expansionProgress);
                }
            ).OnComplete(_locationList.UpdateOtherPosition);

            UpdateBodyVisibility();
        }

        private void UpdateBodyVisibility()
        {
            _bodyTransform.gameObject.SetActive(_expanded);
        }

        private void OpenDetailedView(EGRQuickLocation location)
        {
            _detailedView.SetLocation(location);
            _detailedView.SetActive(true);
            _detailedView.AnimateIn();

            _locationList.SetActive(false);
        }

        private void CloseDetailedView()
        {
            _detailedView.AnimateOut(() => _detailedView.SetActive(false));
            _locationList.SetActive(true);
        }

        private void UpdateNoLocationLabelVisibility()
        {
            _noLocationsLabel.gameObject.SetActive(_locationList.ItemCount == 0);
        }

        private void AddLocation(Vector2d coords)
        {
            Confirmation conf = ScreenManager.GetPopup<Confirmation>();
            conf.SetYesButtonText(Localize(LanguageData.ADD));
            conf.SetNoButtonText(Localize(LanguageData.CANCEL));
            conf.ShowPopup(
                Localize(LanguageData.QUICK_LOCATIONS),
                string.Format(Localize(LanguageData.ADD_CURRENT_LOCATION____0__), coords),
                (_, res) =>
                {
                    if (res == PopupResult.YES)
                    {
                        InputText input = ScreenManager.GetPopup<InputText>();
                        input.ShowPopup(
                            Localize(LanguageData.QUICK_LOCATIONS),
                            Localize(LanguageData.ENTER_LOCATION_NAME),
                            (_, _res) =>
                            {
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

        private void AddLocationFromCurrentLocation()
        {
            Client.LocationService.GetCurrentLocation((success, coords, bearing) =>
            {
                if (success)
                {
                    AddLocation(coords.Value);
                }
            });
        }

        private void AddLocationFromCustom()
        {
            _isChoosingLocation = true;
            UpdateFinishButtonVisibility();

            UpdateMainView(false);

            ScreenManager.MapInterface.Components.LocationOverlay.ChooseLocationOnMap((coords) =>
            {
                AddLocation(coords);
            });
        }

        public void UpdateLocationListFromLocal()
        {
            _locationList.SetLocations(EGRQuickLocation.Locations);
            UpdateNoLocationLabelVisibility();
        }

        private void OnFinishClick()
        {
            if (!_isChoosingLocation)
                return;

            UpdateMainView(true);

            _isChoosingLocation = false;
            UpdateFinishButtonVisibility();

            ScreenManager.MapInterface.Components.LocationOverlay.Finish();
        }

        private void UpdateFinishButtonVisibility()
        {
            _finishButton.gameObject.SetActive(_isChoosingLocation);
        }

        public void OnBackKeyDown()
        {
            if (_detailedView.IsActive)
            {
                _detailedView.Close();
                return;
            }

            if (_expanded)
            {
                UpdateMainView(false);
                return;
            }

            //TODO: hide map interface as well?
            HideScreen();
        }
    }
}

