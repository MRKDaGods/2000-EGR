using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace MRK.UI
{
    public class SpaceFOV : Screen
    {
        private const float VignetteIntensity = 0.259f;

        private static readonly float[] _fovs;
        private ScrollSnap _horizontalSnap;
        private Vignette _vignette;
        private int _page;

        static SpaceFOV()
        {
            _fovs = new float[4] {
                65f, 75f, 90f, 120f
            };
        }

        protected override void OnScreenInit()
        {
            _horizontalSnap = GetElement<ScrollSnap>("Values");
            _vignette = GetElement<PostProcessVolume>("PostProcessing").profile.GetSetting<Vignette>();

            GetElement<Button>("Done").onClick.AddListener(OnDoneClick);
        }

        protected override void OnScreenShow()
        {
            _horizontalSnap.onPageChange += OnPageChanged;

            _page = (int)Settings.SpaceFOV;
            _horizontalSnap.ChangePage(_page);
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            //fade in post processing
            DOTween.To(() => _vignette.intensity.value, x => _vignette.intensity.value = x, VignetteIntensity, 0.3f)
                .ChangeStartValue(0f)
                .SetEase(Ease.OutSine);

            //UI
            if (_lastGraphicsBuf == null)
            {
                _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);
            }

            PushGfxState(GfxStates.Color);

            foreach (Graphic gfx in _lastGraphicsBuf)
            {
                gfx.DOColor(gfx.color, TweenMonitored(0.3f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            SetTweenCount(_lastGraphicsBuf.Length + 1);

            foreach (Graphic gfx in _lastGraphicsBuf)
            {
                gfx.DOColor(Color.clear, TweenMonitored(0.3f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            DOTween.To(() => _vignette.intensity.value, x => _vignette.intensity.value = x, 0f, 0.3f)
                .SetEase(Ease.OutSine)
                .OnComplete(OnTweenFinished);

            return true;
        }

        protected override void OnScreenHide()
        {
            _horizontalSnap.onPageChange -= OnPageChanged;
        }

        private void OnPageChanged(int page)
        {
            Client.GlobeCamera.TargetFOV = _fovs[page];
            _page = page;
        }

        private void OnDoneClick()
        {
            Settings.SpaceFOV = (SettingsSpaceFOV)_page;
            Settings.Save();

            HideScreen();
        }

        public static float GetFOV(SettingsSpaceFOV setting)
        {
            return _fovs[(int)setting];
        }
    }
}
