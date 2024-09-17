using MRK.Localization;
using TMPro;
using UnityEngine.UI;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public class Confirmation : AnimatedLayout, ISupportsBackKey
    {
        private Button _yes;
        private Button _no;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;

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
            base.OnScreenInit();

            _yes = GetElement<Button>("Body/Layout/Yes/Button");
            _no = GetElement<Button>("Body/Layout/No/Button");

            _yes.onClick.AddListener(() => OnButtonClick(PopupResult.YES));
            _no.onClick.AddListener(() => OnButtonClick(PopupResult.NO));

            _title = GetElement<TextMeshProUGUI>("Body/Layout/Title");
            _body = GetElement<TextMeshProUGUI>("Body/Layout/Body");
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

        public void SetYesButtonText(string txt)
        {
            _yes.GetComponentInChildren<TextMeshProUGUI>().text = txt;
        }

        public void SetNoButtonText(string txt)
        {
            _no.GetComponentInChildren<TextMeshProUGUI>().text = txt;
        }

        private void OnButtonClick(PopupResult result)
        {
            _result = result;
            HideScreen();
        }

        protected override void OnScreenHide()
        {
            base.OnScreenHide();

            SetYesButtonText(Localize(LanguageData.YES));
            SetNoButtonText(Localize(LanguageData.NO));
        }

        public void OnBackKeyDown()
        {
            OnButtonClick(PopupResult.NO);
        }
    }
}