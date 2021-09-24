using DG.Tweening;
using MRK.Networking;
using MRK.Networking.Packets;
using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;
using static MRK.UI.EGRUI_Main.EGRScreen_Register;

namespace MRK.UI {
    public class EGRScreenRegister : EGRScreen {
        const string EMAIL_REGEX = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";

        TMP_InputField m_FullName;
        TMP_InputField m_Email;
        TMP_InputField m_Password;
        string m_PasswordRef;
        string m_EmailRef;
        string[] m_NamesRef;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0x00000000;

        protected override void OnScreenInit() {
            m_FullName = GetElement<TMP_InputField>(Textboxes.Nm);
            m_Email = GetElement<TMP_InputField>(Textboxes.Em);
            m_Password = GetElement<TMP_InputField>(Textboxes.Pass);

            GetElement<Button>(Buttons.Register).onClick.AddListener(OnRegisterClick);
            GetElement<Button>(Buttons.SignIn).onClick.AddListener(OnLoginClick);
        }

        protected override void OnScreenShow() {
            GetElement<Image>(Images.Bg).gameObject.SetActive(false);

            //reset preview items
            m_FullName.text = "";
            m_Email.text = "";
            m_Password.text = "";
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShow();

            if (m_LastGraphicsBuf == null)
                m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            PushGfxState(EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, 0.6f + i * 0.03f + (i > 10 ? 0.3f : 0f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            //m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                m_LastGraphicsBuf[i].DOColor(Color.clear, 0.3f + i * 0.03f + (i > 10 ? 0.1f : 0f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        bool GetError(out string info, out string[] names, out string email, out string pwd) {
            info = "";
            names = null;
            email = "";
            pwd = "";

            string nameStr = m_FullName.text.Trim(' ', '\n', '\t', '\r');

            if (string.IsNullOrEmpty(nameStr) || string.IsNullOrWhiteSpace(nameStr)) {
                info = "Name cannot be empty";
                return true;
            }

            string[] _names = nameStr.Split(' ');
            if (_names.Length <= 1) {
                info = "Name is incomplete";
                return true;
            }

            names = new string[2];
            names[0] = _names[0];

            string[] otherNames = new string[_names.Length - 1];
            Array.Copy(_names, 1, otherNames, 0, otherNames.Length);
            names[1] = string.Join(" ", otherNames);

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
            string info;

            if (GetError(out info, out m_NamesRef, out m_EmailRef, out m_PasswordRef)) {
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), info.ToUpper(), null, this);
                return;
            }

            //confirm pwd
            //Manager.GetPopup(EGRUI_Main.EGRPopup_ConfirmPwd.SCREEN_NAME).ShowPopup(Localize(EGRLanguageData.REGISTER), null, OnConfirmPassword, this);

            //USE INPUT TEXT INSTEAD
            EGRPopupInputText popup = ScreenManager.GetPopup<EGRPopupInputText>();
            popup.SetPassword();
            popup.ShowPopup(Localize(EGRLanguageData.REGISTER), Localize(EGRLanguageData.ENTER_YOUR_PASSWORD_AGAIN), OnConfirmPassword, this);
        }

        void OnConfirmPassword(EGRPopup popup, EGRPopupResult result) {
            if (((EGRPopupInputText)popup).Input != m_PasswordRef) {
                //incorrect pwd
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), Localize(EGRLanguageData.PASSWORDS_MISMATCH), null, this);
                return;
            }

            if (!NetworkingClient.MainNetworkExternal.RegisterAccount(string.Join(" ", m_NamesRef), m_EmailRef, m_PasswordRef, OnNetRegister)) {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, this);
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(Localize(EGRLanguageData.REGISTER), Localize(EGRLanguageData.REGISTERING___), null, this);
        }

        void OnNetRegister(PacketInStandardResponse response) {
            MessageBox.HideScreen(() => {
                if (response.Response != EGRStandardResponse.SUCCESS) {
                    MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0___1__),
                        EGRConstants.EGR_ERROR_RESPONSE, (int)response.Response), null, this);

                    return;
                }

                MessageBox.ShowPopup(Localize(EGRLanguageData.REGISTER), Localize(EGRLanguageData.SUCCESS), (x, y) => OnLoginClick(), null);
            }, 1.1f);
        }

        void OnLoginClick() {
            HideScreen();
            ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Login.SCREEN_NAME).ShowScreen();
        }
    }
}
