using DG.Tweening;
using MRK.InputControllers;
using MRK.Localization;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_Main;
using static MRK.Localization.LanguageManager;

namespace MRK.UI.Screens
{
    public class Main : Screen, ISupportsBackKey
    {
        private class NavButton
        {
            private Button _button;
            private float _lastAlpha;

            public NavButton(Button button)
            {
                _button = button;

                SetAlpha(0f);
            }

            public void SetActive(bool active)
            {
                _button.gameObject.SetActive(active);
            }

            public void SetAlpha(float alpha)
            {
                if (alpha == _lastAlpha)
                    return;

                _lastAlpha = alpha;
                foreach (Graphic gfx in _button.GetComponentsInChildren<Graphic>(true))
                {
                    if (gfx.name != "Mask")
                        gfx.color = gfx.color.AlterAlpha(Mathf.Clamp01(alpha));
                }
            }
        }

        private int _currentPage;
        private int _pageCount;
        private NavButton[] _navButtons;
        private Image _baseBg;
        private readonly GameObject[] _regions;
        private Screen[] _regionScreens;
        private Scrollbar _activeScroll;
        private bool _down;

        public Image BaseBackground
        {
            get
            {
                return _baseBg;
            }
        }

        public Scrollbar ActiveScroll
        {
            get
            {
                return _activeScroll;
            }

            set
            {
                _activeScroll = value;
            }
        }

        public bool LastAction
        {
            get; private set;
        }

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

        public Main()
        {
            _regions = new GameObject[4];
        }

        protected override void OnScreenInit()
        {
            _baseBg = GetElement<Image>(Images.BaseBg);

            _navButtons = new NavButton[2];
            string[] navButtons = new string[2] { Buttons.Back, Buttons.Next };
            for (int i = 0; i < _navButtons.Length; i++)
            {
                Button b = GetElement<Button>(navButtons[i]);

                int local = i;
                b.onClick.AddListener(() => NavigationCallback(local));
                _navButtons[i] = new NavButton(b);
            }

            GetElement<Button>(Buttons.TopLeftMenu).onClick.AddListener(() =>
            {
                //m_BaseBg.material = null;
                HideScreen(() =>
                {
                    ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Menu.SCREEN_NAME).ShowScreen(this, true);
                });

                _regionScreens[_currentPage].HideScreen(null, 0f, true);
            });

            _currentPage = 0;
            _pageCount = _regions.Length; // Mathf.CeilToInt(m_Texts.Length / 3f);

            UpdateNavButtonsVisibility();
        }

        protected override void OnScreenShow()
        {
            _down = false;
            LastAction = true;

            Client.SetMapMode(EGRMapMode.Globe);

            if (_regionScreens == null)
            {
                _regionScreens = new Screen[4];
                for (int i = 0; i < _regionScreens.Length; i++)
                { //EGRScreen_MainSub00
                    Type t = Type.GetType("MRK.UI.EGRUI_Main").GetNestedType($"EGRScreen_MainSub0{i}", BindingFlags.Public);
                    _regionScreens[i] = ScreenManager.GetScreen((string)t
                        .GetField("SCREEN_NAME", BindingFlags.Static | BindingFlags.Public).GetValue(null));
                }
            }

            UpdateTemplates(-1);
            Client.RegisterControllerReceiver(OnReceiveControllerMessage);
        }

        protected override void OnScreenHide()
        {
            _regionScreens[_currentPage].HideScreen();
            Client.UnregisterControllerReceiver(OnReceiveControllerMessage);
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            if (_lastGraphicsBuf == null)
                _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);

            PushGfxState(GfxStates.Position | GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.5f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                if (gfx != _baseBg)
                {
                    gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.3f + Mathf.Min(0.1f, i * 0.03f)))
                        .ChangeStartValue(-1f * gfx.transform.position)
                        .SetEase(Ease.OutSine);
                }
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            //m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            //colors + xpos - blur
            SetTweenCount(_lastGraphicsBuf.Length * 2 - 1);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.3f + i * 0.03f + (i > 10 ? 0.1f : 0f)))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);

                if (gfx != _baseBg)
                {
                    gfx.transform.DOMoveX(-gfx.transform.position.x, TweenMonitored((0.3f + i * 0.03f)))
                        .SetEase(Ease.OutSine)
                        .OnComplete(OnTweenFinished);
                }
            }

            return true;
        }

        protected override void OnScreenUpdate()
        {
            UpdateNavButtonsVisibility();
        }

        private void OnReceiveControllerMessage(Message msg)
        {
            if (msg.ContextualKind == MessageContextualKind.Mouse)
            {
                MouseEventKind kind = (MouseEventKind)msg.Payload[0];

                switch (kind)
                {
                    case MouseEventKind.Down:
                        _down = true;
                        break;

                    case MouseEventKind.Up:
                        if (_down)
                        {
                            HandleSwipe();
                        }

                        _down = false;
                        break;
                }
            }
        }

        private void HandleSwipe()
        {
            if (_activeScroll == null)
                return;

            if (_activeScroll.size > 0.9f)
                return;

            NavigationCallback((int)_activeScroll.value);
        }

        private void NavigationCallback(int idx)
        {
            int old = _currentPage;
            _currentPage += idx == 0 ? -1 : 1;
            if (_currentPage == -1)
                _currentPage = _pageCount - 1;

            if (_currentPage == _pageCount)
                _currentPage = 0;

            LastAction = idx != 0;

            UpdateTemplates(old);
            UpdateNavButtonsVisibility();
        }

        private void UpdateTemplates(int old)
        {
            if (old != -1)
            {
                _regionScreens[old].HideScreen(() =>
                {
                    if (!ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Menu.SCREEN_NAME).Visible)
                        _regionScreens[_currentPage].ShowScreen(null, true);
                }, 0f, true, false);
            }
            else
                _regionScreens[_currentPage].ShowScreen(null, true);
        }

        private void UpdateNavButtonsVisibility()
        {
            NavButton back = _navButtons[0];
            back.SetActive(true/*m_CurrentPage > 0*/);

            NavButton next = _navButtons[1];
            next.SetActive(true/*m_CurrentPage < m_PageCount - 1*/);

            if (_activeScroll != null)
            {
                float absSz = 1f - _activeScroll.size;
                back.SetAlpha(((1f - (-1f * (absSz - 1f))) * 2f) - (_activeScroll.value == 0f ? 0f : 0.9f));
                next.SetAlpha(((1f - (-1f * (absSz - 1f))) * 2f) - (_activeScroll.value == 0f ? 0.9f : 0f));
            }
        }

        public void ProcessAction(int s, int idx, string txt)
        {
            //do not proceed if we're still transitioning from General->Globe
            if (Client.InitialModeTransition)
                return;

            Client.SetPostProcessState(true);

            _regionScreens[_currentPage].HideScreen(null, 0.1f, true);

            //TODO: implement a better way to execute section indices delegates

            HUD scr = ScreenManager.GetScreen<HUD>();
            scr.SetContextText(txt);
            scr.OnInterfaceEarlyShow();

            //WTE override
            if (s == 0)
            {
                switch (idx)
                {

                    //QUICK LOCATIONS
                    case 2:
                        //HideScreen(() => {
                        scr.ShowScreen();
                        ScreenManager.GetScreen<QuickLocations>().ShowScreen();
                        //}, 0f, true);

                        break;

                    //WHAT TO EAT
                    case 3:
                        HideScreen(() =>
                        {
                            ScreenManager.GetScreen<WTE>().ShowScreen();
                        }, 0f, true);

                        break;

                }
            }

            HideScreen(() =>
            {
                scr.ShowScreen();
            }, 0f, true);

            Debug.Log($"DOWN {s} - {idx} - {txt}");
        }

        public bool ShouldShowSubScreen(int idx)
        {
            return Visible && _currentPage == idx;
        }

        public void OnBackKeyDown()
        {
            //exit?
            Confirmation popup = ScreenManager.GetPopup<Confirmation>();
            popup.SetYesButtonText(Localize(LanguageData.EXIT));
            popup.SetNoButtonText(Localize(LanguageData.CANCEL));
            popup.ShowPopup(
                Localize(LanguageData.EGR),
                Localize(LanguageData.YOU_ARE_EXITING__b_EGR__b____),
                (_, res) =>
                {
                    if (res == PopupResult.YES)
                    {
                        Application.Quit();
                    }
                },
                this
            );
        }
    }
}
