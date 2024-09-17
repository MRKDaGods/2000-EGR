using DG.Tweening;
using MRK.Networking.Packets;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface
{
    public partial class Navigation
    {
        private class AutoComplete
        {
            private class Item
            {
                public struct GraphicData
                {
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

            private struct Context
            {
                public string Query;
                public float Time;
                public int Index;
            }

            private const float AutoCompleteRequestDelay = 0.15f;

            private RectTransform _transform;
            private ObjectPool<Item> _itemPool;
            private Item _defaultItem;
            private Item _currentLocation;
            private Item _manualMap;
            private float _lastAutoCompleteRequestTime;
            private int _contextIndex;
            private readonly Dictionary<string, EGRGeoAutoComplete> _requestCache;
            private readonly List<Item> _items;
            private TMP_InputField _activeInput;
            private Item _lastActiveItem;
            private Context? _lastContext;

            public bool IsActive
            {
                get
                {
                    return _transform.gameObject.activeInHierarchy;
                }
            }

            public AutoComplete(RectTransform transform)
            {
                _transform = transform;

                _itemPool = new ObjectPool<Item>(() =>
                {
                    Item item = new Item();
                    InitItem(item, Object.Instantiate(_defaultItem.Object, _defaultItem.Object.transform.parent).transform);
                    return item;
                });

                Transform defaultTrans = _transform.Find("Item");
                _defaultItem = new Item();
                InitItem(_defaultItem, defaultTrans);
                _defaultItem.Object.SetActive(false);

                Transform currentTrans = _transform.Find("Current");
                _currentLocation = new Item
                {
                    FontStatic = true
                };
                InitItem(_currentLocation, currentTrans);

                Transform manualTrans = _transform.Find("Manual");
                _manualMap = new Item
                {
                    FontStatic = true
                };
                InitItem(_manualMap, manualTrans);

                _contextIndex = -1;
                _requestCache = new Dictionary<string, EGRGeoAutoComplete>();
                _items = new List<Item>();
            }

            private void InitItem(Item item, Transform itemTransform)
            {
                item.Object = itemTransform.gameObject;
                item.RectTransform = (RectTransform)itemTransform;
                item.Sprite = itemTransform.Find("Sprite")?.GetComponent<Image>();
                item.Text = itemTransform.Find("Text").GetComponent<TextMeshProUGUI>();
                item.Address = itemTransform.Find("Addr")?.GetComponent<TextMeshProUGUI>();
                item.Button = itemTransform.GetComponent<Button>();

                item.Button.onClick.AddListener(() => OnItemClick(item));
            }

            private void ResetActiveItem()
            {
                if (_lastActiveItem != null)
                {
                    _lastActiveItem.Focused = false;
                    _lastActiveItem.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);

                    if (!_lastActiveItem.FontStatic)
                        _lastActiveItem.Text.fontStyle &= ~FontStyles.Bold;
                }
            }

            private void AnimateItem(Item item)
            {
                if (item.GfxData == null)
                {
                    Graphic[] gfx = item.Object.GetComponentsInChildren<Graphic>();
                    item.GfxData = new Item.GraphicData[gfx.Length];

                    for (int i = 0; i < gfx.Length; i++)
                    {
                        item.GfxData[i] = new Item.GraphicData
                        {
                            Gfx = gfx[i],
                            Color = gfx[i].color
                        };
                    }
                }

                foreach (Item.GraphicData gfxData in item.GfxData)
                {
                    gfxData.Gfx.DOColor(gfxData.Color, 0.4f)
                        .ChangeStartValue(gfxData.Color.AlterAlpha(0f))
                        .SetEase(Ease.OutSine);
                }
            }

            private void OnItemClick(Item item)
            {
                if (!item.Focused)
                {
                    ResetActiveItem();

                    item.Focused = true;
                    _activeInput.SetTextWithoutNotify(item.Text.text);

                    item.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 150f);
                    item.Text.fontStyle |= FontStyles.Bold;

                    _lastActiveItem = item;
                }
                else
                {
                    switch (_activeInput.name)
                    {

                        case "From":
                            if (item == _manualMap)
                            {
                                _instance.IsFromCurrentLocation = false;
                                _instance.ChooseLocationManually(0);
                                break;
                            }

                            if (!_instance._top.IsValid(1))
                                _instance._top.SetInputActive(1);

                            _instance._top.SetValidationState(0, true);
                            bool isCurLoc = item == _currentLocation;
                            if (!isCurLoc)
                                _instance.FromCoords = item.Feature.Geometry.Coordinates;

                            _instance.IsFromCurrentLocation = isCurLoc;

                            if (_instance.CanQueryDirections())
                            {
                                _instance.QueryDirections();
                                SetAutoCompleteState(false);
                            }

                            break;

                        case "To":
                            if (item == _manualMap)
                            {
                                _instance.ChooseLocationManually(1);
                                break;
                            }

                            _instance._top.SetValidationState(1, true);
                            _instance.ToCoords = item.Feature.Geometry.Coordinates;

                            if (!_instance.CanQueryDirections())
                            {
                                _instance._top.SetInputActive(0);
                            }
                            else
                            {
                                _instance.QueryDirections();
                                SetAutoCompleteState(false);
                            }

                            break;

                        default:
                            Debug.Log("UNK " + _activeInput.name);
                            break;

                    }
                }
            }

            public void SetActiveInput(TMP_InputField input)
            {
                _activeInput = input;
                //ResetActiveItem();

                FreeCurrentItems();
            }

            public void Update()
            {
                if (_lastContext.HasValue)
                {
                    if (Time.time - _lastContext.Value.Time > AutoCompleteRequestDelay)
                    {
                        CreateRequest(_lastContext.Value.Query);
                        _lastContext = null;
                    }
                }
            }

            public void SetContext(int idx, string txt)
            {
                EGRGeoAutoComplete cachedItems;
                if (_requestCache.TryGetValue(txt, out cachedItems))
                {
                    SetItems(cachedItems);
                    return;
                }

                _lastContext = new Context
                {
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

            private void CreateRequest(string txt)
            {
                if (string.IsNullOrEmpty(txt) || string.IsNullOrWhiteSpace(txt))
                {
                    FreeCurrentItems();
                    return;
                }

                _instance.Client.NetworkingClient.MainNetworkExternal.GeoAutoComplete(txt, _instance.Client.FlatMap.CenterLatLng, (res) => OnNetGeoAutoComplete(res, txt));
            }

            private void OnNetGeoAutoComplete(PacketInGeoAutoComplete response, string query)
            {
                EGRGeoAutoComplete results = Newtonsoft.Json.JsonConvert.DeserializeObject<EGRGeoAutoComplete>(response.Response);
                _requestCache[query] = results;
                SetItems(results);
            }

            private void FreeCurrentItems()
            {
                ResetActiveItem();

                if (_items.Count > 0)
                {
                    foreach (Item item in _items)
                    {
                        item.Object.SetActive(false);
                        _itemPool.Free(item);
                    }

                    _items.Clear();
                }
            }

            private void SetItems(EGRGeoAutoComplete items)
            {
                FreeCurrentItems();

                foreach (EGRGeoAutoCompleteFeature item in items.Features)
                {
                    Item autoCompleteItem = _itemPool.Rent();
                    autoCompleteItem.Text.text = item.Text;
                    autoCompleteItem.Address.text = item.PlaceName;
                    autoCompleteItem.Feature = item;
                    autoCompleteItem.Object.SetActive(true);

                    _items.Add(autoCompleteItem);
                }
            }

            public void SetAutoCompleteState(bool active, bool showCurLoc = true, bool showManual = true)
            {
                _transform.gameObject.SetActive(active);

                if (active)
                {
                    if (_currentLocation.Object.activeInHierarchy != showCurLoc)
                    {
                        _currentLocation.Object.SetActive(showCurLoc);

                        if (showCurLoc)
                            AnimateItem(_currentLocation);
                    }

                    if (_manualMap.Object.activeInHierarchy != showManual)
                    {
                        _manualMap.Object.SetActive(showManual);

                        if (showManual)
                            AnimateItem(_manualMap);
                    }
                }
            }
        }
    }
}