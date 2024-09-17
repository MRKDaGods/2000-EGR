using UnityEngine;

namespace MRK.UI.Usables
{
    [RequireComponent(typeof(RectTransform))]
    public class Reference : BaseBehaviour
    {
        [SerializeField]
        private Usable _usableRef;
        private bool _initialized;

        public Usable Usable
        {
            get; private set;
        }

        private void Start()
        {
            if (_initialized)
            {
                return;
            }

            Usable = _usableRef.Get();
            Usable.transform.SetParent(transform);

            RectTransform rt = Usable.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _initialized = true;
        }

        public void InitializeIfNeeded()
        {
            if (_initialized)
                return;

            Start();
        }

        public Usable GetUsableIntitialized()
        {
            InitializeIfNeeded();
            return Usable;
        }
    }
}
