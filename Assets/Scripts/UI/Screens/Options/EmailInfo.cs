using MRK.Authentication;
using MRK.Cryptography;
using MRK.Localization;
using MRK.Networking;
using MRK.Networking.Packets;
using TMPro;
using UnityEngine.UI;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public class EmailInfo : AnimatedLayout, ISupportsBackKey
    {
        private TMP_InputField _newEmail;
        private TMP_InputField _confEmail;
        private TMP_InputField _password;
        private Button _save;

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
                return 0xFF000000;
            }
        }

        private LocalUser m_LocalUser
        {
            get
            {
                return LocalUser.Instance;
            }
        }

        private bool m_EnableSave
        {
            get
            {
                return _newEmail.text != ""
                    && _confEmail.text != ""
                    && _newEmail.text == _confEmail.text;
            }
        }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);

            _newEmail = GetElement<TMP_InputField>("Layout/EmailTb");
            _confEmail = GetElement<TMP_InputField>("Layout/ConfEmailTb");
            _password = GetElement<TMP_InputField>("Layout/PasswordTb");

            _newEmail.onValueChanged.AddListener(OnTextChanged);
            _confEmail.onValueChanged.AddListener(OnTextChanged);

            _save = GetElement<Button>("Layout/Save");
            _save.onClick.AddListener(OnSaveClick);
        }

        protected override void OnScreenShow()
        {
            _newEmail.text = "";
            _confEmail.text = "";
            _password.text = "";

            _save.interactable = false;
        }

        private void OnSaveClick()
        {
            if (Crypto.Hash(_password.text) != LocalUser.PasswordHash)
            {
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), Localize(LanguageData.INCORRECT_PASSWORD), null, this);
                return;
            }

            if (_newEmail.text != _confEmail.text)
            {
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), Localize(LanguageData.EMAILS_DO_NOT_MATCH), null, this);
                return;
            }

            if (!EGRUtils.ValidateEmail(_newEmail.text))
            {
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), Localize(LanguageData.INVALID_EMAIL), null, this);
                return;
            }

            if (!NetworkingClient.MainNetworkExternal.UpdateAccountInfo(m_LocalUser.FullName, _newEmail.text, m_LocalUser.Gender, OnNetSave))
            {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), string.Format(Localize(LanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, this);
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(Localize(LanguageData.ACCOUNT_INFO), Localize(LanguageData.SAVING___), null, this);
        }

        private void OnNetSave(PacketInStandardResponse response)
        {
            MessageBox.HideScreen(() =>
            {
                if (response.Response != EGRStandardResponse.SUCCESS)
                {
                    MessageBox.ShowPopup(Localize(LanguageData.ERROR), string.Format(Localize(LanguageData.FAILED__EGR__0___1__),
                        EGRConstants.EGR_ERROR_RESPONSE, (int)response.Response), null, this);

                    return;
                }

                LocalUser.Initialize(new ProxyUser
                {
                    Email = _newEmail.text,
                    FirstName = m_LocalUser.FirstName,
                    LastName = m_LocalUser.LastName,
                    Gender = m_LocalUser.Gender,
                    Token = m_LocalUser.Token
                });

                MessageBox.ShowPopup(Localize(LanguageData.ACCOUNT_INFO), Localize(LanguageData.SAVED), (x, y) =>
                {
                    _save.interactable = false;
                    OnBackClick();
                }, null);
            }, 1.1f);
        }

        private void OnTextChanged(string text)
        {
            _save.interactable = m_EnableSave;
        }

        private void OnBackClick()
        {
            //unsaved changes
            if (_save.interactable)
            {
                Confirmation popup = ScreenManager.GetPopup<Confirmation>();
                popup.SetYesButtonText(Localize(LanguageData.SAVE));
                popup.SetNoButtonText(Localize(LanguageData.CANCEL));
                popup.ShowPopup(Localize(LanguageData.ACCOUNT_INFO), Localize(LanguageData.YOU_HAVE_UNSAVED_CHANGES_nWOULD_YOU_LIKE_TO_SAVE_YOUR_CHANGES_), OnUnsavedClose, null);
            }
            else
                HideScreen();
        }

        private void OnUnsavedClose(Popup popup, PopupResult result)
        {
            if (result == PopupResult.YES)
            {
                OnSaveClick();
                return;
            }

            HideScreen();
        }

        public void OnBackKeyDown()
        {
            OnBackClick();
        }
    }
}
