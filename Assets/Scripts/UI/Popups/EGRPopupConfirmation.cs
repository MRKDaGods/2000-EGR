using TMPro;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public class EGRPopupConfirmation : EGRPopupAnimatedLayout, IEGRScreenSupportsBackKey {
        Button m_Yes;
        Button m_No;
        TextMeshProUGUI m_Title;
        TextMeshProUGUI m_Body;

        protected override string LayoutPath => "Body/Layout";
        public override bool CanChangeBar => true;
        public override uint BarColor => 0xB4000000;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            m_Yes = GetElement<Button>("Body/Layout/Yes/Button");
            m_No = GetElement<Button>("Body/Layout/No/Button");

            m_Yes.onClick.AddListener(() => OnButtonClick(EGRPopupResult.YES));
            m_No.onClick.AddListener(() => OnButtonClick(EGRPopupResult.NO));

            m_Title = GetElement<TextMeshProUGUI>("Body/Layout/Title");
            m_Body = GetElement<TextMeshProUGUI>("Body/Layout/Body");
        }

        protected override bool CanAnimate(Graphic gfx, bool moving) {
            return !moving;
        }

        protected override void SetText(string text) {
            m_Body.text = text;
        }

        protected override void SetTitle(string title) {
            m_Title.text = title;
        }

        public void SetYesButtonText(string txt) {
            m_Yes.GetComponentInChildren<TextMeshProUGUI>().text = txt;
        }

        public void SetNoButtonText(string txt) {
            m_No.GetComponentInChildren<TextMeshProUGUI>().text = txt;
        }

        void OnButtonClick(EGRPopupResult result) {
            m_Result = result;
            HideScreen();
        }

        protected override void OnScreenHide() {
            base.OnScreenHide();

            SetYesButtonText(Localize(EGRLanguageData.YES));
            SetNoButtonText(Localize(EGRLanguageData.NO));
        }

        public void OnBackKeyDown() {
            OnButtonClick(EGRPopupResult.NO);
        }
    }
}