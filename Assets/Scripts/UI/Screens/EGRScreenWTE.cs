using Coffee.UIEffects;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Random = UnityEngine.Random;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public class EGRScreenWTE : EGRScreen, IEGRScreenFOVStabilizer {
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

        class ContextArea {
            EGRUIFancyScrollView[] m_ContextualScrollView;
            TextMeshProUGUI[] m_ContextualText;
            Image m_ContextualBg;
            UIGradient m_ContextualBgGradient;
            ScrollSnap m_ScrollSnap;
            int m_LastPage;
            TextAsset m_CuisineList;
            EGRUIScrollViewSortingLetters m_CuisineLettersView;
            readonly Dictionary<char, int> m_CuisineCharTable;
            EGRUIWTESearchConfirmation m_SearchConfirmation;

            public EGRUIFancyScrollView[] ContextualScrollView => m_ContextualScrollView;
            public int Page => m_LastPage;

            public ContextArea(Transform screenspaceTrans) {
                m_ScrollSnap = screenspaceTrans.Find("SSVM").GetComponent<ScrollSnap>();
                m_ScrollSnap.onPageChange += OnPageChanged;

                Transform list = m_ScrollSnap.transform.Find("List");
                m_ContextualScrollView = new EGRUIFancyScrollView[list.childCount];
                m_ContextualText = new TextMeshProUGUI[list.childCount];

                for (int i = 0; i < list.childCount; i++) {
                    Transform owner = list.Find($"{i + 1}"); //1 - 2 - 3
                    m_ContextualText[i] = owner.Find("ContextualText").GetComponent<TextMeshProUGUI>();
                    EGRUIFancyScrollView sv = owner.Find("ContextualButtons")?.GetComponent<EGRUIFancyScrollView>();
                    m_ContextualScrollView[i] = sv;

                    if (sv != null) {
                        int sIdx = i;
                        sv.OnDoubleSelection += x => OnDoubleSelection(sv, sIdx);
                    }
                }

                m_ContextualBg = screenspaceTrans.Find("ContextualBG").GetComponent<Image>();
                m_ContextualBgGradient = m_ContextualBg.GetComponent<UIGradient>();

                m_LastPage = -1;
                m_ScrollSnap.ChangePage(0);
                OnPageChanged(0);

                m_CuisineCharTable = new Dictionary<char, int>();

                m_SearchConfirmation = screenspaceTrans.Find("SearchConfirmation").GetComponent<EGRUIWTESearchConfirmation>();
            }

            public void SetupCellGradients() {
                //cells must've been init before doing this

                //setup cell gradients
                for (int i = 0; i < m_ContextualScrollView.Length; i++) {
                    UIGradient[] grads = m_ContextualScrollView[i]?.GetComponentsInChildren<UIGradient>();
                    if (grads == null)
                        continue;

                    ContextGradient grad = ms_Instance.m_ContextGradients[i];

                    foreach (UIGradient gradient in grads) {
                        gradient.color1 = grad.Third;
                        gradient.color2 = grad.Fourth;
                        gradient.color3 = grad.Fifth;
                        gradient.color4 = grad.Sixth;
                        gradient.offset = grad.Offset;
                        gradient.direction = grad.Direction;
                    }
                }
            }

            void OnPageChanged(int page) {
                if (m_LastPage == page)
                    return;

                ContextGradient curGradient = ms_Instance.m_ContextGradients[page];
                DOTween.To(() => m_ContextualBgGradient.color1, x => m_ContextualBgGradient.color1 = x, curGradient.First, 0.5f).SetEase(Ease.OutSine);
                DOTween.To(() => m_ContextualBgGradient.color2, x => m_ContextualBgGradient.color2 = x, curGradient.Second, 0.5f).SetEase(Ease.OutSine);

                //last page, hide WTE logo
                if (page == 2) {
                    ms_Instance.m_WTELogoMaskTransform.DOSizeDelta(new Vector2(0f, ms_Instance.m_WTELogoSizeDelta.Value.y), 0.5f)
                        .SetEase(Ease.OutSine);

                    if (m_CuisineList == null) {
                        ResourceRequest req = Resources.LoadAsync<TextAsset>("Features/wteCuisines");
                        req.completed += (op) => {
                            m_CuisineList = (TextAsset)req.asset;
                            EGRUIFancyScrollView scrollView = m_ContextualScrollView[2]; //last

                            scrollView.UpdateData(m_CuisineList.text.Split('\n')
                                .Select(x => new EGRUIFancyScrollViewItemData(x.Replace("\r", ""))).ToList());
                            scrollView.SelectCell(0);

                            foreach (char c in EGRUIScrollViewSortingLetters.Letters) {
                                for (int i = 0; i < scrollView.Items.Count; i++) {
                                    if (char.ToUpper(scrollView.Items[i].Text[0]) == c) {
                                        m_CuisineCharTable[c] = i;
                                        break;
                                    }
                                }
                            }

                            SetupCellGradients();

                            ms_Instance.MessageBox.HideScreen();
                        };

                        ms_Instance.MessageBox.ShowButton(false);
                        ms_Instance.MessageBox.ShowPopup(Localize(EGRLanguageData.EGR), Localize(EGRLanguageData.LOADING_CUISINES___), null, ms_Instance);
                    }

                    if (m_CuisineLettersView == null) {
                        EGRUIFancyScrollView sv = m_ContextualScrollView[2];
                        sv.OnSelectionChanged(OnCuisineSelectionChanged);
                        m_CuisineLettersView = sv.transform.parent.Find("CS").GetComponent<EGRUIScrollViewSortingLetters>();
                        m_CuisineLettersView.Initialize();
                        m_CuisineLettersView.OnLetterChanged += OnCuisineLetterChanged;
                    }
                }
                else if (m_LastPage == 2) {
                    ms_Instance.m_WTELogoMaskTransform.DOSizeDelta(ms_Instance.m_WTELogoSizeDelta.Value, 0.5f)
                        .SetEase(Ease.OutSine);
                }

                m_LastPage = page;
            }

            public void SetActive(bool active) {
                for (int i = 0; i < m_ContextualScrollView.Length; i++) {
                    m_ContextualScrollView[i]?.gameObject.SetActive(active);
                    m_ContextualText[i].gameObject.SetActive(active);
                }

                m_ContextualBg.gameObject.SetActive(active);

                if (active) {
                    m_ScrollSnap.ChangePage(0);

                    m_ContextualBg.DOColor(Color.white, 0.5f)
                        .ChangeStartValue(Color.white.AlterAlpha(0f))
                        .SetEase(Ease.OutSine);
                }
            }

            void OnDoubleSelection(EGRUIFancyScrollView sv, int screenIdx) {
                switch (screenIdx) {

                    case 0:
                    case 1:
                        m_ScrollSnap.ChangePage(screenIdx + 1);
                        break;

                    case 2:
                        m_SearchConfirmation.Show(new EGRUIWTESearchConfirmation.WTEContext {
                            People = m_ContextualScrollView[0].SelectedItem.Text,
                            Price = m_ContextualScrollView[1].SelectedIndex,
                            PriceStr = m_ContextualScrollView[1].SelectedItem.Text,
                            Cuisine = m_ContextualScrollView[2].SelectedItem.Text
                        });
                        break;

                }
            }

            void OnCuisineSelectionChanged(int idx) {
                m_CuisineLettersView.SelectLetter(m_ContextualScrollView[2].Items[idx].Text[0]);
            }

            void OnCuisineLetterChanged(char c) {
                if (m_CuisineCharTable.ContainsKey(c)) {
                    m_ContextualScrollView[2].SelectCell(m_CuisineCharTable[c], false);
                }
            }
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
            ms_Instance = this;

            m_ScreenSpaceWTE.SetActive(false);

            m_LinePrefab = m_ScreenSpaceWTE.transform.Find("LinePrefab").GetComponent<Image>();
            m_LinePrefab.gameObject.SetActive(false);

            m_Canvas = Manager.GetScreenSpaceLayer(1);

            RectTransform canvasTransform = (RectTransform)m_Canvas.transform;
            //Debug.Log(canvasTransform.rect.width + " | " + m_LinePrefab.rectTransform.rect.width);
            int hStripCount = Mathf.CeilToInt(canvasTransform.rect.width / m_LinePrefab.rectTransform.rect.width);
            //Debug.Log($"Strips={hStripCount}");

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

            //m_ContextualScrollView = m_ScreenSpaceWTE.transform.Find("ContextualButtons").GetComponent<EGRUIFancyScrollView>(); //GetElement<EGRUIFancyScrollView>("Overlay/ContextualButtons");

            //m_ContextualButtonsMaskTransform = (RectTransform)GetTransform("Overlay/ContextualButtons");
            //m_ContextualButtonsLayoutTransform = (RectTransform)m_ContextualButtonsMaskTransform.Find("Layout");

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

        void OnControllerMessageReceived(EGRControllerMessage msg) {
            if (m_ShouldUpdateAnimFSM) //animating
                return;

            if (msg.ContextualKind == EGRControllerMessageContextualKind.Mouse) {
                EGRControllerMouseEventKind kind = (EGRControllerMouseEventKind)msg.Payload[0];

                switch (kind) {
                    case EGRControllerMouseEventKind.Down:
                        if (m_ContextArea.Page == 0) {
                            m_Down = true;
                            m_DownPos = (Vector3)msg.Payload[msg.ObjectIndex]; //down pos

                            msg.Payload[2] = true;
                        }

                        break;

                    case EGRControllerMouseEventKind.Drag:
                        if (m_Down) {
                            float curY = ((Vector3)msg.Payload[msg.ObjectIndex]).y;
                            float diff = curY - m_DownPos.y;

                            float percyPop = -diff / Screen.width / 0.2f;
                            if (percyPop > 0.3f) { //30% threshold
                                m_BackIndicator.SetAlpha(Mathf.Clamp01(percyPop - 0.3f)); //remove the 30% threshold
                            }
                        }

                        break;

                    case EGRControllerMouseEventKind.Up:
                        if (m_Down) {
                            m_Down = false;

                            if (m_BackIndicator.LastAlpha > 0.7f) {
                                HideScreen(() => {
                                    Manager.MapInterface.ForceHideScreen(true);
                                    Manager.MainScreen.ShowScreen();
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
