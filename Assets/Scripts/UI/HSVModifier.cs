using Coffee.UIEffects;
using UnityEngine;

namespace MRK.UI
{
    [RequireComponent(typeof(UIHsvModifier))]
    public class HSVModifier : BaseBehaviour
    {
        private UIHsvModifier _modifier;
        private float _animDelta;

        private void Start()
        {
            _modifier = GetComponent<UIHsvModifier>();
        }

        private void Update()
        {
            _animDelta += Time.deltaTime * 0.2f;
            if (_animDelta > 0.5f)
            {
                _animDelta = -0.5f;
            }

            _modifier.hue = _animDelta;
        }
    }
}
