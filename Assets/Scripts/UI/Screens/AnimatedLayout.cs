using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.Screens
{
    public class AnimatedLayout : Screen
    {
        private Transform _layout;

        protected virtual string LayoutPath
        {
            get
            {
                return "Layout";
            }
        }

        protected virtual bool IsRTL
        {
            get
            {
                return true;
            }
        }

        protected override void OnScreenInit()
        {
            _layout = GetTransform(LayoutPath);
        }

        protected virtual bool CanAnimate(Graphic gfx, bool moving)
        {
            return true;
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            VerticalLayoutGroup vlayout = _layout.GetComponent<VerticalLayoutGroup>();
            vlayout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_layout);
            vlayout.enabled = false;

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);

            PushGfxState(GfxStates.Position | GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                if (gfx.GfxHasScrollView() || !CanAnimate(gfx, false)) continue;

                if (gfx.name == "imgBg")
                {
                    gfx.DOColor(gfx.color, TweenMonitored(0.2f))
                        .ChangeStartValue(Color.clear)
                        .SetEase(Ease.OutSine);
                }

                SetGfxStateMask(gfx, GfxStates.Color);

                if (gfx.ParentHasGfx(typeof(ScrollRect)) || !CanAnimate(gfx, true))
                    continue;

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue((IsRTL ? 2f : -2f) * gfx.transform.position)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, GfxStates.Color | GfxStates.Position);
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            SetTweenCount(_lastGraphicsBuf.Length);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.3f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }
    }
}
