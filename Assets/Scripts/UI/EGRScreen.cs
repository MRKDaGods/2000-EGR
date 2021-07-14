using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using Gfx = UnityEngine.UI.Graphic;

namespace MRK.UI {
    /// <summary>
    /// Graphic state flags
    /// </summary>
    public enum EGRGfxState {
        None = 0,
        Position = 1,
        Color = 2,
        LocalPosition = 4
    }

    /// <summary>
    /// Base class for all screens
    /// </summary>
    public class EGRScreen : EGRBehaviour {
        /// <summary>
        /// Holds information about a graphic state
        /// </summary>
        class GfxState {
            /// <summary>
            /// Position of graphic
            /// </summary>
            public Vector3 Position;
            /// <summary>
            /// Color of graphic
            /// </summary>
            public Color Color;
            /// <summary>
            /// State mask of graphic
            /// </summary>
            public EGRGfxState Mask;
        }

        /// <summary>
        /// Name of screen
        /// </summary>
        [SerializeField]
        protected string m_ScreenName;
        /// <summary>
        /// Layer of screen
        /// </summary>
        [SerializeField]
        int m_Layer;
        /// <summary>
        /// deprecated: transition of screen
        /// </summary>
        [SerializeField]
        TransitionType m_DefaultTransition;
        /// <summary>
        /// Indicates if the screen is visible
        /// </summary>
        bool m_Visible;
        /// <summary>
        /// Screen update interval in seconds
        /// </summary>
        float m_UpdateInterval;
        /// <summary>
        /// Screen update interval timer
        /// </summary>
        float m_UpdateTimer;
        /// <summary>
        /// The screen that had focus right before this screen
        /// </summary>
        EGRScreen m_PrefocusedScreen;
        /// <summary>
        /// deprecated: Screen transition
        /// </summary>
        protected EGRTransition m_Transition;
        /// <summary>
        /// Gets called when the screen gets hidden
        /// </summary>
        Action m_HiddenCallback;
        /// <summary>
        /// Number of tweens that has finished playing
        /// </summary>
        int m_TweensFinished;
        /// <summary>
        /// Expected number of running tweens
        /// </summary>
        int m_TotalTweens;
        /// <summary>
        /// Last stored graphics buffer
        /// </summary>
        protected Gfx[] m_LastGraphicsBuf;
        /// <summary>
        /// Indicates if the tween callback has been called
        /// </summary>
        bool m_TweenCallbackCalled;
        /// <summary>
        /// Percent at which tween callback should be called, 0-1
        /// </summary>
        float m_TweenCallbackSensitivity;
        /// <summary>
        /// Saved graphic states mask
        /// </summary>
        EGRGfxState m_SavedGfxState;
        /// <summary>
        /// Stored graphic states
        /// </summary>
        Dictionary<Gfx, GfxState> m_GfxStates;
        /// <summary>
        /// Time at which the very first tween has started playing
        /// </summary>
        float m_TweenStart;
        /// <summary>
        /// Maximum time length of a tween playing
        /// </summary>
        float m_MaxTweenLength;

        /// <summary>
        /// The screen manager
        /// </summary>
        protected EGRScreenManager Manager => EGRScreenManager.Instance;
        /// <summary>
        /// Layer of screen
        /// </summary>
        public int Layer {
            get {
                return m_Layer;
            }

            set {
                m_Layer = value;
            }
        }
        /// <summary>
        /// Indicates if the screen can change the status bar color
        /// </summary>
        public virtual bool CanChangeBar => false;
        /// <summary>
        /// Status bar color in ARGB
        /// </summary>
        public virtual uint BarColor => 0x00000000u;
        /// <summary>
        /// Name of screen
        /// </summary>
        public string ScreenName => m_ScreenName;
        /// <summary>
        /// Indicates if the screen is visible
        /// </summary>
        public bool Visible => m_Visible;
        /// <summary>
        /// Indicates if any tween is playing
        /// </summary>
        public bool IsTweening => Time.time < m_TweenStart + m_MaxTweenLength;
        /// <summary>
        /// Initial screen position in world space
        /// </summary>
        public Vector3 OriginalPosition { get; private set; }
        /// <summary>
        /// Proxy storage of data passed along a proxy pipe
        /// </summary>
        public Dictionary<int, object> ProxyInterface { get; private set; }
        /// <summary>
        /// The message box
        /// </summary>
        public EGRPopupMessageBox MessageBox => Manager.GetPopup<EGRPopupMessageBox>();

        /// <summary>
        /// Late initialization
        /// </summary>
        void Start() {
            //store initial position
            OriginalPosition = transform.position;

            ProxyInterface = new Dictionary<int, object>(); //strategical place :)
            m_GfxStates = new Dictionary<Gfx, GfxState>();

            //register our screen
            Manager.AddScreen(m_ScreenName, this);
            //disable our screen
            gameObject.SetActive(false);

            //notch/safe area fixups
            RectTransform rectTransform = transform as RectTransform;
            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            //call init method
            OnScreenInit();
        }

        /// <summary>
        /// Called at every frame
        /// </summary>
        void Update() {
            //skip update if update timer is -1
            if (m_UpdateInterval == -1f)
                return;

            m_UpdateTimer += Time.deltaTime;
            if (m_UpdateTimer >= m_UpdateInterval) {
                //call screen update method
                OnScreenUpdate();
                //reset timer
                m_UpdateTimer = 0f;
            }
        }

        /// <summary>
        /// called when a screen gets destoryed
        /// </summary>
        void OnDestroy() {
            //call screen destroy method
            OnScreenDestroy();
        }

        /// <summary>
        /// Notifies the screen about the tween length, and limits it if needed
        /// </summary>
        /// <param name="time">Length</param>
        /// <returns></returns>
        protected float TweenMonitored(float time) {
            //limit time to max 0.65s
            time = Mathf.Min(0.65f, time);
            m_MaxTweenLength = Mathf.Max(m_MaxTweenLength, time);
            return time;
        }

        /// <summary>
        /// Stores current graphic states
        /// </summary>
        /// <param name="state">State mask</param>
        protected void PushGfxState(EGRGfxState state) {
            //clear old states
            m_GfxStates.Clear();

            foreach (Gfx gfx in m_LastGraphicsBuf) {
                PushGfxStateManual(gfx, state);
            }

            m_SavedGfxState = state;
        }

        /// <summary>
        /// Stores a graphic state manually
        /// </summary>
        /// <param name="gfx">The graphic</param>
        /// <param name="state">State mask</param>
        public void PushGfxStateManual(Gfx gfx, EGRGfxState state) {
            //create a new graphic state with mask of ALL
            m_GfxStates[gfx] = new GfxState {
                Mask = EGRGfxState.Color | EGRGfxState.Position | EGRGfxState.LocalPosition
            };

            if ((state & EGRGfxState.Position) == EGRGfxState.Position) {
                m_GfxStates[gfx].Position = gfx.transform.position;
            }

            if ((state & EGRGfxState.LocalPosition) == EGRGfxState.LocalPosition) {
                m_GfxStates[gfx].Position = gfx.transform.localPosition;
            }

            if ((state & EGRGfxState.Color) == EGRGfxState.Color) {
                m_GfxStates[gfx].Color = gfx.color;
            }
        }

        /// <summary>
        /// Set an already stored graphic state's mask
        /// </summary>
        /// <param name="gfx">The graphic</param>
        /// <param name="mask">State mask</param>
        protected void SetGfxStateMask(Gfx gfx, EGRGfxState mask) {
            m_GfxStates[gfx].Mask = mask;
        }

        /// <summary>
        /// Restores all saved graphic states
        /// </summary>
        protected void PopGfxState() {
            //skip if there was no graphic states saved
            if (m_SavedGfxState == EGRGfxState.None)
                return;

            foreach (Gfx gfx in m_LastGraphicsBuf) {
                //newly added graphic, position should be added in a seperate buffer
                if (!m_GfxStates.ContainsKey(gfx)) {
                    m_GfxStates[gfx] = new GfxState {
                        Color = Color.white
                    };
                }

                GfxState gState = m_GfxStates[gfx];

                if ((m_SavedGfxState & EGRGfxState.Position) == EGRGfxState.Position) {
                    if ((gState.Mask & EGRGfxState.Position) == EGRGfxState.Position) {
                        gfx.transform.position = gState.Position;
                    }
                }

                if ((m_SavedGfxState & EGRGfxState.LocalPosition) == EGRGfxState.LocalPosition) {
                    if ((gState.Mask & EGRGfxState.LocalPosition) == EGRGfxState.LocalPosition) {
                        gfx.transform.localPosition = gState.Position;
                    }
                }

                if ((m_SavedGfxState & EGRGfxState.Color) == EGRGfxState.Color) {
                    if ((gState.Mask & EGRGfxState.Color) == EGRGfxState.Color) {
                        gfx.color = gState.Color;
                    }
                }
            }

            //set back to none
            m_SavedGfxState = EGRGfxState.None;
        }

        /// <summary>
        /// Shows the screen
        /// </summary>
        /// <param name="prefocused">Prefocused screen</param>
        /// <param name="killTweens">Should tweens get killed?</param>
        public void ShowScreen(EGRScreen prefocused = null, bool killTweens = true) {
            //skip if screen is already shown
            if (m_Visible)
                return;

            //assign prefocused screen
            m_PrefocusedScreen = prefocused;
            //mark screen as visible
            m_Visible = true;

            //kill existing tweens if applicable
            if (killTweens && IsTweening && m_LastGraphicsBuf != null) {
                foreach (Gfx gfx in m_LastGraphicsBuf) {
                    DOTween.Kill(gfx, true);
                    DOTween.Kill(gfx.transform, true);
                }
            }

            //enable the screen
            gameObject.SetActive(true);

            //call show method
            OnScreenShow();
            //call show animation method
            OnScreenShowAnim();

            //send a universal event notifying that our screen has been shown
            EGREventManager.Instance.BroadcastEvent<EGREventScreenShown>(new EGREventScreenShown(this));
            //set contextual color of our screen
            EGRMain.SetContextualColor(this);
        }

        /// <summary>
        /// Last step of hiding a screen
        /// </summary>
        void InternalHideScreen() {
            //disable the screen
            gameObject.SetActive(false);
            //call hide method
            OnScreenHide();

            //restore graphics if needed
            if (m_LastGraphicsBuf != null) {
                PopGfxState();
            }

            //reset
            m_TweenCallbackCalled = false;

            //send a universal event notifying that our screen has been hidden
            EGREventManager.Instance.BroadcastEvent<EGREventScreenHidden>(new EGREventScreenHidden(this));
        }

        /// <summary>
        /// Hides the screen
        /// </summary>
        /// <param name="callback">Hidden callback</param>
        /// <param name="sensitivity">Tween sensitivity</param>
        /// <param name="killTweens">Should tweens get killed?</param>
        public void HideScreen(Action callback = null, float sensitivity = 0.1f, bool killTweens = false) {
            //skip if screen is already hidden
            if (!m_Visible)
                return;

            //mark as hidden
            m_Visible = false;
            //assign tween callback sensitivity
            m_TweenCallbackSensitivity = sensitivity;

            //kill tweens if applicable
            if (killTweens && IsTweening && m_LastGraphicsBuf != null) {
                foreach (Gfx gfx in m_LastGraphicsBuf) {
                    DOTween.Kill(gfx, true);
                    DOTween.Kill(gfx.transform, true);
                }
            }

            //call hide animation method
            //if does not exist, screen is hidden immediately
            if (!OnScreenHideAnim(callback)) {
                InternalHideScreen();
                m_HiddenCallback?.Invoke();
            }

            if (m_PrefocusedScreen != null) {
                //change status bar color to prefocused screen
                EGRMain.SetContextualColor(m_PrefocusedScreen);
                m_PrefocusedScreen = null;
            }
        }

        /// <summary>
        /// Forcefully hides a screen
        /// </summary>
        public void ForceHideScreen(bool ignoreVis = false) {
            //skip if screen is already hidden 
            if (!m_Visible && !ignoreVis)
                return;

            //mark as hidden
            m_Visible = false;

            //kill tweens if applicable
            if (IsTweening && m_LastGraphicsBuf != null) {
                foreach (Gfx gfx in m_LastGraphicsBuf) {
                    DOTween.Kill(gfx, true);
                    DOTween.Kill(gfx.transform, true);
                }
            }

            //hide!
            InternalHideScreen();
        }

        /// <summary>
        /// Moves the screen to the top-most layer
        /// </summary>
        public void MoveToFront() {
            EGRScreenManager.Instance.MoveScreenOnTop(this);
        }

        /// <summary>
        /// Gets an element in order of default canvas child, transform child and scene object respectively
        /// </summary>
        /// <typeparam name="T">Type of element</typeparam>
        /// <param name="name">Name of element</param>
        /// <returns></returns>
        protected T GetElement<T>(string name) where T : MonoBehaviour {
            if (name.StartsWith("/EGRDefaultCanvas/")) {
                name = name.Substring(18 + gameObject.name.Length + 1);
            }

            foreach (T t in transform.GetComponentsInChildren<T>()) {
                if (t.name == name) {
                    return t;
                }
            }

            Transform trans = transform.Find(name);
            if (trans != null)
                return trans.GetComponent<T>();

            GameObject go = GameObject.Find(name);
            if (go != null)
                return go.GetComponent<T>();

            return null;
        }

        /// <summary>
        /// Gets a transform in order of default canvas child, transform child and scene object respectively
        /// </summary>
        /// <param name="name">Name of transform</param>
        /// <returns></returns>
        protected Transform GetTransform(string name) {
            if (name.StartsWith("/EGRDefaultCanvas/")) {
                name = name.Substring(18 + gameObject.name.Length + 1);
            }

            for (int i = 0; i < transform.childCount; i++) {
                Transform t = transform.GetChild(i);
                if (t.name == name)
                    return t;
            }

            Transform trans = transform.Find(name);
            if (trans != null)
                return trans;

            GameObject go = GameObject.Find(name);
            if (go != null)
                return go.transform;

            return null;
        }

        /// <summary>
        /// Sets the screen update interval
        /// </summary>
        /// <param name="interval">The interval</param>
        protected void SetUpdateInterval(float interval) {
            m_UpdateInterval = interval;
            m_UpdateTimer = 0f;
        }

        /// <summary>
        /// Sets the expected playing tween count
        /// </summary>
        /// <param name="tweens">Number of tweens</param>
        protected void SetTweenCount(int tweens) {
            m_TotalTweens = tweens;
            m_TweensFinished = 0;
        }

        /// <summary>
        /// Gets called when a tween has finished playing
        /// </summary>
        protected void OnTweenFinished() {
            m_TweensFinished++;

            if ((m_TweensFinished / (float)m_TotalTweens) >= m_TweenCallbackSensitivity) {
                if (!m_TweenCallbackCalled) {
                    m_TweenCallbackCalled = true;
                    m_HiddenCallback?.Invoke();
                }
            }

            if (m_TweensFinished >= m_TotalTweens) {
                InternalHideScreen();

                if (!m_TweenCallbackCalled)
                    m_HiddenCallback?.Invoke();
            }
        }

        /// <summary>
        /// Gets called when a screen is initialized
        /// </summary>
        protected virtual void OnScreenInit() {
        }

        /// <summary>
        /// Gets called when a screen is shown
        /// </summary>
        protected virtual void OnScreenShow() {
        }

        /// <summary>
        /// Gets called when a screen is hidden
        /// </summary>
        protected virtual void OnScreenHide() {
        }

        /// <summary>
        /// Gets called when a screen is updated
        /// </summary>
        protected virtual void OnScreenUpdate() {
        }

        /// <summary>
        /// Gets called when a screen is destroyed
        /// </summary>
        protected virtual void OnScreenDestroy() {
        }

        /// <summary>
        /// Gets called when a screen hide animation should start playing
        /// </summary>
        /// <param name="callback">Hidden callback</param>
        /// <returns>Whether an animation will play or not</returns>
        protected virtual bool OnScreenHideAnim(Action callback) {
            m_HiddenCallback = callback;
            m_TweenStart = Time.time;

            return false;
        }

        /// <summary>
        /// Gets called when a screen show animation should start playing
        /// </summary>
        protected virtual void OnScreenShowAnim() {
            m_TweenStart = Time.time;
        }
    }
}