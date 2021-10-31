using Coffee.UIEffects;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace MRK.UI {
    public partial class EGRScreenWTE : EGRScreen, IEGRScreenFOVStabilizer {
        class Strip {
            public Image Image;
            public float EmissionOffset;
            public float ScaleOffset;
        }

        [Serializable]
        struct ContextGradient {
            public Color First;
            public Color Second;

            public Color Third;
            public Color Fourth;
            public float Offset;

            public Color Fifth;
            public Color Sixth;
            public UIGradient.Direction Direction;
        }

        [Serializable]
        struct ContextOptions {
            public string[] Options;
        }

        class Indicator {
            Graphic[] m_Gfx;

            public float LastAlpha { get; private set; }

            public Indicator(Transform trans) {
                m_Gfx = trans.GetComponentsInChildren<Graphic>(true);
            }

            public void SetAlpha(float alpha) {
                LastAlpha = alpha;

                foreach (Graphic gfx in m_Gfx) {
                    gfx.color = gfx.color.AlterAlpha(alpha);
                }
            }
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
        EGRFiniteStateMachine m_AnimFSM;
        readonly EGRColorFade m_TransitionFade;
        bool m_ShouldUpdateAnimFSM;
        UIDissolve m_WTETextBgDissolve;
        GameObject m_OverlayWTE;
        RectTransform m_WTELogoMaskTransform;
        ContextArea m_ContextArea;
        bool m_Down;
        Vector2 m_DownPos;
        Indicator m_BackIndicator;
        [SerializeField]
        ContextGradient[] m_ContextGradients;
        [SerializeField]
        ContextOptions[] m_ContextOptions;
        Vector2? m_WTELogoSizeDelta;

        public float TargetFOV => EGRConstants.EGR_CAMERA_DEFAULT_FOV;
        static EGRScreenWTE ms_Instance { get; set; }

        public EGRScreenWTE() {
            m_Strips = new List<Strip>();
            m_StripFade = new EGRColorFade(Color.white.AlterAlpha(0f), Color.white, 1f);
            m_TransitionFade = new EGRColorFade(Color.clear, Color.white, 0.8f);

            InitTransitionFSM();
        }

        protected override void OnScreenInit() {
            ms_Instance = this;

            m_ScreenSpaceWTE.SetActive(false);

            m_LinePrefab = m_ScreenSpaceWTE.transform.Find("LinePrefab").GetComponent<Image>();
            m_LinePrefab.gameObject.SetActive(false);

            m_Canvas = ScreenManager.GetScreenSpaceLayer(1);

            RectTransform canvasTransform = (RectTransform)m_Canvas.transform;
            int hStripCount = Mathf.CeilToInt(canvasTransform.rect.width / m_LinePrefab.rectTransform.rect.width);

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

            m_ContextArea = new ContextArea(m_ScreenSpaceWTE.transform);

            m_BackIndicator = new Indicator(GetTransform("Overlay/Indicator"));
        }

        protected override void OnScreenShow() {
            m_ScreenSpaceWTE.SetActive(true);
            m_WTEText.gameObject.SetActive(true);
            Client.ActiveEGRCamera.SetInterfaceState(true);

            m_ContextArea.SetActive(false);
            m_BackIndicator.SetAlpha(0f);

            foreach (Strip s in m_Strips) {
                s.Image.gameObject.SetActive(true);
            }

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

            Client.RegisterControllerReceiver(OnControllerMessageReceived);

            //initials
            m_StripFade.Reset();
            m_StripFade.SetColors(Color.white.AlterAlpha(0f), Color.white, 1f);
            m_TransitionFade.Reset();
            m_TransitionFade.SetColors(Color.clear, Color.white, 0.8f);

            m_Time = 0f;
            m_AnimFSM.ResetMachine();
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

            Client.UnregisterControllerReceiver(OnControllerMessageReceived);
        }

        protected override void OnScreenUpdate() {
            m_Time += Time.deltaTime;

            bool stripFadeUpdated = false;
            if (!m_StripFade.Done) {
                m_StripFade.Update();
                stripFadeUpdated = true;
            }

            foreach (Strip strip in m_Strips) {
                if (!strip.Image.gameObject.activeInHierarchy)
                    break;

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
            m_ShouldUpdateAnimFSM = false;

            m_OverlayWTE.SetActive(true);
            //m_ContextualScrollView.gameObject.SetActive(true);
            m_WTEText.gameObject.SetActive(false);

            m_ContextArea.SetActive(true);

            foreach (Strip s in m_Strips) {
                s.Image.gameObject.SetActive(false);
            }

            foreach (Graphic gfx in m_OverlayWTE.GetComponentsInChildren<Graphic>()) {
                gfx.DOFade(gfx.color.a, 0.5f)
                    .ChangeStartValue(gfx.color.AlterAlpha(0f))
                    .SetEase(Ease.OutSine);
            }

            if (!m_WTELogoSizeDelta.HasValue) {
                m_WTELogoSizeDelta = m_WTELogoMaskTransform.sizeDelta;
            }

            m_WTELogoMaskTransform.DOSizeDelta(m_WTELogoSizeDelta.Value, 0.5f)
                .ChangeStartValue(new Vector2(0f, m_WTELogoSizeDelta.Value.y))
                .SetEase(Ease.OutSine);

            //AnimateStretchableTransform(m_ContextualText.rectTransform, m_ContextualTextMaskTransform);
            //AnimateStretchableTransform(m_ContextualButtonsLayoutTransform, m_ContextualButtonsMaskTransform);

            int idx = 0;
            foreach (EGRUIFancyScrollView view in m_ContextArea.ContextualScrollView) {
                ContextOptions options;
                if (view == null || (options = m_ContextOptions[idx]).Options.Length == 0) {
                    idx++;
                    continue;
                }

                view.UpdateData(options.Options
                    .Select(x => new EGRUIFancyScrollViewItemData(x)).ToList());
                view.SelectCell(0, false);
                idx++;
            }

            m_ContextArea.SetupCellGradients();
        }

        void OnControllerMessageReceived(MRKInputControllerMessage msg) {
            if (m_ShouldUpdateAnimFSM) //animating
                return;

            if (msg.ContextualKind == MRKInputControllerMessageContextualKind.Mouse) {
                MRKInputControllerMouseEventKind kind = (MRKInputControllerMouseEventKind)msg.Payload[0];

                switch (kind) {
                    case MRKInputControllerMouseEventKind.Down:
                        if (m_ContextArea.Page == 0) {
                            m_Down = true;
                            m_DownPos = (Vector3)msg.Payload[msg.ObjectIndex]; //down pos

                            msg.Payload[2] = true;
                        }

                        break;

                    case MRKInputControllerMouseEventKind.Drag:
                        if (m_Down) {
                            float curY = ((Vector3)msg.Payload[msg.ObjectIndex]).y;
                            float diff = curY - m_DownPos.y;

                            float percyPop = -diff / Screen.width / 0.2f;
                            if (percyPop > 0.3f) { //30% threshold
                                m_BackIndicator.SetAlpha(Mathf.Clamp01(percyPop - 0.3f)); //remove the 30% threshold
                            }
                        }

                        break;

                    case MRKInputControllerMouseEventKind.Up:
                        if (m_Down) {
                            m_Down = false;

                            if (m_BackIndicator.LastAlpha > 0.7f) {
                                HideScreen(() => {
                                    ScreenManager.MapInterface.ExternalForceHide(); //MainScreen is shown <---
                                });
                            }

                            DOTween.To(() => m_BackIndicator.LastAlpha, x => m_BackIndicator.SetAlpha(x), 0f, 0.3f);
                        }

                        break;
                }
            }
        }
    }
}
