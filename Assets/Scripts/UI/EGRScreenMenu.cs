using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_Menu;

namespace MRK.UI {
    public class EGRScreenMenu : EGRScreen {
        [SerializeField]
        string[] m_Buttons;
        TextMeshProUGUI[] m_Texts;
        float m_BarWidth;
        Button m_Blur;
        bool m_Dirty;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0x64000000;

        protected override void OnScreenInit() {
            Image bar = GetElement<Image>(Images.Bar);
            bar.gameObject.SetActive(false);

            Button opt = GetElement<Button>(Buttons.Opt);
            opt.gameObject.SetActive(false);

            float lastY = 0f;
            m_Texts = new TextMeshProUGUI[m_Buttons.Length];

            for (int i = 0; i < m_Buttons.Length; i++) {
                Image _bar = Instantiate(bar, bar.transform.parent);
                Button _opt = Instantiate(opt, opt.transform.parent);

                TextMeshProUGUI txt = _opt.GetComponent<TextMeshProUGUI>();
                txt.text = m_Buttons[i];
                m_Texts[i] = txt;

                RectTransform[] trans = new RectTransform[2] {
                    _bar.rectTransform, _opt.transform as RectTransform
                };

                float space = (trans[0].anchoredPosition.y - trans[1].anchoredPosition.y) / 2f;
                float total = m_Buttons.Length * (trans[0].rect.height + space + trans[1].rect.height + space) + trans[0].rect.height;

                float y = -total / 2f + (m_Buttons.Length - 1 - i) * (trans[0].rect.height + space + trans[1].rect.height + space);
                if (i == m_Buttons.Length - 1) {
                    lastY = -total / 2f + m_Buttons.Length * (trans[0].rect.height + space + trans[1].rect.height + space);
                }

                trans[0].anchoredPosition = new Vector2(trans[0].anchoredPosition.x, y);
                trans[1].anchoredPosition = new Vector2(trans[1].anchoredPosition.x, y + trans[0].rect.height + space);

                //trans[1].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, txt.preferredWidth);

                _bar.gameObject.SetActive(i < m_Buttons.Length - 1);
                _opt.gameObject.SetActive(true);

                int local = i;
                _opt.onClick.AddListener(() => ProcessAction(local));
            }

            //Image finalbar = Instantiate(bar, bar.transform.parent);
            //finalbar.rectTransform.anchoredPosition = new Vector2(finalbar.rectTransform.anchoredPosition.x, lastY);
            //finalbar.gameObject.SetActive(true);

            m_BarWidth = bar.rectTransform.sizeDelta.x;

            m_Blur = GetElement<Button>(Buttons.Blur);
            m_Blur.onClick.AddListener(OnBlurClicked);
        }

        protected override void OnScreenShow() {
            if (!m_Dirty) {
                m_Dirty = true;

                foreach (TextMeshProUGUI txt in m_Texts) {
                    txt.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(txt.preferredWidth, m_BarWidth));
                    txt.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, txt.rectTransform.sizeDelta.y * 2f);
                }
            }

            Client.DisableAllScreensExcept<EGRScreenMenu>(typeof(EGRScreenOptions));
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim(); // no extensive workload down there

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            PushGfxState(EGRGfxState.Position | EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.6f + i * 0.03f + (i > 10 ? 0.3f : 0f)))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                if (gfx != m_Blur.targetGraphic) {
                    gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.3f + i * 0.03f))
                        .ChangeStartValue(-1f * gfx.transform.position)
                        .SetEase(Ease.OutSine);
                }
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            /*m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            }); */

            //colors + xpos - blur
            SetTweenCount(m_LastGraphicsBuf.Length * 2 - 1);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.3f + i * 0.03f + (i > 10 ? 0.1f : 0f)))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);

                if (gfx != m_Blur.targetGraphic) {
                    gfx.transform.DOMoveX(-gfx.transform.position.x, TweenMonitored((0.3f + i * 0.03f)))
                        .SetEase(Ease.OutSine)
                        .OnComplete(OnTweenFinished);
                }
            }

            return true;
        }

        protected override void OnScreenHide() {
            //restore base bg mat
            //(Manager.GetScreen(EGRUI_Main.EGRScreen_Main.SCREEN_NAME) as EGRScreenMain).BaseBackground.material = m_Blur.GetComponent<Image>().material;
        }

        void OnBlurClicked() {
            HideScreen(() => {
                Manager.GetScreen(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ShowScreen(null, true);
            }, 0.1f, true);
        }

        void ProcessAction(int idx) {
            switch (idx) {

                case 0:
                    HideScreen(() => Manager.GetScreen<EGRScreenOptions>().ShowScreen(), 0.1f, true);
                    break;

                case 1:
                    HideScreen(() => Manager.GetScreen<EGRScreenOptionsAppSettings>().ShowScreen(), 0.1f, true);
                    break;

            }
        }
    }
}
