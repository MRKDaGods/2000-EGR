using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRPopupInputText : EGRPopup {
        TextMeshProUGUI m_Title;
        TextMeshProUGUI m_Body;
        TMP_InputField m_Input;
        Button m_Ok;

        public string Input {
            get => m_Input.text;
            set => m_Input.text = value;
        }
        public override bool CanChangeBar => true;
        public override uint BarColor => 0xB4000000;

        protected override void OnScreenInit() {
            m_Title = GetElement<TextMeshProUGUI>("ztitleBack/txtzTitle");
            m_Input = GetElement<TMP_InputField>("tbPass");
            m_Body = GetElement<TextMeshProUGUI>("txtBody");

            m_Ok = GetElement<Button>("bOk");
            m_Ok.onClick.AddListener(() => HideScreen());
        }

        protected override void SetTitle(string title) {
            m_Title.text = title;
        }

        protected override void SetText(string txt) {
            m_Body.text = txt;
        }

        protected override void OnScreenHide() {
            base.OnScreenHide();
            m_Ok.gameObject.SetActive(true);
        }

        protected override void OnScreenShow() {
            m_Result = EGRPopupResult.OK;
            m_Input.text = "";
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            PushGfxState(EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.2f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                m_LastGraphicsBuf[i].DOColor(Color.clear, TweenMonitored(0.2f))
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }
    }
}