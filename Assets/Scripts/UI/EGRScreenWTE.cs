using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRK;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using DG.Tweening;
using TMPro;
using Random = UnityEngine.Random;
using Coffee.UIEffects;

namespace MRK.UI {
    public class EGRScreenWTE : EGRScreen {
        class Strip {
            public Image Image;
            public float EmissionOffset;
            public float ScaleOffset;
        }

        [SerializeField]
        GameObject m_ScreenSpaceWTE;
        Image m_LinePrefab;
        Canvas m_Canvas;
        readonly List<Strip> m_Strips;
        float m_Time;
        LensDistortion m_LensDistortion;
        Image m_WTETextBg;
        TextMeshProUGUI m_WTEText;
        readonly EGRColorFade m_StripFade;
        readonly EGRFiniteStateMachine m_AnimFSM;
        readonly EGRColorFade m_TransitionFade;
        bool m_ShouldUpdateAnimFSM;
        UIDissolve m_WTETextBgDissolve;
        GameObject m_OverlayWTE;
        RectTransform m_WTELogoMaskTransform;
        RectTransform m_ContextualTextMaskTransform;
        TextMeshProUGUI m_ContextualText;
        RectTransform m_ContextualButtonsMaskTransform;
        RectTransform m_ContextualButtonsLayoutTransform;
        EGRUIFancyScrollView m_ContextualScrollView;

        public EGRScreenWTE() {
            m_Strips = new List<Strip>();
            m_StripFade = new EGRColorFade(Color.white.AlterAlpha(0f), Color.white, 1f);
            m_TransitionFade = new EGRColorFade(Color.clear, Color.white, 0.8f);

            m_AnimFSM = new EGRFiniteStateMachine(new Tuple<Func<bool>, Action, Action>[]{
                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_TransitionFade.Done;
                }, () => {
                    m_TransitionFade.Update();

                    m_WTETextBg.color = m_TransitionFade.Current;
                    m_WTEText.color = m_TransitionFade.Current.Inverse().AlterAlpha(1f);

                }, () => {
                    m_TransitionFade.Reset();

                    m_StripFade.Reset();
                    m_StripFade.SetColors(m_TransitionFade.Final, Color.clear, 0.3f);

                    m_WTETextBgDissolve.effectFactor = 0f;
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_WTETextBgDissolve.effectFactor >= 1f;
                }, () => {
                }, () => {
                    m_WTETextBgDissolve.effectPlayer.duration = 0.5f;
                    Client.Runnable.RunLater(() => m_WTETextBgDissolve.effectPlayer.Play(false), 1f);
                }),

                //exit
                new Tuple<Func<bool>, Action, Action>(() => {
                    return true;
                }, () => {
                }, OnWTETransitionEnd)
            });
        }

        protected override void OnScreenInit() {
            m_ScreenSpaceWTE.SetActive(false);

            m_LinePrefab = m_ScreenSpaceWTE.transform.Find("LinePrefab").GetComponent<Image>();
            m_LinePrefab.gameObject.SetActive(false);

            m_Canvas = Manager.GetScreenSpaceLayer();

            RectTransform canvasTransform = (RectTransform)m_Canvas.transform;
            Debug.Log(canvasTransform.rect.width + " | " + m_LinePrefab.rectTransform.rect.width);
            int hStripCount = Mathf.CeilToInt(canvasTransform.rect.width / m_LinePrefab.rectTransform.rect.width);
            Debug.Log($"Strips={hStripCount}");

            m_LinePrefab.rectTransform.sizeDelta = new Vector2(m_LinePrefab.rectTransform.sizeDelta.x, canvasTransform.rect.height);

            for (int i = 0; i < hStripCount; i++) {
                Image strip = Instantiate(m_LinePrefab, m_LinePrefab.transform.parent);
                strip.rectTransform.anchoredPosition = strip.rectTransform.rect.size * new Vector2(i + 0.5f, -0.5f);
                Material stripMat = Instantiate(strip.material);
                stripMat.color = Color.white.AlterAlpha(0f);

                float startEmission = Random.Range(0f, 2f);
                strip.material.SetFloat("_Emission", GetPingPongedValue(startEmission));

                float startScale = Random.Range(0f, 2f);
                strip.material.mainTextureScale = new Vector2(1f, GetPingPongedValue(startScale));

                strip.material = stripMat;
                strip.gameObject.SetActive(true);

                m_Strips.Add(new Strip {
                    Image = strip,
                    EmissionOffset = startEmission,
                    ScaleOffset = startScale
                });
            }

            m_LensDistortion = GetPostProcessingEffect<LensDistortion>();

            m_WTETextBg = m_ScreenSpaceWTE.transform.Find("WTEText").GetComponent<Image>();
            m_WTETextBg.transform.SetAsLastSibling();

            m_WTEText = m_WTETextBg.GetComponentInChildren<TextMeshProUGUI>();

            m_WTETextBgDissolve = m_WTETextBg.GetComponent<UIDissolve>();

            m_OverlayWTE = GetTransform("Overlay").gameObject;
            m_WTELogoMaskTransform = (RectTransform)GetTransform("Overlay/WTEText");

            m_ContextualTextMaskTransform = (RectTransform)GetTransform("Overlay/ContextualText");
            m_ContextualText = m_ContextualTextMaskTransform.GetComponentInChildren<TextMeshProUGUI>();

            m_ContextualScrollView = GetElement<EGRUIFancyScrollView>("Overlay/ContextualButtons");

            //m_ContextualButtonsMaskTransform = (RectTransform)GetTransform("Overlay/ContextualButtons");
            //m_ContextualButtonsLayoutTransform = (RectTransform)m_ContextualButtonsMaskTransform.Find("Layout");
        }

        protected override void OnScreenShow() {
            m_ScreenSpaceWTE.SetActive(true);
            Client.ActiveEGRCamera.SetInterfaceState(true);

            //set initial lens distortion values
            m_LensDistortion.intensity.value = 0f;
            m_LensDistortion.centerX.value = 0f;
            m_LensDistortion.centerY.value = 0f;
            m_LensDistortion.scale.value = 1f;

            //we dont wanna see that yet
            m_WTETextBg.gameObject.SetActive(false);
            m_OverlayWTE.SetActive(false);

            StartInitialTransition();

            Client.Runnable.RunLater(StartWTETransition, 1.2f);
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            if (m_LastGraphicsBuf == null)
                m_LastGraphicsBuf = m_ScreenSpaceWTE.GetComponentsInChildren<Graphic>(true);

            PushGfxState(EGRGfxState.Color);

            foreach (Graphic gfx in m_LastGraphicsBuf) {
                gfx.DOColor(gfx.color, 0.4f)
                    .ChangeStartValue(gfx.color.AlterAlpha(0f))
                    .SetEase(Ease.OutSine);
            }
        }

        protected override void OnScreenHide() {
            m_ScreenSpaceWTE.SetActive(false);
            Client.ActiveEGRCamera.SetInterfaceState(false);
        }

        protected override void OnScreenUpdate() {
            m_Time += Time.deltaTime;

            bool stripFadeUpdated = false;
            if (!m_StripFade.Done) {
                m_StripFade.Update();
                stripFadeUpdated = true;
            }

            foreach (Strip strip in m_Strips) {
                if (stripFadeUpdated) {
                    strip.Image.material.color = m_StripFade.Current;
                }

                strip.Image.material.SetFloat("_Emission", GetPingPongedValue(strip.EmissionOffset));
                strip.Image.material.mainTextureScale = new Vector2(1f, GetPingPongedValue(strip.ScaleOffset));
            }

            if (m_ShouldUpdateAnimFSM) {
                m_AnimFSM.UpdateFSM();
            }
        }

        float GetPingPongedValue(float offset) {
            return Mathf.PingPong(m_Time + offset, 2f);
        }

        T GetPostProcessingEffect<T>() where T : PostProcessEffectSettings {
            return m_ScreenSpaceWTE.GetComponent<PostProcessVolume>().profile.GetSetting<T>();
        }

        void StartInitialTransition() {
            DOTween.To(() => m_LensDistortion.intensity.value, x => m_LensDistortion.intensity.value = x, -100f, 1f);
            DOTween.To(() => m_LensDistortion.centerX.value, x => m_LensDistortion.centerX.value = x, -0.5f, 1f);
            DOTween.To(() => m_LensDistortion.centerY.value, x => m_LensDistortion.centerY.value = x, 0.68f, 1f);
            DOTween.To(() => m_LensDistortion.scale.value, x => m_LensDistortion.scale.value = x, 1.55f, 1f);
        }

        void StartWTETransition() {
            //enable text
            m_WTETextBg.gameObject.SetActive(true);
            m_WTETextBg.color = Color.clear;
            m_WTEText.color = m_WTETextBg.color.InverseWithAlpha();

            m_ShouldUpdateAnimFSM = true;

            DOTween.To(() => m_LensDistortion.intensity.value, x => m_LensDistortion.intensity.value = x, 0f, 1f);
            DOTween.To(() => m_LensDistortion.centerX.value, x => m_LensDistortion.centerX.value = x, 0f, 1f);
            DOTween.To(() => m_LensDistortion.centerY.value, x => m_LensDistortion.centerY.value = x, 0f, 1f);
            DOTween.To(() => m_LensDistortion.scale.value, x => m_LensDistortion.scale.value = x, 1f, 1f);
        }

        void AnimateStretchableTransform(RectTransform staticTransform, RectTransform stretchableTransform) {
            Rect oldRect = staticTransform.rect;
            Vector3 oldPos = staticTransform.position;
            staticTransform.anchorMin = staticTransform.anchorMax = new Vector2(0f, 1f);
            staticTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, oldRect.width);
            staticTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, oldRect.height);

            Rect oldStretchRect = stretchableTransform.rect;
            Vector3 oldStretchPos = stretchableTransform.position;
            stretchableTransform.anchorMin = stretchableTransform.anchorMax = new Vector2(0f, 1f);

            stretchableTransform.DOSizeDelta(oldStretchRect.size, 0.5f)
                .ChangeStartValue(new Vector2(0f, oldStretchRect.height))
                .OnUpdate(() => {
                    staticTransform.position = oldPos;
                    stretchableTransform.position = oldStretchPos;
                })
                .SetEase(Ease.OutSine);
        }

        void OnWTETransitionEnd() {
            m_OverlayWTE.SetActive(true);

            foreach (Graphic gfx in m_OverlayWTE.GetComponentsInChildren<Graphic>()) {
                gfx.DOFade(gfx.color.a, 0.5f)
                    .ChangeStartValue(gfx.color.AlterAlpha(0f))
                    .SetEase(Ease.OutSine);
            }

            m_WTELogoMaskTransform.DOSizeDelta(m_WTELogoMaskTransform.sizeDelta, 0.5f)
                .ChangeStartValue(new Vector2(0f, m_WTELogoMaskTransform.sizeDelta.y))
                .SetEase(Ease.OutSine);

            AnimateStretchableTransform(m_ContextualText.rectTransform, m_ContextualTextMaskTransform);
            //AnimateStretchableTransform(m_ContextualButtonsLayoutTransform, m_ContextualButtonsMaskTransform);

            var items = Enumerable.Range(0, 10)
                .Select(i => new EGRUIFancyScrollViewItemData($"{i}"))
                .ToArray();

            m_ContextualScrollView.UpdateData(items);
            m_ContextualScrollView.SelectCell(0);
        }
    }
}
