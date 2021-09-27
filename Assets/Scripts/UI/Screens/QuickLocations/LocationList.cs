using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public partial class EGRScreenQuickLocations {
        class LocationList {
            class Item {
                RectTransform m_Transform;
                TextMeshProUGUI m_Name;
                EGRQuickLocation m_Location;

                public Item(RectTransform transform) {
                    m_Transform = transform;

                    m_Name = transform.GetElement<TextMeshProUGUI>("Name");

                    transform.GetComponent<Button>().onClick.AddListener(OnButtonClick);
                }

                public void SetActive(bool active) {
                    m_Transform.gameObject.SetActive(active);
                }

                public void SetLocation(EGRQuickLocation location) {
                    m_Location = location;
                    m_Name.text = m_Location.Name;
                }

                void OnButtonClick() {
                    ms_Instance.OpenDetailedView(m_Location);
                }
            }

            GameObject m_ItemPrefab;
            readonly ObjectPool<Item> m_ItemPool;
            readonly List<Item> m_ActiveItems;
            RectTransform m_Transform;
            ScrollRect m_ScrollRect;

            public int ItemCount => m_ActiveItems.Count;
            public Transform ContentTransform { get; private set; }
            public RectTransform Other { get; private set; }

            public LocationList(RectTransform transform) {
                m_Transform = transform;

                ContentTransform = transform.Find("Viewport/Content");
                m_ItemPrefab = ContentTransform.Find("Item").gameObject;
                m_ItemPrefab.SetActive(false);

                Other = (RectTransform)transform.Find("Other");
                m_ScrollRect = transform.GetComponent<ScrollRect>();

                m_ItemPool = new ObjectPool<Item>(() => {
                    GameObject obj = Instantiate(m_ItemPrefab, m_ItemPrefab.transform.parent);
                    Item item = new Item((RectTransform)obj.transform);
                    return item;
                });

                m_ActiveItems = new List<Item>();
            }

            public void SetLocations(List<EGRQuickLocation> locs) {
                int dif = m_ActiveItems.Count - locs.Count;
                if (dif > 0) {
                    for (int i = 0; i < dif; i++) {
                        Item item = m_ActiveItems[0];
                        item.SetActive(false);
                        m_ActiveItems.RemoveAt(0);
                        m_ItemPool.Free(item);
                    }
                }
                else if (dif < 0) {
                    for (int i = 0; i < -dif; i++) {
                        Item item = m_ItemPool.Rent();
                        m_ActiveItems.Add(item);
                    }
                }

                for (int i = 0; i < locs.Count; i++) {
                    Item item = m_ActiveItems[i];
                    item.SetActive(true);
                    item.SetLocation(locs[i]);
                }

                ms_Instance.Client.Runnable.RunLaterFrames(UpdateOtherPosition, 1);
            }

            public void SetActive(bool active) {
                m_Transform.gameObject.SetActive(active);
            }

            public void UpdateOtherPosition() {
                return;

                Rect viewportRect = m_ScrollRect.viewport.rect;
                Rect contentRect = ((RectTransform)ContentTransform).rect;

                //check if contentRect bottom is below other
                float baseY = contentRect.y < viewportRect.y ? m_ScrollRect.viewport.position.y - (viewportRect.height * 1.1f)
                    : ContentTransform.position.y - contentRect.height * 1.1f;

                Debug.Log($"METHOD={contentRect.y < viewportRect.y}, out baseY={baseY}");

                Vector3 oldPos = Other.position;
                Other.position = new Vector3(oldPos.x, baseY - Other.rect.height * 0.5f, oldPos.z);
            }
        }
    }
}
