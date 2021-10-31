using UnityEngine;

namespace MRK.UI {
    [RequireComponent(typeof(RectTransform))]
    public class EGRUIUsableReference : MRKBehaviour {
        [SerializeField]
        EGRUIUsable m_UsableRef;
        bool m_Initialized;

        public EGRUIUsable Usable { get; private set; }

        void Start() {
            if (m_Initialized) {
                return;
            }

            Usable = m_UsableRef.Get();
            Usable.transform.SetParent(transform);

            RectTransform rt = Usable.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            m_Initialized = true;
        }

        public void InitializeIfNeeded() {
            if (m_Initialized)
                return;

            Start();
        }

        public EGRUIUsable GetUsableIntitialized() {
            InitializeIfNeeded();
            return Usable;
        }
    }
}
