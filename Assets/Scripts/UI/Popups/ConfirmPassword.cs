using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRPopup_ConfirmPwd;

namespace MRK.UI
{
    public class ConfirmPassword : Popup
    {
        private TextMeshProUGUI _title;
        private TMP_InputField _password;
        private Button _ok;

        public string Password
        {
            get
            {
                return _password.text;
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
            _title = GetElement<TextMeshProUGUI>(Labels.zTitle);
            _password = GetElement<TMP_InputField>(Textboxes.Pass);

            _ok = GetElement<Button>(Buttons.Ok);
            _ok.onClick.AddListener(() => HideScreen());
        }

        protected override void SetTitle(string title)
        {
            _title.text = title;
        }

        protected override void OnScreenHide()
        {
            base.OnScreenHide();
            _ok.gameObject.SetActive(true);
        }

        protected override void OnScreenShow()
        {
            _result = PopupResult.OK;
            _password.text = "";
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            PushGfxState(GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(gfx.color, 0.1f + i * 0.03f + (i > 10 ? 0.3f : 0f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            SetTweenCount(_lastGraphicsBuf.Length);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                _lastGraphicsBuf[i].DOColor(Color.clear, 0.1f + i * 0.03f + (i > 10 ? 0.1f : 0f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }
    }
}
