using DG.Tweening;
using MRK.Localization;
using MRK.Networking;
using MRK.Networking.Packets;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.Localization.LanguageManager;
using static MRK.UI.EGRUI_Main.EGRScreen_Register;

namespace MRK.UI
{
    public class Register : Screen
    {
        private TMP_InputField _fullName;
        private TMP_InputField _email;
        private TMP_InputField _password;
        private string _passwordRef;
        private string _emailRef;
        private string[] _namesRef;

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
            _fullName = GetElement<TMP_InputField>(Textboxes.Nm);
            _email = GetElement<TMP_InputField>(Textboxes.Em);
            _password = GetElement<TMP_InputField>(Textboxes.Pass);

            GetElement<Button>(Buttons.Register).onClick.AddListener(OnRegisterClick);
            GetElement<Button>(Buttons.SignIn).onClick.AddListener(OnLoginClick);
        }

        protected override void OnScreenShow()
        {
            GetElement<Image>(Images.Bg).gameObject.SetActive(false);

            //reset preview items
            _fullName.text = "";
            _email.text = "";
            _password.text = "";
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShow();

            if (_lastGraphicsBuf == null)
                _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            PushGfxState(GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(gfx.color, 0.6f + i * 0.03f + (i > 10 ? 0.3f : 0f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            //m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            SetTweenCount(_lastGraphicsBuf.Length);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                _lastGraphicsBuf[i].DOColor(Color.clear, 0.3f + i * 0.03f + (i > 10 ? 0.1f : 0f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        private bool GetError(out string info, out string[] names, out string email, out string pwd)
        {
            info = "";
            names = null;
            email = "";
            pwd = "";

            string nameStr = _fullName.text.Trim(' ', '\n', '\t', '\r');

            if (string.IsNullOrEmpty(nameStr) || string.IsNullOrWhiteSpace(nameStr))
            {
                info = "Name cannot be empty";
                return true;
            }

            string[] _names = nameStr.Split(' ');
            if (_names.Length <= 1)
            {
                info = "Name is incomplete";
                return true;
            }

            names = new string[2];
            names[0] = _names[0];

            string[] otherNames = new string[_names.Length - 1];
            Array.Copy(_names, 1, otherNames, 0, otherNames.Length);
            names[1] = string.Join(" ", otherNames);

            email = _email.text.Trim(' ', '\n', '\t', '\r');
            if (string.IsNullOrEmpty(email) || string.IsNullOrWhiteSpace(email))
            {
                info = "Email cannot be empty";
                return true;
            }

            if (!EGRUtils.ValidateEmail(email))
            {
                info = "Email is invalid";
                return true;
            }

            pwd = _password.text.Trim(' ', '\n', '\t', '\r');
            if (string.IsNullOrEmpty(pwd) || string.IsNullOrWhiteSpace(pwd))
            {
                info = "Password cannot be empty";
                return true;
            }

            if (pwd.Length < 8)
            {
                info = "Password must consist of atleast 8 characters";
                return true;
            }

            return false;
        }

        private void OnRegisterClick()
        {
            string info;

            if (GetError(out info, out _namesRef, out _emailRef, out _passwordRef))
            {
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), info.ToUpper(), null, this);
                return;
            }

            //confirm pwd
            //Manager.GetPopup(EGRUI_Main.EGRPopup_ConfirmPwd.SCREEN_NAME).ShowPopup(Localize(EGRLanguageData.REGISTER), null, OnConfirmPassword, this);

            //USE INPUT TEXT INSTEAD
            InputText popup = ScreenManager.GetPopup<InputText>();
            popup.SetPassword();
            popup.ShowPopup(Localize(LanguageData.REGISTER), Localize(LanguageData.ENTER_YOUR_PASSWORD_AGAIN), OnConfirmPassword, this);
        }

        private void OnConfirmPassword(Popup popup, PopupResult result)
        {
            if (((InputText)popup).Input != _passwordRef)
            {
                //incorrect pwd
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), Localize(LanguageData.PASSWORDS_MISMATCH), null, this);
                return;
            }

            if (!NetworkingClient.MainNetworkExternal.RegisterAccount(string.Join(" ", _namesRef), _emailRef, _passwordRef, OnNetRegister))
            {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), string.Format(Localize(LanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, this);
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(Localize(LanguageData.REGISTER), Localize(LanguageData.REGISTERING___), null, this);
        }

        private void OnNetRegister(PacketInStandardResponse response)
        {
            MessageBox.HideScreen(() =>
            {
                if (response.Response != EGRStandardResponse.SUCCESS)
                {
                    MessageBox.ShowPopup(Localize(LanguageData.ERROR), string.Format(Localize(LanguageData.FAILED__EGR__0___1__),
                        EGRConstants.EGR_ERROR_RESPONSE, (int)response.Response), null, this);

                    return;
                }

                MessageBox.ShowPopup(Localize(LanguageData.REGISTER), Localize(LanguageData.SUCCESS), (x, y) => OnLoginClick(), null);
            }, 1.1f);
        }

        private void OnLoginClick()
        {
            HideScreen();
            ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Login.SCREEN_NAME).ShowScreen();
        }
    }
}
