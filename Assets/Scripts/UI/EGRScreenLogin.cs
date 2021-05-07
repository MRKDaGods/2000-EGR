﻿using DG.Tweening;
using MRK.Networking;
using MRK.Networking.Packets;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;
using static MRK.UI.EGRUI_Main.EGRScreen_Login;

namespace MRK.UI {
    public class EGRScreenLogin : EGRScreen {
        const string EMAIL_REGEX = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";

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

            m_RememberMe.isOn = PlayerPrefs.GetInt(EGRConstants.EGR_LOCALPREFS_REMEMBERME, 0) == 1 ? true : false;
            if (m_RememberMe.isOn) {
                m_Email.text = PlayerPrefs.GetString(EGRConstants.EGR_LOCALPREFS_USERNAME, "");
                m_Password.text = PlayerPrefs.GetString(EGRConstants.EGR_LOCALPREFS_PASSWORD, "");

                //login with token instead uh?
                LoginWithToken();
            }
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            if (m_SkipAnims)
                return;

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

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                m_LastGraphicsBuf[i].DOColor(Color.clear, TweenMonitored(0.3f + i * 0.03f + (i > 10 ? 0.1f : 0f)))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        bool GetError(out string info, out string email, out string pwd) {
            info = "";
            pwd = "";

            email = m_Email.text.Trim(' ', '\n', '\t', '\r');
            if (string.IsNullOrEmpty(email) || string.IsNullOrWhiteSpace(email)) {
                info = "Email cannot be empty";
                return true;
            }

            if (!Regex.IsMatch(email, EMAIL_REGEX, RegexOptions.IgnoreCase)) {
                info = "Email is invalid";
                return true;
            }

            pwd = m_Password.text.Trim(' ', '\n', '\t', '\r');
            if (string.IsNullOrEmpty(pwd) || string.IsNullOrWhiteSpace(pwd)) {
                info = "Password cannot be empty";
                return true;
            }

            if (pwd.Length < 8) {
                info = "Password must consist of atleast 8 characters";
                return true;
            }

            return false;
        }

        void OnRegisterClick() {
            HideScreen(() => Manager.GetScreen("Register").ShowScreen());
        }

        void OnLoginClick() {
            if (m_Email.text == "x") {
                Client.RegisterDevSettings<EGRDevSettingsServerInfo>();
                MessageBox.ShowPopup("EGR DEV", "Enabled EGRDevSettingsServerInfo", null, this);
                return;
            }

            string info, email, pwd;

            if (GetError(out info, out email, out pwd)) {
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), info.ToUpper(), null, this);
                return;
            }

            if (!Client.NetLoginAccount(email, pwd, OnNetLogin)) {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, this);
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(Localize(EGRLanguageData.LOGIN), Localize(EGRLanguageData.LOGGING_IN___), null, this);

            PlayerPrefs.SetInt(EGRConstants.EGR_LOCALPREFS_REMEMBERME, m_RememberMe.isOn ? 1 : 0);
            if (m_RememberMe.isOn) {
                PlayerPrefs.SetString(EGRConstants.EGR_LOCALPREFS_USERNAME, m_Email.text);
                PlayerPrefs.SetString(EGRConstants.EGR_LOCALPREFS_PASSWORD, m_Password.text);
            }
        }

        void LoginWithToken() {
            /*
                mxr 2
                mxv 200 m0
                mxv token.Length m1
                mxcmp
            */
            string token = PlayerPrefs.GetString(EGRConstants.EGR_LOCALPREFS_TOKEN, "");

            string shellcode = "mxr 2 \n" +
                               "mxv 200 m0 \n" +
                               $"mxv {token.Length} m1 \n" +
                               "mxcmp m0 m1";

#if UNITY_EDITOR
            bool res = MRKAssembly.Execute(shellcode).m2._1;
            Debug.Log($"shellcode res={res}");

            if (!res) {
#else
            if (token.Length != 200) {
#endif
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_INVALID_TOKEN), null, this);
                return;
            }

            if (!Client.NetLoginAccountToken(token, OnNetLogin)) {
                //find local one?
                EGRProxyUser user = JsonUtility.FromJson<EGRProxyUser>(PlayerPrefs.GetString(EGRConstants.EGR_LOCALPREFS_LOCALUSER));
                if (user.Token != token) {
                    MessageBox.HideScreen();
                    MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, this);
                }

                m_SkipAnims = true;
                StartCoroutine(LoginWithLocalUser(user));
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(Localize(EGRLanguageData.LOGIN), Localize(EGRLanguageData.LOGGING_IN___), null, this);
        }

        IEnumerator LoginWithLocalUser(EGRProxyUser user) {
            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(Localize(EGRLanguageData.LOGIN), Localize(EGRLanguageData.LOGGING_IN_OFFLINE___), null, this);

            yield return new WaitForSeconds(1f);
            OnNetLogin(new PacketInLoginAccount(user));
        }

        void OnNetLogin(PacketInLoginAccount response) {
            MessageBox.HideScreen(() => {
                if (response.Response != EGRStandardResponse.SUCCESS) {
                    MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0___1__),
                        EGRConstants.EGR_ERROR_RESPONSE, (int)response.Response), null, this);

                    return;
                }

                EGRLocalUser.Initialize(response.ProxyUser);
                Debug.Log(EGRLocalUser.Instance.ToString());

                if (m_RememberMe.isOn) {
                    PlayerPrefs.SetString(EGRConstants.EGR_LOCALPREFS_TOKEN, response.ProxyUser.Token);
                }

                HideScreen(() => {
                    Manager.GetScreen(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ShowScreen();
                }, 0.1f, true);

                //better + cooler transition?
                //MessageBox.ShowPopup(Localize(EGRLanguageData.LOGIN), string.Format(Localize(EGRLanguageData.WELCOME__0_), EGRLocalUser.Instance.FirstName), OnWelcomeClose, null);

                //EGRUITextRenderer.Render(string.Format(Localize(EGRLanguageData.WELCOME__0_), $"\n<color=#1D9AD2>{EGRLocalUser.Instance.FirstName}</color>\nTO EGR"), 1.5f, 0);
                //EGRUITextRenderer.Modify(x => x.fontSize = Mathf.RoundToInt(x.fontSize / 1.1f));

                //EGRFadeManager.Fade(1f, 5f, () => {
                //    HideScreen(() => {
                //        Manager.GetScreen(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ShowScreen();
                //    }, 0.1f, true);
                //});

            }, 1.1f);
        }

        void OnWelcomeClose(EGRPopup popup, EGRPopupResult result) {
            HideScreen(() => {
                Manager.GetScreen(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ShowScreen();
            }, 0.1f, true);
        }

        void OnLoginDevClick() {
            if (!Client.NetLoginAccountDev(OnNetLogin)) {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, this);
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(Localize(EGRLanguageData.LOGIN), Localize(EGRLanguageData.LOGGING_IN___), null, this);

            PlayerPrefs.SetInt(EGRConstants.EGR_LOCALPREFS_REMEMBERME, m_RememberMe.isOn ? 1 : 0);
        }
    }
}
