using MRK.Events;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MRK.UI.MapInterface
{
    public enum MapButtonGroupAlignment
    {
        BottomLeft,
        BottomRight,
        BottomCenter
    }

    public class MapButtonGroup : BaseBehaviour
    {
        [SerializeField]
        private MapButtonGroupAlignment _groupAlignment;
        [SerializeField]
        private bool _customLayout = false;
        [SerializeField]
        private MapButtonEffectorType _effectorType;
        [SerializeField]
        private float _idleDistanceFactorV = 0.2f;
        [SerializeField]
        private float _expandedDistanceFactorV = 0.5f; //0.5 means distance=0.5 * Screen.height
        [SerializeField]
        private float _idleDistanceFactorH = 0.2f;
        [SerializeField]
        private float _expandedDistanceFactorH = 0.5f; //0.5 means distance=0.5 * Screen.height
        [SerializeField]
        private float _idleAlpha = 0.5f;
        [SerializeField]
        private float _expandedAlpha = 1f;
        [SerializeField]
        private float _expansionTimeout = 3f;
        [SerializeField]
        private BaseBehaviour _buttonPrefab;
        private CanvasGroup _canvasGroup;
        private readonly ObjectPool<MapButton> _buttonPool;
        private Reference<bool> _lastCancellationReference;

        private static MapButtons _mapButtons;

        public MapButtonGroupAlignment GroupAlignment
        {
            get
            {
                return _groupAlignment;
            }
        }

        public bool Expanded
        {
            get; private set;
        }

        private static MapButtons MapButtons
        {
            get
            {
                return _mapButtons ??= ScreenManager.Instance.MapInterface.Components.MapButtons;
            }
        }

        public MapButtonGroup()
        {
            _buttonPool = new ObjectPool<MapButton>(() =>
            {
                BaseBehaviour newButton = Instantiate(_buttonPrefab, transform);
                newButton.gameObject.SetActive(true);
                return new MapButton(newButton, this);
            }, false, OnFreeButton);
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            _buttonPrefab.gameObject.SetActive(false);
            EventManager.Register<AppInitialized>(OnAppInitialized);
        }

        private void OnAppInitialized(AppInitialized evt)
        {
            //SetExpanded(false);
            EventManager.Unregister<AppInitialized>(OnAppInitialized);
        }

        public void SetExpanded(bool expanded, bool force = false)
        {
            if (Expanded == expanded && !force)
                return;

            Expanded = expanded;

            float oldCanvasAlpha = _canvasGroup.alpha;
            float targetCanvasAlpha = expanded ? _expandedAlpha : _idleAlpha;
            Tweener.Tween(0.6f, (progress) =>
            {
                _canvasGroup.alpha = Mathf.Lerp(oldCanvasAlpha, targetCanvasAlpha, progress);
            });

            if (!_customLayout)
            {
                Rect canvasRect = ((RectTransform)ScreenManager.GetLayer(ScreenManager.MapInterface).transform).rect;

                //vertical
                {
                    //distance = -canvasHeight * (1 - factor)
                    float factor = expanded ? _expandedDistanceFactorV : _idleDistanceFactorV;
                    float canvasHeight = canvasRect.height;
                    float distance = -canvasHeight * (1f - factor);

                    Vector2 offsetMax = rectTransform.offsetMax;
                    offsetMax.y = distance;

                    Vector2 oldOffsetMax = rectTransform.offsetMax;

                    Tweener.Tween(0.4f, (progress) =>
                    {
                        rectTransform.offsetMax = Vector2.Lerp(oldOffsetMax, offsetMax, progress);
                    });
                }

                //horizontal
                {
                    //distance = -factor * canvasWidth
                    float factor = expanded ? _expandedDistanceFactorH : _idleDistanceFactorH;
                    float canvasWidth = canvasRect.width;
                    float distance = -factor * canvasWidth;

                    Vector2 offsetMin = rectTransform.offsetMin;
                    offsetMin.x = distance;

                    Vector2 oldOffsetMin = rectTransform.offsetMin;

                    Tweener.Tween(0.4f, (progress) =>
                    {
                        rectTransform.offsetMin = Vector2.Lerp(oldOffsetMin, offsetMin, progress);
                    }, expanded ? null : (Action)UpdateButtonTextActiveState); //Unity throws a weird error if not casted
                }
            }

            if (Expanded /*|| m_CustomLayout*/)
            {
                UpdateButtonTextActiveState();
            }

            EventManager.BroadcastEvent(new UIMapButtonGroupExpansionStateChanged(this, expanded));

            if (Expanded)
            {
                Reference<bool> cancellationReference = ReferencePool<bool>.Default.Rent();
                Client.Runnable.RunLater(() => ScheduledExpansionClose(cancellationReference), _expansionTimeout);

                _lastCancellationReference = cancellationReference;
            }
            else
            {
                if (_lastCancellationReference != null)
                {
                    _lastCancellationReference.Value = true;
                }

                _lastCancellationReference = null;
            }
        }

        private void ScheduledExpansionClose(Reference<bool> cancellationReference)
        {
            if (!cancellationReference.Value)
            {
                SetExpanded(false);
            }

            ReferencePool<bool>.Default.Free(cancellationReference);
        }

        private void UpdateButtonTextActiveState()
        {
            if (_buttonPool.ActiveCount > 0)
            {
                foreach (MapButton button in _buttonPool.ActiveObjects)
                {
                    button.SetTextActive(Expanded);
                }
            }
        }

        public void NotifyChildButtonClicked(MapButtonID buttonID)
        {
            SetExpanded(!Expanded);

            //callback
            if (!Expanded)
            {
                MapButtonCallbacksRegistry.Global[buttonID]();
            }
            else
            {
                //notify other groups that we've been expanded, so they can shrink
                MapButtons.ShrinkOtherGroups(this);
            }
        }

        public void SetButtons(HashSet<MapButtonID> buttons)
        {
            SetExpanded(false, true);

            //free all?
            _buttonPool.FreeAll();

            if (buttons != null && buttons.Count > 0)
            {
                foreach (MapButtonID id in buttons)
                {
                    AddButton(id, true);
                }
            }
        }

        public void AddButton(MapButtonID id, bool noCheck = false, bool checkState = false, bool expand = false)
        {
            //check if exists
            if (!noCheck && HasButton(id, out _))
            {
                return;
            }

            MapButtonInfo buttonInfo = MapButtons.GetButtonInfo(id);
            MapButton button = _buttonPool.Rent();
            button.Initialize(buttonInfo, GetNewEffector(), _buttonPool.ActiveCount - 1);
            button.Behaviour.gameObject.SetActive(true);

            //handle event incase the pooled button had diff changes
            button.Effector.OnParentGroupExpansionStateChanged(new UIMapButtonGroupExpansionStateChanged(this, Expanded));

            if (checkState)
            {
                button.SetTextActive(Expanded);
            }

            if (expand)
            {
                SetExpanded(true);
            }
        }

        public void RemoveButton(MapButtonID id)
        {
            MapButton button;
            if (HasButton(id, out button))
            {
                _buttonPool.Free(button);
            }
        }

        public bool HasButton(MapButtonID id, out MapButton button)
        {
            button = null;

            if (_buttonPool.ActiveCount > 0)
            {
                button = _buttonPool.ActiveObjects.Find(x => x.Info.ID == id);
                return button != null;
            }

            return false;
        }

        private void OnFreeButton(MapButton button)
        {
            button.Effector.Destroy();
            FreeEffector(button.Effector);
            button.Behaviour.gameObject.SetActive(false);
        }

        public void OnParentComponentShow()
        {
            SetExpanded(false, true);
        }

        public void OnParentComponentHide()
        {
            _buttonPool.FreeAll();
            SetExpanded(false);
        }

        private MapButtonEffector GetNewEffector()
        {
            switch (_effectorType)
            {
                case MapButtonEffectorType.Default:
                    return ObjectPool<MapButtonEffector>.Default.Rent();

                case MapButtonEffectorType.Centered:
                    return ObjectPool<MapButtonEffectorCentered>.Default.Rent();
            }

            return null;
        }

        private void FreeEffector(MapButtonEffector effector)
        {
            switch (effector.EffectorType)
            {
                case MapButtonEffectorType.Default:
                    ObjectPool<MapButtonEffector>.Default.Free(effector);
                    break;

                case MapButtonEffectorType.Centered:
                    ObjectPool<MapButtonEffectorCentered>.Default.Free((MapButtonEffectorCentered)effector);
                    break;
            }
        }
    }
}
