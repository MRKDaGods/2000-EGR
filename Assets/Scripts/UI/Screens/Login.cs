using DG.Tweening;
using MRK.Authentication;
using MRK.Cryptography;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_Login;

namespace MRK.UI
{
    public class Login : Screen
    {
        private TMP_InputField _email;
        private TMP_InputField _password;
        private Toggle _rememberMe;
        private bool _skipAnims;

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
                return 0x00000000;
            }
        }

        protected override void OnScreenInit()
        {
            _email = GetElement<TMP_InputField>(Textboxes.Em);
            _password = GetElement<TMP_InputField>(Textboxes.Pass);
            _rememberMe = GetElement<Toggle>(Toggles.zRemember);

            GetElement<Button>(Buttons.Register).onClick.AddListener(OnRegisterClick);
            GetElement<Button>(Buttons.SignIn).onClick.AddListener(OnLoginClick);
            GetElement<Button>(Buttons.Dev).onClick.AddListener(OnLoginDevClick);

            GetElement<Button>("txtForgotPwd").onClick.AddListener(Client.AuthenticationManager.BuiltInLogin);

            //clear our preview strs
            _email.text = "";
            _password.text = "";
        }

        protected override void OnScreenShow()
        {
            _skipAnims = false;

            GetElement<Image>(Images.Bg).gameObject.SetActive(false);
            Client.SetMapMode(EGRMapMode.General);

            _rememberMe.isOn = CryptoPlayerPrefs.Get<bool>(EGRConstants.EGR_LOCALPREFS_REMEMBERME, false);
            if (_rememberMe.isOn)
            {
                _email.text = CryptoPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_USERNAME, "");
                _password.text = CryptoPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_PASSWORD, "");

                //login with token instead uh?
                LoginWithToken();
            }
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            if (_skipAnims)
                return;

            //we know nothing is going to change here
            if (_lastGraphicsBuf == null)
                _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            PushGfxState(GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.6f + i * 0.03f + (i > 10 ? 0.3f : 0f)))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            if (_skipAnims)
                return false;

            SetTweenCount(_lastGraphicsBuf.Length);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                _lastGraphicsBuf[i].DOColor(Color.clear, TweenMonitored(0.3f + i * 0.03f + (i > 10 ? 0.1f : 0f)))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        private void OnRegisterClick()
        {
            HideScreen(() => ScreenManager.GetScreen<Register>().ShowScreen());
        }

        private void OnLoginClick()
        {
            if (_email.text == "x")
            {
                Client.RegisterDevSettings<DevSettingsServerInfo>();
                Client.RegisterDevSettings<DevSettingsUsersInfo>();
                MessageBox.ShowPopup("EGR DEV", "Enabled EGRDevSettings", null, this);
                return;
            }

            AuthenticationData data = new AuthenticationData
            {
                Type = AuthenticationType.Default,
                Reserved0 = _email.text,
                Reserved1 = _password.text,
                Reserved3 = _rememberMe.isOn
            };

            Client.AuthenticationManager.Login(ref data);
        }

        private void LoginWithToken()
        {
            string token = CryptoPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_TOKEN, "");
            AuthenticationData data = new AuthenticationData
            {
                Type = AuthenticationType.Token,
                Reserved0 = token,
                Reserved3 = _rememberMe.isOn
            };

            Client.AuthenticationManager.Login(ref data);
            _skipAnims = data.Reserved4;
        }

        private void OnLoginDevClick()
        {
            AuthenticationData data = new AuthenticationData
            {
                Type = AuthenticationType.Device,
                Reserved3 = _rememberMe.isOn
            };

            Client.AuthenticationManager.Login(ref data);
        }
    }
}
