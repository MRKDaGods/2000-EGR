using DG.Tweening;
using MRK.Authentication;
using MRK.Localization;
using MRK.Networking;
using MRK.Networking.Packets;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using static MRK.UI.EGRUI_Main.EGRScreen_OptionsAccInfo;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public class AccountInfo : Screen, ISupportsBackKey
    {
        private TMP_InputField _firstName;
        private TMP_InputField _lastName;
        private SegmentedControl _gender;
        private Button _save;
        private bool _listeningToChanges;

        private LocalUser m_LocalUser
        {
            get
            {
                return LocalUser.Instance;
            }
        }

        private bool EnableSave
        {
            get
            {
                return _firstName.text != m_LocalUser.FirstName
                    || _lastName.text != m_LocalUser.LastName
                    || _gender.selectedSegmentIndex != m_LocalUser.Gender;
            }
        }

        protected override void OnScreenInit()
        {
            _firstName = GetElement<TMP_InputField>(Textboxes.Fn);
            _lastName = GetElement<TMP_InputField>(Textboxes.Ln);

            _firstName.onValueChanged.AddListener(OnTextChanged);
            _lastName.onValueChanged.AddListener(OnTextChanged);

            _gender = GetElement<SegmentedControl>(Others.Gender);
            _gender.onValueChanged.AddListener(OnGenderChanged);

            _save = GetElement<Button>(Buttons.Save);
            _save.onClick.AddListener(OnSaveClick);

            GetElement<Button>(Buttons.Back).onClick.AddListener(OnBackClick);
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            _gender.LayoutSegments();

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(_lastGraphicsBuf, (x, y) =>
            {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            PushGfxState(GfxStates.Position | GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

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

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();
            Array.Sort(_lastGraphicsBuf, (x, y) =>
            {
                return y.transform.position.y.CompareTo(x.transform.position.y);
            });

            SetTweenCount(_lastGraphicsBuf.Length);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(Color.clear, TweenMonitored(0.2f + i * 0.03f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        protected override void OnScreenShow()
        {
            _save.interactable = false;

            _firstName.text = m_LocalUser.FirstName;
            _lastName.text = m_LocalUser.LastName;

            _gender.selectedSegmentIndex = m_LocalUser.Gender;

            GetElement<TextMeshProUGUI>(Labels.Emz).text = m_LocalUser.Email;

            _listeningToChanges = true;
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
            {
                HideScreen();
            }
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

        private void OnTextChanged(string newValue)
        {
            if (_listeningToChanges)
                _save.interactable = EnableSave;
        }

        private void OnGenderChanged(int newValue)
        {
            if (_listeningToChanges && newValue != -1)
                _save.interactable = EnableSave;
        }

        private bool IsValidName(string s)
        {
            return !string.IsNullOrEmpty(s) && !string.IsNullOrWhiteSpace(s);
        }

        private void OnSaveClick()
        {
            if (!IsValidName(_firstName.text) || !IsValidName(_lastName.text))
            {
                MessageBox.ShowPopup(Localize(LanguageData.ERROR), Localize(LanguageData.INVALID_NAME), null, this);
                return;
            }

            if (!NetworkingClient.MainNetworkExternal.UpdateAccountInfo(string.Join(" ", _firstName.text, _lastName.text), m_LocalUser.Email, (sbyte)_gender.selectedSegmentIndex, OnNetSave))
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
                    Email = m_LocalUser.Email,
                    FirstName = _firstName.text,
                    LastName = _lastName.text,
                    Gender = (sbyte)_gender.selectedSegmentIndex,
                    Token = m_LocalUser.Token
                });

                MessageBox.ShowPopup(Localize(LanguageData.ACCOUNT_INFO), Localize(LanguageData.SAVED), (x, y) =>
                {
                    _save.interactable = false;
                    OnBackClick();
                }, null);
            }, 1.1f);
        }

        protected override void OnScreenHide()
        {
            _save.interactable = false;
            _listeningToChanges = false;

            ScreenManager.GetScreen<Options>().UpdateProfile();
        }

        public void OnBackKeyDown()
        {
            OnBackClick();
        }
    }
}
