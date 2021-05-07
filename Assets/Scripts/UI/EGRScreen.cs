using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using Gfx = UnityEngine.UI.Graphic;

namespace MRK.UI {
    public enum EGRGfxState {
        None = 0,
        Position = 1,
        Color = 2
    }

    public class EGRScreen : EGRBehaviour {
        class GfxState {
            public Vector3 Position;
            public Color Color;
            public EGRGfxState Mask;
        }

        [SerializeField]
        protected string m_ScreenName;

        [SerializeField]
        int m_Layer;

        [SerializeField]
        TransitionType m_DefaultTransition;

        bool m_Visible;
        float m_UpdateInterval;
        float m_UpdateTimer;
        EGRScreen m_PrefocusedScreen;
        protected EGRTransition m_Transition;
        Action m_HiddenCallback;
        int m_TweensFinished;
        int m_TotalTweens;
        protected Color[] m_ColorBuf;
        protected Gfx[] m_LastGraphicsBuf;
        protected Dictionary<Gfx, Color> m_Colors;
        protected Dictionary<Gfx, Vector3> m_Positions;
        bool m_TweenCallbackCalled;
        float m_TweenCallbackSensitivity;
        EGRGfxState m_SavedGfxState;
        Dictionary<Gfx, GfxState> m_GfxStates;
        float m_TweenStart;
        float m_MaxTweenLength;

        protected EGRScreenManager Manager => EGRScreenManager.Instance;

        public int Layer {
            get {
                return m_Layer;
            }

            set {
                m_Layer = value;
            }
        }

        public virtual bool CanChangeBar => false;
        public virtual uint BarColor => 0x00000000u;

        public string ScreenName => m_ScreenName;
        public bool Visible => m_Visible;
        public bool IsTweening => Time.time < m_TweenStart + m_MaxTweenLength;

        public Vector3 OriginalPosition { get; private set; }
        public Dictionary<int, object> ProxyInterface { get; private set; }
        public EGRPopupMessageBox MessageBox => (EGRPopupMessageBox)Manager.GetScreen(EGRUI_Main.EGRPopup_MessageBox.SCREEN_NAME);

        void Start() {
            OriginalPosition = transform.position;
            ProxyInterface = new Dictionary<int, object>(); //strategical place :)
            m_Colors = new Dictionary<Gfx, Color>();
            m_Positions = new Dictionary<Gfx, Vector3>();
            m_GfxStates = new Dictionary<Gfx, GfxState>();
            Manager.AddScreen(m_ScreenName, this);
            gameObject.SetActive(false);

            //notch fixups
            /*RectTransform rectTransform = transform as RectTransform;
            Rect safeArea = Screen.safeArea;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, safeArea.height);
            rectTransform.position -= new Vector3(0f, safeArea.y); */

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

            OnScreenInit();
        }

        void Update() {
            if (m_UpdateTimer == -1f)
                return;

            m_UpdateTimer += Time.deltaTime;
            if (m_UpdateTimer >= m_UpdateInterval) {
                OnScreenUpdate();
                m_UpdateTimer = 0f;
            }

            if (m_Transition != null)
                m_Transition.OnUpdate();
        }

        void OnDestroy() {
            //if (Visible)
            //    HideScreen();

            OnScreenDestroy();
        }

        protected float TweenMonitored(float time) {
            time = Mathf.Min(0.65f, time);
            m_MaxTweenLength = Mathf.Max(m_MaxTweenLength, time);
            return time;
        }

        protected void PushGfxState(EGRGfxState state) {
            m_GfxStates.Clear();

            foreach (Gfx gfx in m_LastGraphicsBuf) {
                PushGfxStateManual(gfx, state);
            }

            m_SavedGfxState = state;
        }

        public void PushGfxStateManual(Gfx gfx, EGRGfxState state) {
            m_GfxStates[gfx] = new GfxState {
                Mask = EGRGfxState.Color | EGRGfxState.Position
            };

            if ((state & EGRGfxState.Position) == EGRGfxState.Position) {
                m_GfxStates[gfx].Position = gfx.transform.position;
            }

            if ((state & EGRGfxState.Color) == EGRGfxState.Color) {
                m_GfxStates[gfx].Color = gfx.color;
            }
        }

        protected void SetGfxStateMask(Gfx gfx, EGRGfxState mask) {
            m_GfxStates[gfx].Mask = mask;
        }

        protected void PopGfxState() {
            if (m_SavedGfxState == EGRGfxState.None)
                return;

            foreach (Gfx gfx in m_LastGraphicsBuf) {
                //newly added gfx, pos should be added in a sep buffer
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

                if ((m_SavedGfxState & EGRGfxState.Color) == EGRGfxState.Color) {
                    if ((gState.Mask & EGRGfxState.Color) == EGRGfxState.Color) {
                        gfx.color = gState.Color;
                    }
                }
            }

            m_SavedGfxState = EGRGfxState.None;
        }

        public void ShowScreen(EGRScreen prefocused = null, bool killTweens = true) {
            if (m_Visible)
                return;

            m_PrefocusedScreen = prefocused;

            m_Visible = true;

            if (killTweens && IsTweening && m_LastGraphicsBuf != null) {
                foreach (Gfx gfx in m_LastGraphicsBuf) {
                    DOTween.Kill(gfx, true);
                    DOTween.Kill(gfx.transform, true);
                }
            }

            gameObject.SetActive(true);
            m_Transition = EGRTransitionFactory.GetFreeTransition<EGRTransition>(m_DefaultTransition);
            m_Transition.SetTarget(transform);
            OnScreenShow();
            OnScreenShowAnim();
            m_Transition.OnShow();

            EGREventManager.Instance.BroadcastEvent<EGREventScreenShown>(new EGREventScreenShown(this));

            EGRMain.SetContextualColor(this);
        }

        void InternalHideScreen() {
            gameObject.SetActive(false);
            OnScreenHide();

            if (m_LastGraphicsBuf != null) {
                PopGfxState();
            }

            m_TweenCallbackCalled = false;

            EGREventManager.Instance.BroadcastEvent<EGREventScreenHidden>(new EGREventScreenHidden(this));
        }

        public void HideScreen(Action callback = null, float sensitivity = 0.1f, bool killTweens = false) {
            if (!m_Visible)
                return;

            m_Visible = false;

            m_TweenCallbackSensitivity = sensitivity;

            if (killTweens && IsTweening && m_LastGraphicsBuf != null) {
                foreach (Gfx gfx in m_LastGraphicsBuf) {
                    DOTween.Kill(gfx, true);
                    DOTween.Kill(gfx.transform, true);
                }
            }

            if (!OnScreenHideAnim(callback)) {
                InternalHideScreen();
                m_HiddenCallback?.Invoke();
            }

            if (m_Transition != null)
                m_Transition.Free = true;

            if (m_PrefocusedScreen != null) {
                //change bar col to prefocused
                EGRMain.SetContextualColor(m_PrefocusedScreen);
                m_PrefocusedScreen = null;
            }
        }

        public void ForceHideScreen() {
            if (!m_Visible)
                return;

            m_Visible = false;

            if (IsTweening && m_LastGraphicsBuf != null) {
                foreach (Gfx gfx in m_LastGraphicsBuf) {
                    DOTween.Kill(gfx, true);
                    DOTween.Kill(gfx.transform, true);
                }
            }

            InternalHideScreen();
        }

        public void MoveToFront() {
            EGRScreenManager.Instance.MoveScreenOnTop(this);
        }

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

        protected void SetUpdateInterval(float interval) {
            m_UpdateInterval = interval;
            m_UpdateTimer = 0f;
        }

        protected void SetTweenCount(int tweens) {
            m_TotalTweens = tweens;
            m_TweensFinished = 0;
        }

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

        protected virtual void OnScreenInit() {
        }

        protected virtual void OnScreenShow() {
        }

        protected virtual void OnScreenHide() {
        }

        protected virtual void OnScreenUpdate() {
        }

        protected virtual void OnScreenDestroy() {
        }

        //returns anim state
        protected virtual bool OnScreenHideAnim(Action callback) {
            m_HiddenCallback = callback;
            m_TweenStart = Time.time;

            return false;
        }

        protected virtual void OnScreenShowAnim() {
            m_TweenStart = Time.time;
        }
    }
}