using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MRK.UI {
    public class EGRScreenAnimatedLayout : EGRScreen {
        Transform m_Layout;

        protected virtual string m_LayoutPath => "Layout";
        protected virtual bool m_IsRTL => true;

        protected override void OnScreenInit() {
            m_Layout = GetTransform(m_LayoutPath);
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
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            PushGfxState(EGRGfxState.Position | EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                if (gfx.GfxHasScrollView() || !CanAnimate(gfx, false)) continue;

                gfx.DOColor(gfx.color, TweenMonitored(0.3f + i * 0.03f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color);

                if (gfx.ParentHasGfx(typeof(ScrollRect)) || !CanAnimate(gfx, true))
                    continue;

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue((m_IsRTL ? 2f : -2f) * gfx.transform.position)
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
