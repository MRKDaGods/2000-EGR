using TMPro;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRPopupInputText : EGRPopupAnimatedLayout, IEGRScreenSupportsBackKey {
        TextMeshProUGUI m_Title;
        TextMeshProUGUI m_Body;
        TMP_InputField m_Input;
        Button m_Ok;

        protected override string LayoutPath => "Body/Layout";
        public override bool CanChangeBar => true;
        public override uint BarColor => 0xB4000000;
        public string Input {
            get => m_Input.text;
            set => m_Input.text = value;
        }

        protected override void OnScreenInit() {
            base.OnScreenInit();

            m_Title = GetElement<TextMeshProUGUI>("Body/Layout/Title");
            m_Body = GetElement<TextMeshProUGUI>("Body/Layout/Body");
            m_Input = GetElement<TMP_InputField>("Body/Layout/Input/Textbox");

            m_Ok = GetElement<Button>("Body/Layout/Ok/Button");
            m_Ok.onClick.AddListener(() => HideScreen());
        }

        protected override bool CanAnimate(Graphic gfx, bool moving) {
            return !moving;
        }

        protected override void SetTitle(string title) {
            m_Title.text = title;
        }

        protected override void SetText(string txt) {
            m_Body.text = txt;
        }

        protected override void OnScreenHide() {
            base.OnScreenHide();

            //we reset the input content type here
            m_Input.contentType = TMP_InputField.ContentType.Standard;
        }

        protected override void OnScreenShow() {
            m_Result = EGRPopupResult.OK;
            Input = "";
        }

        public void SetPassword() {
            m_Input.contentType = TMP_InputField.ContentType.Password;
        }

        public void OnBackKeyDown() {
            //prevent lower-z screens from exeing
        }
    }
}