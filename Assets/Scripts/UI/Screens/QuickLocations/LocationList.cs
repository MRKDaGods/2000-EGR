using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public partial class QuickLocations
    {
        private class LocationList
        {
            private class Item
            {
                private RectTransform _transform;
                private TextMeshProUGUI _name;
                private EGRQuickLocation _location;

                public Item(RectTransform transform)
                {
                    _transform = transform;
                    _name = transform.GetElement<TextMeshProUGUI>("Name");

                    transform.GetComponent<Button>().onClick.AddListener(OnButtonClick);
                }

                public void SetActive(bool active)
                {
                    _transform.gameObject.SetActive(active);
                }

                public void SetLocation(EGRQuickLocation location)
                {
                    _location = location;
                    _name.text = _location.Name;
                }

                private void OnButtonClick()
                {
                    _instance.OpenDetailedView(_location);
                }
            }

            private GameObject _itemPrefab;
            private readonly ObjectPool<Item> _itemPool;
            private readonly List<Item> _activeItems;
            private RectTransform _transform;
            private ScrollRect _scrollRect;

            public int ItemCount
            {
                get
                {
                    return _activeItems.Count;
                }
            }

            public Transform ContentTransform
            {
                get; private set;
            }

            public RectTransform Other
            {
                get; private set;
            }

            public LocationList(RectTransform transform)
            {
                _transform = transform;

                ContentTransform = transform.Find("Viewport/Content");
                _itemPrefab = ContentTransform.Find("Item").gameObject;
                _itemPrefab.SetActive(false);

                Other = (RectTransform)transform.Find("Other");
                _scrollRect = transform.GetComponent<ScrollRect>();

                _itemPool = new ObjectPool<Item>(() =>
                {
                    GameObject obj = Instantiate(_itemPrefab, _itemPrefab.transform.parent);
                    Item item = new Item((RectTransform)obj.transform);
                    return item;
                });

                _activeItems = new List<Item>();
            }

            public void SetLocations(List<EGRQuickLocation> locs)
            {
                int dif = _activeItems.Count - locs.Count;
                if (dif > 0)
                {
                    for (int i = 0; i < dif; i++)
                    {
                        Item item = _activeItems[0];
                        item.SetActive(false);
                        _activeItems.RemoveAt(0);
                        _itemPool.Free(item);
                    }
                }
                else if (dif < 0)
                {
                    for (int i = 0; i < -dif; i++)
                    {
                        Item item = _itemPool.Rent();
                        _activeItems.Add(item);
                    }
                }

                for (int i = 0; i < locs.Count; i++)
                {
                    Item item = _activeItems[i];
                    item.SetActive(true);
                    item.SetLocation(locs[i]);
                }

                _instance.Client.Runnable.RunLaterFrames(UpdateOtherPosition, 1);
            }

            public void SetActive(bool active)
            {
                _transform.gameObject.SetActive(active);
            }

            public void UpdateOtherPosition()
            {
                return;

                Rect viewportRect = _scrollRect.viewport.rect;
                Rect contentRect = ((RectTransform)ContentTransform).rect;

                //check if contentRect bottom is below other
                float baseY = contentRect.y < viewportRect.y ? _scrollRect.viewport.position.y - (viewportRect.height * 1.1f)
                    : ContentTransform.position.y - contentRect.height * 1.1f;

                Debug.Log($"METHOD={contentRect.y < viewportRect.y}, out baseY={baseY}");

                Vector3 oldPos = Other.position;
                Other.position = new Vector3(oldPos.x, baseY - Other.rect.height * 0.5f, oldPos.z);
            }
        }
    }
}
