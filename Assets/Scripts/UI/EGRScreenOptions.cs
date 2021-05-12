using TMPro;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;
using static MRK.UI.EGRUI_Main.EGRScreen_Options;

namespace MRK.UI {
    public class EGRScreenOptions : EGRScreenAnimatedLayout {
        Image m_Background;
        TextMeshProUGUI m_Name;

        protected override bool m_IsRTL => false;
        public override bool CanChangeBar => true;
        public override uint BarColor => 0xFF000000;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>(Buttons.TopLeftMenu).onClick.AddListener(() => {
                HideScreen(() => Manager.GetScreen<EGRScreenMenu>().ShowScreen(), 0.1f, false);
            });

            GetElement<Button>("Layout/Account").onClick.AddListener(() => {
                Manager.GetScreen<EGRScreenOptionsAccInfo>().ShowScreen();
            });

            GetElement<Button>("Layout/Logout").onClick.AddListener(OnLogoutClick);

            TextMeshProUGUI bInfo = GetElement<TextMeshProUGUI>(Labels.BuildInfo);
            bInfo.text = string.Format(bInfo.text, $"{EGRVersion.VersionString()} - {EGRVersion.VersionSignature()}");

            m_Background = GetElement<Image>(Images.Bg);
            m_Name = GetElement<TextMeshProUGUI>("Layout/Profile/Name");
        }

        protected override bool CanAnimate(Graphic gfx, bool moving) {
            return !(moving && gfx == m_Background);
        }

        protected override void OnScreenShow() {
            m_Name.text = EGRLocalUser.Instance.FullName;
        }

        void OnLogoutClick() {
            EGRPopupConfirmation popup = Manager.GetPopup<EGRPopupConfirmation>();
            popup.SetYesButtonText(Localize(EGRLanguageData.LOGOUT));
            popup.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
            popup.ShowPopup(Localize(EGRLanguageData.ACCOUNT_INFO), Localize(EGRLanguageData.ARE_YOU_SURE_THAT_YOU_WANT_TO_LOGOUT_OF_EGR_), OnLogoutClosed, null);
        }

        void OnLogoutClosed(EGRPopup popup, EGRPopupResult res) {
            if (res == EGRPopupResult.YES) {
                Client.Logout();
            }
        }
    }
}
