using DG.Tweening;
using MRK.Maps;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public class MapChooser : Screen
    {
        [Serializable]
        private struct StyleInfo
        {
            public string Tileset;
            public string Text;
        }

        private class MapStyle : BaseBehaviourPlain
        {
            private RectTransform _transform;
            private GameObject _indicator;
            private StyleInfo _styleInfo;
            private readonly Loading _mapPreviewLoading;

            public RectTransform Transform
            {
                get
                {
                    return _transform;
                }
            }

            public float Multiplier
            {
                get; set;
            }

            public int Index
            {
                get; private set;
            }

            public ColorMaskedRawImage Preview
            {
                get; private set;
            }

            public MapStyle(Transform root, StyleInfo style, int idx)
            {
                _transform = (RectTransform)root;
                _styleInfo = style;

                _indicator = _transform.Find("Indicator").gameObject;
                _indicator.SetActive(false);

                Preview = _transform.Find("Scroll View/Viewport/Map").GetComponent<ColorMaskedRawImage>();
                _transform.Find("Text").GetComponent<TextMeshProUGUI>().text = style.Text;
                _transform.GetComponent<Button>().onClick.AddListener(OnStyleClicked);

                Multiplier = 1f;
                Index = idx;

                Reference loadingRef = _transform.GetComponent<Reference>();
                loadingRef.InitializeIfNeeded();
                _mapPreviewLoading = (Loading)loadingRef.Usable;
            }

            private void OnStyleClicked()
            {
                _instance.OnStyleClicked(this);
            }

            public void SetIndicatorState(bool active)
            {
                _indicator.SetActive(active);
            }

            public void LoadPreview()
            {
                if (Preview.texture != null)
                {
                    _mapPreviewLoading.gameObject.SetActive(false);
                    return;
                }

                _mapPreviewLoading.gameObject.SetActive(true);

                TileID tileID = new TileID(2, 2, 1);
                Client.Runnable.Run(TileRequestor.Instance.RequestTile(tileID, false, OnReceivedMapPreviewResponse, _styleInfo.Tileset));
            }

            private void OnReceivedMapPreviewResponse(TileFetcherContext ctx)
            {
                _mapPreviewLoading.gameObject.SetActive(false);

                if (ctx.Error)
                {
                    MRKLogger.LogError("Cannot load map preview");
                    return;
                }

                if (ctx.Texture != null)
                {
                    Preview.texture = ctx.MonitoredTexture.Value.Texture;
                }
            }
        }

        [SerializeField]
        private StyleInfo[] _styles;
        [SerializeField]
        private GameObject _mapPrefab;
        private MapStyle[] _mapStyles;
        private VerticalLayoutGroup _layout;
        private float? _idleSize;
        private object _tween;
        private float _currentMultiplier;
        private MapStyle _selectedStyle;

        private static MapChooser _instance;

        public Action<int> MapStyleCallback
        {
            get; set;
        }

        protected override void OnScreenInit()
        {
            _instance = this;

            _mapStyles = new MapStyle[_styles.Length];
            int styleIdx = 0;
            foreach (StyleInfo style in _styles)
            {
                GameObject obj = Instantiate(_mapPrefab, _mapPrefab.transform.parent);
                _mapStyles[styleIdx++] = new MapStyle(obj.transform as RectTransform, style, styleIdx - 1);

                Destroy(obj.GetComponent<DisableAtRuntime>());
                obj.SetActive(true);
            }

            _layout = GetElement<VerticalLayoutGroup>("Layout");
        }

        protected override void OnScreenShow()
        {
            foreach (MapStyle mapStyle in _mapStyles)
            {
                mapStyle.LoadPreview();
            }
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);

            PushGfxState(GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            SetTweenCount(_lastGraphicsBuf.Length);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                _lastGraphicsBuf[i].DOColor(Color.clear, TweenMonitored(0.3f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        private void OnStyleClicked(MapStyle style)
        {
            if (!_idleSize.HasValue)
                _idleSize = _mapStyles[0].Transform.rect.height;

            if (_tween != null)
                DOTween.Kill(_tween);

            if (_selectedStyle == style)
            {
                HideScreen();

                if (MapStyleCallback != null)
                    MapStyleCallback(_selectedStyle.Index);

                return;
            }

            //okay so
            _layout.childControlHeight = false;
            _selectedStyle = style;

            _currentMultiplier = 1f;
            _tween = DOTween.To(() => _currentMultiplier, x => _currentMultiplier = x, 2f, 0.1f)
                .SetEase(Ease.OutSine)
                .OnUpdate(UpdateSizes)
                .OnComplete(OnTweenComplete);

            foreach (MapStyle mStyle in _mapStyles)
            {
                mStyle.SetIndicatorState(mStyle == style);
            }
        }

        private void UpdateSizes()
        {
            foreach (MapStyle mStyle in _mapStyles)
            {
                float target = _selectedStyle == mStyle ? _currentMultiplier : 1f / _currentMultiplier;
                float current = Mathf.Lerp(mStyle.Multiplier, target, ((Tween)_tween).ElapsedPercentage());
                mStyle.Transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, current * _idleSize.Value);
            }
        }

        private void OnTweenComplete()
        {
            foreach (MapStyle mStyle in _mapStyles)
            {
                mStyle.Multiplier = _selectedStyle == mStyle ? 2f : 0.5f;
            }
        }
    }
}
