using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public class EGRScreenPlaceList : EGRScreen {
        class SearchArea {
            TMP_InputField m_Input;
            bool m_InputVisible;
            RectTransform m_InputRectTransform;
            float m_InputTweenProgress;
            float m_InputHiddenOffsetMin;

            public SearchArea(Transform transform) {
                transform.GetElement<Button>("Button").onClick.AddListener(OnButtonClick);

                m_Input = transform.GetElement<TMP_InputField>("Input");
                m_InputRectTransform = (RectTransform)m_Input.transform;
                m_InputHiddenOffsetMin = m_InputRectTransform.rect.width;
            }

            void OnButtonClick() {
                m_InputVisible = !m_InputVisible;

                if (m_InputVisible)
                    Show();
                else
                    Hide();
            }

            public void Hide() {
                m_InputVisible = false;
                //upon diagnosis of the current input anchors, I concluded the following:
                //sizeDelta_HIDDEN = sizeDelta_INITIAL.x - rect_INITIAL.width
                //sizeDelta_SHOWN = sizeDelta_INITIAL.x

                DOTween.To(() => m_InputTweenProgress, x => m_InputTweenProgress = x, 1f, 0.3f)
                    .SetEase(Ease.OutSine)
                    .OnUpdate(OnInputTweenUpdate);
            }

            public void Show() {
                m_InputVisible = true;

                DOTween.To(() => m_InputTweenProgress, x => m_InputTweenProgress = x, 0f, 0.3f)
                    .SetEase(Ease.OutSine)
                    .OnUpdate(OnInputTweenUpdate);
            }

            void OnInputTweenUpdate() {
                m_InputRectTransform.offsetMin = new Vector2(Mathf.Lerp(0f, m_InputHiddenOffsetMin, m_InputTweenProgress), m_InputRectTransform.offsetMin.y);
            }
        }

        class PlaceItem {
            Transform m_Transform;
            RawImage m_Image;
            TextMeshProUGUI m_Name;
            TextMeshProUGUI m_Tags;

            public PlaceItem(Transform transform) {
                m_Transform = transform;
                m_Image = transform.GetElement<RawImage>("Icon");
                m_Name = transform.GetElement<TextMeshProUGUI>("Name");
                m_Tags = transform.GetElement<TextMeshProUGUI>("Tags");
            }

            public void SetInfo(string name, string tags, Texture2D img = null) {
                m_Name.text = name;
                m_Tags.text = tags;
                m_Image.texture = img;
            }

            public void SetActive(bool active) {
                m_Transform.gameObject.SetActive(active);
            }
        }

        SearchArea m_SearchArea;
        readonly ObjectPool<PlaceItem> m_PlaceItemPool;
        GameObject m_PlaceItemPrefab;
        TextMeshProUGUI m_ResultLabel;
        readonly List<PlaceItem> m_Items;

        public EGRScreenPlaceList() {
            m_PlaceItemPool = new ObjectPool<PlaceItem>(() => {
                Transform trans = Instantiate(m_PlaceItemPrefab, m_PlaceItemPrefab.transform.parent).transform;
                PlaceItem item = new PlaceItem(trans);
                return item;
            });

            m_Items = new List<PlaceItem>();
        }

        protected override void OnScreenInit() {
            m_SearchArea = new SearchArea(GetTransform("Search"));
            m_PlaceItemPrefab = GetTransform("Scroll View/Viewport/Content/Item").gameObject;
            m_PlaceItemPrefab.SetActive(false);

            m_ResultLabel = GetElement<TextMeshProUGUI>("txtSub");

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);
        }

        protected override void OnScreenShow() {
            m_SearchArea.Hide();

            SetResultsText(0);
        }

        protected override void OnScreenHide() {
            foreach (PlaceItem item in m_Items) {
                item.SetActive(false);
                m_PlaceItemPool.Free(item);
            }

            m_Items.Clear();
        }

        void SetResultsText(int n) {
            m_ResultLabel.text = string.Format(Localize(EGRLanguageData._b__0___b__RESULTS), n);
        }

        public void SetPlaces(List<EGRWTEProxyPlace> places) {
            foreach (EGRWTEProxyPlace place in places) {
                PlaceItem item = m_PlaceItemPool.Rent();
                item.SetInfo(place.Name, place.Tags.StringifyList(", "));
                item.SetActive(true);
            }

            SetResultsText(places.Count);
        }

        void OnBackClick() {
            HideScreen();
        }
    }
}