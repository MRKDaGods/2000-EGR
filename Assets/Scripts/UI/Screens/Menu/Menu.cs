using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_Menu;

namespace MRK.UI
{
    public class Menu : Screen, ISupportsBackKey
    {
        [SerializeField]
        private string[] _buttons;
        private TextMeshProUGUI[] _texts;
        private float _barWidth;
        private Button _blur;
        private bool _dirty;

        public override bool CanChangeBar
        {
            get
            {
                return true;
            }
        }

        public override uint BarColor
        {
            get
            {
                return 0x64000000;
            }
        }

        protected override void OnScreenInit()
        {
            Image bar = GetElement<Image>(Images.Bar);
            bar.gameObject.SetActive(false);

            Button opt = GetElement<Button>(Buttons.Opt);
            opt.gameObject.SetActive(false);

            float lastY = 0f;
            _texts = new TextMeshProUGUI[_buttons.Length];

            for (int i = 0; i < _buttons.Length; i++)
            {
                Image _bar = Instantiate(bar, bar.transform.parent);
                Button _opt = Instantiate(opt, opt.transform.parent);

                TextMeshProUGUI txt = _opt.GetComponent<TextMeshProUGUI>();
                txt.text = _buttons[i];
                _texts[i] = txt;

                RectTransform[] trans = new RectTransform[2] {
                    _bar.rectTransform, _opt.transform as RectTransform
                };

                float space = (trans[0].anchoredPosition.y - trans[1].anchoredPosition.y) / 2f;
                float total = _buttons.Length * (trans[0].rect.height + space + trans[1].rect.height + space) + trans[0].rect.height;

                float y = -total / 2f + (_buttons.Length - 1 - i) * (trans[0].rect.height + space + trans[1].rect.height + space);
                if (i == _buttons.Length - 1)
                {
                    lastY = -total / 2f + _buttons.Length * (trans[0].rect.height + space + trans[1].rect.height + space);
                }

                trans[0].anchoredPosition = new Vector2(trans[0].anchoredPosition.x, y);
                trans[1].anchoredPosition = new Vector2(trans[1].anchoredPosition.x, y + trans[0].rect.height + space);

                //trans[1].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, txt.preferredWidth);

                _bar.gameObject.SetActive(i < _buttons.Length - 1);
                _opt.gameObject.SetActive(true);

                int local = i;
                _opt.onClick.AddListener(() => ProcessAction(local));
            }

            //Image finalbar = Instantiate(bar, bar.transform.parent);
            //finalbar.rectTransform.anchoredPosition = new Vector2(finalbar.rectTransform.anchoredPosition.x, lastY);
            //finalbar.gameObject.SetActive(true);

            _barWidth = bar.rectTransform.sizeDelta.x;

            _blur = GetElement<Button>(Buttons.Blur);
            _blur.onClick.AddListener(OnBlurClicked);
        }

        protected override void OnScreenShow()
        {
            if (!_dirty)
            {
                _dirty = true;

                foreach (TextMeshProUGUI txt in _texts)
                {
                    txt.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(txt.preferredWidth, _barWidth));
                    txt.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, txt.rectTransform.sizeDelta.y * 2f);
                }
            }

            Client.DisableAllScreensExcept<Menu>(typeof(Options));
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim(); // no extensive workload down there

            if (_lastGraphicsBuf == null)
            {
                _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
                Array.Sort(_lastGraphicsBuf, (x, y) =>
                {
                    return y.transform.position.y.CompareTo(x.transform.position.y);
                });
            }

            PushGfxState(GfxStates.Position | GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.6f + i * 0.03f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                if (gfx != _blur.targetGraphic)
                {
                    gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.3f + i * 0.03f))
                        .ChangeStartValue(-1f * gfx.transform.position)
                        .SetEase(Ease.OutSine);
                }
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            //colors + xpos - blur
            SetTweenCount(_lastGraphicsBuf.Length * 2 - 1);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.3f + i * 0.03f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);

                if (gfx != _blur.targetGraphic)
                {
                    gfx.transform.DOMoveX(-gfx.transform.position.x, TweenMonitored((0.3f + i * 0.03f)))
                        .SetEase(Ease.OutSine)
                        .OnComplete(OnTweenFinished);
                }
            }

            return true;
        }

        private void OnBlurClicked()
        {
            HideScreen(() =>
            {
                ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ShowScreen(null, true);
            }, 0.1f, true);
        }

        private void ProcessAction(int idx)
        {
            switch (idx)
            {

                case 0:
                    HideScreen(() => ScreenManager.GetScreen<Options>().ShowScreen(), 0.1f, true);
                    break;

                case 1:
                    HideScreen(() => ScreenManager.GetScreen<AppSettings>().ShowScreen(), 0.1f, true);
                    break;

            }
        }

        public void OnBackKeyDown()
        {
            OnBlurClicked();
        }
    }
}
