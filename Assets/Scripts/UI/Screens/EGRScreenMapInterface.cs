using Coffee.UIEffects;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_MapInterface;
using System.Linq;
using MRK.GeoJson;

namespace MRK.UI {
    [Serializable]
    public class EGRMapInterfaceResources {
        public Image CurrentLocationSprite;
        public AnimationCurve CurrentLocationScaleCurve;
        public Image LocationPinSprite;
    }

    public class EGRScreenMapInterface : EGRScreen {
        class MapButton {
            enum MapButtonUpdate {
                None = 1 << 0,
                Position = 1 << 1,
                Opacity = 1 << 2,
                Label = 1 << 3,
                Size = 1 << 4
            }

            const float ACTIVE_SIZE_MOD = 1.6f;

            bool m_State;
            MapButtonUpdate m_QueuedUpdates;
            Vector2 m_InitialPosition;
            Vector2 m_ActivePosition;
            readonly float[] m_Delta;
            readonly float m_InitialSize;
            readonly Color m_InitialColor;
            readonly TextMeshProUGUI m_Text;
            Vector2 m_InitialTextPosition;
            Vector2 m_ActiveTextPosition;
            readonly bool[] m_Dirty;
            readonly MapButtonInfo m_Info;
            bool m_InvokedLateInit;
            readonly int m_Index;
            float m_LastChangeTime;

            static Transform ms_TemplateTransform;
            static EGRScreenMapInterface ms_Owner;
            static TextMeshProUGUI ms_TextTemplate;

            public Button Button { get; private set; }
            public Image Image { get; private set; }
            public TextMeshProUGUI Text => m_Text;
            Vector2 m_PreferredPosition => m_State ? m_ActivePosition : m_InitialPosition;
            Vector2 m_InversePosition => m_State ? m_InitialPosition : m_ActivePosition;
            float m_PreferredSize => m_InitialSize * (m_State ? ACTIVE_SIZE_MOD : 1f);
            float m_InverseSize => m_InitialSize * (m_State ? 1f : ACTIVE_SIZE_MOD);
            Color m_PreferredColor => m_InitialColor.AlterAlpha(m_State ? 1f : 0.5f);
            Color m_InverseColor => m_InitialColor.AlterAlpha(m_State ? 0.5f : 1f);
            Vector2 m_PreferredTextPosition => m_State ? m_ActiveTextPosition : m_InitialTextPosition;
            Vector2 m_InverseTextPosition => m_State ? m_InitialTextPosition : m_ActiveTextPosition;
            public MapButtonInfo Info => m_Info;

            public MapButton(EGRScreenMapInterface owner, int idx, MapButtonInfo info, Tuple<GameObject, TextMeshProUGUI> pooled) {
                m_Info = info;
                m_InvokedLateInit = false;
                m_Index = idx;

                if (ms_TemplateTransform == null) {
                    ms_TemplateTransform = owner.GetTransform(Others.MapActiveTemplate);
                }

                if (ms_Owner == null) {
                    ms_Owner = owner;
                }

                if (ms_TextTemplate == null) {
                    ms_TextTemplate = owner.GetElement<TextMeshProUGUI>(Labels.MapDsc);
                }

                if (pooled != null) {
                    Button = pooled.Item1.GetComponent<Button>();
                    Button.onClick.RemoveAllListeners();
                }
                else {
                    Button = Instantiate(owner.m_MapButtonPrefab, owner.m_MapButtonPrefab.transform.parent).GetComponent<Button>();
                }

                Button.onClick.AddListener(() => OnButtonClick(true));
                Button.gameObject.SetActive(true);

                Image = Button.GetComponentInChildren<Image>();
                Image.color = m_InitialColor = Image.color.AlterAlpha(0.5f);
                Image.sprite = info.Sprite;

                m_InitialSize = Image.rectTransform.sizeDelta.x; //w=h
                ms_Owner.SetPositionersSize(m_InitialSize, m_InitialSize * ACTIVE_SIZE_MOD);

                //float scaledSpace = 140f.ScaleX();
                //float totalSpace = (owner.m_MapButtons.Length - 1) * scaledSpace + owner.m_MapButtons.Length * Image.rectTransform.sizeDelta.x * ACTIVE_SIZE_MOD; //modified sz
                //float x = ms_TemplateTransform.position.x - totalSpace / 4f + idx * totalSpace / owner.m_MapButtons.Length;
                //m_ActivePosition = new Vector2(ms_Owner.m_ActiveButtonPositioner.GetWorldPositionX(idx), ms_TemplateTransform.position.y);

                m_Delta = new float[4];

                m_Text = pooled != null ? pooled.Item2 : Instantiate(ms_TextTemplate, owner.transform);

                m_Text.text = info.Text;
                m_Text.gameObject.SetActive(true);

                m_Dirty = new bool[3];

                ms_Owner.Client.Runnable.RunLater(LateInit, 0.2f);
            }

            void LateInit() {
                if (!m_InvokedLateInit) {
                    m_InvokedLateInit = true;

                    m_InitialPosition = new Vector2(ms_Owner.m_IdleButtonPositioner.GetWorldPositionX(m_Index), Button.transform.position.y);
                    m_ActivePosition = new Vector2(ms_Owner.m_ActiveButtonPositioner.GetWorldPositionX(m_Index), ms_TemplateTransform.position.y);

                    m_InitialTextPosition = m_Text.transform.position;
                    m_InitialTextPosition.x = m_InitialPosition.x; //m_ActivePosition.x; //centre with active pos
                    m_Text.transform.position = m_InitialTextPosition;

                    m_ActiveTextPosition = m_ActivePosition;
                    m_ActiveTextPosition.y -= Mathf.Min(100f, m_InitialSize) * ACTIVE_SIZE_MOD / 2f + m_Text.rectTransform.sizeDelta.y * 0.7f;

                    Reset();
                }
            }

            void OnButtonClick(bool broadcast) {
                if (ms_Owner.Navigation.IsActive)
                    return;

                int interactivityMask = 1 << Info.Index; //bit shifted
                if ((ms_Owner.MapButtonsInteractivityMask & interactivityMask) != interactivityMask)
                    return;

                m_State = !m_State;
                m_LastChangeTime = Time.time;

                //broadcast to other buttons
                if (broadcast) {
                    foreach (MapButton mapButton in ms_Owner.m_MapButtons) {
                        if (mapButton != this) {
                            mapButton.OnButtonClick(false);
                        }
                    }

                    if (!m_State) {
                        int mask = 1 << Info.Index;
                        if ((ms_Owner.MapButtonsMask & mask) == mask) {
                            if (m_Info.OnDown != null)
                                m_Info.OnDown();
                        }
                    }
                }

                for (int i = 0; i < m_Dirty.Length; i++) {
                    m_Dirty[i] = false;
                }

                for (int i = 0; i < m_Delta.Length; i++) {
                    m_Delta[i] = 0f;
                }

                m_QueuedUpdates = MapButtonUpdate.None;
                if ((Vector2)Button.transform.position != m_PreferredPosition) {
                    m_QueuedUpdates |= MapButtonUpdate.Position;
                }
            }

            public void Update() {
                if (!m_InvokedLateInit)
                    return;

                if ((m_QueuedUpdates & MapButtonUpdate.Position) == MapButtonUpdate.Position) {
                    m_Delta[0] += Time.deltaTime * 5f;
                    Button.transform.position = Vector3.Lerp(m_InversePosition, m_PreferredPosition, m_Delta[0]);

                    if (m_Delta[0] >= 0.5f && (m_QueuedUpdates & MapButtonUpdate.Size) == 0 && !m_Dirty[0]) {
                        m_Dirty[0] = true;
                        m_QueuedUpdates |= MapButtonUpdate.Size;
                    }

                    if (m_Delta[0] >= 0.2f && (m_QueuedUpdates & MapButtonUpdate.Label) == 0 && !m_Dirty[1]) {
                        m_Dirty[1] = true;
                        m_QueuedUpdates |= MapButtonUpdate.Label;
                    }

                    if ((Vector2)Button.transform.position == m_PreferredPosition) {
                        m_QueuedUpdates &= ~MapButtonUpdate.Position;
                        m_Delta[0] = 0f;
                    }
                }

                if ((m_QueuedUpdates & MapButtonUpdate.Size) == MapButtonUpdate.Size) {
                    m_Delta[1] += Time.deltaTime * 5f;

                    float newSize = Mathf.Lerp(m_InverseSize, m_PreferredSize, m_Delta[1]);
                    Image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize);
                    Image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize);

                    if (m_Delta[1] >= 0.5f && (m_QueuedUpdates & MapButtonUpdate.Opacity) == 0 && !m_Dirty[2]) {
                        m_Dirty[2] = true;
                        m_QueuedUpdates |= MapButtonUpdate.Opacity;
                    }

                    if (Mathf.Approximately(newSize, m_PreferredSize)) {
                        m_QueuedUpdates &= ~MapButtonUpdate.Size;
                        m_Delta[1] = 0f;
                    }
                }

                if ((m_QueuedUpdates & MapButtonUpdate.Opacity) == MapButtonUpdate.Opacity) {
                    m_Delta[2] += Time.deltaTime * 4f;

                    Image.color = Color.Lerp(m_InverseColor, m_PreferredColor, m_Delta[2]);

                    if (Image.color == m_PreferredColor) {
                        m_QueuedUpdates &= ~MapButtonUpdate.Opacity;
                        m_Delta[2] = 0f;
                    }
                }

                if ((m_QueuedUpdates & MapButtonUpdate.Label) == MapButtonUpdate.Label) {
                    m_Delta[3] += Time.deltaTime * 7f;

                    m_Text.transform.position = Vector3.Lerp(m_InverseTextPosition, m_PreferredTextPosition, m_Delta[3]);

                    if ((Vector2)m_Text.transform.position == m_PreferredTextPosition) {
                        m_QueuedUpdates &= ~MapButtonUpdate.Label;
                        m_Delta[3] = 0f;
                    }
                }

                if (m_State && Time.time - m_LastChangeTime > 3f) {
                    OnButtonClick(false);
                }
            }

            public void Reset() {
                m_State = false;
                Button.transform.position = m_PreferredPosition;
                Image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_PreferredSize);
                Image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_PreferredSize);
                Image.color = m_PreferredColor;
                m_Text.transform.position = m_PreferredTextPosition;
            }
        }

        class Positioner {
            const int MAX_CHILD_COUNT = 12;

            Transform m_Transform;
            GameObject[] m_Children;
            int m_Count;

            public Positioner(Transform trans) {
                m_Transform = trans;
                m_Children = new GameObject[MAX_CHILD_COUNT];
                for (int i = 0; i < m_Children.Length; i++) {
                    GameObject go = new GameObject();
                    go.transform.parent = trans;
                    go.AddComponent<RectTransform>();
                    go.SetActive(false);
                    m_Children[i] = go;
                }
            }

            public void SetChildSize(float sz) {
                for (int i = 0; i < m_Count; i++) {
                    (m_Children[i].transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sz);
                }
            }

            public void SetChildCount(int count) {
                m_Count = count;

                for (int i = 0; i < m_Children.Length; i++) {
                    m_Children[i].SetActive(i < count);
                }
            }

            public float GetWorldPositionX(int idx) {
                return m_Transform.GetChild(idx).position.x;
            }
        }

        [Serializable]
        public class MapButtonInfo {
            public string Text;
            public Sprite Sprite;
            [NonSerialized]
            public Action OnDown;
            [NonSerialized]
            public int Index;
        }

        [Serializable]
        struct MarkerSprite {
            public EGRPlaceType Type;
            public Sprite Sprite;
        }

        public static class MapButtonIDs {
            public static int CURRENT_LOCATION = 0;
            public static int HOTTEST_TRENDS = 1;
            public static int SETTINGS = 2;
            public static int NAVIGATION = 3;
            public static int BACK_TO_EARTH = 4;
            public static int SPACE_FOV = 5;
            public static int MAX = 6;

            public static int ALL_MASK = EGRUtils.FillBits(MAX);
        }

        MRKMap m_Map;
        readonly List<MapButton> m_MapButtons;
        readonly List<MapButtonInfo> m_TempMapButtonInfoBuffer;
        TextMeshProUGUI m_CamDistLabel;
        [SerializeField]
        TextMeshPro m_ContextLabel;
        [SerializeField]
        TextMeshPro m_TimeLabel;
        [SerializeField]
        TextMeshPro m_DistLabel;
        [SerializeField]
        GameObject m_MapButtonPrefab;
        float m_LastTimeUpdate;
        [SerializeField]
        AnimationCurve m_MarkerScaleCurve;
        [SerializeField]
        AnimationCurve m_MarkerOpacityCurve;
        RawImage m_TransitionImg;
        [SerializeField]
        MapButtonInfo[] m_ButtonInfos;
        readonly List<Tuple<GameObject, TextMeshProUGUI>> m_MapButtonsPool;
        Positioner m_IdleButtonPositioner;
        Positioner m_ActiveButtonPositioner;
        bool m_PositionersDirty;
        Action[] m_ButtonInfoDelegates;
        [SerializeField]
        MarkerSprite[] m_MarkerSprites;
        bool m_MouseDown;
        Vector3 m_MouseDownPos;
        bool m_ZoomHasChanged;
        Dictionary<Transform, TextMeshPro> m_PlanetNames;
        readonly Dictionary<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> m_InterfaceComponents;
        [SerializeField]
        EGRMapInterfacePlaceMarkersResources m_PlaceMarkersResources;
        [SerializeField]
        EGRMapInterfaceResources m_MapInterfaceResources;
        bool m_MapButtonsEnabled;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0x00000000;
        EGRCamera m_EGRCamera => EGRMain.Instance.ActiveEGRCamera;
        public string ContextText => m_ContextLabel.text;
        public bool IsInTransition => m_TransitionImg.gameObject.activeInHierarchy;
        public Transform ObservedTransform { get; private set; }
        public bool ObservedTransformDirty { get; set; }
        public Transform ScalebarParent { get; private set; }
        public EGRMapInterfacePlaceMarkersResources PlaceMarkersResources => m_PlaceMarkersResources;
        public EGRMapInterfaceResources MapInterfaceResources => m_MapInterfaceResources;
        public bool MapButtonsEnabled {
            get {  return m_MapButtonsEnabled; }
            set {
                if (m_MapButtonsEnabled != value) {
                    m_MapButtonsEnabled = value;
                    RegenerateMapButtons();
                }
            }
        }
        public int MapButtonsMask { get; set; }
        public int MapButtonsInteractivityMask { get; set; }

        public EGRMapInterfaceComponentPlaceMarkers PlaceMarkers =>
            (EGRMapInterfaceComponentPlaceMarkers)m_InterfaceComponents[EGRMapInterfaceComponentType.PlaceMarkers];
        public EGRMapInterfaceComponentScaleBar ScaleBar =>
            (EGRMapInterfaceComponentScaleBar)m_InterfaceComponents[EGRMapInterfaceComponentType.ScaleBar];
        public EGRMapInterfaceComponentNavigation Navigation =>
            (EGRMapInterfaceComponentNavigation)m_InterfaceComponents[EGRMapInterfaceComponentType.Navigation];
        public EGRMapInterfaceComponentLocationOverlay LocationOverlay =>
            (EGRMapInterfaceComponentLocationOverlay)m_InterfaceComponents[EGRMapInterfaceComponentType.LocationOverlay];

        public EGRScreenMapInterface() {
            m_InterfaceComponents = new Dictionary<EGRMapInterfaceComponentType, EGRMapInterfaceComponent>();
            m_MapButtonsPool = new List<Tuple<GameObject, TextMeshProUGUI>>();
            m_MapButtons = new List<MapButton>();
            m_TempMapButtonInfoBuffer = new List<MapButtonInfo>();

            MapButtonsMask = MapButtonIDs.ALL_MASK;
            MapButtonsInteractivityMask = MapButtonIDs.ALL_MASK;
        }

        protected override void OnScreenInit() {
            m_Map = Client.FlatMap;
            m_Map.gameObject.SetActive(false);

            GetElement<Button>(Buttons.Back).onClick.AddListener(OnBackClick);
            m_MapButtonPrefab.SetActive(false); //disable our template button

            m_CamDistLabel = GetElement<TextMeshProUGUI>(Labels.CamDist);
            m_TransitionImg = GetElement<RawImage>(Images.Transition);
            m_TransitionImg.gameObject.SetActive(false);

            ScalebarParent = GetTransform(Others.DistProg);

            m_IdleButtonPositioner = new Positioner(GetTransform(Others.BotHorPos));
            m_ActiveButtonPositioner = new Positioner(GetTransform(Others.MapActiveTemplate));

            //assign mapinfo delegates
            m_ButtonInfoDelegates = new Action[6] {
                OnCurrentLocationClick,
                OnHottestTrendsClick,
                OnSettingsClick, //settings
                OnNavigationClick,
                OnBackToEarthClick,
                OnSpaceFOVClick
            };

            for (int i = 0; i < Mathf.Min(m_ButtonInfoDelegates.Length, m_ButtonInfos.Length); i++) {
                m_ButtonInfos[i].OnDown = m_ButtonInfoDelegates[i];
                m_ButtonInfos[i].Index = i;
            }

            ObservedTransform = Client.GlobalMap.transform;

            RegisterInterfaceComponent(EGRMapInterfaceComponentType.PlaceMarkers, new EGRMapInterfaceComponentPlaceMarkers());
            RegisterInterfaceComponent(EGRMapInterfaceComponentType.ScaleBar, new EGRMapInterfaceComponentScaleBar());
            RegisterInterfaceComponent(EGRMapInterfaceComponentType.Navigation, new EGRMapInterfaceComponentNavigation());
            RegisterInterfaceComponent(EGRMapInterfaceComponentType.LocationOverlay, new EGRMapInterfaceComponentLocationOverlay());
        }

        public void OnInterfaceEarlyShow() {
            m_EGRCamera.SetInterfaceState(true);

            m_ContextLabel.gameObject.SetActive(true);
            m_TimeLabel.gameObject.SetActive(EGRSettings.ShowTime);
            m_DistLabel.gameObject.SetActive(EGRSettings.ShowDistance);

            UpdateTime();
        }

        protected override void OnScreenShow() {
            //hide bg since it's only for designing
            GetElement<Image>(Images.BaseBg).gameObject.SetActive(false);

            m_Map.OnMapUpdated += OnMapUpdated;
            m_Map.OnMapFullyUpdated += OnMapFullyUpdated;
            m_Map.OnMapZoomUpdated += OnMapZoomUpdated;

            Client.RegisterMapModeDelegate(OnMapModeChanged);
            Client.RegisterControllerReceiver(OnControllerMessageReceived);

            //map mode might've changed when visible=false
            OnMapModeChanged(Client.MapMode);

            if (m_PlanetNames == null) {
                m_PlanetNames = new Dictionary<Transform, TextMeshPro>();

                foreach (Transform planet in Client.Planets) {
                    TextMeshPro txt = planet.Find("Name").GetComponent<TextMeshPro>();
                    txt.gameObject.SetActive(false);
                    m_PlanetNames[planet] = txt;
                }
            }

            Client.DisableAllScreensExcept<EGRScreenMapInterface>();

            //SetMapButtons(m_ButtonInfos);

            foreach (KeyValuePair<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> pair in m_InterfaceComponents) {
                pair.Value.OnComponentShow();
            }

            MapButtonsEnabled = true;
        }

        protected override void OnScreenHide() {
            m_Map.OnMapUpdated -= OnMapUpdated;
            m_Map.OnMapFullyUpdated -= OnMapFullyUpdated;
            m_Map.OnMapZoomUpdated -= OnMapZoomUpdated;

            Client.UnregisterMapModeDelegate(OnMapModeChanged);
            Client.UnregisterControllerReceiver(OnControllerMessageReceived);

            Manager.MainScreen.ShowScreen();

            Client.SetPostProcessState(false);
            m_ContextLabel.gameObject.SetActive(false);
            m_TimeLabel.gameObject.SetActive(false);
            m_DistLabel.gameObject.SetActive(false);

            foreach (KeyValuePair<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> pair in m_InterfaceComponents) {
                pair.Value.OnComponentHide();
            }

            //reset map button mask
            MapButtonsMask = MapButtonIDs.ALL_MASK;
            MapButtonsInteractivityMask = MapButtonIDs.ALL_MASK;
        }

        protected override void OnScreenUpdate() {
            if (m_MapButtons.Count > 0) {
                foreach (MapButton button in m_MapButtons) {
                    button.Update();
                }
            }

            if (Time.time - m_LastTimeUpdate >= 60f) {
                UpdateTime();
            }

            foreach (EGRMapInterfaceComponent component in m_InterfaceComponents.Values) {
                component.OnComponentUpdate();
            }
        }

        void RegisterInterfaceComponent(EGRMapInterfaceComponentType type, EGRMapInterfaceComponent component) {
            m_InterfaceComponents[type] = component;
            component.OnComponentInit(this);
        }

        void OnMapModeChanged(EGRMapMode mode) {
            m_CamDistLabel.gameObject.SetActive(/*isGlobe*/false);
            ScaleBar.SetActive(mode == EGRMapMode.Flat);
            Client.ActiveEGRCamera.ResetStates();

            //from globe to flat
            if (mode == EGRMapMode.Flat && Client.PreviousMapMode == EGRMapMode.Globe) {
                Client.FlatCamera.UpdateMapViewingAngles(null, 0f);
            }

            RegenerateMapButtons();
        }

        void RegenerateMapButtons() {
            if (MapButtonsEnabled) {
                //set base map buttons
                List<int> ids = ListPool<int>.Default.Rent();
                ids.Add(MapButtonIDs.SETTINGS);
                ids.Add(MapButtonIDs.CURRENT_LOCATION);

                EGRMapMode mode = Client.MapMode;
                if (mode == EGRMapMode.Globe) {
                    if (ObservedTransform != Client.GlobalMap.transform) {
                        ids.Add(MapButtonIDs.BACK_TO_EARTH);
                    }

                    ids.Add(MapButtonIDs.SPACE_FOV);
                }
                else if (mode == EGRMapMode.Flat) {
                    ids.Add(MapButtonIDs.HOTTEST_TRENDS);
                    ids.Add(MapButtonIDs.NAVIGATION);
                    ids.Add(MapButtonIDs.BACK_TO_EARTH);
                }

                SetMapButtons(ids.ToArray());
                ListPool<int>.Default.Free(ids);
            }
            else
                SetMapButtons(new MapButtonInfo[0]);
        }

        void OnControllerMessageReceived(EGRControllerMessage msg) {
            if (Client.MapMode != EGRMapMode.Globe)
                return;

            if (!m_EGRCamera.ShouldProcessControllerMessage(msg))
                return;

            if (msg.ContextualKind == EGRControllerMessageContextualKind.Mouse) {
                EGRControllerMouseEventKind kind = (EGRControllerMouseEventKind)msg.Payload[0];

                switch (kind) {

                    case EGRControllerMouseEventKind.Down:
                        m_MouseDown = true;
                        m_MouseDownPos = (Vector3)msg.Payload[3];
                        break;

                    case EGRControllerMouseEventKind.Up:
                        if (m_MouseDown) {
                            m_MouseDown = false;

                            Vector3 pos = (Vector3)msg.Payload[1];
                            if ((pos - m_MouseDownPos).sqrMagnitude < 9f)
                                ChangeObservedTransform((Vector3)msg.Payload[1]);
                        }
                        break;

                }
            }
        }

        void SetObservedTransformNameState(bool active) {
            if (ObservedTransform != Client.GlobalMap.transform) {
                TextMeshPro txt = m_PlanetNames[ObservedTransform];
                txt.gameObject.SetActive(active);

                if (active) {
                    StartCoroutine(SetTextEnumerator(x => txt.text = x, txt.text, 0.3f, ""));
                }
            }
        }

        void ChangeObservedTransform(Vector3 pos) {
            Ray ray = Client.ActiveCamera.ScreenPointToRay(pos);

            //simulate physics
            Physics.Simulate(0.1f);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Client.ActiveCamera.farClipPlane, 1 << 6, QueryTriggerInteraction.Collide)) {
                if (hit.transform != ObservedTransform) {
                    SetObservedTransformNameState(false);

                    ObservedTransform = hit.transform;
                    ObservedTransformDirty = true;

                    SetObservedTransformNameState(true);
                    OnObservedTransformChanged();
                }
            }
        }

        void OnObservedTransformChanged() {
            if (ObservedTransform == Client.GlobalMap.transform) {
                RemoveMapButton(MapButtonIDs.BACK_TO_EARTH);
            }
            else {
                AddMapButton(MapButtonIDs.BACK_TO_EARTH);
            }
        }

        public void SetDistanceText(string txt, bool animated = false) {
            //legacy distance label
            //m_CamDistLabel.text = txt;

            if (m_DistLabel.gameObject.activeInHierarchy) {
                if (animated)
                    StartCoroutine(SetTextEnumerator(x => m_DistLabel.text = x, txt, 0.9f, "m"));
                else
                    m_DistLabel.text = txt;
            }
        }

        public void SetContextText(string txt) {
            Client.StartCoroutine(SetTextEnumerator(x => m_ContextLabel.text = x, txt, 0.7f, "\n"));
        }

        void UpdateTime() {
            m_LastTimeUpdate = Time.time;
            Client.StartCoroutine(SetTextEnumerator(x => m_TimeLabel.text = x, DateTime.Now.ToString("HH:mm"), 1f, ":"));
        }

        public void SetTransitionTex(RenderTexture rt, TweenCallback callback = null, float speed = 0.6f) {
            m_TransitionImg.texture = rt;
            m_TransitionImg.gameObject.SetActive(true);

            m_TransitionImg.DOColor(Color.white.AlterAlpha(0f), speed)
                .ChangeStartValue(Color.white.AlterAlpha(1f))
                .SetEase(Ease.Linear)
                .OnComplete(() => {
                    m_TransitionImg.gameObject.SetActive(false);
                });

            UIDissolve dis = m_TransitionImg.GetComponent<UIDissolve>();
            DOTween.To(() => dis.effectFactor, x => dis.effectFactor = x, 1f, speed)
                .SetEase(Ease.OutSine)
                .ChangeStartValue(0f)
                .OnComplete(callback);
        }

        IEnumerator SetTextEnumerator(Action<string> set, string txt, float speed, string prohibited) {
            string real = "";
            List<int> linesIndices = new List<int>();
            for (int i = 0; i < txt.Length; i++)
                foreach (char p in prohibited) {
                    if (txt[i] == p) {
                        linesIndices.Add(i);
                        break;
                    }
                }

            float timePerChar = speed / txt.Length;

            foreach (char c in txt) {
                bool leave = false;
                foreach (char p in prohibited) {
                    if (c == p) {
                        real += p;
                        leave = true;
                        break;
                    }
                }

                if (leave)
                    continue;

                float secsElaped = 0f;
                while (secsElaped < timePerChar) {
                    yield return new WaitForSeconds(0.02f);
                    secsElaped += 0.02f;

                    string renderedTxt = real + EGRUtils.GetRandomString(txt.Length - real.Length);
                    foreach (int index in linesIndices)
                        renderedTxt = renderedTxt.ReplaceAt(index, prohibited[prohibited.IndexOf(txt[index])]);

                    set(renderedTxt);
                }

                real += c;
            }

            set(txt);
        }

        public void OnBackClick() {
            SetMapButtons(new MapButtonInfo[0]);

            m_EGRCamera.SetInterfaceState(false);

            if (ObservedTransform != Client.GlobalMap.transform) {
                SetObservedTransformNameState(false);
                ObservedTransform = Client.GlobalMap.transform;
                ObservedTransformDirty = true;
            }

            HideScreen();
        }

        void OnMapUpdated() {
            if (Client.MapMode != EGRMapMode.Flat)
                return;

            foreach (KeyValuePair<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> pair in m_InterfaceComponents) {
                pair.Value.OnMapUpdated();
            }
        }

        void OnMapZoomUpdated(int oldZoom, int newZoom) {
            if (m_TransitionImg.gameObject.activeInHierarchy)
                return;

            //Debug.Log($"Zoom updated {oldZoom} -> {newZoom}");
            m_ZoomHasChanged = true;
        }

        void OnMapFullyUpdated() {
            if (m_ZoomHasChanged) {
                m_ZoomHasChanged = false;
                //SetTransitionTex(Client.CaptureScreenBuffer());
            }

            foreach (KeyValuePair<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> pair in m_InterfaceComponents) {
                pair.Value.OnMapFullyUpdated();
            }
        }

        public void Warmup() {
            foreach (KeyValuePair<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> pair in m_InterfaceComponents) {
                pair.Value.OnWarmup();
            }

            ScreenManager.GetScreen<EGRPopupPlaceGroup>().Warmup();
        }

        //EGRGeoJson? m_GeoJson;

        /*void OnGUI() {
            //render geojson borders
            /*if (Client.MapMode != EGRMapMode.Flat)
                return;

            if (!m_GeoJson.HasValue) {
                m_GeoJson = Newtonsoft.Json.JsonConvert.DeserializeObject<EGRGeoJson>(Resources.Load<TextAsset>("Map/countries").text);
            }

            int drawCount = 0;

            foreach (EGRGeoJsonFeature feature in m_GeoJson.Value.Features) {
                if (feature.Geometry == null || feature.Geometry.Polygons.Count == 0)
                    continue;

                foreach (List<Vector2d> poly in feature.Geometry.Polygons) {
                    Vector2? lastPos = null;

                    foreach (Vector2d geoPoint in poly) {
                        //calculate current spos
                        Vector3 wPos = m_Map.GeoToWorldPosition(geoPoint);
                        Vector2 sPos = Client.ActiveCamera.WorldToScreenPoint(wPos);
                        sPos.y = Screen.height - sPos.y;

                        if (lastPos.HasValue) {
                            EGRGL.DrawLine(lastPos.Value, sPos, Color.blue, 1.2f);
                            drawCount++;
                        }

                        lastPos = sPos;
                    }
                }
            }

            Debug.Log($"DC={drawCount}");
        } */

        public float EvaluateMarkerScale(float time) {
            return m_MarkerScaleCurve.Evaluate(time);
        }

        public float EvaluateMarkerOpacity(float time) {
            return m_MarkerOpacityCurve.Evaluate(time);
        }

        public void SetPositionersSize(float idleSz, float activeSz) {
            if (!m_PositionersDirty)
                return;

            m_PositionersDirty = false;
            m_IdleButtonPositioner.SetChildSize(idleSz);
            m_ActiveButtonPositioner.SetChildSize(activeSz);
        }

        void SetMapButtons(params int[] indices) {
            if (m_TempMapButtonInfoBuffer.Count > 0) {
                m_TempMapButtonInfoBuffer.Clear();
            }

            foreach (int i in indices) {
                m_TempMapButtonInfoBuffer.Add(m_ButtonInfos[i]);
            }

            SetMapButtons(m_TempMapButtonInfoBuffer.ToArray());
        }

        void SetMapButtonsBitShifted(int shift, params int[] indices) {
            if (m_TempMapButtonInfoBuffer.Count > 0) {
                m_TempMapButtonInfoBuffer.Clear();
            }

            foreach (int i in indices) {
                m_TempMapButtonInfoBuffer.Add(m_ButtonInfos[i >> shift]);
            }

            SetMapButtons(m_TempMapButtonInfoBuffer.ToArray());
        }

        void AddMapButton(int idx) {
            if (m_TempMapButtonInfoBuffer.Count > 0) {
                m_TempMapButtonInfoBuffer.Clear();
            }

            foreach (MapButton mb in m_MapButtons) {
                if (mb.Info.Index == idx) {
                    //we already have the button
                    return;
                }

                m_TempMapButtonInfoBuffer.Add(mb.Info);
            }

            m_TempMapButtonInfoBuffer.Add(m_ButtonInfos[idx]);
            SetMapButtons(m_TempMapButtonInfoBuffer.ToArray());
        }

        void RemoveMapButton(int idx) {
            if (m_TempMapButtonInfoBuffer.Count > 0) {
                m_TempMapButtonInfoBuffer.Clear();
            }

            foreach (MapButton mb in m_MapButtons) {
                if (mb.Info.Index == idx) {
                    continue;
                }

                m_TempMapButtonInfoBuffer.Add(mb.Info);
            }

            SetMapButtons(m_TempMapButtonInfoBuffer.ToArray());
        }

        public void SetMapButtons(MapButtonInfo[] infos) {
            //free active buttons
            if (m_MapButtons.Count > 0) {
                for (int i = 0; i < m_MapButtons.Count; i++) {
                    GameObject obj = m_MapButtons[i].Button.gameObject;
                    obj.SetActive(false);
                    TextMeshProUGUI txt = m_MapButtons[i].Text;
                    txt.gameObject.SetActive(false);

                    m_MapButtons[i].Reset();
                    //m_MapButtons[i] = null;
                    m_MapButtonsPool.Add(new Tuple<GameObject, TextMeshProUGUI>(obj, txt));
                }

                m_MapButtons.Clear();
            }

            m_IdleButtonPositioner.SetChildCount(infos.Length);
            m_ActiveButtonPositioner.SetChildCount(infos.Length);
            m_PositionersDirty = true;

            for (int i = 0; i < infos.Length; i++) {
                Tuple<GameObject, TextMeshProUGUI> pooled = null;
                if (m_MapButtonsPool.Count > 0) {
                    pooled = m_MapButtonsPool[0];
                    m_MapButtonsPool.RemoveAt(0);
                }

                m_MapButtons.Add(new MapButton(this, i, infos[i], pooled));
            }
        }

        void OnHottestTrendsClick() {
            Manager.GetScreen<EGRScreenHottestTrends>().ShowScreen(this);
        }

        void OnSettingsClick() {
            EGRScreen screen = Client.MapMode == EGRMapMode.Globe ? Manager.GetScreen<EGRScreenOptionsGlobeSettings>() : (EGRScreen)Manager.GetScreen<EGRScreenOptionsMapSettings>();
            screen.ShowScreen(this);
        }

        public Sprite GetSpriteForPlaceType(EGRPlaceType type) {
            foreach (MarkerSprite ms in m_MarkerSprites) {
                if (ms.Type == type)
                    return ms.Sprite;
            }

            return m_MarkerSprites[0].Sprite; //NONE
        }

        void OnNavigationClick() {
            if (m_EGRCamera is EGRCameraGlobe) {
                Client.GlobeCamera.SwitchToFlatMapExternal(() => {
                    Client.Runnable.RunLater(EnterNavigation, 0.2f);
                });
            }
            else
                EnterNavigation();
        }

        void EnterNavigation() {
            Client.FlatCamera.EnterNavigation();
            Navigation.Show();
        }

        void OnCurrentLocationClick() {
            if (Client.MapMode == EGRMapMode.Flat) {
                Client.LocationManager.RequestCurrentLocation(false, true, true);
            }
            else if (Client.MapMode == EGRMapMode.Globe) {
                Client.GlobeCamera.SwitchToFlatMapExternal(() => {
                    Client.Runnable.RunLater(() => {
                        Client.LocationManager.RequestCurrentLocation(false, true, true);
                    }, 0.2f);
                });
            }
        }

        void OnBackToEarthClick() {
            if (Client.MapMode == EGRMapMode.Globe) {
                if (ObservedTransform != Client.GlobalMap.transform) {
                    SetObservedTransformNameState(false);

                    //set back to earth
                    ObservedTransform = Client.GlobalMap.transform;
                    ObservedTransformDirty = true;
                    OnObservedTransformChanged();
                }
            }
            else if (Client.MapMode == EGRMapMode.Flat) {
                Client.FlatCamera.SwitchToGlobe();
            }
        }

        void OnSpaceFOVClick() {
            ScreenManager.GetScreen<EGRScreenSpaceFOV>().ShowScreen();
        }
    }
}