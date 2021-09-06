using DG.Tweening;
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

            //m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

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

            if (!EGRUtils.ValidateEmail(email)) {
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
            HideScreen(() => Manager.GetScreen<EGRScreenRegister>().ShowScreen());
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

            if (!NetworkingClient.MainNetworkExternal.LoginAccount(email, pwd, OnNetLogin)) {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, this);
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(Localize(EGRLanguageData.LOGIN), Localize(EGRLanguageData.LOGGING_IN___), null, this);

            MRKPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_REMEMBERME, m_RememberMe.isOn);
            if (m_RememberMe.isOn) {
                MRKPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_USERNAME, m_Email.text);
                MRKPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_PASSWORD, m_Password.text);
            }

            MRKPlayerPrefs.Save();
        }

        void LoginWithToken() {
            /*
                mxr 2
                mxv 200 m0
                mxv token.Length m1
                mxcmp
            */
            string token = MRKPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_TOKEN, "");

            string shellcode = "mxr 2 \n" +
                               "mxv 200 m0 \n" +
                               $"mxv {token.Length} m1 \n" +
                               "mxcmp m0 m1";

#if UNITY_EDITOR && MRK_SUPPORTS_ASSEMBLY
            bool res = MRKAssembly.Execute(shellcode).m2._1;
            Debug.Log($"shellcode res={res}");

            if (!res) {
#else
            if (token.Length != 200) {
#endif
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_INVALID_TOKEN), null, this);
                return;
            }

            if (!NetworkingClient.MainNetworkExternal.LoginAccountToken(token, OnNetLogin)) {
                //find local one?
                EGRProxyUser user = JsonUtility.FromJson<EGRProxyUser>(MRKPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_LOCALUSER, ""));
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

            yield return new WaitForSeconds(0.5f);
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
                EGRLocalUser.PasswordHash = response.PasswordHash;
                Debug.Log(EGRLocalUser.Instance.ToString());

                if (m_RememberMe.isOn) {
                    MRKPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_TOKEN, response.ProxyUser.Token);
                    MRKPlayerPrefs.Save();
                }

                HideScreen(() => {
                    Manager.GetScreen<EGRScreenMain>().ShowScreen();
                }, 0.1f, true);

            }, 1.1f);
        }

        void OnLoginDevClick() {
            if (!NetworkingClient.MainNetworkExternal.LoginAccountDev(OnNetLogin)) {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, this);
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(Localize(EGRLanguageData.LOGIN), Localize(EGRLanguageData.LOGGING_IN___), null, this);

            MRKPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_REMEMBERME, m_RememberMe.isOn);
            MRKPlayerPrefs.Save();
        }
    }
}
