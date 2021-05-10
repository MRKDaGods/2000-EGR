using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenOptionsAppSettings : EGRScreen {
        Image m_Background;

        protected override void OnScreenInit() {
            GetElement<Button>("bTopLeftMenu").onClick.AddListener(() => {
                HideScreen(() => Manager.GetScreen<EGRScreenMenu>().ShowScreen(), 0.1f, false);
            });

            GetElement<Button>("Layout/Display").onClick.AddListener(() => {
                Manager.GetScreen<EGRScreenOptionsDisplaySettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Audio").onClick.AddListener(() => {
                Manager.GetScreen<EGRScreenOptionsAudioSettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Globe").onClick.AddListener(() => {
                Manager.GetScreen<EGRScreenOptionsGlobeSettings>().ShowScreen();
            });

            GetElement<Button>("Layout/Map").onClick.AddListener(() => {
                Manager.GetScreen<EGRScreenOptionsMapSettings>().ShowScreen();
            });

            m_Background = GetElement<Image>("imgBg");
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            PushGfxState(EGRGfxState.LocalPosition | EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color);

                if (gfx == m_Background || gfx.ParentHasGfx()) {
                    continue;
                }

                gfx.transform.DOLocalMoveX(gfx.transform.localPosition.x, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue(-1f * gfx.transform.position)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color | EGRGfxState.LocalPosition);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.2f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }
    }
}
