using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MRK.UI {
    public class EGRScreenMapChooser : EGRScreen {
        [Serializable]
        struct StyleInfo {
            public Sprite Sprite;
            public string Text;
        }

        class MapStyle {
            RectTransform m_Transform;
            GameObject m_Indicator;

            public RectTransform Transform => m_Transform;
            public float Multiplier { get; set; }

            public MapStyle(Transform root, StyleInfo style) {
                m_Transform = (RectTransform)root;

                m_Indicator = m_Transform.Find("Indicator").gameObject;
                m_Indicator.SetActive(false);

                m_Transform.Find("Scroll View/Viewport/Content/Map").GetComponent<Image>().sprite = style.Sprite;
                m_Transform.Find("Text").GetComponent<TextMeshProUGUI>().text = style.Text;
                m_Transform.GetComponent<Button>().onClick.AddListener(OnStyleClicked);

                Multiplier = 1f;
            }

            void OnStyleClicked() {
                if (ms_Instance.m_SelectedStyle == this)
                    ms_Instance.HideScreen();
                else 
                    ms_Instance.OnStyleClicked(this);
            }

            public void SetIndicatorState(bool active) {
                m_Indicator.SetActive(active);
            }
        }

        [SerializeField]
        StyleInfo[] m_Styles;
        [SerializeField]
        GameObject m_MapPrefab;
        MapStyle[] m_MapStyles;
        static EGRScreenMapChooser ms_Instance;
        VerticalLayoutGroup m_Layout;
        float? m_IdleSize;
        object m_Tween;
        float m_CurrentMultiplier;
        MapStyle m_SelectedStyle;

        protected override void OnScreenInit() {
            ms_Instance = this;

            m_MapPrefab.gameObject.SetActive(false);

            m_MapStyles = new MapStyle[m_Styles.Length];
            int styleIdx = 0;
            foreach (StyleInfo style in m_Styles) {
                GameObject obj = Instantiate(m_MapPrefab, m_MapPrefab.transform.parent);
                m_MapStyles[styleIdx++] = new MapStyle(obj.transform as RectTransform, style);

                obj.SetActive(true);
            }

            m_Layout = GetElement<VerticalLayoutGroup>("Layout");
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);

            PushGfxState(EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                m_LastGraphicsBuf[i].DOColor(Color.clear, TweenMonitored(0.3f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        void OnStyleClicked(MapStyle style) {
            if (!m_IdleSize.HasValue)
                m_IdleSize = m_MapStyles[0].Transform.rect.height;

            if (m_Tween != null)
                DOTween.Kill(m_Tween);

            //okay so
            m_Layout.childControlHeight = false;
            m_SelectedStyle = style;

            m_CurrentMultiplier = 1f;
            m_Tween = DOTween.To(() => m_CurrentMultiplier, x => m_CurrentMultiplier = x, 2f, 0.1f)
                .SetEase(Ease.OutSine)
                .OnUpdate(UpdateSizes)
                .OnComplete(OnTweenComplete);

            foreach (MapStyle mStyle in m_MapStyles) {
                mStyle.SetIndicatorState(mStyle == style);
            }
        }

        void UpdateSizes() {
            foreach (MapStyle mStyle in m_MapStyles) {
                float target = m_SelectedStyle == mStyle ? m_CurrentMultiplier : 1f / m_CurrentMultiplier;
                float current = Mathf.Lerp(mStyle.Multiplier, target, ((Tween)m_Tween).ElapsedPercentage());
                mStyle.Transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, current * m_IdleSize.Value);
            }
        }

        void OnTweenComplete() {
            foreach (MapStyle mStyle in m_MapStyles) {
                mStyle.Multiplier = m_SelectedStyle == mStyle ? 2f : 0.5f;
            }
        }
    }
}
