using DG.Tweening;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_Main;

namespace MRK.UI {
    public class EGRScreenMain : EGRScreen {
        class NavButton {
            Button m_Button;
            float m_LastAlpha;

            public NavButton(Button button) {
                m_Button = button;

                SetAlpha(0f);
            }

            public void SetActive(bool active) {
                m_Button.gameObject.SetActive(active);
            }

            public void SetAlpha(float alpha) {
                if (alpha == m_LastAlpha)
                    return;

                m_LastAlpha = alpha;
                foreach (Graphic gfx in m_Button.GetComponentsInChildren<Graphic>(true)) {
                    if (gfx.name != "Mask")
                        gfx.color = gfx.color.AlterAlpha(Mathf.Clamp01(alpha));
                }
            }
        }

        int m_CurrentPage;
        int m_PageCount;
        NavButton[] m_NavButtons;
        Image m_BaseBg;
        readonly GameObject[] m_Regions;
        EGRScreen[] m_RegionScreens;
        Scrollbar m_ActiveScroll;
        bool m_Down;

        public Image BaseBackground => m_BaseBg;
        public Scrollbar ActiveScroll {
            get {
                return m_ActiveScroll;
            }

            set {
                if (m_ActiveScroll != null) {
                    m_ActiveScroll.onValueChanged.RemoveListener(OnScrollValueChanged);
                }

                m_ActiveScroll = value;
                if (m_ActiveScroll != null) {
                    m_ActiveScroll.onValueChanged.AddListener(OnScrollValueChanged);
                }
            }
        }
        public bool LastAction { get; private set; }

        public override bool CanChangeBar => true;
        public override uint BarColor => 0x64000000;

        public EGRScreenMain() {
            m_Regions = new GameObject[4];
        }

        protected override void OnScreenInit() {
            m_BaseBg = GetElement<Image>(Images.BaseBg);

            m_NavButtons = new NavButton[2];
            string[] navButtons = new string[2] { Buttons.Back, Buttons.Next };
            for (int i = 0; i < m_NavButtons.Length; i++) {
                Button b = GetElement<Button>(navButtons[i]);

                int local = i;
                b.onClick.AddListener(() => NavigationCallback(local));
                m_NavButtons[i] = new NavButton(b);
            }

            GetElement<Button>(Buttons.TopLeftMenu).onClick.AddListener(() => {
                //m_BaseBg.material = null;
                HideScreen(() => {
                    ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Menu.SCREEN_NAME).ShowScreen(this, true);
                });

                m_RegionScreens[m_CurrentPage].HideScreen(null, 0f, true);
            });

            m_CurrentPage = 0;
            m_PageCount = m_Regions.Length; // Mathf.CeilToInt(m_Texts.Length / 3f);

            UpdateNavButtonsVisibility();
        }

        protected override void OnScreenShow() {
            m_Down = false;
            LastAction = true;

            Client.SetMapMode(EGRMapMode.Globe);

            if (m_RegionScreens == null) {
                m_RegionScreens = new EGRScreen[4];
                for (int i = 0; i < m_RegionScreens.Length; i++) { //EGRScreen_MainSub00
                    Type t = Type.GetType("MRK.UI.EGRUI_Main").GetNestedType($"EGRScreen_MainSub0{i}", BindingFlags.Public);
                    m_RegionScreens[i] = ScreenManager.GetScreen((string)t
                        .GetField("SCREEN_NAME", BindingFlags.Static | BindingFlags.Public).GetValue(null));
                }
            }

            UpdateTemplates(-1);
            Client.RegisterControllerReceiver(OnReceiveControllerMessage);
        }

        protected override void OnScreenHide() {
            m_RegionScreens[m_CurrentPage].HideScreen();
            Client.UnregisterControllerReceiver(OnReceiveControllerMessage);
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            if (m_LastGraphicsBuf == null)
                m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);

            PushGfxState(EGRGfxState.Position | EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.5f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                if (gfx != m_BaseBg) {
                    gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.3f + Mathf.Min(0.1f, i * 0.03f)))
                        .ChangeStartValue(-1f * gfx.transform.position)
                        .SetEase(Ease.OutSine);
                }
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            //m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            //colors + xpos - blur
            SetTweenCount(m_LastGraphicsBuf.Length * 2 - 1);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.3f + i * 0.03f + (i > 10 ? 0.1f : 0f)))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);

                if (gfx != m_BaseBg) {
                    gfx.transform.DOMoveX(-gfx.transform.position.x, TweenMonitored((0.3f + i * 0.03f)))
                        .SetEase(Ease.OutSine)
                        .OnComplete(OnTweenFinished);
                }
            }

            return true;
        }

        protected override void OnScreenUpdate() {
            UpdateNavButtonsVisibility();
        }

        void OnReceiveControllerMessage(EGRControllerMessage msg) {
            if (msg.ContextualKind == EGRControllerMessageContextualKind.Mouse) {
                EGRControllerMouseEventKind kind = (EGRControllerMouseEventKind)msg.Payload[0];

                switch (kind) {

                    case EGRControllerMouseEventKind.Down:
                        m_Down = true;
                        break;

                    case EGRControllerMouseEventKind.Up:
                        if (m_Down) {
                            HandleSwipe();
                        }

                        m_Down = false;
                        break;

                }
            }
        }

        void HandleSwipe() {
            if (m_ActiveScroll == null)
                return;

            if (m_ActiveScroll.size > 0.9f)
                return;

            NavigationCallback((int)m_ActiveScroll.value);
        }

        void NavigationCallback(int idx) {
            int old = m_CurrentPage;
            m_CurrentPage += idx == 0 ? -1 : 1;
            if (m_CurrentPage == -1)
                m_CurrentPage = m_PageCount - 1;

            if (m_CurrentPage == m_PageCount)
                m_CurrentPage = 0;

            LastAction = idx == 0 ? false : true;

            UpdateTemplates(old);
            UpdateNavButtonsVisibility();
        }

        void UpdateTemplates(int old) {
            if (old != -1) {
                m_RegionScreens[old].HideScreen(() => {
                    if (!ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Menu.SCREEN_NAME).Visible)
                        m_RegionScreens[m_CurrentPage].ShowScreen(null, true);
                }, 0f, true);
            }
            else
                m_RegionScreens[m_CurrentPage].ShowScreen(null, true);
        }

        void UpdateNavButtonsVisibility() {
            NavButton back = m_NavButtons[0];
            back.SetActive(m_CurrentPage > 0);

            NavButton next = m_NavButtons[1];
            next.SetActive(m_CurrentPage < m_PageCount - 1);

            if (m_ActiveScroll != null) {
                float absSz = 1f - m_ActiveScroll.size;
                back.SetAlpha((1f - -1f * (absSz - 1f)) * 2f - (m_ActiveScroll.value == 0f ? 0f : 0.9f));
                next.SetAlpha((1f - -1f * (absSz - 1f)) * 2f - (m_ActiveScroll.value == 0f ? 0.9f : 0f));
            }
        }

        public void ProcessAction(int s, int idx, string txt) {
            //do not proceed if we're still transitioning from General->Globe
            if (Client.InitialModeTransition)
                return;

            Client.SetPostProcessState(true);

            m_RegionScreens[m_CurrentPage].HideScreen(null, 0.1f, true);

            //TODO: implement a better way to execute section indices delegates

            EGRScreenMapInterface scr = ScreenManager.GetScreen<EGRScreenMapInterface>();
            scr.SetContextText(txt);
            scr.OnInterfaceEarlyShow();

            //WTE override
            if (s == 0) {
                switch (idx) {

                    //QUICK LOCATIONS
                    case 2:
                        //HideScreen(() => {
                            scr.ShowScreen();
                            ScreenManager.GetScreen<EGRScreenQuickLocations>().ShowScreen();
                        //}, 0f, true);

                        break;
                    
                    //WHAT TO EAT
                    case 3:
                        HideScreen(() => {
                            ScreenManager.GetScreen<EGRScreenWTE>().ShowScreen();
                        }, 0f, true);

                        break;

                }
            }

            HideScreen(() => {
                scr.ShowScreen();
            }, 0f, true);

            Debug.Log($"DOWN {s} - {idx} - {txt}");
        }

        void OnScrollValueChanged(float newVal) {
            /*int idx = GetDesiredTitleIdx(newVal);
            if (idx != m_CurrentTitleIdx) {
                m_CurrentTitleIdx = idx;
                UpdateTitleVisibility();
            }*/

            //UpdateNavButtonsVisibility();

            //Debug.Log("NEW VAL " + newVal);
        }

        public bool ShouldShowSubScreen(int idx) {
            return Visible && m_CurrentPage == idx;
        }
    }
}
