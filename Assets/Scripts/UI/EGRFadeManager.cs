using System;
using UnityEngine;

namespace MRK.UI {
    /// <summary>
    /// Renders screen fades
    /// </summary>
    public class EGRFadeManager : MonoBehaviour {
        /// <summary>
        /// Holds information of a fade
        /// </summary>
        class FadeSetup {
            /// <summary>
            /// Length of fade in
            /// </summary>
            public float In;
            /// <summary>
            /// Length of fade out
            /// </summary>
            public float Out;
            /// <summary>
            /// Gets called in between
            /// </summary>
            public Action Act;
            /// <summary>
            /// Current stage, in or out
            /// </summary>
            public byte Stage;
        }

        /// <summary>
        /// Current fade
        /// </summary>
        FadeSetup m_CurrentFade;
        /// <summary>
        /// Fade color interpolator
        /// </summary>
        EGRColorFade m_Fade;
        /// <summary>
        /// EGRFadeManager singleton instance
        /// </summary>
        static EGRFadeManager ms_Instance;

        /// <summary>
        /// EGRFadeManager singleton instance
        /// </summary>
        static EGRFadeManager Instance {
            get {
                //manually initialize if does not exist
                if (ms_Instance == null)
                    ms_Instance = new GameObject("EGRFadeManager").AddComponent<EGRFadeManager>();

                return ms_Instance;
            }
        }
        /// <summary>
        /// Indicates if there is a fade being rendered
        /// </summary>
        public static bool IsFading => Instance.m_CurrentFade != null;

        /// <summary>
        /// Render GUI
        /// </summary>
        void OnGUI() {
            //skip if there is no fade present
            if (m_CurrentFade == null)
                return;

            //update the color interpolator
            m_Fade.Update();

            //fade in stage
            if (m_CurrentFade.Stage == 0x0) {
                //check if the interpolator has finished
                if (m_Fade.Done) {
                    //switch to fade out stage
                    m_CurrentFade.Stage = 0x1;

                    //invoke the mid stage callback
                    if (m_CurrentFade.Act != null)
                        m_CurrentFade.Act();

                    //create a new interpolator
                    m_Fade = new EGRColorFade(m_Fade.Current, Color.black.AlterAlpha(0f), 1f / m_CurrentFade.Out);
                    //update the interpolator, to prevent a color hiccup
                    m_Fade.Update();
                }
            }
            else if (m_CurrentFade.Stage == 0x1) {
                //clear the current fade if done
                if (m_Fade.Done)
                    m_CurrentFade = null;
            }

            //render the fade
            GUI.DrawTexture(Screen.safeArea, EGRUIUtilities.GetPlainTexture(m_Fade.Current));
        }

        /// <summary>
        /// Internally renders a fade
        /// </summary>
        /// <param name="fIn">Length of fade in</param>
        /// <param name="fOut">Length of fade out</param>
        /// <param name="betweenAct">Action in between</param>
        void InternalFade(float fIn, float fOut, Action betweenAct) {
            m_CurrentFade = new FadeSetup {
                In = fIn,
                Out = fOut,
                Act = betweenAct,
                Stage = 0x0
            };

            //initializes the interpolator
            m_Fade = new EGRColorFade(Color.black.AlterAlpha(0f), Color.black, 1f / fIn);
        }

        /// <summary>
        /// Renders a fade
        /// </summary>
        /// <param name="fIn">Length of fade in</param>
        /// <param name="fOut">Length of fade out</param>
        /// <param name="betweenAct">Action in between</param>
        public static void Fade(float fIn, float fOut, Action betweenAct) {
            Instance.InternalFade(fIn, fOut, betweenAct);
        }
    }
}
