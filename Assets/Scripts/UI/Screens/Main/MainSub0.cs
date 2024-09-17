using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public class MainSub0 : Screen
    {
        private int _index;
        private readonly string[] _stringTable;

        public ScrollRect Scroll
        {
            get; private set;
        }

        public MainSub0()
        {
            _stringTable = new string[] {
                "TRENDING\nNOW", "EGR\nMAPS", "QUICK\nLOCATIONS",
                "WHAT\nTO\nEAT", "EGR\nFOOD", "DELIVERY\nSERVICE",
                "MOSQUES\nMAP", "EGR\nGYMS", "SMOKING\nMAP"
            };
        }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            _index = int.Parse(ScreenName.Replace("MainSub", ""));

            for (int i = 0; i < 3; i++)
            {
                Transform child = GetTransform($"Scroll View/Viewport/Content/Template{i}");
                int _i = i;
                Button but = child.Find("Button").GetComponent<Button>();
                but.onClick.AddListener(() =>
                {
                    int idx = _index * 3 + _i;
                    ScreenManager.GetScreen<Main>(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ProcessAction(0, idx, GetText(but, idx));
                });
            }

            Scroll = GetElement<ScrollRect>("Scroll View");
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            //TODO: Add canvas bounds
            //rectTransform.DOAnchorPosX(0f, TweenMonitored(0.7f))
            //    .ChangeStartValue((ScreenManager.MainScreen.LastAction ? 1f : -1f) * Screen.width);

            if (_lastGraphicsBuf == null)
                _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(); //.Where(gfx => gfx.GetComponent<ScrollRect>() != null).ToArray();

            PushGfxState(GfxStates.Position | GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                if (gfx.GetComponent<ScrollRect>() == null)
                {
                    gfx.DOColor(gfx.color, TweenMonitored(0.3f + i * 0.03f))
                        .ChangeStartValue(Color.clear)
                        .SetEase(Ease.OutSine);

                    SetGfxStateMask(gfx, GfxStates.Color);
                    continue;
                }

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.4f))
                    .ChangeStartValue((ScreenManager.MainScreen.LastAction ? 2f : -1f) * gfx.transform.position)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            //SetTweenCount(1);
            //rectTransform.DOAnchorPosX((ScreenManager.MainScreen.LastAction ? -1f : 1f) * Screen.width, TweenMonitored(0.7f));

            //colors + xpos - blur
            SetTweenCount(_lastGraphicsBuf.Length + 1);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.2f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);

                if (gfx.GetComponent<ScrollRect>() != null)
                {
                    gfx.transform.DOMoveX((ScreenManager.MainScreen.LastAction ? -1f : 2f) * gfx.transform.position.x, TweenMonitored(0.3f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
                }
            }

            return true;
        }

        protected override void OnScreenUpdate()
        {
            if (!ScreenManager.MainScreen.ShouldShowSubScreen(_index))
            {
                ForceHideScreen();
            }
            else
                ShowScreen();
        }

        protected override void OnScreenShow()
        {
            ScreenManager.MainScreen.ActiveScroll = Scroll.horizontalScrollbar;
        }

        protected override void OnScreenHide()
        {
            if (ScreenManager.MainScreen.ActiveScroll == Scroll.horizontalScrollbar)
                ScreenManager.MainScreen.ActiveScroll = null;
        }

        private string GetText(Button b, int idx)
        {
            if (idx < _stringTable.Length)
                return _stringTable[idx];

            Transform trans = b.transform.parent;
            string txt = "";

            Transform buf = trans.Find("Text");
            if (buf != null)
                txt += buf.GetComponent<TextMeshProUGUI>().text;

            buf = trans.Find("Text0");
            if (buf != null)
                txt += buf.GetComponent<TextMeshProUGUI>().text;

            buf = trans.Find("Text1");

            if (buf != null)
                txt += buf.GetComponent<TextMeshProUGUI>().text;

            return txt.Trim('\n', '\r');
        }
    }
}
