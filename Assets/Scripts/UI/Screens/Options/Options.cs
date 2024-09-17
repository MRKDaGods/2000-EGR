using MRK.Localization;
using TMPro;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_Options;
using static MRK.Localization.LanguageManager;
using MRK.Authentication;

namespace MRK.UI
{
    public class Options : AnimatedLayout, ISupportsBackKey
    {
        private Image _background;
        private TextMeshProUGUI _name;

        protected override bool IsRTL
        {
            get
            {
                return false;
            }
        }

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

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            GetElement<Button>(Buttons.TopLeftMenu).onClick.AddListener(OnBackClicked);

            GetElement<Button>("Layout/Account").onClick.AddListener(() =>
            {
                ScreenManager.GetScreen<AccountInfo>().ShowScreen();
            });

            GetElement<Button>("Layout/ChngEmail").onClick.AddListener(() =>
            {
                if (LocalUser.Instance.IsDeviceID())
                {
                    MessageBox.ShowPopup(
                        Localize(LanguageData.ERROR),
                        Localize(LanguageData.ACCOUNTS_LINKED_WITH_A_DEVICE_ID_CAN_NOT_HAVE_THEIR_EMAILS_CHANGED),
                        null,
                        this
                    );
                    return;
                }

                ScreenManager.GetScreen<EmailInfo>().ShowScreen();
            });

            GetElement<Button>("Layout/ChngPwd").onClick.AddListener(() =>
            {
                if (LocalUser.Instance.IsDeviceID())
                {
                    MessageBox.ShowPopup(
                        Localize(LanguageData.ERROR),
                        Localize(LanguageData.ACCOUNTS_LINKED_WITH_A_DEVICE_ID_CAN_NOT_HAVE_THEIR_PASSWORDS_CHANGED),
                        null,
                        this
                    );
                    return;
                }

                ScreenManager.GetScreen<PasswordInfo>().ShowScreen();
            });

            GetElement<Button>("Layout/Logout").onClick.AddListener(OnLogoutClick);

            TextMeshProUGUI bInfo = GetElement<TextMeshProUGUI>(Labels.BuildInfo);
            bInfo.text = string.Format(bInfo.text, $"{EGRVersion.VersionString()} - {EGRVersion.VersionSignature()}");

            _background = GetElement<Image>(Images.Bg);
            _name = GetElement<TextMeshProUGUI>("Layout/Profile/Name");
        }

        protected override bool CanAnimate(Graphic gfx, bool moving)
        {
            return !(moving && gfx == _background);
        }

        protected override void OnScreenShow()
        {
            UpdateProfile();
        }

        public void UpdateProfile()
        {
            _name.text = LocalUser.Instance.FullName;
        }

        private void OnLogoutClick()
        {
            Confirmation popup = ScreenManager.GetPopup<Confirmation>();
            popup.SetYesButtonText(Localize(LanguageData.LOGOUT));
            popup.SetNoButtonText(Localize(LanguageData.CANCEL));
            popup.ShowPopup(
                Localize(LanguageData.ACCOUNT_INFO),
                Localize(LanguageData.ARE_YOU_SURE_THAT_YOU_WANT_TO_LOGOUT_OF_EGR_),
                OnLogoutClosed,
                null
            );
        }

        private void OnLogoutClosed(Popup popup, PopupResult res)
        {
            if (res == PopupResult.YES)
            {
                Client.Logout();
            }
        }

        private void OnBackClicked()
        {
            HideScreen(() => ScreenManager.GetScreen<Menu>().ShowScreen(), 0.1f, false);
        }

        public void OnBackKeyDown()
        {
            OnBackClicked();
        }
    }
}
