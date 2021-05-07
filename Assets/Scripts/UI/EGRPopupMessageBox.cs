using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using DG.Tweening;

using static MRK.UI.EGRUI_Main.EGRPopup_MessageBox;

namespace MRK.UI {
    public class EGRPopupMessageBox : EGRPopup {
        TextMeshProUGUI m_Title;
        TextMeshProUGUI m_Body;
        Button m_Ok;
        Image m_Blur;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0xB4000000;

        protected override void OnScreenInit() {
            m_Title = GetElement<TextMeshProUGUI>(Labels.zTitle);
            m_Body = GetElement<TextMeshProUGUI>(Labels.Body);

            m_Ok = GetElement<Button>(Buttons.Ok);
            m_Ok.onClick.AddListener(() => HideScreen());

            m_Blur = GetElement<Image>(Images.Bg);
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
        }

        protected override void OnScreenShow() {
            m_Result = EGRPopupResult.OK;
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
