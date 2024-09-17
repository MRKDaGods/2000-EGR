using DG.Tweening;
using MRK.Events;
using MRK.UI.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using Gfx = UnityEngine.UI.Graphic;
using UScreen = UnityEngine.Screen;

namespace MRK.UI
{
    /// <summary>
    /// Graphic state flags
    /// </summary>
    public enum GfxStates
    {
        None = 0,
        Position = 1,
        Color = 2,
        LocalPosition = 4
    }

    /// <summary>
    /// Base class for all screens
    /// </summary>
    public class Screen : BaseBehaviour
    {
        /// <summary>
        /// Holds information about a graphic state
        /// </summary>
        private class GfxState
        {
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
            public GfxStates Mask;
        }

        /// <summary>
        /// Name of screen
        /// </summary>
        [SerializeField]
        protected string _screenName;
        /// <summary>
        /// Layer of screen
        /// </summary>
        [SerializeField]
        private int _layer;

        /// <summary>
        /// Indicates if the screen is visible
        /// </summary>
        private bool _visible;

        /// <summary>
        /// Screen update interval in seconds
        /// </summary>
        private float _updateInterval;

        /// <summary>
        /// Screen update interval timer
        /// </summary>
        private float _updateTimer;

        /// <summary>
        /// The screen that had focus right before this screen
        /// </summary>
        private Screen _prefocusedScreen;

        /// <summary>
        /// Gets called when the screen gets hidden
        /// </summary>
        private Action _hiddenCallback;

        /// <summary>
        /// Number of tweens that has finished playing
        /// </summary>
        private int _tweensFinished;

        /// <summary>
        /// Expected number of running tweens
        /// </summary>
        private int _totalTweens;
        /// <summary>
        /// Last stored graphics buffer
        /// </summary>
        protected Gfx[] _lastGraphicsBuf;

        /// <summary>
        /// Indicates if the tween callback has been called
        /// </summary>
        private bool _tweenCallbackCalled;

        /// <summary>
        /// Percent at which tween callback should be called, 0-1
        /// </summary>
        private float _tweenCallbackSensitivity;

        /// <summary>
        /// Saved graphic states mask
        /// </summary>
        private GfxStates _savedGfxState;

        /// <summary>
        /// Stored graphic states
        /// </summary>
        private Dictionary<Gfx, GfxState> _gfxStates;

        /// <summary>
        /// Time at which the very first tween has started playing
        /// </summary>
        private float _tweenStart;

        /// <summary>
        /// Maximum time length of a tween playing
        /// </summary>
        private float _maxTweenLength;
        [SerializeField]
        private List<UIAttribute> _attributes;

        /// <summary>
        /// Layer of screen
        /// </summary>
        public int Layer
        {
            get
            {
                return _layer;
            }

            set
            {
                _layer = value;
            }
        }

        /// <summary>
        /// Indicates if the screen can change the status bar color
        /// </summary>
        public virtual bool CanChangeBar
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Status bar color in ARGB
        /// </summary>
        public virtual uint BarColor
        {
            get
            {
                return 0x00000000u;
            }
        }

        /// <summary>
        /// Name of screen
        /// </summary>
        public string ScreenName
        {
            get
            {
                return _screenName;
            }
        }

        /// <summary>
        /// Indicates if the screen is visible
        /// </summary>
        public bool Visible
        {
            get
            {
                return _visible;
            }
        }

        /// <summary>
        /// Indicates if any tween is playing
        /// </summary>
        public bool IsTweening
        {
            get
            {
                return Time.time < _tweenStart + _maxTweenLength;
            }
        }

        /// <summary>
        /// Initial screen position in world space
        /// </summary>
        public Vector3 OriginalPosition
        {
            get; private set;
        }

        /// <summary>
        /// Proxy storage of data passed along a proxy pipe
        /// </summary>
        public Dictionary<int, object> ProxyInterface
        {
            get; private set;
        }

        /// <summary>
        /// The message box
        /// </summary>
        public MessageBox MessageBox
        {
            get
            {
                return ScreenManager.GetPopup<MessageBox>();
            }
        }

        public RectTransform Body
        {
            get; private set;
        }

        /// <summary>
        /// Late initialization
        /// </summary>
        private void Start()
        {
            //store initial position
            OriginalPosition = transform.position;

            ProxyInterface = new Dictionary<int, object>(); //strategical place :)
            _gfxStates = new Dictionary<Gfx, GfxState>();

            //register our screen
            ScreenManager.AddScreen(_screenName, this);
            //disable our screen
            gameObject.SetActive(false);

            //find body if exists
            foreach (UIAttribute attr in _attributes)
            {
                var _attr = attr.Get(UIAttributes.ContentType);
                if (_attr != null)
                {
                    if (_attr.Value == ContentType.Body)
                    {
                        Body = attr.rectTransform;
                        break;
                    }
                }
            }

            //notch/safe area fixups
            Rect safeArea = UScreen.safeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= UScreen.width;
            anchorMin.y /= UScreen.height;
            anchorMax.x /= UScreen.width;
            anchorMax.y /= UScreen.height;

            //RectTransform doesnt support ??
            RectTransform rectTransform = Body != null ? Body : base.rectTransform;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            //call init method
            OnScreenInit();
        }

        /// <summary>
        /// Called at every frame
        /// </summary>
        private void Update()
        {
            //skip update if update timer is -1
            if (_updateInterval == -1f)
                return;

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= _updateInterval)
            {
                //call screen update method
                OnScreenUpdate();
                //reset timer
                _updateTimer = 0f;
            }
        }

        /// <summary>
        /// called when a screen gets destoryed
        /// </summary>
        private void OnDestroy()
        {
            //call screen destroy method
            OnScreenDestroy();
        }

        /// <summary>
        /// Notifies the screen about the tween length, and limits it if needed
        /// </summary>
        /// <param name="time">Length</param>
        /// <returns></returns>
        protected float TweenMonitored(float time)
        {
            //limit time to max 0.65s
            time = Mathf.Min(0.65f, time);
            _maxTweenLength = Mathf.Max(_maxTweenLength, time);
            return time;
        }

        /// <summary>
        /// Stores current graphic states
        /// </summary>
        /// <param name="state">State mask</param>
        protected void PushGfxState(GfxStates state)
        {
            //clear old states
            _gfxStates.Clear();

            foreach (Gfx gfx in _lastGraphicsBuf)
            {
                PushGfxStateManual(gfx, state);
            }

            _savedGfxState = state;
        }

        /// <summary>
        /// Stores a graphic state manually
        /// </summary>
        /// <param name="gfx">The graphic</param>
        /// <param name="state">State mask</param>
        public void PushGfxStateManual(Gfx gfx, GfxStates state)
        {
            //create a new graphic state with mask of ALL
            _gfxStates[gfx] = new GfxState
            {
                Mask = GfxStates.Color | GfxStates.Position | GfxStates.LocalPosition
            };

            if ((state & GfxStates.Position) == GfxStates.Position)
            {
                _gfxStates[gfx].Position = gfx.transform.position;
            }

            if ((state & GfxStates.LocalPosition) == GfxStates.LocalPosition)
            {
                _gfxStates[gfx].Position = gfx.transform.localPosition;
            }

            if ((state & GfxStates.Color) == GfxStates.Color)
            {
                _gfxStates[gfx].Color = gfx.color;
            }
        }

        /// <summary>
        /// Set an already stored graphic state's mask
        /// </summary>
        /// <param name="gfx">The graphic</param>
        /// <param name="mask">State mask</param>
        protected void SetGfxStateMask(Gfx gfx, GfxStates mask)
        {
            _gfxStates[gfx].Mask = mask;
        }

        /// <summary>
        /// Restores all saved graphic states
        /// </summary>
        protected void PopGfxState()
        {
            //skip if there was no graphic states saved
            if (_savedGfxState == GfxStates.None)
                return;

            foreach (Gfx gfx in _lastGraphicsBuf)
            {
                //newly added graphic, position should be added in a seperate buffer
                if (!_gfxStates.ContainsKey(gfx))
                {
                    _gfxStates[gfx] = new GfxState
                    {
                        Color = Color.white
                    };
                }

                GfxState gState = _gfxStates[gfx];

                if ((_savedGfxState & GfxStates.Position) == GfxStates.Position)
                {
                    if ((gState.Mask & GfxStates.Position) == GfxStates.Position)
                    {
                        gfx.transform.position = gState.Position;
                    }
                }

                if ((_savedGfxState & GfxStates.LocalPosition) == GfxStates.LocalPosition)
                {
                    if ((gState.Mask & GfxStates.LocalPosition) == GfxStates.LocalPosition)
                    {
                        gfx.transform.localPosition = gState.Position;
                    }
                }

                if ((_savedGfxState & GfxStates.Color) == GfxStates.Color)
                {
                    if ((gState.Mask & GfxStates.Color) == GfxStates.Color)
                    {
                        gfx.color = gState.Color;
                    }
                }
            }

            //set back to none
            _savedGfxState = GfxStates.None;
        }

        /// <summary>
        /// Shows the screen
        /// </summary>
        /// <param name="prefocused">Prefocused screen</param>
        /// <param name="killTweens">Should tweens get killed?</param>
        public void ShowScreen(Screen prefocused = null, bool killTweens = true)
        {
            //skip if screen is already shown
            if (_visible)
                return;

            //assign prefocused screen
            _prefocusedScreen = prefocused;
            //mark screen as visible
            _visible = true;

            //kill existing tweens if applicable
            if (killTweens && IsTweening && _lastGraphicsBuf != null)
            {
                foreach (Gfx gfx in _lastGraphicsBuf)
                {
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
            EventManager.Instance.BroadcastEvent<ScreenShown>(new ScreenShown(this));
            //set contextual color of our screen
            EGR.SetContextualColor(this);
        }

        /// <summary>
        /// Last step of hiding a screen
        /// </summary>
        private void InternalHideScreen()
        {
            //disable the screen
            gameObject.SetActive(false);
            //call hide method
            OnScreenHide();

            //restore graphics if needed
            if (_lastGraphicsBuf != null)
            {
                PopGfxState();
            }

            //reset
            _tweenCallbackCalled = false;

            //send a universal event notifying that our screen has been hidden
            EventManager.Instance.BroadcastEvent(new ScreenHidden(this));
        }

        /// <summary>
        /// Hides the screen
        /// </summary>
        /// <param name="callback">Hidden callback</param>
        /// <param name="sensitivity">Tween sensitivity</param>
        /// <param name="killTweens">Should tweens get killed?</param>
        public void HideScreen(Action callback = null, float sensitivity = 0.1f, bool killTweens = false, bool immediateSensitivtyCheck = false)
        {
            //skip if screen is already hidden
            if (!_visible)
                return;

            //notify of hide request
            ScreenHideRequest req = new ScreenHideRequest(this);
            EventManager.Instance.BroadcastEvent(req);
            if (req.Cancelled)
            {
                return;
            }

            //mark as hidden
            _visible = false;
            //assign tween callback sensitivity
            _tweenCallbackSensitivity = sensitivity;

            //kill tweens if applicable
            if (killTweens && IsTweening && _lastGraphicsBuf != null)
            {
                foreach (Gfx gfx in _lastGraphicsBuf)
                {
                    DOTween.Kill(gfx, true);
                    DOTween.Kill(gfx.transform, true);
                }
            }

            //call hide animation method
            //if does not exist, screen is hidden immediately
            if (!OnScreenHideAnim(callback))
            {
                InternalHideScreen();
                _hiddenCallback?.Invoke();
            }
            else if (sensitivity == 0f && immediateSensitivtyCheck)
            {
                _tweenCallbackCalled = true;
                _hiddenCallback?.Invoke();
            }

            if (_prefocusedScreen != null)
            {
                //change status bar color to prefocused screen
                EGR.SetContextualColor(_prefocusedScreen);
                _prefocusedScreen = null;
            }
        }

        /// <summary>
        /// Forcefully hides a screen
        /// </summary>
        public void ForceHideScreen(bool ignoreVis = false)
        {
            //skip if screen is already hidden 
            if (!_visible && !ignoreVis)
                return;

            //mark as hidden
            _visible = false;

            //kill tweens if applicable
            if (IsTweening && _lastGraphicsBuf != null)
            {
                foreach (Gfx gfx in _lastGraphicsBuf)
                {
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
        public void MoveToFront()
        {
            ScreenManager.Instance.MoveScreenOnTop(this);
        }

        /// <summary>
        /// Gets an element in order of default canvas child, transform child and scene object respectively
        /// </summary>
        /// <typeparam name="T">Type of element</typeparam>
        /// <param name="name">Name of element</param>
        /// <returns></returns>
        protected T GetElement<T>(string name) where T : MonoBehaviour
        {
            if (name.StartsWith("/EGRDefaultCanvas/"))
            {
                name = name.Substring(18 + gameObject.name.Length + 1);
            }

            foreach (T t in transform.GetComponentsInChildren<T>())
            {
                if (t.name == name)
                {
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
        protected Transform GetTransform(string name)
        {
            if (name.StartsWith("/EGRDefaultCanvas/"))
            {
                name = name.Substring(18 + gameObject.name.Length + 1);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
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
        protected void SetUpdateInterval(float interval)
        {
            _updateInterval = interval;
            _updateTimer = 0f;
        }

        /// <summary>
        /// Sets the expected playing tween count
        /// </summary>
        /// <param name="tweens">Number of tweens</param>
        protected void SetTweenCount(int tweens)
        {
            _totalTweens = tweens;
            _tweensFinished = 0;
        }

        /// <summary>
        /// Gets called when a tween has finished playing
        /// </summary>
        protected void OnTweenFinished()
        {
            _tweensFinished++;

            if ((_tweensFinished / (float)_totalTweens) >= _tweenCallbackSensitivity)
            {
                if (!_tweenCallbackCalled)
                {
                    _tweenCallbackCalled = true;
                    _hiddenCallback?.Invoke();
                }
            }

            if (_tweensFinished >= _totalTweens)
            {
                InternalHideScreen();

                if (!_tweenCallbackCalled)
                    _hiddenCallback?.Invoke();
            }
        }

        /// <summary>
        /// Gets called when a screen is initialized
        /// </summary>
        protected virtual void OnScreenInit()
        {
        }

        /// <summary>
        /// Gets called when a screen is shown
        /// </summary>
        protected virtual void OnScreenShow()
        {
        }

        /// <summary>
        /// Gets called when a screen is hidden
        /// </summary>
        protected virtual void OnScreenHide()
        {
        }

        /// <summary>
        /// Gets called when a screen is updated
        /// </summary>
        protected virtual void OnScreenUpdate()
        {
        }

        /// <summary>
        /// Gets called when a screen is destroyed
        /// </summary>
        protected virtual void OnScreenDestroy()
        {
        }

        /// <summary>
        /// Gets called when a screen hide animation should start playing
        /// </summary>
        /// <param name="callback">Hidden callback</param>
        /// <returns>Whether an animation will play or not</returns>
        protected virtual bool OnScreenHideAnim(Action callback)
        {
            _hiddenCallback = callback;
            _tweenStart = Time.time;

            return false;
        }

        /// <summary>
        /// Gets called when a screen show animation should start playing
        /// </summary>
        protected virtual void OnScreenShowAnim()
        {
            _tweenStart = Time.time;
        }
    }
}