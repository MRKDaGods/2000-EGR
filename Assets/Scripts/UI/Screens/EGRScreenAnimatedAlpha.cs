using DG.Tweening;
using System;
using UnityEngine;

namespace MRK.UI {
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class EGRScreenAnimatedAlpha : EGRScreen {
        CanvasGroup m_CanvasGroup;

        protected virtual float AlphaFadeSpeed => 0.3f;

        protected override void OnScreenInit() {
            m_CanvasGroup = GetComponent<CanvasGroup>();
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            DOTween.To(
                () => m_CanvasGroup.alpha,
                x => m_CanvasGroup.alpha = x,
                1f,
                TweenMonitored(AlphaFadeSpeed)
            ).ChangeStartValue(0f);
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            SetTweenCount(1);

            DOTween.To(
                () => m_CanvasGroup.alpha,
                x => m_CanvasGroup.alpha = x,
                0f,
                TweenMonitored(AlphaFadeSpeed)
            ).SetEase(Ease.OutSine)
            .OnComplete(OnTweenFinished);

            return true;
        }
    }
}
