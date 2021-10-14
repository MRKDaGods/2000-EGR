using DG.Tweening;
using MRK.Networking.Packets;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface {
    public partial class EGRMapInterfaceComponentNavigation {
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

            struct Context {
                public string Query;
                public float Time;
                public int Index;
            }

            const float AUTOCOMPLETE_REQUEST_DELAY = 0.15f;

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
            Context? m_LastContext;

            public bool IsActive => m_Transform.gameObject.activeInHierarchy;

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
                            if (item == m_ManualMap) {
                                ms_Instance.IsFromCurrentLocation = false;
                                ms_Instance.ChooseLocationManually(0);
                                break;
                            }

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
                                ms_Instance.ChooseLocationManually(1);
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

            public void Update() {
                if (m_LastContext.HasValue) {
                    if (Time.time - m_LastContext.Value.Time > AUTOCOMPLETE_REQUEST_DELAY) {
                        CreateRequest(m_LastContext.Value.Query);
                        m_LastContext = null;
                    }
                }
            }

            public void SetContext(int idx, string txt) {
                EGRGeoAutoComplete cachedItems;
                if (m_RequestCache.TryGetValue(txt, out cachedItems)) {
                    SetItems(cachedItems);
                    return;
                }

                m_LastContext = new Context {
                    Query = txt,
                    Time = Time.time,
                    Index = idx
                };

                /*if (m_ContextIndex == idx && Time.time - m_LastAutoCompleteRequestTime < AUTOCOMPLETE_REQUEST_DELAY)
                    return;

                m_ContextIndex = idx;
                m_LastAutoCompleteRequestTime = Time.time;*/

                //CreateRequest(txt);
            }

            void CreateRequest(string txt) {
                if (string.IsNullOrEmpty(txt) || string.IsNullOrWhiteSpace(txt)) {
                    FreeCurrentItems();
                    return;
                }

                ms_Instance.Client.NetworkingClient.MainNetworkExternal.GeoAutoComplete(txt, ms_Instance.Client.FlatMap.CenterLatLng, (res) => OnNetGeoAutoComplete(res, txt));
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
    }
}