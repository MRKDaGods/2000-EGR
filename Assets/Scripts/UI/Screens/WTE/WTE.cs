using Coffee.UIEffects;
using DG.Tweening;
using MRK.InputControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace MRK.UI
{
    public partial class WTE : Screen, IFOVStabilizer
    {
        private class Strip
        {
            public Image Image;
            public float EmissionOffset;
            public float ScaleOffset;
        }

        [Serializable]
        private struct ContextGradient
        {
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
        private struct ContextOptions
        {
            public string[] Options;
        }

        private class Indicator
        {
            private Graphic[] m_Gfx;

            public float LastAlpha
            {
                get; private set;
            }

            public Indicator(Transform trans)
            {
                m_Gfx = trans.GetComponentsInChildren<Graphic>(true);
            }

            public void SetAlpha(float alpha)
            {
                LastAlpha = alpha;

                foreach (Graphic gfx in m_Gfx)
                {
                    gfx.color = gfx.color.AlterAlpha(alpha);
                }
            }
        }

        [SerializeField]
        private GameObject _screenSpaceWTE;
        private Image _linePrefab;
        private Canvas _canvas;
        private readonly List<Strip> _strips;
        private float _time;
        private LensDistortion _lensDistortion;
        private Image _wteTextBg;
        private TextMeshProUGUI _wteText;
        private readonly EGRColorFade _stripFade;
        private EGRFiniteStateMachine _animFSM;
        private readonly EGRColorFade _transitionFade;
        private bool _shouldUpdateAnimFSM;
        private UIDissolve _wteTextBgDissolve;
        private GameObject _overlayWTE;
        private RectTransform m_wteLogoMaskTransform;
        private ContextArea _contextArea;
        private bool _down;
        private Vector2 _downPos;
        private Indicator _backIndicator;
        [SerializeField]
        private ContextGradient[] _contextGradients;
        [SerializeField]
        private ContextOptions[] _contextOptions;
        private Vector2? m_wteLogoSizeDelta;

        public float TargetFOV
        {
            get
            {
                return EGRConstants.EGR_CAMERA_DEFAULT_FOV;
            }
        }

        private static WTE ms_Instance
        {
            get; set;
        }

        public WTE()
        {
            _strips = new List<Strip>();
            _stripFade = new EGRColorFade(Color.white.AlterAlpha(0f), Color.white, 1f);
            _transitionFade = new EGRColorFade(Color.clear, Color.white, 0.8f);

            InitTransitionFSM();
        }

        protected override void OnScreenInit()
        {
            ms_Instance = this;

            _screenSpaceWTE.SetActive(false);

            _linePrefab = _screenSpaceWTE.transform.Find("LinePrefab").GetComponent<Image>();
            _linePrefab.gameObject.SetActive(false);

            _canvas = ScreenManager.GetScreenSpaceLayer(1);

            RectTransform canvasTransform = (RectTransform)_canvas.transform;
            int hStripCount = Mathf.CeilToInt(canvasTransform.rect.width / _linePrefab.rectTransform.rect.width);

            _linePrefab.rectTransform.sizeDelta = new Vector2(_linePrefab.rectTransform.sizeDelta.x, canvasTransform.rect.height);

            for (int i = 0; i < hStripCount; i++)
            {
                Image strip = Instantiate(_linePrefab, _linePrefab.transform.parent);
                strip.rectTransform.anchoredPosition = strip.rectTransform.rect.size * new Vector2(i + 0.5f, -0.5f);

                Material stripMat = Instantiate(strip.material);
                stripMat.color = Color.white.AlterAlpha(0f);

                float startEmission = Random.Range(0f, 2f);
                strip.material.SetFloat("_Emission", GetPingPongedValue(startEmission));

                float startScale = Random.Range(0f, 2f);
                strip.material.mainTextureScale = new Vector2(1f, GetPingPongedValue(startScale));

                strip.material = stripMat;
                strip.gameObject.SetActive(true);

                _strips.Add(new Strip
                {
                    Image = strip,
                    EmissionOffset = startEmission,
                    ScaleOffset = startScale
                });
            }

            _lensDistortion = GetPostProcessingEffect<LensDistortion>();

            _wteTextBg = _screenSpaceWTE.transform.Find("WTEText").GetComponent<Image>();
            _wteTextBg.transform.SetAsLastSibling();

            _wteText = _wteTextBg.GetComponentInChildren<TextMeshProUGUI>();

            _wteTextBgDissolve = _wteTextBg.GetComponent<UIDissolve>();

            _overlayWTE = GetTransform("Overlay").gameObject;
            m_wteLogoMaskTransform = (RectTransform)GetTransform("Overlay/WTEText");

            _contextArea = new ContextArea(_screenSpaceWTE.transform);

            _backIndicator = new Indicator(GetTransform("Overlay/Indicator"));
        }

        protected override void OnScreenShow()
        {
            _screenSpaceWTE.SetActive(true);
            _wteText.gameObject.SetActive(true);
            Client.ActiveEGRCamera.SetInterfaceState(true);

            _contextArea.SetActive(false);
            _backIndicator.SetAlpha(0f);

            foreach (Strip s in _strips)
            {
                s.Image.gameObject.SetActive(true);
            }

            //set initial lens distortion values
            _lensDistortion.intensity.value = 0f;
            _lensDistortion.centerX.value = 0f;
            _lensDistortion.centerY.value = 0f;
            _lensDistortion.scale.value = 1f;

            //we dont wanna see that yet
            _wteTextBg.gameObject.SetActive(false);
            _overlayWTE.SetActive(false);

            StartInitialTransition();

            Client.Runnable.RunLater(StartWTETransition, 1.2f);

            Client.RegisterControllerReceiver(OnControllerMessageReceived);

            //initials
            _stripFade.Reset();
            _stripFade.SetColors(Color.white.AlterAlpha(0f), Color.white, 1f);
            _transitionFade.Reset();
            _transitionFade.SetColors(Color.clear, Color.white, 0.8f);

            _time = 0f;
            _animFSM.ResetMachine();

            Client.Sun.gameObject.SetActive(false);
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            if (_lastGraphicsBuf == null)
                _lastGraphicsBuf = _screenSpaceWTE.GetComponentsInChildren<Graphic>(true);

            PushGfxState(GfxStates.Color);

            foreach (Graphic gfx in _lastGraphicsBuf)
            {
                gfx.DOColor(gfx.color, 0.4f)
                    .ChangeStartValue(gfx.color.AlterAlpha(0f))
                    .SetEase(Ease.OutSine);
            }
        }

        protected override void OnScreenHide()
        {
            _screenSpaceWTE.SetActive(false);
            Client.ActiveEGRCamera.SetInterfaceState(false);

            Client.UnregisterControllerReceiver(OnControllerMessageReceived);

            Client.Sun.gameObject.SetActive(true);
        }

        protected override void OnScreenUpdate()
        {
            _time += Time.deltaTime;

            bool stripFadeUpdated = false;
            if (!_stripFade.Done)
            {
                _stripFade.Update();
                stripFadeUpdated = true;
            }

            foreach (Strip strip in _strips)
            {
                if (!strip.Image.gameObject.activeInHierarchy)
                    break;

                if (stripFadeUpdated)
                {
                    strip.Image.material.color = _stripFade.Current;
                }

                strip.Image.material.SetFloat("_Emission", GetPingPongedValue(strip.EmissionOffset));
                strip.Image.material.mainTextureScale = new Vector2(1f, GetPingPongedValue(strip.ScaleOffset));
            }

            if (_shouldUpdateAnimFSM)
            {
                _animFSM.UpdateFSM();
            }
        }

        private float GetPingPongedValue(float offset)
        {
            return Mathf.PingPong(_time + offset, 2f);
        }

        private T GetPostProcessingEffect<T>() where T : PostProcessEffectSettings
        {
            return _screenSpaceWTE.GetComponent<PostProcessVolume>().profile.GetSetting<T>();
        }

        private void StartInitialTransition()
        {
            DOTween.To(() => _lensDistortion.intensity.value, x => _lensDistortion.intensity.value = x, -100f, 1f);
            DOTween.To(() => _lensDistortion.centerX.value, x => _lensDistortion.centerX.value = x, -0.5f, 1f);
            DOTween.To(() => _lensDistortion.centerY.value, x => _lensDistortion.centerY.value = x, 0.68f, 1f);
            DOTween.To(() => _lensDistortion.scale.value, x => _lensDistortion.scale.value = x, 1.55f, 1f);
        }

        private void StartWTETransition()
        {
            //enable text
            _wteTextBg.gameObject.SetActive(true);
            _wteTextBg.color = Color.clear;
            _wteText.color = _wteTextBg.color.InverseWithAlpha();

            _shouldUpdateAnimFSM = true;

            DOTween.To(() => _lensDistortion.intensity.value, x => _lensDistortion.intensity.value = x, 0f, 1f);
            DOTween.To(() => _lensDistortion.centerX.value, x => _lensDistortion.centerX.value = x, 0f, 1f);
            DOTween.To(() => _lensDistortion.centerY.value, x => _lensDistortion.centerY.value = x, 0f, 1f);
            DOTween.To(() => _lensDistortion.scale.value, x => _lensDistortion.scale.value = x, 1f, 1f);
        }

        private void AnimateStretchableTransform(RectTransform staticTransform, RectTransform stretchableTransform)
        {
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
                .OnUpdate(() =>
                {
                    staticTransform.position = oldPos;
                    stretchableTransform.position = oldStretchPos;
                })
                .SetEase(Ease.OutSine);
        }

        private void OnWTETransitionEnd()
        {
            _shouldUpdateAnimFSM = false;

            _overlayWTE.SetActive(true);
            //m_ContextualScrollView.gameObject.SetActive(true);
            _wteText.gameObject.SetActive(false);

            _contextArea.SetActive(true);

            foreach (Strip s in _strips)
            {
                s.Image.gameObject.SetActive(false);
            }

            foreach (Graphic gfx in _overlayWTE.GetComponentsInChildren<Graphic>())
            {
                gfx.DOFade(gfx.color.a, 0.5f)
                    .ChangeStartValue(gfx.color.AlterAlpha(0f))
                    .SetEase(Ease.OutSine);
            }

            if (!m_wteLogoSizeDelta.HasValue)
            {
                m_wteLogoSizeDelta = m_wteLogoMaskTransform.sizeDelta;
            }

            m_wteLogoMaskTransform.DOSizeDelta(m_wteLogoSizeDelta.Value, 0.5f)
                .ChangeStartValue(new Vector2(0f, m_wteLogoSizeDelta.Value.y))
                .SetEase(Ease.OutSine);

            //AnimateStretchableTransform(m_ContextualText.rectTransform, m_ContextualTextMaskTransform);
            //AnimateStretchableTransform(m_ContextualButtonsLayoutTransform, m_ContextualButtonsMaskTransform);

            int idx = 0;
            foreach (FancyScrollView view in _contextArea.ContextualScrollView)
            {
                ContextOptions options;
                if (view == null || (options = _contextOptions[idx]).Options.Length == 0)
                {
                    idx++;
                    continue;
                }

                view.UpdateData(options.Options
                    .Select(x => new FancyScrollViewItemData(x)).ToList());
                view.SelectCell(0, false);
                idx++;
            }

            _contextArea.SetupCellGradients();
        }

        private void OnControllerMessageReceived(Message msg)
        {
            if (_shouldUpdateAnimFSM) //animating
                return;

            if (msg.ContextualKind == MessageContextualKind.Mouse)
            {
                MouseEventKind kind = (MouseEventKind)msg.Payload[0];

                switch (kind)
                {
                    case MouseEventKind.Down:
                        if (_contextArea.Page == 0)
                        {
                            _down = true;
                            _downPos = (Vector3)msg.Payload[msg.ObjectIndex]; //down pos

                            msg.Payload[2] = true;
                        }

                        break;

                    case MouseEventKind.Drag:
                        if (_down)
                        {
                            float curY = ((Vector3)msg.Payload[msg.ObjectIndex]).y;
                            float diff = curY - _downPos.y;

                            float percyPop = -diff / Screen.width / 0.2f;
                            if (percyPop > 0.3f)
                            { //30% threshold
                                _backIndicator.SetAlpha(Mathf.Clamp01(percyPop - 0.3f)); //remove the 30% threshold
                            }
                        }

                        break;

                    case MouseEventKind.Up:
                        if (_down)
                        {
                            _down = false;

                            if (_backIndicator.LastAlpha > 0.7f)
                            {
                                HideScreen(() =>
                                {
                                    ScreenManager.MapInterface.ExternalForceHide(); //MainScreen is shown <---
                                });
                            }

                            DOTween.To(() => _backIndicator.LastAlpha, x => _backIndicator.SetAlpha(x), 0f, 0.3f);
                        }

                        break;
                }
            }
        }
    }
}
