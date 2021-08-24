using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRPopupPlaceGroup : EGRPopup {
        class Item {
            const float ITEM_SIZE = 600f;
            const float MID_SIZE = 300f;

            RectTransform m_Transform;
            RectTransform m_Mid;
            TextMeshProUGUI m_Title;
            TextMeshProUGUI m_Type;
            TextMeshProUGUI m_Ex;
            Image m_Sprite;
            TextMeshProUGUI m_Address;
            bool m_State;
            Scrollbar m_Scroll;
            bool m_MoreShown;
            int m_ScrollTween;
            float m_SizeProgress;
            int m_SizeTween;
            EGRPlace m_Place;
            static EGRScreenMapInterface ms_MapInterface;

            public float Size => m_State ? ITEM_SIZE : ITEM_SIZE - MID_SIZE;

            public Item(Transform transform) {
                m_Transform = (RectTransform)transform;
                m_Mid = (RectTransform)m_Transform.Find("Mid");

                m_Transform.GetComponent<Button>().onClick.AddListener(OnMaskButtonClick);
                m_Mid.Find("Scroll View/Viewport/Content/MaskButton").GetComponent<Button>().onClick.AddListener(OnInternalMaskButtonClick);

                m_Title = m_Transform.Find("Top/Title").GetComponent<TextMeshProUGUI>();
                m_Type = m_Transform.Find("Top/Type").GetComponent<TextMeshProUGUI>();
                m_Ex = m_Mid.Find("Scroll View/Viewport/Content/Ex").GetComponent<TextMeshProUGUI>();
                m_Sprite = m_Transform.Find("Top/Image").GetComponent<Image>();
                m_Address = m_Transform.Find("Bot/Address").GetComponent<TextMeshProUGUI>();

                m_Scroll = m_Mid.Find("Scroll View").GetComponent<ScrollRect>().horizontalScrollbar;

                SetState(false);
            }

            void OnInternalMaskButtonClick() {
                m_MoreShown = !m_MoreShown;

                if (m_ScrollTween.IsValidTween())
                    DOTween.Kill(m_ScrollTween);

                m_ScrollTween = DOTween.To(() => m_Scroll.value, x => m_Scroll.value = x, m_MoreShown ? 1f : 0f, 0.5f)
                    .SetEase(Ease.OutBack)
                    .intId = EGRTweenIDs.IntId;
            }

            void OnMaskButtonClick() {
                SetState(!m_State);
            }

            void SetState(bool active, bool force = false) {
                m_State = active;

                if (active) {
                    m_Scroll.value = 0f;
                }

                if (!force) {
                    if (m_SizeTween.IsValidTween())
                        DOTween.Kill(m_SizeTween);

                    m_SizeTween = DOTween.To(() => m_SizeProgress, x => m_SizeProgress = x, active ? 1f : 0f, 0.5f)
                        .SetEase(Ease.OutBack)
                        .OnUpdate(() => {
                            m_Transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.LerpUnclamped(ITEM_SIZE - MID_SIZE, ITEM_SIZE, m_SizeProgress));
                            m_Mid.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.LerpUnclamped(0f, MID_SIZE, m_SizeProgress));
                        }).intId = EGRTweenIDs.IntId;
                }
                else {
                    m_SizeProgress = active ? 1f : 0f;
                    m_Transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(ITEM_SIZE - MID_SIZE, ITEM_SIZE, m_SizeProgress));
                    m_Mid.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(0f, MID_SIZE, m_SizeProgress));
                }

                ms_Instance.RecalculateContentSize();
            }

            public void SetPlace(EGRPlace place) {
                m_Place = place;

                m_Title.text = m_Place.Name;
                m_Type.text = m_Place.Type;

                string ex = "";
                bool _redirected = false;

            __redirection:
                if (place.Ex.Length == 0 || _redirected) {
                    ex = "NO DETAILS AVAILABLE";
                }
                else {
                    int count = 0;
                    foreach (string _ex in place.Ex) {
                        if (count == 3)
                            break;

                        if (string.IsNullOrEmpty(_ex) || string.IsNullOrWhiteSpace(_ex) || _ex.Length == 1)
                            continue;

                        ex += $"{_ex}\n";
                        count++;
                    }
                }

                if (!_redirected) {
                    string fakeEx = ex.Replace("\n", "");
                    if (string.IsNullOrWhiteSpace(fakeEx) || string.IsNullOrEmpty(fakeEx)) {
                        _redirected = true;
                        goto __redirection;
                    }
                    else {
                        ex = ex.Remove(ex.Length - 1);
                    }
                }

                m_Ex.text = ex;

                if (ms_MapInterface == null) {
                    ms_MapInterface = ms_Instance.ScreenManager.GetScreen<EGRScreenMapInterface>();
                }

                m_Sprite.sprite = ms_MapInterface.GetSpriteForPlaceType(m_Place.Types[Mathf.Min(2, m_Place.Types.Length) - 1]);
                m_Address.text = m_Place.Address;
            }

            IEnumerator DelayedItemShow(float secs) {
                yield return new WaitForSeconds(secs);

                m_Transform.DOScale(Vector3.one, 0.2f)
                    .ChangeStartValue(new Vector3(0f, 0f, 1f))
                    .SetEase(Ease.OutSine);
            }

            IEnumerator DelayedItemHide(float secs, Action callback) {
                yield return new WaitForSeconds(secs);

                m_Transform.DOScale(new Vector3(0f, 0f, 1f), 0.2f)
                    .ChangeStartValue(Vector3.one)
                    .SetEase(Ease.OutSine);

                yield return new WaitForSeconds(0.2f);
                callback();
            }

            public void OnItemShow(int idx) {
                //enable us :)
                m_Transform.localScale = new Vector3(0f, 0f, 1f);

                m_Scroll.value = 0f;
                m_MoreShown = false;
                SetState(false, true);

                m_Transform.SetSiblingIndex(idx);
                m_Transform.gameObject.SetActive(true);
                ms_Instance.Client.Runnable.Run(DelayedItemShow(idx * (0.2f / ms_Instance.m_Items.Count)));
            }

            public void OnItemHide(int idx, Action callback) {
                ms_Instance.Client.Runnable.Run(DelayedItemHide(idx * (0.2f / ms_Instance.m_Items.Count), callback));
            }

            public void Disable() {
                m_Transform.gameObject.SetActive(false);
            }
        }

        static EGRPopupPlaceGroup ms_Instance;
        GameObject m_ItemPrefab;
        readonly ObjectPool<Item> m_ItemPool;
        readonly List<Item> m_Items;
        Image m_Blur;
        Color m_BlurColor;
        RectTransform m_Content;
        Scrollbar m_ContentScroll;

        public EGRPopupPlaceGroup() {
            m_ItemPool = new ObjectPool<Item>(() => {
                return new Item(Instantiate(m_ItemPrefab, m_ItemPrefab.transform.parent).transform);
            });

            m_Items = new List<Item>();
        }

        protected override void OnScreenInit() {
            ms_Instance = this;

            m_Blur = GetElement<Image>("imgBlur");
            m_Blur.GetComponent<Button>().onClick.AddListener(OnBlurClick);
            m_BlurColor = m_Blur.color;

            GetElement<Button>("Scroll View/Viewport/BlurMask").onClick.AddListener(OnBlurClick);

            m_ItemPrefab = GetTransform("Scroll View/Viewport/Content/Item").gameObject;
            //m_Item = new Item(m_ItemPrefab.transform); //quick disable stuff, gc? fuck it | uh nvm
            m_ItemPrefab.SetActive(false);

            m_Content = (RectTransform)GetTransform("Scroll View/Viewport/Content");
            m_ContentScroll = GetElement<ScrollRect>("Scroll View").verticalScrollbar;
        }

        public void SetGroup(EGRPlaceGroup group) {
            EGRPlaceMarker owner = group.Owner;
            Item ownerItem = m_ItemPool.Rent();
            ownerItem.SetPlace(owner.Place);
            m_Items.Add(ownerItem);

            foreach (EGRPlaceMarker child in owner.Overlappers) {
                Item item = m_ItemPool.Rent();
                item.SetPlace(child.Place);
                m_Items.Add(item);
            }
        }

        protected override void OnScreenShow() {
            for (int i = 0; i < m_Items.Count; i++) {
                m_Items[i].OnItemShow(i);
            }

            RecalculateContentSize();
            m_ContentScroll.value = 1f;
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            m_Blur.DOColor(m_BlurColor, TweenMonitored(0.3f))
                .ChangeStartValue(Color.clear)
                .SetEase(Ease.OutSine);
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            SetTweenCount(m_Items.Count + 1);

            for (int i = 0; i < m_Items.Count; i++) {
                m_Items[i].OnItemHide(i, OnTweenFinished);
            }

            m_Blur.DOColor(Color.clear, TweenMonitored(0.3f))
                .SetEase(Ease.OutSine)
                .OnComplete(OnTweenFinished);

            return true;
        }

        protected override void OnScreenHide() {
            for (int i = 0; i < m_Items.Count; i++) {
                m_Items[i].Disable();
                m_ItemPool.Free(m_Items[i]);
            }

            m_Items.Clear();
            Client.ActiveEGRCamera.ResetStates();
        }

        void OnBlurClick() {
            HideScreen();
        }

        void RecalculateContentSize() {
            float size = 0f;
            for (int i = 0; i < m_Items.Count; i++) {
                size += m_Items[i].Size;
            }

            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1716f, size));
        }

        public void Warmup() {
            m_ItemPool.Reserve(50);
        }
    }
}
