using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;
using static MRK.UI.EGRUI_Main.EGRPopup_Confirmation;

namespace MRK.UI {
    public class EGRPopupConfirmation : EGRPopup {
        Button m_Yes;
        Button m_No;
        TextMeshProUGUI m_Title;
        TextMeshProUGUI m_Body;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0xB4000000;

        protected override void OnScreenInit() {
            m_Yes = GetElement<Button>(Buttons.Yes);
            m_No = GetElement<Button>(Buttons.No);

            m_Yes.onClick.AddListener(() => OnButtonClick(EGRPopupResult.YES));
            m_No.onClick.AddListener(() => OnButtonClick(EGRPopupResult.NO));

            m_Title = GetElement<TextMeshProUGUI>(Labels.zTitle);
            m_Body = GetElement<TextMeshProUGUI>(Labels.Body);
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
            SetYesButtonText(Localize(EGRLanguageData.NO));
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