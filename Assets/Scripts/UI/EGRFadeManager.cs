using System;
using UnityEngine;

namespace MRK.UI {
    public class EGRFadeManager : MonoBehaviour {
        class FadeSetup {
            public float In;
            public float Out;
            public Action Act;
            public byte Stage;
        }

        FadeSetup m_CurrentFade;
        EGRColorFade m_Fade;

        static EGRFadeManager ms_Instance;

        static EGRFadeManager Instance {
            get {
                if (ms_Instance == null)
                    ms_Instance = new GameObject("EGRFadeManager").AddComponent<EGRFadeManager>();

                return ms_Instance;
            }
        }
        public static bool IsFading => Instance.m_CurrentFade != null;

        void OnGUI() {
            if (m_CurrentFade == null)
                return;

            m_Fade.Update();

            if (m_CurrentFade.Stage == 0x0) {
                if (m_Fade.Done) {
                    m_CurrentFade.Stage = 0x1;
                    if (m_CurrentFade.Act != null)
                        m_CurrentFade.Act();


                    m_Fade = new EGRColorFade(m_Fade.Current, Color.black.AlterAlpha(0f), 1f / m_CurrentFade.Out);
                    m_Fade.Update();
                }
            }
            else if (m_CurrentFade.Stage == 0x1) {
                if (m_Fade.Done)
                    m_CurrentFade = null;
            }

            GUI.DrawTexture(Screen.safeArea, EGRUIUtilities.GetPlainTexture(m_Fade.Current));
        }

        void InternalFade(float fIn, float fOut, Action betweenAct) {
            m_CurrentFade = new FadeSetup {
                In = fIn,
                Out = fOut,
                Act = betweenAct,
                Stage = 0x0
            };

            m_Fade = new EGRColorFade(Color.black.AlterAlpha(0f), Color.black, 1f / fIn);
        }

        public static void Fade(float fIn, float fOut, Action betweenAct) {
            Instance.InternalFade(fIn, fOut, betweenAct);
        }
    }
}
