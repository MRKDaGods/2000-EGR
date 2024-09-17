using System;
using UnityEngine;

namespace MRK.UI
{
    /// <summary>
    /// Renders screen fades
    /// </summary>
    public class FadeManager : MonoBehaviour
    {
        /// <summary>
        /// Holds information of a fade
        /// </summary>
        private class FadeSetup
        {
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
        private FadeSetup _currentFade;

        /// <summary>
        /// Fade color interpolator
        /// </summary>
        private EGRColorFade _fade;

        /// <summary>
        /// EGRFadeManager singleton instance
        /// </summary>
        private static FadeManager _instance;

        /// <summary>
        /// Indicates if there is a fade being rendered
        /// </summary>
        public static bool IsFading
        {
            get
            {
                return Instance._currentFade != null;
            }
        }

        /// <summary>
        /// EGRFadeManager singleton instance
        /// </summary>
        private static FadeManager Instance
        {
            get
            {
                //manually initialize if does not exist
                if (_instance == null)
                {
                    _instance = new GameObject("EGRFadeManager").AddComponent<FadeManager>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Render GUI
        /// </summary>
        private void OnGUI()
        {
            //skip if there is no fade present
            if (_currentFade == null)
                return;

            //update the color interpolator
            _fade.Update();

            //fade in stage
            if (_currentFade.Stage == 0x0)
            {
                //check if the interpolator has finished
                if (_fade.Done)
                {
                    //switch to fade out stage
                    _currentFade.Stage = 0x1;

                    //invoke the mid stage callback
                    if (_currentFade.Act != null)
                        _currentFade.Act();

                    //create a new interpolator
                    _fade = new EGRColorFade(_fade.Current, Color.black.AlterAlpha(0f), 1f / _currentFade.Out);
                    //update the interpolator, to prevent a color hiccup
                    _fade.Update();
                }
            }
            else if (_currentFade.Stage == 0x1)
            {
                //clear the current fade if done
                if (_fade.Done)
                    _currentFade = null;
            }

            //render the fade
            GUI.DrawTexture(Screen.safeArea, Utilities.GetPlainTexture(_fade.Current));
        }

        /// <summary>
        /// Internally renders a fade
        /// </summary>
        /// <param name="fIn">Length of fade in</param>
        /// <param name="fOut">Length of fade out</param>
        /// <param name="betweenAct">Action in between</param>
        private void InternalFade(float fIn, float fOut, Action betweenAct)
        {
            _currentFade = new FadeSetup
            {
                In = fIn,
                Out = fOut,
                Act = betweenAct,
                Stage = 0x0
            };

            //initializes the interpolator
            _fade = new EGRColorFade(Color.black.AlterAlpha(0f), Color.black, 1f / fIn);
        }

        /// <summary>
        /// Renders a fade
        /// </summary>
        /// <param name="fIn">Length of fade in</param>
        /// <param name="fOut">Length of fade out</param>
        /// <param name="betweenAct">Action in between</param>
        public static void Fade(float fIn, float fOut, Action betweenAct)
        {
            Instance.InternalFade(fIn, fOut, betweenAct);
        }
    }
}
