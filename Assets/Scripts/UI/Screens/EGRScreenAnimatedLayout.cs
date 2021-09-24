using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenAnimatedLayout : EGRScreen {
        Transform m_Layout;

        protected virtual string LayoutPath => "Layout";
        protected virtual bool IsRTL => true;

        protected override void OnScreenInit() {
            m_Layout = GetTransform(LayoutPath);
        }

        protected virtual bool CanAnimate(Graphic gfx, bool moving) {
            return true;
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            VerticalLayoutGroup vlayout = m_Layout.GetComponent<VerticalLayoutGroup>();
            vlayout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_Layout);
            vlayout.enabled = false;

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);

            PushGfxState(EGRGfxState.Position | EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                if (gfx.GfxHasScrollView() || !CanAnimate(gfx, false)) continue;

                if (gfx.name == "imgBg") {
                    gfx.DOColor(gfx.color, TweenMonitored(0.2f))
                        .ChangeStartValue(Color.clear)
                        .SetEase(Ease.OutSine);
                }

                SetGfxStateMask(gfx, EGRGfxState.Color);

                if (gfx.ParentHasGfx(typeof(ScrollRect)) || !CanAnimate(gfx, true))
                    continue;

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue((IsRTL ? 2f : -2f) * gfx.transform.position)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color | EGRGfxState.Position);
            }
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
