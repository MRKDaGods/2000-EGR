using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRPopup_ConfirmPwd;

namespace MRK.UI {
    public class EGRPopupConfirmPassword : EGRPopup {
        TextMeshProUGUI m_Title;
        TMP_InputField m_Password;
        Button m_Ok;

        public string Password => m_Password.text;
        public override bool CanChangeBar => true;
        public override uint BarColor => 0xB4000000;

        protected override void OnScreenInit() {
            m_Title = GetElement<TextMeshProUGUI>(Labels.zTitle);
            m_Password = GetElement<TMP_InputField>(Textboxes.Pass);

            m_Ok = GetElement<Button>(Buttons.Ok);
            m_Ok.onClick.AddListener(() => HideScreen());
        }

        protected override void SetTitle(string title) {
            m_Title.text = title;
        }

        protected override void OnScreenHide() {
            base.OnScreenHide();
            m_Ok.gameObject.SetActive(true);
        }

        protected override void OnScreenShow() {
            m_Result = EGRPopupResult.OK;
            m_Password.text = "";
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            PushGfxState(EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, 0.1f + i * 0.03f + (i > 10 ? 0.3f : 0f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                m_LastGraphicsBuf[i].DOColor(Color.clear, 0.1f + i * 0.03f + (i > 10 ? 0.1f : 0f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }
    }
}
