using MRK.Networking;
using MRK.Networking.Packets;
using MRK.UI;
using System.Collections;
using UnityEngine;
using static MRK.EGRLanguageManager;

namespace MRK {
    public class EGRAuthenticationManager : MRKBehaviourPlain {
        readonly MRKSelfContainedPtr<EGRScreenLogin> m_LoginScreen;
        bool m_ShouldRememberUser;

        EGRPopupMessageBox MessageBox => ScreenManager.MessageBox;

        public EGRAuthenticationManager() {
            m_LoginScreen = new MRKSelfContainedPtr<EGRScreenLogin>(() => ScreenManager.GetScreen<EGRScreenLogin>());
        }

        public void Login(ref EGRAuthenticationData data) {
            m_ShouldRememberUser = data.Reserved3;

            switch (data.Type) {
                case EGRAuthenticationType.Default:
                    LoginDefault(ref data);
                    break;

                case EGRAuthenticationType.Device:
                    LoginDevice(ref data);
                    break;

                case EGRAuthenticationType.Token:
                    LoginToken(ref data);
                    break;
            }
        }

        public void BuiltInLogin() {
            string builtInJson = Resources.Load<TextAsset>("Login/BuiltInUser").text;
            EGRProxyUser builtIn = JsonUtility.FromJson<EGRProxyUser>(builtInJson);
            LoginProxyUser(builtIn);
        }

        void LoginDefault(ref EGRAuthenticationData data) {
            if (GetError(ref data)) {
                MessageBox.ShowPopup(
                    Localize(EGRLanguageData.ERROR),
                    data.Reserved2,
                    null,
                    m_LoginScreen);

                return;
            }

            if (!NetworkingClient.MainNetworkExternal.LoginAccount(data.Reserved0, data.Reserved1, OnNetLogin)) {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(
                    Localize(EGRLanguageData.ERROR),
                    string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED),
                    null,
                    m_LoginScreen);

                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(
                Localize(EGRLanguageData.LOGIN),
                Localize(EGRLanguageData.LOGGING_IN___),
                null,
                m_LoginScreen);

            MRKPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_REMEMBERME, data.Reserved3);
            if (data.Reserved3) {
                MRKPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_USERNAME, data.Reserved0);
                MRKPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_PASSWORD, data.Reserved1);
            }

            MRKPlayerPrefs.Save();
        }

        void LoginDevice(ref EGRAuthenticationData data) {
            if (!NetworkingClient.MainNetworkExternal.LoginAccountDev(OnNetLogin)) {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(
                    Localize(EGRLanguageData.ERROR),
                    string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED),
                    null,
                    m_LoginScreen);

                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(
                Localize(EGRLanguageData.LOGIN),
                Localize(EGRLanguageData.LOGGING_IN___),
                null,
                m_LoginScreen);

            MRKPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_REMEMBERME, data.Reserved3);
            MRKPlayerPrefs.Save();
        }

        void LoginToken(ref EGRAuthenticationData data) {
            /*
                mxr 2
                mxv 200 m0
                mxv token.Length m1
                mxcmp
            */
            string token = data.Reserved0;

            string shellcode = "mxr 2 \n" +
                               "mxv 200 m0 \n" +
                               $"mxv {token.Length} m1 \n" +
                               "mxcmp m0 m1";

#if MRK_SUPPORTS_ASSEMBLY
            bool res = MRKAssembly.Execute(shellcode).m2._1;
            Debug.Log($"shellcode res={res}");

            if (!res) {
#else
            if (token.Length != EGRConstants.EGR_AUTHENTICATION_TOKEN_LENGTH) {
#endif
                MessageBox.ShowPopup(
                    Localize(EGRLanguageData.ERROR),
                    string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_INVALID_TOKEN),
                    null,
                    m_LoginScreen);

                return;
            }

            if (!NetworkingClient.MainNetworkExternal.LoginAccountToken(token, OnNetLogin)) {
                //find local one?
                EGRProxyUser user = JsonUtility.FromJson<EGRProxyUser>(MRKPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_LOCALUSER, ""));
                if (user.Token != token) {
                    MessageBox.HideScreen();
                    MessageBox.ShowPopup(
                        Localize(EGRLanguageData.ERROR),
                        string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED),
                        null,
                        m_LoginScreen);
                }

                //SKIP ANIM!!
                data.Reserved4 = true;
                LoginProxyUser(user);
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(
                Localize(EGRLanguageData.LOGIN),
                Localize(EGRLanguageData.LOGGING_IN___),
                null,
                m_LoginScreen);
        }

        void LoginProxyUser(EGRProxyUser user) {
            Client.Runnable.Run(LoginWithLocalUser(user));
        }

        IEnumerator LoginWithLocalUser(EGRProxyUser user) {
            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(
                Localize(EGRLanguageData.LOGIN),
                Localize(EGRLanguageData.LOGGING_IN_OFFLINE___),
                null,
                m_LoginScreen);

            yield return new WaitForSeconds(0.5f);
            OnNetLogin(new PacketInLoginAccount(user));
        }

        void OnNetLogin(PacketInLoginAccount response) {
            MessageBox.HideScreen(() => {
                if (response.Response != EGRStandardResponse.SUCCESS) {
                    MessageBox.ShowPopup(
                        Localize(EGRLanguageData.ERROR),
                        string.Format(Localize(EGRLanguageData.FAILED__EGR__0___1__), EGRConstants.EGR_ERROR_RESPONSE, (int)response.Response),
                        null,
                        m_LoginScreen);

                    return;
                }

                EGRLocalUser.Initialize(response.ProxyUser);
                EGRLocalUser.PasswordHash = response.PasswordHash;
                Debug.Log(EGRLocalUser.Instance.ToString());

                if (m_ShouldRememberUser) {
                    MRKPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_TOKEN, response.ProxyUser.Token);
                    MRKPlayerPrefs.Save();
                }

                m_LoginScreen.Value.HideScreen(() => {
                    ScreenManager.MainScreen.ShowScreen();
                }, 0.1f, true);

            }, 1.1f);
        }

        bool GetError(ref EGRAuthenticationData data) {
            data.Reserved0 = data.Reserved0.Trim(' ', '\n', '\t', '\r');
            if (string.IsNullOrEmpty(data.Reserved0) || string.IsNullOrWhiteSpace(data.Reserved0)) {
                data.Reserved2 = Localize(EGRLanguageData.Email_cannot_be_empty);
                return true;
            }

            if (!EGRUtils.ValidateEmail(data.Reserved0)) {
                data.Reserved2 = Localize(EGRLanguageData.Email_is_invalid);
                return true;
            }

            data.Reserved1 = data.Reserved1.Trim(' ', '\n', '\t', '\r');
            if (string.IsNullOrEmpty(data.Reserved1) || string.IsNullOrWhiteSpace(data.Reserved1)) {
                data.Reserved2 = Localize(EGRLanguageData.Password_cannot_be_empty);
                return true;
            }

            if (data.Reserved1.Length < 8) {
                data.Reserved2 = Localize(EGRLanguageData.Password_must_consist_of_atleast_8_characters);
                return true;
            }

            return false;
        }
    }
}
