using MRK.Localization;
using TMPro;
using UnityEngine.UI;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public class MessageBox : AnimatedLayout, ISupportsBackKey
    {
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
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

        protected override void OnScreenInit()
        {
            base.OnScreenInit(); //init layout

            _title = GetElement<TextMeshProUGUI>("Body/Layout/Title");
            _body = GetElement<TextMeshProUGUI>("Body/Layout/Body");

            _ok = GetElement<Button>("Body/Layout/Ok/Button");
            _ok.onClick.AddListener(() => HideScreen());
        }

        protected override bool CanAnimate(Graphic gfx, bool moving)
        {
            return !moving;
        }

        protected override void SetText(string text)
        {
            _body.text = text;
        }

        protected override void SetTitle(string title)
        {
            _title.text = title;
        }

        public void ShowButton(bool show)
        {
            _ok.gameObject.SetActive(show);
        }

        protected override void OnScreenHide()
        {
            base.OnScreenHide();
            _ok.gameObject.SetActive(true);
            SetOkButtonText(Localize(LanguageData.OK));
        }

        protected override void OnScreenShow()
        {
            _result = PopupResult.OK;
        }

        public void SetOkButtonText(string txt)
        {
            _ok.GetComponentInChildren<TextMeshProUGUI>().text = txt;
        }

        public void OnBackKeyDown()
        {
            if (_ok.gameObject.activeInHierarchy)
            {
                HideScreen();
            }
        }
    }
}
