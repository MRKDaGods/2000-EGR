using Coffee.UIEffects;
using DG.Tweening;
using MRK.Navigation;
using MRK.Networking.Packets;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public class EGRMapInterfaceComponentNavigation : EGRMapInterfaceComponent {
        class Top {
            RectTransform m_Transform;
            TMP_InputField m_From;
            TMP_InputField m_To;
            Button[] m_Profiles;
            int m_SelectedProfileIndex;
            static Color ms_SelectedProfileColor;
            static Color ms_IdleProfileColor;
            readonly UIHsvModifier[] m_ValidationModifiers;
            float m_InitialY;

            public string From => m_From.text;
            public string To => m_To.text;
            public TMP_InputField ToInput => m_To;
            public byte SelectedProfile => (byte)m_SelectedProfileIndex;

            static Top() {
                ms_SelectedProfileColor = new Color(0.4588235294117647f, 0.6980392156862745f, 1f, 1f);
                ms_IdleProfileColor = new Color(0.5176470588235294f, 0.5176470588235294f, 0.5176470588235294f, 1f);
            }

            public Top(RectTransform transform) {
                m_Transform = transform;

                m_From = m_Transform.Find("Main/Places/From").GetComponent<TMP_InputField>();
                m_To = m_Transform.Find("Main/Places/To").GetComponent<TMP_InputField>();

                m_From.text = m_To.text = "";
                m_From.onSelect.AddListener((val) => OnSelect(0));
                m_To.onSelect.AddListener((val) => OnSelect(1));
                m_From.onValueChanged.AddListener((val) => OnTextChanged(0, val));
                m_To.onValueChanged.AddListener((val) => OnTextChanged(1, val));

                m_Profiles = m_Transform.Find("Main/Places/Profiles").GetComponentsInChildren<Button>();
                for (int i = 0; i < m_Profiles.Length; i++) {
                    int _i = i;
                    m_Profiles[i].onClick.AddListener(() => OnProfileClicked(_i));
                }

                m_SelectedProfileIndex = 0;
                UpdateSelectedProfile();

                m_ValidationModifiers = new UIHsvModifier[2]{
                    m_From.GetComponent<UIHsvModifier>(),
                    m_To.GetComponent<UIHsvModifier>()
                };

                foreach (UIHsvModifier modifier in m_ValidationModifiers) {
                    modifier.enabled = false;
                }

                m_InitialY = m_Transform.anchoredPosition.y;
                transform.anchoredPosition = new Vector3(m_Transform.anchoredPosition.x, m_InitialY + m_Transform.rect.height); //initially
            }

            void OnProfileClicked(int index) {
                m_SelectedProfileIndex = index;
                UpdateSelectedProfile();
            }

            void UpdateSelectedProfile() {
                for (int i = 0; i < m_Profiles.Length; i++) {
                    m_Profiles[i].GetComponent<Image>().color = m_SelectedProfileIndex == i ? ms_SelectedProfileColor : ms_IdleProfileColor;
                }
            }

            void OnTextChanged(int idx, string value) {
                ms_Instance.m_AutoComplete.SetContext(idx, value);

                //invalidate
                SetValidationState(idx, false);
            }

            void OnSelect(int idx) {
                ms_Instance.m_AutoComplete.SetAutoCompleteState(true, idx == 0, idx == 1);
                TMP_InputField active = idx == 0 ? m_From : m_To;
                ms_Instance.m_AutoComplete.SetActiveInput(active);

                OnTextChanged(idx, active.text);
            }

            public void Show(bool clear = true) {
                if (clear) {
                    m_From.text = m_To.text = "";
                }

                m_Transform.DOAnchorPosY(m_InitialY, 0.3f)
                    .ChangeStartValue(new Vector3(0f, m_InitialY + m_Transform.rect.height))
                    .SetEase(Ease.OutSine);
            }

            public void Hide() {
                m_Transform.DOAnchorPosY(m_InitialY + m_Transform.rect.height, 0.3f)
                    .SetEase(Ease.OutSine);
            }

            public void SetInputActive(int idx) {
                (idx == 0 ? m_From : m_To).ActivateInputField();
            }

            public void SetValidationState(int idx, bool state) {
                m_ValidationModifiers[idx].enabled = state;

                if (!state) {
                    if (idx == 0)
                        ms_Instance.FromCoords = null;
                    else
                        ms_Instance.ToCoords = null;
                }
            }

            public bool IsValid(int idx) {
                return m_ValidationModifiers[idx].enabled;
            }
        }

        class Bottom {
            class Route {
                public GameObject Object;
                public TextMeshProUGUI Text;
                public Button Button;
                public int Index;
            }

            readonly RectTransform m_Transform;
            readonly TextMeshProUGUI m_Distance;
            readonly TextMeshProUGUI m_Time;
            readonly Button m_Start;
            readonly GameObject m_RoutePrefab;
            float m_StartAnimDelta;
            readonly UIHsvModifier m_StartAnimHSV;
            float m_InitialY;
            readonly UITransitionEffect m_BackAnim;
            readonly ObjectPool<Route> m_RoutePool;
            readonly List<Route> m_CurrentRoutes;
            EGRNavigationDirections m_CurrentDirs;

            public Bottom(RectTransform transform) {
                m_Transform = transform;

                m_Distance = m_Transform.Find("Main/Info/Distance").GetComponent<TextMeshProUGUI>();
                m_Time = m_Transform.Find("Main/Info/Time").GetComponent<TextMeshProUGUI>();

                m_Start = m_Transform.Find("Main/Destination/Button").GetComponent<Button>();
                m_Start.onClick.AddListener(OnStartClick);
                m_StartAnimHSV = m_Start.transform.Find("Sep").GetComponent<UIHsvModifier>();

                m_RoutePrefab = m_Transform.Find("Routes/Route").gameObject;
                m_RoutePrefab.gameObject.SetActive(false);

                Transform back = m_Transform.Find("Back");
                back.GetComponent<Button>().onClick.AddListener(OnBackClick);
                m_BackAnim = back.GetComponent<UITransitionEffect>();
                m_BackAnim.effectFactor = 0f;

                m_InitialY = m_Transform.anchoredPosition.y;
                transform.anchoredPosition = new Vector3(m_Transform.anchoredPosition.x, m_InitialY - m_Transform.rect.height); //initially

                m_RoutePool = new ObjectPool<Route>(() => {
                    Route route = new Route();
                    route.Object = Object.Instantiate(m_RoutePrefab, m_RoutePrefab.transform.parent);
                    route.Text = route.Object.transform.Find("Text").GetComponent<TextMeshProUGUI>();
                    route.Button = route.Object.GetComponent<Button>();
                    route.Button.onClick.AddListener(() => OnRouteClick(route));

                    route.Object.SetActive(false);
                    return route;
                });

                m_CurrentRoutes = new List<Route>();
            }

            public void Update() {
                if (m_Start.gameObject.activeInHierarchy) {
                    m_StartAnimDelta += Time.deltaTime * 0.2f;
                    if (m_StartAnimDelta > 0.5f)
                        m_StartAnimDelta = -0.5f;

                    m_StartAnimHSV.hue = m_StartAnimDelta;
                }
            }

            public void Show() {
                m_Transform.DOAnchorPosY(m_InitialY, 0.3f)
                    .ChangeStartValue(new Vector3(0f, m_InitialY - m_Transform.rect.height))
                    .SetEase(Ease.OutSine);
            }

            public void Hide() {
                m_Transform.DOAnchorPosY(m_InitialY - m_Transform.rect.height, 0.3f)
                    .SetEase(Ease.OutSine);
            }

            void OnBackClick() {
                if (ms_Instance.Hide()) {
                    DOTween.To(() => m_BackAnim.effectFactor, x => m_BackAnim.effectFactor = x, 0f, 0.3f);
                }
            }

            public void ShowBackButton() {
                DOTween.To(() => m_BackAnim.effectFactor, x => m_BackAnim.effectFactor = x, 1f, 0.7f);
            }

            public void ClearDirections() {
                if (m_CurrentRoutes.Count > 0) {
                    foreach (Route r in m_CurrentRoutes) {
                        r.Object.SetActive(false);
                        m_RoutePool.Free(r);
                    }

                    m_CurrentRoutes.Clear();
                }
            }

            public void SetDirections(EGRNavigationDirections dirs) {
                ClearDirections();

                m_CurrentDirs = dirs;

                int idx = 0;
                foreach (EGRNavigationRoute route in dirs.Routes) {
                    Route r = m_RoutePool.Rent();
                    r.Index = idx;
                    r.Text.text = $"ROUTE {(idx++) + 1}";
                    r.Object.SetActive(true);

                    m_CurrentRoutes.Add(r);
                }

                ms_Instance.Client.NavigationManager.PrepareDirections();

                if (idx > 0) {
                    SetCurrentRoute(m_CurrentRoutes[0]);
                }
            }

            void SetCurrentRoute(Route route) {
                EGRNavigationRoute r = m_CurrentDirs.Routes[route.Index];

                string dUnits = "M";
                double dist = r.Distance;
                if (r.Distance > 1000d) {
                    dUnits = "KM";
                    dist /= 1000d;
                }

                m_Distance.text = $"{dist:F} {dUnits}";

                string units = "S";
                double dur = r.Duration;
                if (r.Duration > 3600d) {
                    units = "HR";
                    dur /= 3600d;
                }
                else if (r.Duration > 60d) {
                    units = "MIN";
                    dur /= 60d;
                }

                m_Time.text = $"{dur:F} {units}";

                ms_Instance.Client.NavigationManager.SelectedRouteIndex = route.Index;
            }

            void OnRouteClick(Route route) {
                SetCurrentRoute(route);
            }

            void OnStartClick() {
                ms_Instance.Start();
            }

            public void SetStartText(string txt) {
                m_Start.GetComponentInChildren<TextMeshProUGUI>().text = txt;
            }
        }

        class AutoComplete {
            class Item {
                public struct GraphicData {
                    public Graphic Gfx;
                    public Color Color;
                }

                public GameObject Object;
                public RectTransform RectTransform;
                public Image Sprite;
                public TextMeshProUGUI Text;
                public TextMeshProUGUI Address;
                public Button Button;
                public bool Focused;
                public bool FontStatic;
                public GraphicData[] GfxData;
                public EGRGeoAutoCompleteFeature Feature;
            }

            const float AUTOCOMPLETE_REQUEST_DELAY = 1f;

            RectTransform m_Transform;
            ObjectPool<Item> m_ItemPool;
            Item m_DefaultItem;
            Item m_CurrentLocation;
            Item m_ManualMap;
            float m_LastAutoCompleteRequestTime;
            int m_ContextIndex;
            readonly Dictionary<string, EGRGeoAutoComplete> m_RequestCache;
            readonly List<Item> m_Items;
            TMP_InputField m_ActiveInput;
            Item m_LastActiveItem;

            public AutoComplete(RectTransform transform) {
                m_Transform = transform;

                m_ItemPool = new ObjectPool<Item>(() => {
                    Item item = new Item();
                    InitItem(item, Object.Instantiate(m_DefaultItem.Object, m_DefaultItem.Object.transform.parent).transform);
                    return item;
                });

                Transform defaultTrans = m_Transform.Find("Item");
                m_DefaultItem = new Item();
                InitItem(m_DefaultItem, defaultTrans);
                m_DefaultItem.Object.SetActive(false);

                Transform currentTrans = m_Transform.Find("Current");
                m_CurrentLocation = new Item {
                    FontStatic = true
                };
                InitItem(m_CurrentLocation, currentTrans);

                Transform manualTrans = m_Transform.Find("Manual");
                m_ManualMap = new Item {
                    FontStatic = true
                };
                InitItem(m_ManualMap, manualTrans);

                m_ContextIndex = -1;
                m_RequestCache = new Dictionary<string, EGRGeoAutoComplete>();
                m_Items = new List<Item>();
            }

            void InitItem(Item item, Transform itemTransform) {
                item.Object = itemTransform.gameObject;
                item.RectTransform = (RectTransform)itemTransform;
                item.Sprite = itemTransform.Find("Sprite")?.GetComponent<Image>();
                item.Text = itemTransform.Find("Text").GetComponent<TextMeshProUGUI>();
                item.Address = itemTransform.Find("Addr")?.GetComponent<TextMeshProUGUI>();
                item.Button = itemTransform.GetComponent<Button>();

                item.Button.onClick.AddListener(() => OnItemClick(item));
            }

            void ResetActiveItem() {
                if (m_LastActiveItem != null) {
                    m_LastActiveItem.Focused = false;
                    m_LastActiveItem.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);

                    if (!m_LastActiveItem.FontStatic)
                        m_LastActiveItem.Text.fontStyle &= ~FontStyles.Bold;
                }
            }

            void AnimateItem(Item item) {
                if (item.GfxData == null) {
                    Graphic[] gfx = item.Object.GetComponentsInChildren<Graphic>();
                    item.GfxData = new Item.GraphicData[gfx.Length];

                    for (int i = 0; i < gfx.Length; i++) {
                        item.GfxData[i] = new Item.GraphicData {
                            Gfx = gfx[i],
                            Color = gfx[i].color
                        };
                    }
                }

                foreach (Item.GraphicData gfxData in item.GfxData) {
                    gfxData.Gfx.DOColor(gfxData.Color, 0.4f)
                        .ChangeStartValue(gfxData.Color.AlterAlpha(0f))
                        .SetEase(Ease.OutSine);
                }
            }

            void OnItemClick(Item item) {
                if (!item.Focused) {
                    ResetActiveItem();

                    item.Focused = true;
                    m_ActiveInput.SetTextWithoutNotify(item.Text.text);

                    item.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 150f);
                    item.Text.fontStyle |= FontStyles.Bold;

                    m_LastActiveItem = item;
                }
                else {
                    switch (m_ActiveInput.name) {

                        case "From":
                            if (!ms_Instance.m_Top.IsValid(1))
                                ms_Instance.m_Top.SetInputActive(1);

                            ms_Instance.m_Top.SetValidationState(0, true);
                            bool isCurLoc = item == m_CurrentLocation;
                            if (!isCurLoc)
                                ms_Instance.FromCoords = item.Feature.Geometry.Coordinates;

                            ms_Instance.IsFromCurrentLocation = isCurLoc;

                            if (ms_Instance.CanQueryDirections()) {
                                ms_Instance.QueryDirections();
                                SetAutoCompleteState(false);
                            }

                            break;

                        case "To":
                            if (item == m_ManualMap) {
                                ms_Instance.ChooseToLocationManually();
                                break;
                            }

                            ms_Instance.m_Top.SetValidationState(1, true);
                            ms_Instance.ToCoords = item.Feature.Geometry.Coordinates;

                            if (!ms_Instance.CanQueryDirections()) {
                                ms_Instance.m_Top.SetInputActive(0);
                            }
                            else {
                                ms_Instance.QueryDirections();
                                SetAutoCompleteState(false);
                            }

                            break;

                        default:
                            Debug.Log("UNK " + m_ActiveInput.name);
                            break;

                    }
                }
            }

            public void SetActiveInput(TMP_InputField input) {
                m_ActiveInput = input;
                //ResetActiveItem();

                FreeCurrentItems();
            }

            public void SetContext(int idx, string txt) {
                EGRGeoAutoComplete cachedItems;
                if (m_RequestCache.TryGetValue(txt, out cachedItems)) {
                    SetItems(cachedItems);
                    return;
                }

                if (m_ContextIndex == idx && Time.time - m_LastAutoCompleteRequestTime < AUTOCOMPLETE_REQUEST_DELAY)
                    return;

                m_ContextIndex = idx;
                m_LastAutoCompleteRequestTime = Time.time;

                CreateRequest(txt);
            }

            void CreateRequest(string txt) {
                if (string.IsNullOrEmpty(txt) || string.IsNullOrWhiteSpace(txt)) {
                    FreeCurrentItems();
                    return;
                }

                ms_Instance.Client.NetGeoAutoComplete(txt, ms_Instance.Client.FlatMap.CenterLatLng, (res) => OnNetGeoAutoComplete(res, txt));
            }

            void OnNetGeoAutoComplete(PacketInGeoAutoComplete response, string query) {
                EGRGeoAutoComplete results = Newtonsoft.Json.JsonConvert.DeserializeObject<EGRGeoAutoComplete>(response.Response);
                m_RequestCache[query] = results;
                SetItems(results);
            }

            void FreeCurrentItems() {
                ResetActiveItem();

                if (m_Items.Count > 0) {
                    foreach (Item item in m_Items) {
                        item.Object.SetActive(false);
                        m_ItemPool.Free(item);
                    }

                    m_Items.Clear();
                }
            }

            void SetItems(EGRGeoAutoComplete items) {
                FreeCurrentItems();

                foreach (EGRGeoAutoCompleteFeature item in items.Features) {
                    Item autoCompleteItem = m_ItemPool.Rent();
                    autoCompleteItem.Text.text = item.Text;
                    autoCompleteItem.Address.text = item.PlaceName;
                    autoCompleteItem.Feature = item;
                    autoCompleteItem.Object.SetActive(true);

                    m_Items.Add(autoCompleteItem);
                }
            }

            public void SetAutoCompleteState(bool active, bool showCurLoc = true, bool showManual = true) {
                m_Transform.gameObject.SetActive(active);

                if (active) {
                    if (m_CurrentLocation.Object.activeInHierarchy != showCurLoc) {
                        m_CurrentLocation.Object.SetActive(showCurLoc);

                        if (showCurLoc)
                            AnimateItem(m_CurrentLocation);
                    }

                    if (m_ManualMap.Object.activeInHierarchy != showManual) {
                        m_ManualMap.Object.SetActive(showManual);

                        if (showManual)
                            AnimateItem(m_ManualMap);
                    }
                }
            }
        }

        Transform m_NavigationTransform;
        Top m_Top;
        Bottom m_Bottom;
        AutoComplete m_AutoComplete;
        static EGRMapInterfaceComponentNavigation ms_Instance;
        bool m_QueryCancelled;
        bool m_IsManualLocating;

        public override EGRMapInterfaceComponentType ComponentType => EGRMapInterfaceComponentType.Navigation;
        public bool IsActive { get; private set; }
        Vector2d? FromCoords { get; set; }
        Vector2d? ToCoords { get; set; }
        bool IsFromCurrentLocation { get; set; }
        bool IsPreviewStartMode { get; set; }

        public override void OnComponentInit(EGRScreenMapInterface mapInterface) {
            base.OnComponentInit(mapInterface);

            ms_Instance = this;

            m_NavigationTransform = mapInterface.transform.Find("Navigation");
            m_NavigationTransform.gameObject.SetActive(false);

            m_Top = new Top((RectTransform)m_NavigationTransform.Find("Top"));
            m_Bottom = new Bottom((RectTransform)m_NavigationTransform.Find("Bot"));

            m_AutoComplete = new AutoComplete((RectTransform)m_NavigationTransform.Find("Top/AutoComplete"));
            m_AutoComplete.SetAutoCompleteState(false);
        }

        public override void OnComponentUpdate() {
            m_Bottom.Update();
        }

        public void Show() {
            m_NavigationTransform.gameObject.SetActive(true);
            m_Top.Show();

            Client.Runnable.RunLater(m_Bottom.ShowBackButton, 0.5f);

            IsActive = true;
            FromCoords = ToCoords = null;
            IsFromCurrentLocation = false;
        }

        public bool Hide() {
            m_Top.Hide();
            m_Bottom.Hide();

            if (!m_IsManualLocating) {
                Client.Runnable.RunLater(() => {
                    m_NavigationTransform.gameObject.SetActive(false);
                    m_Bottom.ClearDirections();

                    Client.NavigationManager.ExitNavigation();
                    Client.FlatCamera.ExitNavigation();

                    IsActive = false;
                }, 0.3f);

                return true;
            }
            else {
                MapInterface.LocationOverlay.Finish();
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
                        MapInterface.MessageBox.ShowPopup(Localize(EGRLanguageData.EGR), Localize(EGRLanguageData.CANNOT_OBTAIN_CURRENT_LOCATION), null, MapInterface);
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
                MapInterface.MessageBox.ShowPopup(Localize(EGRLanguageData.EGR), Localize(EGRLanguageData.RETRIEVING_CURRENT_LOCATION___), null, MapInterface);

                Client.LocationService.GetCurrentLocation(OnReceiveLocation, true);
                return;
            }

            if (!Client.NetQueryDirections(FromCoords.Value, ToCoords.Value, m_Top.SelectedProfile, OnNetQueryDirections)) {
                MapInterface.MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), 
                    string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, MapInterface);

                return;
            }

            m_QueryCancelled = false;

            MapInterface.MessageBox.SetOkButtonText(Localize(EGRLanguageData.CANCEL));
            MapInterface.MessageBox.ShowPopup(Localize(EGRLanguageData.NAVIGATION), Localize(EGRLanguageData.FINDING_AVAILABLE_ROUTES), OnPopupCallback, MapInterface);
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

        void ChooseToLocationManually() {
            m_IsManualLocating = true;

            m_Top.Hide();
            m_AutoComplete.SetAutoCompleteState(false);
            m_Bottom.ShowBackButton();

            MapInterface.LocationOverlay.ChooseLocationOnMap((geo) => {
                m_IsManualLocating = false;
                ToCoords = geo;

                m_Top.ToInput.SetTextWithoutNotify($"[{geo.y:F5}, {geo.x:F5}]");
                m_Top.SetValidationState(1, true);
                m_Top.Show(false);

                if (!CanQueryDirections()) {
                    ms_Instance.m_Top.SetInputActive(0);
                }
                else {
                    ms_Instance.QueryDirections();
                    m_AutoComplete.SetAutoCompleteState(false);
                }
            });
        }

        void Start() {
            Client.NavigationManager.StartNavigation(IsPreviewStartMode);
        }
    }
}
