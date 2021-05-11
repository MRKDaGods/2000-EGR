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
        Image m_Background;
        Transform m_Layout;
        TextMeshProUGUI m_Name;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0xFF000000;

        protected override void OnScreenInit() {
            GetElement<Button>(Buttons.TopLeftMenu).onClick.AddListener(() => {
                HideScreen(() => Manager.GetScreen<EGRScreenMenu>().ShowScreen(), 0.1f, false);
            });

            GetElement<Button>("Layout/Account").onClick.AddListener(() => {
                Manager.GetScreen<EGRScreenOptionsAccInfo>().ShowScreen();
            });

            //GetElement<Button>(Buttons.Settings).onClick.AddListener(() => {
            //    Manager.GetScreen<EGRScreenMenuSettings>().ShowScreen();
            //});

            GetElement<Button>("Layout/Logout").onClick.AddListener(OnLogoutClick);

            TextMeshProUGUI bInfo = GetElement<TextMeshProUGUI>(Labels.BuildInfo);
            bInfo.text = string.Format(bInfo.text, $"{EGRVersion.VersionString()} - {EGRVersion.VersionSignature()}");

            m_Background = GetElement<Image>(Images.Bg);
            m_Layout = GetTransform("Layout");
            m_Name = GetElement<TextMeshProUGUI>("Layout/Profile/Name");
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim(); // no extensive workload down there

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_Layout);
            m_Layout.GetComponent<VerticalLayoutGroup>().enabled = false;

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            PushGfxState(EGRGfxState.LocalPosition | EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.5f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color);

                if (gfx == m_Background || gfx.ParentHasGfx()) {
                    continue;
                }

                gfx.transform.DOLocalMoveX(gfx.transform.localPosition.x, TweenMonitored(0.3f + i * 0.03f))
                    .ChangeStartValue(-1f * gfx.transform.position)
                    .SetEase(Ease.OutSine);

                SetGfxStateMask(gfx, EGRGfxState.Color | EGRGfxState.LocalPosition);
            }

            //m_Layout.DOMoveX(m_Layout.position.x, TweenMonitored(0.4f))
            //        .ChangeStartValue(-1f * m_Layout.position)
            //        .SetEase(Ease.OutSine);
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.2f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        protected override void OnScreenShow() {
            m_Name.text = EGRLocalUser.Instance.FullName;
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
