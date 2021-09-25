using TMPro;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public class EGRPopupMessageBox : EGRPopupAnimatedLayout, IEGRScreenSupportsBackKey {
        TextMeshProUGUI m_Title;
        TextMeshProUGUI m_Body;
        Button m_Ok;

        protected override string LayoutPath => "Body/Layout";
        public override bool CanChangeBar => true;
        public override uint BarColor => 0xB4000000;

        protected override void OnScreenInit() {
            base.OnScreenInit(); //init layout

            m_Title = GetElement<TextMeshProUGUI>("Body/Layout/Title");
            m_Body = GetElement<TextMeshProUGUI>("Body/Layout/Body");

            m_Ok = GetElement<Button>("Body/Layout/Ok/Button");
            m_Ok.onClick.AddListener(() => HideScreen());
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

        public void ShowButton(bool show) {
            m_Ok.gameObject.SetActive(show);
        }

        protected override void OnScreenHide() {
            base.OnScreenHide();
            m_Ok.gameObject.SetActive(true);
            SetOkButtonText(Localize(EGRLanguageData.OK));
        }

        protected override void OnScreenShow() {
            m_Result = EGRPopupResult.OK;
        }

        public void SetOkButtonText(string txt) {
            m_Ok.GetComponentInChildren<TextMeshProUGUI>().text = txt;
        }

        public void OnBackKeyDown() {
            if (m_Ok.gameObject.activeInHierarchy) {
                HideScreen();
            }
        }
    }
}
