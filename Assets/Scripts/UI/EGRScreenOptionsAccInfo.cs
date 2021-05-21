using DG.Tweening;
using MRK.Networking;
using MRK.Networking.Packets;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using static MRK.EGRLanguageManager;
using static MRK.UI.EGRUI_Main.EGRScreen_OptionsAccInfo;

namespace MRK.UI {
    public class EGRScreenOptionsAccInfo : EGRScreen {
        TMP_InputField m_FirstName;
        TMP_InputField m_LastName;
        SegmentedControl m_Gender;
        Button m_Save;
        bool m_ListeningToChanges;

        EGRLocalUser m_LocalUser => EGRLocalUser.Instance;
        bool m_EnableSave => m_FirstName.text != m_LocalUser.FirstName || m_LastName.text != m_LocalUser.LastName || m_Gender.selectedSegmentIndex != m_LocalUser.Gender;

        protected override void OnScreenInit() {
            m_FirstName = GetElement<TMP_InputField>(Textboxes.Fn);
            m_LastName = GetElement<TMP_InputField>(Textboxes.Ln);

            m_FirstName.onValueChanged.AddListener(OnTextChanged);
            m_LastName.onValueChanged.AddListener(OnTextChanged);

            m_Gender = GetElement<SegmentedControl>(Others.Gender);
            m_Gender.onValueChanged.AddListener(OnGenderChanged);

            m_Save = GetElement<Button>(Buttons.Save);
            m_Save.onClick.AddListener(OnSaveClick);

            GetElement<Button>(Buttons.Back).onClick.AddListener(OnBackClick);
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            m_Gender.LayoutSegments();

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            PushGfxState(EGRGfxState.Position | EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.3f + i * 0.03f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                if (gfx.ParentHasGfx())
                    continue;

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.2f + i * 0.03f))
                    .ChangeStartValue(2f * gfx.transform.position)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(m_LastGraphicsBuf, (x, y) => {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.2f + i * 0.03f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        protected override void OnScreenShow() {
            m_Save.interactable = false;

            m_FirstName.text = m_LocalUser.FirstName;
            m_LastName.text = m_LocalUser.LastName;

            m_Gender.selectedSegmentIndex = m_LocalUser.Gender;

            GetElement<TextMeshProUGUI>(Labels.Emz).text = m_LocalUser.Email;

            m_ListeningToChanges = true;
        }

        void OnBackClick() {
            //unsaved changes
            if (m_Save.interactable) {
                EGRPopupConfirmation popup = Manager.GetPopup<EGRPopupConfirmation>(EGRUI_Main.EGRPopup_Confirmation.SCREEN_NAME);
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

        void OnTextChanged(string newValue) {
            if (m_ListeningToChanges)
                m_Save.interactable = m_EnableSave;
        }

        void OnGenderChanged(int newValue) {
            if (m_ListeningToChanges && newValue != -1)
                m_Save.interactable = m_EnableSave;
        }

        bool IsValidName(string s) {
            return !string.IsNullOrEmpty(s) && !string.IsNullOrWhiteSpace(s);
        }

        void OnSaveClick() {
            if (!IsValidName(m_FirstName.text) || !IsValidName(m_LastName.text)) {
                MessageBox.ShowPopup(Localize(EGRLanguageData.ERROR), Localize(EGRLanguageData.INVALID_NAME), null, this);
                return;
            }

            if (!Client.NetUpdateAccountInfo(string.Join(" ", m_FirstName.text, m_LastName.text), m_LocalUser.Email, (sbyte)m_Gender.selectedSegmentIndex, OnNetSave)) {
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
                    Email = m_LocalUser.Email,
                    FirstName = m_FirstName.text,
                    LastName = m_LastName.text,
                    Gender = (sbyte)m_Gender.selectedSegmentIndex,
                    Token = m_LocalUser.Token
                });

                MessageBox.ShowPopup(Localize(EGRLanguageData.ACCOUNT_INFO), Localize(EGRLanguageData.SAVED), (x, y) => {
                    m_Save.interactable = false;
                    OnBackClick();
                }, null);
            }, 1.1f);
        }

        protected override void OnScreenHide() {
            m_Save.interactable = false;
            m_ListeningToChanges = false;
        }
    }
}
