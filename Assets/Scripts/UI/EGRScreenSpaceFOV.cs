using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using MRK;
using UnityEngine.UI.Extensions;
using UnityEngine.Rendering.PostProcessing;
using DG.Tweening;

namespace MRK.UI {
    public class EGRScreenSpaceFOV : EGRScreen {
        const float VIGNETTE_INTENSITY = 0.259f;

        readonly static float[] ms_FOVs;
        ScrollSnap m_HorizontalSnap;
        Vignette m_Vignette;
        int m_Page;

        static EGRScreenSpaceFOV() {
            ms_FOVs = new float[4] {
                65f, 75f, 90f, 120f
            };
        }

        protected override void OnScreenInit() {
            m_HorizontalSnap = GetElement<ScrollSnap>("Values");
            m_Vignette = GetElement<PostProcessVolume>("PostProcessing").profile.GetSetting<Vignette>();

            GetElement<Button>("Done").onClick.AddListener(OnDoneClick);
        }
        
        protected override void OnScreenShow() {
            m_HorizontalSnap.onPageChange += OnPageChanged;

            m_Page = (int)EGRSettings.SpaceFOV;
            m_HorizontalSnap.ChangePage(m_Page);
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            //fade in post processing
            DOTween.To(() => m_Vignette.intensity.value, x => m_Vignette.intensity.value = x, VIGNETTE_INTENSITY, 0.3f)
                .ChangeStartValue(0f)
                .SetEase(Ease.OutSine);

            //UI
            if (m_LastGraphicsBuf == null) {
                m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);
            }

            PushGfxState(EGRGfxState.Color);

            foreach (Graphic gfx in m_LastGraphicsBuf) {
                gfx.DOColor(gfx.color, TweenMonitored(0.3f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            SetTweenCount(m_LastGraphicsBuf.Length + 1);

            foreach (Graphic gfx in m_LastGraphicsBuf) {
                gfx.DOColor(Color.clear, TweenMonitored(0.3f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            DOTween.To(() => m_Vignette.intensity.value, x => m_Vignette.intensity.value = x, 0f, 0.3f)
                .SetEase(Ease.OutSine)
                .OnComplete(OnTweenFinished);

            return true;
        }

        protected override void OnScreenHide() {
            m_HorizontalSnap.onPageChange -= OnPageChanged;
        }

        void OnPageChanged(int page) {
            Client.GlobeCamera.TargetFOV = ms_FOVs[page];
            m_Page = page;
        }

        void OnDoneClick() {
            EGRSettings.SpaceFOV = (EGRSettingsSpaceFOV)m_Page;
            EGRSettings.Save();

            HideScreen();
        }

        public static float GetFOV(EGRSettingsSpaceFOV setting) {
            return ms_FOVs[(int)setting];
        }
    }
}
