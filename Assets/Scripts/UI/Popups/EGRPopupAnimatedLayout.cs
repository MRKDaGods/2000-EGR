using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRPopupAnimatedLayout : EGRPopup {
        ContentSizeFitter m_ContentSizeFitter;
        //TODO CHECK IF CSZ HAS VERTICAL FITTING, IF TRUE THEN DO NOT DISABLE LAYOUT

        protected virtual string LayoutPath => "Layout";
        protected virtual bool IsRTL => true;
        protected VerticalLayoutGroup Layout { get; private set; }

        protected override void OnScreenInit() {
            Layout = GetTransform(LayoutPath).GetComponent<VerticalLayoutGroup>();
            m_ContentSizeFitter = Layout.GetComponent<ContentSizeFitter>();
        }

        protected virtual bool CanAnimate(Graphic gfx, bool moving) {
            return true;
        }

        protected override void OnScreenShowAnim() {
            //AMAZING
            //TODO: OPTIMIZE THIS IS VERY EXPENSIVE DUE TO MANY GETCOMPONENTS !!!!!!!!!!!
            base.OnScreenShowAnim();

            VerticalLayoutGroup vlayout = Layout.GetComponent<VerticalLayoutGroup>();
            vlayout.enabled = true;

            vlayout.SetLayoutVertical();
            vlayout.SetLayoutHorizontal();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Layout.transform);

            if (m_ContentSizeFitter == null || m_ContentSizeFitter.verticalFit == ContentSizeFitter.FitMode.Unconstrained) {
                vlayout.enabled = false;
            }

            if (m_LastGraphicsBuf == null)
                m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);

            PushGfxState(EGRGfxState.Position | EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];
                if (!CanAnimate(gfx, false)) continue;

                gfx.DOColor(gfx.color, TweenMonitored(0.4f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color);

                if (!CanAnimate(gfx, true))
                    continue;

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue((IsRTL ? 2f : -2f) * gfx.transform.position)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color | EGRGfxState.Position);
            }

            //animate Layout itself
            Layout.transform.DOMoveX(Layout.transform.position.x, TweenMonitored(0.3f))
                    .ChangeStartValue((IsRTL ? 2f : -2f) * Layout.transform.position)
                    .SetEase(Ease.OutSine);
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.3f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }
    }
}
