using DG.Tweening;
using System;
using UnityEngine;

namespace MRK.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class AnimatedAlpha : Screen
    {
        private CanvasGroup _canvasGroup;

        protected virtual float AlphaFadeSpeed
        {
            get
            {
                return 0.3f;
            }
        }

        protected override void OnScreenInit()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            DOTween.To(
                () => _canvasGroup.alpha,
                x => _canvasGroup.alpha = x,
                1f,
                TweenMonitored(AlphaFadeSpeed)
            ).ChangeStartValue(0f);
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            SetTweenCount(1);

            DOTween.To(
                () => _canvasGroup.alpha,
                x => _canvasGroup.alpha = x,
                0f,
                TweenMonitored(AlphaFadeSpeed)
            ).SetEase(Ease.OutSine)
            .OnComplete(OnTweenFinished);

            return true;
        }
    }
}
