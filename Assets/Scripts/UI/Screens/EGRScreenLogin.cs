using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_Login;

namespace MRK.UI {
    public class EGRScreenLogin : EGRScreen {
        TMP_InputField m_Email;
        TMP_InputField m_Password;
        Toggle m_RememberMe;
        bool m_SkipAnims;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0x00000000;

        protected override void OnScreenInit() {
            m_Email = GetElement<TMP_InputField>(Textboxes.Em);
            m_Password = GetElement<TMP_InputField>(Textboxes.Pass);
            m_RememberMe = GetElement<Toggle>(Toggles.zRemember);

            GetElement<Button>(Buttons.Register).onClick.AddListener(OnRegisterClick);
            GetElement<Button>(Buttons.SignIn).onClick.AddListener(OnLoginClick);
            GetElement<Button>(Buttons.Dev).onClick.AddListener(OnLoginDevClick);

            //clear our preview strs
            m_Email.text = "";
            m_Password.text = "";
        }

        protected override void OnScreenShow() {
            m_SkipAnims = false;

            GetElement<Image>(Images.Bg).gameObject.SetActive(false);
            Client.SetMapMode(EGRMapMode.General);

            m_RememberMe.isOn = MRKPlayerPrefs.Get<bool>(EGRConstants.EGR_LOCALPREFS_REMEMBERME, false);
            if (m_RememberMe.isOn) {
                m_Email.text = MRKPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_USERNAME, "");
                m_Password.text = MRKPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_PASSWORD, "");

                //login with token instead uh?
                LoginWithToken();
            }
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            if (m_SkipAnims)
                return;

            //we know nothing is going to change here
            if (m_LastGraphicsBuf == null)
                m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            PushGfxState(EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.6f + i * 0.03f + (i > 10 ? 0.3f : 0f)))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            if (m_SkipAnims)
                return false;

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                m_LastGraphicsBuf[i].DOColor(Color.clear, TweenMonitored(0.3f + i * 0.03f + (i > 10 ? 0.1f : 0f)))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        void OnRegisterClick() {
            HideScreen(() => Manager.GetScreen<EGRScreenRegister>().ShowScreen());
        }

        void OnLoginClick() {
            if (m_Email.text == "x") {
                Client.RegisterDevSettings<EGRDevSettingsServerInfo>();
                Client.RegisterDevSettings<EGRDevSettingsUsersInfo>();
                MessageBox.ShowPopup("EGR DEV", "Enabled EGRDevSettings", null, this);
                return;
            }

            EGRAuthenticationData data = new EGRAuthenticationData {
                Type = EGRAuthenticationType.Default,
                Reserved0 = m_Email.text,
                Reserved1 = m_Password.text,
                Reserved3 = m_RememberMe.isOn
            };

            Client.AuthenticationManager.Login(ref data);
        }

        void LoginWithToken() {
            string token = MRKPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_TOKEN, "");
            EGRAuthenticationData data = new EGRAuthenticationData {
                Type = EGRAuthenticationType.Token,
                Reserved0 = token,
                Reserved3 = m_RememberMe.isOn
            };

            Client.AuthenticationManager.Login(ref data);
            m_SkipAnims = data.Reserved4;
        }

        void OnLoginDevClick() {
            EGRAuthenticationData data = new EGRAuthenticationData {
                Type = EGRAuthenticationType.Device,
                Reserved3 = m_RememberMe.isOn
            };

            Client.AuthenticationManager.Login(ref data);
        }
    }
}
