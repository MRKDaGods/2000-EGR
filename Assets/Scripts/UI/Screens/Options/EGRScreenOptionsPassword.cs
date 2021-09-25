using MRK.Networking;
using MRK.Networking.Packets;
using TMPro;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public class EGRScreenOptionsPassword : EGRScreenAnimatedLayout, IEGRScreenSupportsBackKey {
        TMP_InputField m_CurrentPassword;
        TMP_InputField m_NewPassword;
        TMP_InputField m_ConfirmPassword;
        Toggle m_LogoutAll;
        Button m_Save;
        string m_PassBuf;

        bool m_EnableSave => m_NewPassword.text != "" && m_ConfirmPassword.text != "" && m_NewPassword.text == m_ConfirmPassword.text;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);

            m_CurrentPassword = GetElement<TMP_InputField>("Layout/CurrentPasswordTb");
            m_NewPassword = GetElement<TMP_InputField>("Layout/PasswordTb");
            m_ConfirmPassword = GetElement<TMP_InputField>("Layout/ConfPasswordTb");

            m_NewPassword.onValueChanged.AddListener(OnTextChanged);
            m_ConfirmPassword.onValueChanged.AddListener(OnTextChanged);

            m_LogoutAll = GetElement<Toggle>("Layout/LogoutAll/Toggle");

            m_Save = GetElement<Button>("Layout/Save");
            m_Save.onClick.AddListener(OnSaveClick);
        }

        protected override void OnScreenShow() {
            m_CurrentPassword.text = "";
            m_NewPassword.text = "";
            m_ConfirmPassword.text = "";

            m_LogoutAll.isOn = false;
            m_Save.interactable = false;
        }

        void OnSaveClick() {
            if (MRKCryptography.Hash(m_CurrentPassword.text) != EGRLocalUser.PasswordHash) {
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), Localize(EGRLanguageData.INCORRECT_PASSWORD), null, this);
                return;
            }

            if (m_NewPassword.text != m_ConfirmPassword.text) {
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), Localize(EGRLanguageData.PASSWORDS_MISMATCH), null, this);
                return;
            }

            m_PassBuf = m_NewPassword.text;
            if (!EGRUtils.ValidatePassword(ref m_PassBuf)) {
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), Localize(EGRLanguageData.INVALID_PASSWORD), null, this);
                return;
            }

            if (!NetworkingClient.MainNetworkExternal.UpdateAccountPassword(m_PassBuf, m_LogoutAll.isOn, OnNetSave)) {
                MessageBox.HideScreen();
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0__), EGRConstants.EGR_ERROR_NOTCONNECTED), null, this);
                return;
            }

            MessageBox.ShowButton(false);
            MessageBox.ShowPopup(Localize(EGRLanguageData.ACCOUNT_INFO), Localize(EGRLanguageData.SAVING___), null, this);
        }

        void OnNetSave(PacketInStandardResponse response) {
            MessageBox.HideScreen(() => {
                if (response.Response != EGRStandardResponse.SUCCESS) {
                    MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), string.Format(Localize(EGRLanguageData.FAILED__EGR__0___1__),
                        EGRConstants.EGR_ERROR_RESPONSE, (int)response.Response), null, this);

                    return;
                }

                EGRLocalUser.PasswordHash = MRKCryptography.Hash(m_PassBuf);

                MessageBox.ShowPopup(Localize(EGRLanguageData.ACCOUNT_INFO), Localize(EGRLanguageData.SAVED), (x, y) => {
                    m_Save.interactable = false;
                    OnBackClick();
                }, null);
            }, 1.1f);
        }

        void OnTextChanged(string text) {
            m_Save.interactable = m_EnableSave;
        }

        void OnBackClick() {
            //unsaved changes
            if (m_Save.interactable) {
                EGRPopupConfirmation popup = ScreenManager.GetPopup<EGRPopupConfirmation>();
                popup.SetYesButtonText(Localize(EGRLanguageData.SAVE));
                popup.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
                popup.ShowPopup(
                    Localize(EGRLanguageData.ACCOUNT_INFO),
                    Localize(EGRLanguageData.YOU_HAVE_UNSAVED_CHANGES_nWOULD_YOU_LIKE_TO_SAVE_YOUR_CHANGES_),
                    OnUnsavedClose,
                    null
                );
            }
            else
                HideScreen();
        }

        void OnUnsavedClose(EGRPopup popup, EGRPopupResult result) {
            if (result == EGRPopupResult.YES) {
                OnSaveClick();
                return;
            }

            HideScreen();
        }

        public void OnBackKeyDown() {
            OnBackClick();
        }
    }
}
