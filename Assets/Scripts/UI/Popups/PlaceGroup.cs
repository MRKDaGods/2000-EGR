using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public class PlaceGroup : Popup
    {
        private class Item
        {
            private const float ItemSize = 600f;
            private const float MidSize = 300f;

            private RectTransform _transform;
            private RectTransform _mid;
            private TextMeshProUGUI _title;
            private TextMeshProUGUI _type;
            private TextMeshProUGUI _ex;
            private Image _sprite;
            private TextMeshProUGUI _address;
            private bool _state;
            private Scrollbar _scroll;
            private bool _moreShown;
            private int _scrollTween;
            private float _sizeProgress;
            private int _sizeTween;
            private EGRPlace _place;

            private static MapInterface _mapInterface;

            public float Size
            {
                get
                {
                    return _state ? ItemSize : ItemSize - MidSize;
                }
            }

            public Item(Transform transform)
            {
                _transform = (RectTransform)transform;
                _mid = (RectTransform)_transform.Find("Mid");

                _transform.GetComponent<Button>().onClick.AddListener(OnMaskButtonClick);
                _mid.Find("Scroll View/Viewport/Content/MaskButton").GetComponent<Button>().onClick.AddListener(OnInternalMaskButtonClick);

                _title = _transform.Find("Top/Title").GetComponent<TextMeshProUGUI>();
                _type = _transform.Find("Top/Type").GetComponent<TextMeshProUGUI>();
                _ex = _mid.Find("Scroll View/Viewport/Content/Ex").GetComponent<TextMeshProUGUI>();
                _sprite = _transform.Find("Top/Image").GetComponent<Image>();
                _address = _transform.Find("Bot/Address").GetComponent<TextMeshProUGUI>();

                _scroll = _mid.Find("Scroll View").GetComponent<ScrollRect>().horizontalScrollbar;

                SetState(false);
            }

            private void OnInternalMaskButtonClick()
            {
                _moreShown = !_moreShown;

                if (_scrollTween.IsValidTween())
                    DOTween.Kill(_scrollTween);

                _scrollTween = DOTween.To(() => _scroll.value, x => _scroll.value = x, _moreShown ? 1f : 0f, 0.5f)
                    .SetEase(Ease.OutBack)
                    .intId = EGRTweenIDs.IntId;
            }

            private void OnMaskButtonClick()
            {
                SetState(!_state);
            }

            private void SetState(bool active, bool force = false)
            {
                _state = active;

                if (active)
                {
                    _scroll.value = 0f;
                }

                if (!force)
                {
                    if (_sizeTween.IsValidTween())
                        DOTween.Kill(_sizeTween);

                    _sizeTween = DOTween.To(() => _sizeProgress, x => _sizeProgress = x, active ? 1f : 0f, 0.5f)
                        .SetEase(Ease.OutBack)
                        .OnUpdate(() =>
                        {
                            _transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.LerpUnclamped(ItemSize - MidSize, ItemSize, _sizeProgress));
                            _mid.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.LerpUnclamped(0f, MidSize, _sizeProgress));
                        }).intId = EGRTweenIDs.IntId;
                }
                else
                {
                    _sizeProgress = active ? 1f : 0f;
                    _transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(ItemSize - MidSize, ItemSize, _sizeProgress));
                    _mid.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(0f, MidSize, _sizeProgress));
                }

                _instance.RecalculateContentSize();
            }

            public void SetPlace(EGRPlace place)
            {
                _place = place;

                _title.text = _place.Name;
                _type.text = _place.Type;

                string ex = "";
                bool _redirected = false;

            __redirection:
                if (place.Ex.Length == 0 || _redirected)
                {
                    ex = "NO DETAILS AVAILABLE";
                }
                else
                {
                    int count = 0;
                    foreach (string _ex in place.Ex)
                    {
                        if (count == 3)
                            break;

                        if (string.IsNullOrEmpty(_ex) || string.IsNullOrWhiteSpace(_ex) || _ex.Length == 1)
                            continue;

                        ex += $"{_ex}\n";
                        count++;
                    }
                }

                if (!_redirected)
                {
                    string fakeEx = ex.Replace("\n", "");
                    if (string.IsNullOrWhiteSpace(fakeEx) || string.IsNullOrEmpty(fakeEx))
                    {
                        _redirected = true;
                        goto __redirection;
                    }
                    else
                    {
                        ex = ex.Remove(ex.Length - 1);
                    }
                }

                _ex.text = ex;

                if (_mapInterface == null)
                {
                    _mapInterface = _instance.ScreenManager.GetScreen<MapInterface>();
                }

                _sprite.sprite = _mapInterface.GetSpriteForPlaceType(_place.Types[Mathf.Min(2, _place.Types.Length) - 1]);
                _address.text = _place.Address;
            }

            private IEnumerator DelayedItemShow(float secs)
            {
                yield return new WaitForSeconds(secs);

                _transform.DOScale(Vector3.one, 0.2f)
                    .ChangeStartValue(new Vector3(0f, 0f, 1f))
                    .SetEase(Ease.OutSine);
            }

            private IEnumerator DelayedItemHide(float secs, Action callback)
            {
                yield return new WaitForSeconds(secs);

                _transform.DOScale(new Vector3(0f, 0f, 1f), 0.2f)
                    .ChangeStartValue(Vector3.one)
                    .SetEase(Ease.OutSine);

                yield return new WaitForSeconds(0.2f);
                callback();
            }

            public void OnItemShow(int idx)
            {
                //enable us :)
                _transform.localScale = new Vector3(0f, 0f, 1f);

                _scroll.value = 0f;
                _moreShown = false;
                SetState(false, true);

                _transform.SetSiblingIndex(idx);
                _transform.gameObject.SetActive(true);
                _instance.Client.Runnable.Run(DelayedItemShow(idx * (0.2f / _instance._items.Count)));
            }

            public void OnItemHide(int idx, Action callback)
            {
                _instance.Client.Runnable.Run(DelayedItemHide(idx * (0.2f / _instance._items.Count), callback));
            }

            public void Disable()
            {
                _transform.gameObject.SetActive(false);
            }
        }

        private GameObject _itemPrefab;
        private readonly ObjectPool<Item> _itemPool;
        private readonly List<Item> _items;
        private Image _blur;
        private Color _blurColor;
        private RectTransform _content;
        private Scrollbar _contentScroll;

        private static PlaceGroup _instance;

        public PlaceGroup()
        {
            _itemPool = new ObjectPool<Item>(() =>
            {
                return new Item(Instantiate(_itemPrefab, _itemPrefab.transform.parent).transform);
            });

            _items = new List<Item>();
        }

        protected override void OnScreenInit()
        {
            _instance = this;

            _blur = GetElement<Image>("imgBlur");
            _blur.GetComponent<Button>().onClick.AddListener(OnBlurClick);
            _blurColor = _blur.color;

            GetElement<Button>("Scroll View/Viewport/BlurMask").onClick.AddListener(OnBlurClick);

            _itemPrefab = GetTransform("Scroll View/Viewport/Content/Item").gameObject;
            //m_Item = new Item(m_ItemPrefab.transform); //quick disable stuff, gc? fuck it | uh nvm
            _itemPrefab.SetActive(false);

            _content = (RectTransform)GetTransform("Scroll View/Viewport/Content");
            _contentScroll = GetElement<ScrollRect>("Scroll View").verticalScrollbar;
        }

        public void SetGroup(EGRPlaceGroup group)
        {
            EGRPlaceMarker owner = group.Owner;
            Item ownerItem = _itemPool.Rent();
            ownerItem.SetPlace(owner.Place);
            _items.Add(ownerItem);

            foreach (EGRPlaceMarker child in owner.Overlappers)
            {
                Item item = _itemPool.Rent();
                item.SetPlace(child.Place);
                _items.Add(item);
            }
        }

        protected override void OnScreenShow()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].OnItemShow(i);
            }

            RecalculateContentSize();
            _contentScroll.value = 1f;
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            _blur.DOColor(_blurColor, TweenMonitored(0.3f))
                .ChangeStartValue(Color.clear)
                .SetEase(Ease.OutSine);
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            SetTweenCount(_items.Count + 1);

            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].OnItemHide(i, OnTweenFinished);
            }

            _blur.DOColor(Color.clear, TweenMonitored(0.3f))
                .SetEase(Ease.OutSine)
                .OnComplete(OnTweenFinished);

            return true;
        }

        protected override void OnScreenHide()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Disable();
                _itemPool.Free(_items[i]);
            }

            _items.Clear();
            Client.ActiveEGRCamera.ResetStates();
        }

        private void OnBlurClick()
        {
            HideScreen();
        }

        private void RecalculateContentSize()
        {
            float size = 0f;
            for (int i = 0; i < _items.Count; i++)
            {
                size += _items[i].Size;
            }

            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1716f, size));
        }

        public void Warmup()
        {
            _itemPool.Reserve(50);
        }
    }
}
