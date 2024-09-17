using TMPro;
using UnityEngine.UI;

namespace MRK.UI
{
    public class InputText : AnimatedLayout, ISupportsBackKey
    {
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private TMP_InputField _input;
        private Button _ok;

        protected override string LayoutPath
        {
            get
            {
                return "Body/Layout";
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
                return 0xB4000000;
            }
        }

        public string Input
        {
            get
            {
                return _input.text;
            }

            set
            {
                _input.text = value;
            }
        }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            _title = GetElement<TextMeshProUGUI>("Body/Layout/Title");
            _body = GetElement<TextMeshProUGUI>("Body/Layout/Body");
            _input = GetElement<TMP_InputField>("Body/Layout/Input/Textbox");

            _ok = GetElement<Button>("Body/Layout/Ok/Button");
            _ok.onClick.AddListener(() => HideScreen());
        }

        protected override bool CanAnimate(Graphic gfx, bool moving)
        {
            return !moving;
        }

        protected override void SetTitle(string title)
        {
            _title.text = title;
        }

        protected override void SetText(string txt)
        {
            _body.text = txt;
        }

        protected override void OnScreenHide()
        {
            base.OnScreenHide();

            //we reset the input content type here
            _input.contentType = TMP_InputField.ContentType.Standard;
        }

        protected override void OnScreenShow()
        {
            _result = PopupResult.OK;
            Input = "";
        }

        public void SetPassword()
        {
            _input.contentType = TMP_InputField.ContentType.Password;
        }

        public void OnBackKeyDown()
        {
            //prevent lower-z screens from exeing
        }
    }
}