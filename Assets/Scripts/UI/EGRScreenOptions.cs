using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;
using static MRK.UI.EGRUI_Main.EGRScreen_Options;

namespace MRK.UI {
    public class EGRScreenOptions : EGRScreen {
        readonly Image[] m_Backgrounds; //we have 3

        public override bool CanChangeBar => true;
        public override uint BarColor => 0xFF000000;

        public EGRScreenOptions() {
            m_Backgrounds = new Image[3];
        }

        protected override void OnScreenInit() {
            GetElement<Button>(Buttons.TopLeftMenu).onClick.AddListener(() => {
                HideScreen(() => Manager.GetScreen(EGRUI_Main.EGRScreen_Menu.SCREEN_NAME).ShowScreen(), 0.1f, false);
            });

            GetElement<Button>(Buttons.Acc).onClick.AddListener(() => {
                Manager.GetScreen(EGRUI_Main.EGRScreen_OptionsAccInfo.SCREEN_NAME).ShowScreen();
            });

            GetElement<Button>(Buttons.Settings).onClick.AddListener(() => {
                Manager.GetScreen<EGRScreenMenuSettings>().ShowScreen();
            });

            GetElement<Button>(Buttons.Logout).onClick.AddListener(OnLogoutClick);

            TextMeshProUGUI bInfo = GetElement<TextMeshProUGUI>(Labels.BuildInfo);
            bInfo.text = string.Format(bInfo.text, $"{EGRVersion.VersionString()} - {EGRVersion.VersionSignature()}");

            m_Backgrounds[0] = GetElement<Image>(Images.Bg);
            m_Backgrounds[1] = GetElement<Image>(Images.TopBg);
            m_Backgrounds[2] = GetElement<Image>(Images.BotBg);
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

                gfx.DOColor(gfx.color, TweenMonitored(0.4f + i * 0.03f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color);

                bool isBg = false;
                foreach (Image bg in m_Backgrounds) {
                    if (gfx == bg) {
                        isBg = true;
                        break;
                    }
                }

                //text is a child of button which is an existing gfx, BETTER check for button comp in parent later
                if (isBg || gfx.ParentHasGfx()) {
                    continue;
                }

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue(-1f * gfx.transform.position)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color | EGRGfxState.Position);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.2f + i * 0.03f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        void OnLogoutClick() {
            EGRPopupConfirmation popup = Manager.GetPopup<EGRPopupConfirmation>();
            popup.SetYesButtonText(Localize(EGRLanguageData.LOGOUT));
            popup.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
            popup.ShowPopup(Localize(EGRLanguageData.ACCOUNT_INFO), Localize(EGRLanguageData.ARE_YOU_SURE_THAT_YOU_WANT_TO_LOGOUT_OF_EGR_), OnLogoutClosed, null);
        }

        void OnLogoutClosed(EGRPopup popup, EGRPopupResult res) {
            if (res == EGRPopupResult.YES) {
                Client.Logout();
            }
        }
    }
}
