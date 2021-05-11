using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenOptionsAudioSettings : EGRScreen {
        Transform m_Layout;

        protected override void OnScreenInit() {
            GetElement<Button>("bBack").onClick.AddListener(() => HideScreen());
            m_Layout = GetTransform("Layout");
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

                if (gfx.GfxHasScrollView()) continue;

                gfx.DOColor(gfx.color, TweenMonitored(0.3f + i * 0.03f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color);

                if (gfx.ParentHasGfx(typeof(ScrollRect)))
                    continue;

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue(2f * gfx.transform.position)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color | EGRGfxState.Position);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.2f + i * 0.03f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }
    }
}
