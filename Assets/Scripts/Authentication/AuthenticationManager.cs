using MRK.Networking;
using MRK.Networking.Packets;
using MRK.UI;
using System.Collections;
using UnityEngine;
using static MRK.LanguageManager;

namespace MRK.Authentication
{
    public class AuthenticationManager : BaseBehaviourPlain
    {
        private readonly MRKSelfContainedPtr<Login> _loginScreen;
        private bool _shouldRememberUser;

        private MessageBox MessageBox
        {
            get
            {
                return ScreenManager.MessageBox;
            }
        }

        public AuthenticationManager()
        {
            _loginScreen = new MRKSelfContainedPtr<Login>(() => ScreenManager.GetScreen<Login>());
        }

        public void Login(ref AuthenticationData data)
        {
            _shouldRememberUser = data.Reserved3;

            switch (data.Type)
            {
                case AuthenticationType.Default:
                    LoginDefault(ref data);
                    break;

                case AuthenticationType.Device:
                    LoginDevice(ref data);
                    break;

                case AuthenticationType.Token:
                    LoginToken(ref data);
                    break;
            }
        }

        public void BuiltInLogin()
        {
            string builtInJson = Resources.Load<TextAsset>("Login/BuiltInUser").text;
            ProxyUser builtIn = JsonUtility.FromJson<ProxyUser>(builtInJson);
            LoginProxyUser(builtIn);
        }

        private void LoginDefault(ref AuthenticationData data)
        {
            if (GetError(ref data))
            {
                MessageBox.ShowPopup(
                    Localize(LanguageData.ERROR),
                    data.Reserved2,
                    null,
                    _loginScreen
                );

                return;
            }

            if (!NetworkingClient.MainNetworkExternal.LoginAccount(data.Reserved0, data.Reserved1, OnNetLogin))
            {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(
                    Localize(LanguageData.ERROR),
                    string.Format(Localize(LanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED),
                    null,
                    _loginScreen
                );

                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(
                Localize(LanguageData.LOGIN),
                Localize(LanguageData.LOGGING_IN___),
                null,
                _loginScreen
            );

            CryptoPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_REMEMBERME, data.Reserved3);
            if (data.Reserved3)
            {
                CryptoPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_USERNAME, data.Reserved0);
                CryptoPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_PASSWORD, data.Reserved1);
            }

            CryptoPlayerPrefs.Save();
        }

        private void LoginDevice(ref AuthenticationData data)
        {
            if (!NetworkingClient.MainNetworkExternal.LoginAccountDev(OnNetLogin))
            {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(
                    Localize(LanguageData.ERROR),
                    string.Format(Localize(LanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED),
                    null,
                    _loginScreen);

                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(
                Localize(LanguageData.LOGIN),
                Localize(LanguageData.LOGGING_IN___),
                null,
                _loginScreen);

            CryptoPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_REMEMBERME, data.Reserved3);
            CryptoPlayerPrefs.Save();
        }

        private void LoginToken(ref AuthenticationData data)
        {
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
            if (token.Length != EGRConstants.EGR_AUTHENTICATION_TOKEN_LENGTH)
            {
#endif
                MessageBox.ShowPopup(
                    Localize(LanguageData.ERROR),
                    string.Format(Localize(LanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_INVALID_TOKEN),
                    null,
                    _loginScreen);

                return;
            }

            if (!NetworkingClient.MainNetworkExternal.LoginAccountToken(token, OnNetLogin))
            {
                //find local one?
                ProxyUser user = JsonUtility.FromJson<ProxyUser>(CryptoPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_LOCALUSER, ""));
                if (user.Token != token)
                {
                    MessageBox.HideScreen();
                    MessageBox.ShowPopup(
                        Localize(LanguageData.ERROR),
                        string.Format(Localize(LanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED),
                        null,
                        _loginScreen);
                }

                //SKIP ANIM!!
                data.Reserved4 = true;
                LoginProxyUser(user);
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(
                Localize(LanguageData.LOGIN),
                Localize(LanguageData.LOGGING_IN___),
                null,
                _loginScreen);
        }

        private void LoginProxyUser(ProxyUser user)
        {
            Client.Runnable.Run(LoginWithLocalUser(user));
        }

        private IEnumerator LoginWithLocalUser(ProxyUser user)
        {
            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(
                Localize(LanguageData.LOGIN),
                Localize(LanguageData.LOGGING_IN_OFFLINE___),
                null,
                _loginScreen);

            yield return new WaitForSeconds(0.5f);
            OnNetLogin(new PacketInLoginAccount(user));
        }

        private void OnNetLogin(PacketInLoginAccount response)
        {
            MessageBox.HideScreen(() => {
                if (response.Response != EGRStandardResponse.SUCCESS)
                {
                    MessageBox.ShowPopup(
                        Localize(LanguageData.ERROR),
                        string.Format(Localize(LanguageData.FAILED__EGR__0___1__), EGRConstants.EGR_ERROR_RESPONSE, (int)response.Response),
                        null,
                        _loginScreen);

                    return;
                }

                LocalUser.Initialize(response.ProxyUser);
                LocalUser.PasswordHash = response.PasswordHash;
                Debug.Log(LocalUser.Instance.ToString());

                if (_shouldRememberUser)
                {
                    CryptoPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_TOKEN, response.ProxyUser.Token);
                    CryptoPlayerPrefs.Save();
                }

                _loginScreen.Value.HideScreen(() => {
                    ScreenManager.MainScreen.ShowScreen();
                }, 0.1f, true);

            }, 1.1f);
        }

        private bool GetError(ref AuthenticationData data)
        {
            data.Reserved0 = data.Reserved0.Trim(' ', '\n', '\t', '\r');
            if (string.IsNullOrEmpty(data.Reserved0) || string.IsNullOrWhiteSpace(data.Reserved0))
            {
                data.Reserved2 = Localize(LanguageData.Email_cannot_be_empty);
                return true;
            }

            if (!EGRUtils.ValidateEmail(data.Reserved0))
            {
                data.Reserved2 = Localize(LanguageData.Email_is_invalid);
                return true;
            }

            data.Reserved1 = data.Reserved1.Trim(' ', '\n', '\t', '\r');
            if (string.IsNullOrEmpty(data.Reserved1) || string.IsNullOrWhiteSpace(data.Reserved1))
            {
                data.Reserved2 = Localize(LanguageData.Password_cannot_be_empty);
                return true;
            }

            if (data.Reserved1.Length < 8)
            {
                data.Reserved2 = Localize(LanguageData.Password_must_consist_of_atleast_8_characters);
                return true;
            }

            return false;
        }
    }
}
