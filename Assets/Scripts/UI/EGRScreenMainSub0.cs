using DG.Tweening;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using static MRK.UI.EGRUI_Main.EGRScreen_MainSub00;

namespace MRK.UI {
    public class EGRScreenMainSub0 : EGRScreen {
        int m_Index;
        readonly string[] m_StringTable;

        public ScrollRect Scroll { get; private set; }

        public EGRScreenMainSub0() {
            m_StringTable = new string[] {
                "TRENDING\nNOW", "EGR\nMAPS", "QUICK\nLOCATIONS",
                "WHAT\nTO\nEAT", "EGR\nFOOD", "DELIVERY\nSERVICE",
                "MOSQUES\nMAP", "EGR\nGYMS", "SMOKING\nMAP"
            };
        }

        protected override void OnScreenInit() {
            m_Index = int.Parse(ScreenName.Replace("MainSub", ""));

            for (int i = 0; i < 3; i++) {
                Transform child = GetTransform($"Scroll View/Viewport/Content/Template{i}");
                int _i = i;
                Button but = child.Find("Button").GetComponent<Button>();
                but.onClick.AddListener(() => {
                    int idx = m_Index * 3 + _i;
                    Manager.GetScreen<EGRScreenMain>(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ProcessAction(0, idx, GetText(but, idx));
                });
            }

            Scroll = GetElement<ScrollRect>("Scroll View");
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(); //.Where(gfx => gfx.GetComponent<ScrollRect>() != null).ToArray();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            PushGfxState(EGRGfxState.Position | EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                if (gfx.GetComponent<ScrollRect>() == null) {
                    gfx.DOColor(gfx.color, TweenMonitored(0.3f + i * 0.03f))
                        .ChangeStartValue(Color.clear)
                        .SetEase(Ease.OutSine);

                    SetGfxStateMask(gfx, EGRGfxState.Color);
                    continue;
                }

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.4f))
                    .ChangeStartValue((Manager.MainScreen.LastAction ? 2f : -1f) * gfx.transform.position)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            //colors + xpos - blur
            SetTweenCount(m_LastGraphicsBuf.Length + 1);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.2f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);

                if (gfx.GetComponent<ScrollRect>() != null) {
                    gfx.transform.DOMoveX((Manager.MainScreen.LastAction ? -1f : 2f) * gfx.transform.position.x, TweenMonitored(0.3f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
                }
            }

            return true;
        }

        protected override void OnScreenUpdate() {
            if (!Manager.MainScreen.ShouldShowSubScreen(m_Index)) {
                ForceHideScreen();
            }
            else
                ShowScreen();
        }

        protected override void OnScreenShow() {
            Manager.MainScreen.ActiveScroll = Scroll.horizontalScrollbar;
        }

        protected override void OnScreenHide() {
            if (Manager.MainScreen.ActiveScroll == Scroll.horizontalScrollbar)
                Manager.MainScreen.ActiveScroll = null;
        }

        string GetText(Button b, int idx) {
            if (idx < m_StringTable.Length)
                return m_StringTable[idx];

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
