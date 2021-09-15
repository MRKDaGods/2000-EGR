using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_Loading;

namespace MRK.UI {
    public class EGRScreenLoading : EGRScreen {
        EGRFiniteStateMachine m_StateMachine;
        TextMeshProUGUI m_EgrText;
        TextMeshProUGUI m_NumText;
        Image m_EgrBg;
        EGRColorFade m_ColorFade;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0x00000000u;

        protected override void OnScreenInit() {
            m_EgrText = GetElement<TextMeshProUGUI>(Labels.Egr);
            m_EgrText.color = Color.clear;

            m_NumText = GetElement<TextMeshProUGUI>(Labels.Num);
            m_NumText.color = Color.clear;

            m_EgrBg = GetElement<Image>(Images.EgrBg);

            float targetY = 0f;
            float deltaY = 0f;

            m_StateMachine = new EGRFiniteStateMachine(new Tuple<Func<bool>, Action, Action>[] {
                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_ColorFade.Done;
                },
                () => {
                    m_ColorFade.Update();
                    m_EgrText.color = m_ColorFade.Current;
                },
                () => {
                    m_ColorFade = new EGRColorFade(Color.clear, Color.white, 1.5f);
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return deltaY >= 1f;
                },
                () => {
                    deltaY += Time.deltaTime * 5f;
                    m_EgrText.rectTransform.anchoredPosition = new Vector2(m_EgrText.rectTransform.anchoredPosition.x, Mathf.Lerp(0f, targetY, deltaY));
                },
                () => {
                    targetY = -m_EgrText.rectTransform.sizeDelta.y / 2f;
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_ColorFade.Done;
                },
                () => {
                    m_ColorFade.Update();
                    m_NumText.color = m_ColorFade.Current;
                },
                () => {
                    m_ColorFade = new EGRColorFade(Color.clear, Color.white, 1.5f);
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_ColorFade.Done;
                },
                () => {
                    m_ColorFade.Update();
                    m_EgrText.color = m_ColorFade.Current;
                    m_EgrBg.color = m_ColorFade.Current.Inverse();
                },
                () => {
                    m_ColorFade = new EGRColorFade(Color.white, Color.black, 2f);
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return true;
                },
                () => { },
                () => {
                    StartCoroutine(Load());
                    Client.InitializeMaps();
                    Client.SetPostProcessState(true);
                    Manager.GetScreen<EGRScreenMapInterface>().Warmup();

                    //SO MUCH TIME, USE WISELY
                    //Client.FixInvalidTiles();
                })
            });
        }

        IEnumerator Load() {
            for (int i = 0; i < 10; i++)
                yield return new WaitForEndOfFrame();

            EGRFadeManager.Fade(1f, 0.5f, () => {
                Client.Initialize();

                Manager.GetScreen<EGRScreenLogin>().ShowScreen();
                HideScreen();
            });
        }

        protected override void OnScreenUpdate() {
            m_StateMachine.UpdateFSM();
        }
    }
}
