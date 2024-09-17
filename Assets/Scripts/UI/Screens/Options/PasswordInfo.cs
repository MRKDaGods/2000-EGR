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
    public class PasswordInfo : AnimatedLayout, ISupportsBackKey
    {
        private TMP_InputField _currentPassword;
        private TMP_InputField _newPassword;
        private TMP_InputField _confirmPassword;
        private Toggle _logoutAll;
        private Button _save;
        private string _passBuf;

        private bool m_EnableSave
        {
            get
            {
                return _newPassword.text != ""
                    && _confirmPassword.text != ""
                    && _newPassword.text == _confirmPassword.text;
            }
        }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);

            _currentPassword = GetElement<TMP_InputField>("Layout/CurrentPasswordTb");
            _newPassword = GetElement<TMP_InputField>("Layout/PasswordTb");
            _confirmPassword = GetElement<TMP_InputField>("Layout/ConfPasswordTb");

            _newPassword.onValueChanged.AddListener(OnTextChanged);
            _confirmPassword.onValueChanged.AddListener(OnTextChanged);

            _logoutAll = GetElement<Toggle>("Layout/LogoutAll/Toggle");

            _save = GetElement<Button>("Layout/Save");
            _save.onClick.AddListener(OnSaveClick);
        }

        protected override void OnScreenShow()
        {
            _currentPassword.text = "";
            _newPassword.text = "";
            _confirmPassword.text = "";

            _logoutAll.isOn = false;
            _save.interactable = false;
        }

        private void OnSaveClick()
        {
            if (Crypto.Hash(_currentPassword.text) != LocalUser.PasswordHash)
            {
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), Localize(LanguageData.INCORRECT_PASSWORD), null, this);
                return;
            }

            if (_newPassword.text != _confirmPassword.text)
            {
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), Localize(LanguageData.PASSWORDS_MISMATCH), null, this);
                return;
            }

            _passBuf = _newPassword.text;
            if (!EGRUtils.ValidatePassword(ref _passBuf))
            {
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), Localize(LanguageData.INVALID_PASSWORD), null, this);
                return;
            }

            if (!NetworkingClient.MainNetworkExternal.UpdateAccountPassword(_passBuf, _logoutAll.isOn, OnNetSave))
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

                LocalUser.PasswordHash = Crypto.Hash(_passBuf);

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
                popup.ShowPopup(
                    Localize(LanguageData.ACCOUNT_INFO),
                    Localize(LanguageData.YOU_HAVE_UNSAVED_CHANGES_nWOULD_YOU_LIKE_TO_SAVE_YOUR_CHANGES_),
                    OnUnsavedClose,
                    null
                );
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
