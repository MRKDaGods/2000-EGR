using System;
using System.Collections.Generic;
using UnityEngine;

namespace MRK.UI.MapInterface {
    public enum EGRUIMapButtonsGroupAlignment {
        BottomLeft,
        BottomRight,
        BottomCenter
    }

    public class EGRUIMapButtonsGroup : MRKBehaviour {
        [SerializeField]
        EGRUIMapButtonsGroupAlignment m_GroupAlignment;
        [SerializeField]
        bool m_CustomLayout = false;
        [SerializeField]
        EGRUIMapButtonEffectorType m_EffectorType;
        [SerializeField]
        float m_IdleDistanceFactorV = 0.2f;
        [SerializeField]
        float m_ExpandedDistanceFactorV = 0.5f; //0.5 means distance=0.5 * Screen.height
        [SerializeField]
        float m_IdleDistanceFactorH = 0.2f;
        [SerializeField]
        float m_ExpandedDistanceFactorH = 0.5f; //0.5 means distance=0.5 * Screen.height
        [SerializeField]
        float m_IdleAlpha = 0.5f;
        [SerializeField]
        float m_ExpandedAlpha = 1f;
        [SerializeField]
        float m_ExpansionTimeout = 3f;
        [SerializeField]
        MRKBehaviour m_ButtonPrefab;
        CanvasGroup m_CanvasGroup;
        readonly ObjectPool<EGRUIMapButton> m_ButtonPool;
        Reference<bool> m_LastCancellationReference;

        static EGRMapInterfaceComponentMapButtons ms_MapButtons;

        public EGRUIMapButtonsGroupAlignment GroupAlignment => m_GroupAlignment;
        public bool Expanded { get; private set; }

        static EGRMapInterfaceComponentMapButtons MapButtons => ms_MapButtons ??= EGRScreenManager.Instance.MapInterface.Components.MapButtons;

        public EGRUIMapButtonsGroup() {
            m_ButtonPool = new ObjectPool<EGRUIMapButton>(() => {
                MRKBehaviour newButton = Instantiate(m_ButtonPrefab, transform);
                newButton.gameObject.SetActive(true);
                return new EGRUIMapButton(newButton, this);
            }, false, OnFreeButton);
        }

        void Awake() {
            m_CanvasGroup = GetComponent<CanvasGroup>();
        }

        void Start() {
            m_ButtonPrefab.gameObject.SetActive(false);
            EventManager.Register<EGREventAppInitialized>(OnAppInitialized);
        }

        void OnAppInitialized(EGREventAppInitialized evt) {
            //SetExpanded(false);
            EventManager.Unregister<EGREventAppInitialized>(OnAppInitialized);
        }

        public void SetExpanded(bool expanded, bool force = false) {
            if (Expanded == expanded && !force)
                return;

            Expanded = expanded;

            float oldCanvasAlpha = m_CanvasGroup.alpha;
            float targetCanvasAlpha = expanded ? m_ExpandedAlpha : m_IdleAlpha;
            MRKTweener.Tween(0.6f, (progress) => {
                m_CanvasGroup.alpha = Mathf.Lerp(oldCanvasAlpha, targetCanvasAlpha, progress);
            });

            if (!m_CustomLayout) {
                Rect canvasRect = ((RectTransform)ScreenManager.GetLayer(ScreenManager.MapInterface).transform).rect;

                //vertical
                {
                    //distance = -canvasHeight * (1 - factor)
                    float factor = expanded ? m_ExpandedDistanceFactorV : m_IdleDistanceFactorV;
                    float canvasHeight = canvasRect.height;
                    float distance = -canvasHeight * (1f - factor);

                    Vector2 offsetMax = rectTransform.offsetMax;
                    offsetMax.y = distance;

                    Vector2 oldOffsetMax = rectTransform.offsetMax;

                    MRKTweener.Tween(0.4f, (progress) => {
                        rectTransform.offsetMax = Vector2.Lerp(oldOffsetMax, offsetMax, progress);
                    });
                }

                //horizontal
                {
                    //distance = -factor * canvasWidth
                    float factor = expanded ? m_ExpandedDistanceFactorH : m_IdleDistanceFactorH;
                    float canvasWidth = canvasRect.width;
                    float distance = -factor * canvasWidth;

                    Vector2 offsetMin = rectTransform.offsetMin;
                    offsetMin.x = distance;

                    Vector2 oldOffsetMin = rectTransform.offsetMin;

                    MRKTweener.Tween(0.4f, (progress) => {
                        rectTransform.offsetMin = Vector2.Lerp(oldOffsetMin, offsetMin, progress);
                    }, expanded ? null : (Action)UpdateButtonTextActiveState); //Unity throws a weird error if not casted
                }
            }

            if (Expanded /*|| m_CustomLayout*/) {
                UpdateButtonTextActiveState();
            }

            EventManager.BroadcastEvent(new EGREventUIMapButtonGroupExpansionStateChanged(this, expanded));

            if (Expanded) {
                Reference<bool> cancellationReference = ReferencePool<bool>.Default.Rent();
                Client.Runnable.RunLater(() => ScheduledExpansionClose(cancellationReference), m_ExpansionTimeout);

                m_LastCancellationReference = cancellationReference;
            }
            else {
                if (m_LastCancellationReference != null) {
                    m_LastCancellationReference.Value = true;
                }

                m_LastCancellationReference = null;
            }
        }

        void ScheduledExpansionClose(Reference<bool> cancellationReference) {
            if (!cancellationReference.Value) {
                SetExpanded(false);
            }

            ReferencePool<bool>.Default.Free(cancellationReference);
        }

        void UpdateButtonTextActiveState() {
            if (m_ButtonPool.ActiveCount > 0) {
                foreach (EGRUIMapButton button in m_ButtonPool.ActiveObjects) {
                    button.SetTextActive(Expanded);
                }
            }
        }

        public void NotifyChildButtonClicked(EGRUIMapButtonID buttonID) {
            SetExpanded(!Expanded);

            //callback
            if (!Expanded) {
                MRKRegistryUIMapButtonCallbacks.Global[buttonID]();
            }
            else {
                //notify other groups that we've been expanded, so they can shrink
                MapButtons.ShrinkOtherGroups(this);
            }
        }

        public void SetButtons(HashSet<EGRUIMapButtonID> buttons) {
            SetExpanded(false, true);

            //free all?
            m_ButtonPool.FreeAll();

            if (buttons != null && buttons.Count > 0) {
                foreach (EGRUIMapButtonID id in buttons) {
                    AddButton(id, true);
                }
            }
        }

        public void AddButton(EGRUIMapButtonID id, bool noCheck = false, bool checkState = false, bool expand = false) {
            //check if exists
            if (!noCheck && HasButton(id, out _)) {
                return;
            }

            EGRUIMapButtonInfo buttonInfo = MapButtons.GetButtonInfo(id);
            EGRUIMapButton button = m_ButtonPool.Rent();
            button.Initialize(buttonInfo, GetNewEffector(), m_ButtonPool.ActiveCount - 1);
            button.Behaviour.gameObject.SetActive(true);

            //handle event incase the pooled button had diff changes
            button.Effector.OnParentGroupExpansionStateChanged(new EGREventUIMapButtonGroupExpansionStateChanged(this, Expanded));

            if (checkState) {
                button.SetTextActive(Expanded);
            }

            if (expand) {
                SetExpanded(true);
            }
        }

        public void RemoveButton(EGRUIMapButtonID id) {
            EGRUIMapButton button;
            if (HasButton(id, out button)) {
                m_ButtonPool.Free(button);
            }
        }

        public bool HasButton(EGRUIMapButtonID id, out EGRUIMapButton button) {
            button = null;

            if (m_ButtonPool.ActiveCount > 0) {
                button = m_ButtonPool.ActiveObjects.Find(x => x.Info.ID == id);
                return button != null;
            }

            return false;
        }

        void OnFreeButton(EGRUIMapButton button) {
            button.Effector.Destroy();
            FreeEffector(button.Effector);
            button.Behaviour.gameObject.SetActive(false);
        }

        public void OnParentComponentShow() {
            SetExpanded(false, true);
        }

        public void OnParentComponentHide() {
            m_ButtonPool.FreeAll();
            SetExpanded(false);
        }

        EGRUIMapButtonEffector GetNewEffector() {
            switch (m_EffectorType) {
                case EGRUIMapButtonEffectorType.Default:
                    return ObjectPool<EGRUIMapButtonEffector>.Default.Rent();

                case EGRUIMapButtonEffectorType.Centered:
                    return ObjectPool<EGRUIMapButtonEffectorCentered>.Default.Rent();
            }

            return null;
        }

        void FreeEffector(EGRUIMapButtonEffector effector) {
            switch (effector.EffectorType) {
                case EGRUIMapButtonEffectorType.Default:
                    ObjectPool<EGRUIMapButtonEffector>.Default.Free(effector);
                    break;

                case EGRUIMapButtonEffectorType.Centered:
                    ObjectPool<EGRUIMapButtonEffectorCentered>.Default.Free((EGRUIMapButtonEffectorCentered)effector);
                    break;
            }
        }
    }
}
