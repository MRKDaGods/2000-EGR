using MRK.Networking;
using MRK.Networking.Packets;
using TMPro;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public class EGRScreenOptionsEmail : EGRScreenAnimatedLayout {
        TMP_InputField m_NewEmail;
        TMP_InputField m_ConfEmail;
        TMP_InputField m_Password;
        Button m_Save;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0xFF000000;
        EGRLocalUser m_LocalUser => EGRLocalUser.Instance;
        bool m_EnableSave => m_NewEmail.text != "" && m_ConfEmail.text != "" && m_NewEmail.text == m_ConfEmail.text;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);

            m_NewEmail = GetElement<TMP_InputField>("Layout/EmailTb");
            m_ConfEmail = GetElement<TMP_InputField>("Layout/ConfEmailTb");
            m_Password = GetElement<TMP_InputField>("Layout/PasswordTb");

            m_NewEmail.onValueChanged.AddListener(OnTextChanged);
            m_ConfEmail.onValueChanged.AddListener(OnTextChanged);

            m_Save = GetElement<Button>("Layout/Save");
            m_Save.onClick.AddListener(OnSaveClick);
        }

        protected override void OnScreenShow() {
            m_NewEmail.text = "";
            m_ConfEmail.text = "";
            m_Password.text = "";

            m_Save.interactable = false;
        }

        void OnSaveClick() {
            if (EGRMain.CalculateHash(m_Password.text) != EGRLocalUser.PasswordHash) {
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), Localize(EGRLanguageData.INCORRECT_PASSWORD), null, this);
                return;
            }

            if (m_NewEmail.text != m_ConfEmail.text) {
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), Localize(EGRLanguageData.EMAILS_DO_NOT_MATCH), null, this);
                return;
            }

            if (!EGRUtils.ValidateEmail(m_NewEmail.text)) {
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), Localize(EGRLanguageData.INVALID_EMAIL), null, this);
                return;
            }

            if (!Client.NetUpdateAccountInfo(m_LocalUser.FullName, m_NewEmail.text, m_LocalUser.Gender, OnNetSave)) {
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

                EGRLocalUser.Initialize(new EGRProxyUser {
                    Email = m_NewEmail.text,
                    FirstName = m_LocalUser.FirstName,
                    LastName = m_LocalUser.LastName,
                    Gender = m_LocalUser.Gender,
                    Token = m_LocalUser.Token
                });

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
                EGRPopupConfirmation popup = Manager.GetPopup<EGRPopupConfirmation>();
                popup.SetYesButtonText(Localize(EGRLanguageData.SAVE));
                popup.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
                popup.ShowPopup(Localize(EGRLanguageData.ACCOUNT_INFO), Localize(EGRLanguageData.YOU_HAVE_UNSAVED_CHANGES_nWOULD_YOU_LIKE_TO_SAVE_YOUR_CHANGES_), OnUnsavedClose, null);
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
    }
}
