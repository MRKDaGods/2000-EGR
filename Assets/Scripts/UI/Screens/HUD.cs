using Coffee.UIEffects;
using DG.Tweening;
using MRK.Cameras;
using MRK.InputControllers;
using MRK.UI.MapInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_MapInterface;

namespace MRK.UI
{
    [Serializable]
    public class MapInterfaceResources
    {
        public GameObject CurrentLocationSprite;
        public AnimationCurve CurrentLocationScaleCurve;
        public Image LocationPinSprite;
    }

    public class HUD : AnimatedAlpha
    {
        [Serializable]
        private struct MarkerSprite
        {
            public EGRPlaceType Type;
            public Sprite Sprite;
        }

        //deprecated
        public static class MapButtonIDs
        {
            public static int CURRENT_LOCATION = 0;
            public static int HOTTEST_TRENDS = 1;
            public static int SETTINGS = 2;
            public static int NAVIGATION = 3;
            public static int BACK_TO_EARTH = 4;
            public static int SPACE_FOV = 5;
            public static int MAX = 6;

            public static int ALL_MASK = EGRUtils.FillBits(MAX);
        }

        private Map _map;
        private TextMeshProUGUI _camDistLabel;
        [SerializeField]
        private GameObject _spaceLabelsRoot;
        [SerializeField]
        private TextMeshPro _contextLabel;
        [SerializeField]
        private TextMeshPro _timeLabel;
        [SerializeField]
        private TextMeshPro _distLabel;
        [SerializeField]
        private GameObject _mapButtonPrefab;
        private float _lastTimeUpdate;
        [SerializeField]
        private AnimationCurve _markerScaleCurve;
        [SerializeField]
        private AnimationCurve _markerOpacityCurve;
        private RawImage _transitionImg;
        [SerializeField]
        private MarkerSprite[] _markerSprites;
        private bool _mouseDown;
        private Vector3 _mouseDownPos;
        private bool _zoomHasChanged;
        private Dictionary<Transform, TextMeshPro> _planetNames;
        [SerializeField]
        private PlaceMarkersResources _placeMarkersResources;
        [SerializeField]
        private MapInterfaceResources _mapInterfaceResources;
        [SerializeField]
        private MapButtonInfo[] _mapButtonsInfo;
        private bool _mapButtonsEnabled;
        private Button _backButton;

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
                return 0x00000000;
            }
        }

        private BaseCamera m_EGRCamera
        {
            get
            {
                return EGR.Instance.ActiveEGRCamera;
            }
        }

        public string ContextText
        {
            get
            {
                return _contextLabel.text;
            }
        }

        public bool IsInTransition
        {
            get
            {
                return _transitionImg.gameObject.activeInHierarchy;
            }
        }

        public Transform ObservedTransform
        {
            get; set;
        }

        public bool ObservedTransformDirty
        {
            get; set;
        }

        public Transform ScalebarParent
        {
            get; private set;
        }

        public PlaceMarkersResources PlaceMarkersResources
        {
            get
            {
                return _placeMarkersResources;
            }
        }

        public MapInterfaceResources MapInterfaceResources
        {
            get
            {
                return _mapInterfaceResources;
            }
        }

        public bool MapButtonsEnabled
        {
            get
            {
                return _mapButtonsEnabled;
            }
            set
            {
                if (_mapButtonsEnabled != value)
                {
                    _mapButtonsEnabled = value;
                    RegenerateMapButtons();
                }
            }
        }

        public int MapButtonsMask
        {
            get; set;
        }

        public int MapButtonsInteractivityMask
        {
            get; set;
        }

        public ComponentCollection Components
        {
            get; private set;
        }

        public HUD()
        {
            MapButtonsMask = MapButtonIDs.ALL_MASK;
            MapButtonsInteractivityMask = MapButtonIDs.ALL_MASK;

            Components = new ComponentCollection();
        }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            _map = Client.FlatMap;
            _map.gameObject.SetActive(false);

            _backButton = GetElement<Button>(Buttons.Back);
            _backButton.onClick.AddListener(OnBackClick);
            _mapButtonPrefab.SetActive(false); //disable our template button

            _camDistLabel = GetElement<TextMeshProUGUI>(Labels.CamDist);
            _transitionImg = GetElement<RawImage>(Images.Transition);
            _transitionImg.gameObject.SetActive(false);

            ScalebarParent = GetTransform(Others.DistProg);

            ObservedTransform = Client.GlobalMap.transform;

            RegisterInterfaceComponent(ComponentType.PlaceMarkers, new PlaceMarkers());
            RegisterInterfaceComponent(ComponentType.ScaleBar, new ScaleBar());
            RegisterInterfaceComponent(ComponentType.Navigation, new MapInterface.Navigation());
            RegisterInterfaceComponent(ComponentType.LocationOverlay, new LocationOverlay());
            RegisterInterfaceComponent(ComponentType.MapButtons, new MapButtons(_mapButtonsInfo));
        }

        public void OnInterfaceEarlyShow()
        {
            m_EGRCamera.SetInterfaceState(true);

            _spaceLabelsRoot.SetActive(true);
            _contextLabel.gameObject.SetActive(true);
            _timeLabel.gameObject.SetActive(Settings.ShowTime);
            _distLabel.gameObject.SetActive(Settings.ShowDistance);

            UpdateTime();
        }

        protected override void OnScreenShow()
        {
            _mapButtonsEnabled = true;

            //hide bg since it's only for designing
            GetElement<Image>(Images.BaseBg).gameObject.SetActive(false);

            _map.MapUpdated += OnMapUpdated;
            _map.MapFullyUpdated += OnMapFullyUpdated;
            _map.MapZoomUpdated += OnMapZoomUpdated;

            Client.RegisterMapModeDelegate(OnMapModeChanged);
            Client.RegisterControllerReceiver(OnControllerMessageReceived);

            //map mode might've changed when visible=false
            OnMapModeChanged(Client.MapMode);

            /*if (m_PlanetNames == null) {
                m_PlanetNames = new Dictionary<Transform, TextMeshPro>();

                foreach (Transform planet in Client.Planets) {
                    TextMeshPro txt = planet.Find("Name").GetComponent<TextMeshPro>();
                    txt.gameObject.SetActive(false);
                    m_PlanetNames[planet] = txt;
                }
            }*/

            Client.DisableAllScreensExcept<HUD>();

            Components.OnComponentsShow();
        }

        protected override void OnScreenHide()
        {
            _map.MapUpdated -= OnMapUpdated;
            _map.MapFullyUpdated -= OnMapFullyUpdated;
            _map.MapZoomUpdated -= OnMapZoomUpdated;

            Client.UnregisterMapModeDelegate(OnMapModeChanged);
            Client.UnregisterControllerReceiver(OnControllerMessageReceived);

            //copied to direct hidescreen
            //ScreenManager.MainScreen.ShowScreen();

            Client.SetPostProcessState(false);

            Components.OnComponentsHide();

            //reset map button mask
            MapButtonsMask = MapButtonIDs.ALL_MASK;
            MapButtonsInteractivityMask = MapButtonIDs.ALL_MASK;

            ShowBackButton(true);
        }

        protected override void OnScreenUpdate()
        {
            if (Time.time - _lastTimeUpdate >= 60f)
            {
                UpdateTime();
            }

            Components.OnComponentsUpdate();
        }

        public void ShowBackButton(bool show)
        {
            _backButton.gameObject.SetActive(show);
        }

        private void RegisterInterfaceComponent(ComponentType type, MapInterface.Component component)
        {
            Components[type] = component;
            component.OnComponentInit(this);
        }

        private void OnMapModeChanged(EGRMapMode mode)
        {
            _camDistLabel.gameObject.SetActive(/*isGlobe*/false);
            Components.ScaleBar.SetActive(mode == EGRMapMode.Flat);
            Client.ActiveEGRCamera.ResetStates();

            //from globe to flat
            if (mode == EGRMapMode.Flat && Client.PreviousMapMode == EGRMapMode.Globe)
            {
                Client.FlatCamera.UpdateMapViewingAngles(null, 0f);
            }

            if (Visible)
            {
                _spaceLabelsRoot.SetActive(mode == EGRMapMode.Globe);
            }

            RegenerateMapButtons();
        }

        public void RegenerateMapButtons()
        {
            //remove all buttons anyway
            Components.MapButtons.RemoveAllButtons();

            if (MapButtonsEnabled)
            {
                HashSet<MapButtonID> ids = HashSetPool<MapButtonID>.Default.Rent();

                ids.Add(MapButtonID.Settings);
                ids.Add(MapButtonID.CurrentLocation);

                if (Client.MapMode == EGRMapMode.Flat)
                {
                    ids.Add(MapButtonID.Trending);
                    ids.Add(MapButtonID.Navigation);
                    ids.Add(MapButtonID.BackToEarth);
                }
                else
                {
                    //FOV hidden till further notice
                    //ids.Add(EGRUIMapButtonID.FieldOfView);
                }

                Components.MapButtons.SetButtons(MapButtonGroupAlignment.BottomRight, ids);

                if (Client.MapMode == EGRMapMode.Globe && ObservedTransform != Client.GlobalMap.transform)
                {
                    ids.Clear();
                    ids.Add(MapButtonID.BackToEarth);
                    Components.MapButtons.SetButtons(MapButtonGroupAlignment.BottomCenter, ids);
                    Components.MapButtons.SetGroupExpansionState(MapButtonGroupAlignment.BottomCenter, true);
                }

                HashSetPool<MapButtonID>.Default.Free(ids);
            }
        }

        private void OnControllerMessageReceived(Message msg)
        {
            if (Client.MapMode != EGRMapMode.Globe)
                return;

            if (!m_EGRCamera.ShouldProcessControllerMessage(msg))
                return;

            if (msg.ContextualKind == MessageContextualKind.Mouse)
            {
                MouseEventKind kind = (MouseEventKind)msg.Payload[0];

                switch (kind)
                {
                    case MouseEventKind.Down:
                        _mouseDown = true;
                        _mouseDownPos = (Vector3)msg.Payload[3];
                        break;

                    case MouseEventKind.Up:
                        if (_mouseDown)
                        {
                            _mouseDown = false;

                            Vector3 pos = (Vector3)msg.Payload[1];
                            if ((pos - _mouseDownPos).sqrMagnitude < 9f)
                                ChangeObservedTransform((Vector3)msg.Payload[1]);
                        }
                        break;
                }
            }
        }

        public void SetObservedTransformNameState(bool active)
        {
            if (ObservedTransform != Client.GlobalMap.transform)
            {
                //TextMeshPro txt = m_PlanetNames[ObservedTransform];
                //txt.gameObject.SetActive(active);

                if (active)
                {
                    //StartCoroutine(EGRUtils.SetTextEnumerator(x => txt.text = x, txt.text, 0.3f, ""));
                }
            }
        }

        private void ChangeObservedTransform(Vector3 pos)
        {
            Ray ray = Client.ActiveCamera.ScreenPointToRay(pos);

            //simulate physics
            Physics.Simulate(0.1f);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Client.ActiveCamera.farClipPlane, 1 << 6, QueryTriggerInteraction.Collide))
            {
                if (hit.transform != ObservedTransform)
                {
                    SetObservedTransformNameState(false);

                    ObservedTransform = hit.transform;
                    ObservedTransformDirty = true;

                    SetObservedTransformNameState(true);
                    OnObservedTransformChanged();
                }
            }
        }

        public bool IsObservedTransformEarth()
        {
            return ObservedTransform == Client.GlobalMap.transform;
        }

        private IEnumerator WaitForCameraLockRelease(Action callback)
        {
            if (callback == null)
                yield break;

            //TODO: check if map mode changes while waiting?
            while (Client.GlobeCamera.IsLocked)
                yield return new WaitForEndOfFrame();

            callback();
        }

        public void SetObservedTransformToEarth(Action callback = null)
        {
            if (!IsObservedTransformEarth())
            {
                SetObservedTransformNameState(false);
                ObservedTransform = Client.GlobalMap.transform;
                ObservedTransformDirty = true;

                if (callback != null)
                {
                    Client.Runnable.Run(WaitForCameraLockRelease(callback));
                }
            }
        }

        public void OnObservedTransformChanged()
        {
            Components.MapButtons.ShrinkOtherGroups(null);

            if (IsObservedTransformEarth())
            {
                Components.MapButtons.RemoveButton(MapButtonGroupAlignment.BottomCenter, MapButtonID.BackToEarth);

                //eyad: hide all map buttons when not in earth
                RegenerateMapButtons();
            }
            else
            {
                Components.MapButtons.AddButton(MapButtonGroupAlignment.BottomCenter, MapButtonID.BackToEarth, expand: true);
                Components.MapButtons.SetButtons(MapButtonGroupAlignment.BottomRight, null);
            }
        }

        public void SetDistanceText(string txt, bool animated = false)
        {
            if (_distLabel.gameObject.activeInHierarchy)
            {
                if (animated)
                    StartCoroutine(EGRUtils.SetTextEnumerator(x => _distLabel.text = x, txt, 0.9f, "m"));
                else
                    _distLabel.text = txt;
            }
        }

        public void SetContextText(string txt)
        {
            Client.StartCoroutine(EGRUtils.SetTextEnumerator(x => _contextLabel.text = x, txt, 0.7f, "\n"));
        }

        private void UpdateTime()
        {
            _lastTimeUpdate = Time.time;
            Client.StartCoroutine(EGRUtils.SetTextEnumerator(x => _timeLabel.text = x, DateTime.Now.ToString("HH:mm"), 1f, ":"));
        }

        public void SetTransitionTex(RenderTexture rt, TweenCallback callback = null, float speed = 0.6f)
        {
            _transitionImg.texture = rt;
            _transitionImg.gameObject.SetActive(true);

            _transitionImg.DOColor(Color.white.AlterAlpha(0f), speed)
                .ChangeStartValue(Color.white.AlterAlpha(1f))
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    _transitionImg.gameObject.SetActive(false);
                });

            UIDissolve dis = _transitionImg.GetComponent<UIDissolve>();
            DOTween.To(() => dis.effectFactor, x => dis.effectFactor = x, 1f, speed)
                .SetEase(Ease.OutSine)
                .ChangeStartValue(0f)
                .OnComplete(callback);
        }

        public void OnBackClick()
        {
            m_EGRCamera.SetInterfaceState(false);
            SetObservedTransformToEarth();
            HideScreen();

            _spaceLabelsRoot.SetActive(false);
            ScreenManager.MainScreen.ShowScreen();
        }

        public void ExternalForceHide()
        {
            m_EGRCamera.SetInterfaceState(false);
            SetObservedTransformToEarth();
            ForceHideScreen(true);

            _spaceLabelsRoot.SetActive(false);
            ScreenManager.MainScreen.ShowScreen();
        }

        private void OnMapUpdated()
        {
            if (Client.MapMode != EGRMapMode.Flat)
                return;

            Components.OnMapUpdated();
        }

        private void OnMapZoomUpdated(int oldZoom, int newZoom)
        {
            if (_transitionImg.gameObject.activeInHierarchy)
                return;

            //Debug.Log($"Zoom updated {oldZoom} -> {newZoom}");
            _zoomHasChanged = true;
        }

        private void OnMapFullyUpdated()
        {
            if (_zoomHasChanged)
            {
                _zoomHasChanged = false;
                //SetTransitionTex(Client.CaptureScreenBuffer());
            }

            Components.OnMapFullyUpdated();
        }

        public void Warmup()
        {
            Components.OnComponentsWarmUp();
            ScreenManager.GetScreen<PlaceGroup>().Warmup();
        }

        public float EvaluateMarkerScale(float time)
        {
            return _markerScaleCurve.Evaluate(time);
        }

        public float EvaluateMarkerOpacity(float time)
        {
            return _markerOpacityCurve.Evaluate(time);
        }

        public Sprite GetSpriteForPlaceType(EGRPlaceType type)
        {
            foreach (MarkerSprite ms in _markerSprites)
            {
                if (ms.Type == type)
                    return ms.Sprite;
            }

            return _markerSprites[0].Sprite; //NONE
        }
    }
}