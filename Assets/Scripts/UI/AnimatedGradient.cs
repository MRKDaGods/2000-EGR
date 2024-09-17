using Coffee.UIEffects;
using UnityEngine;

namespace MRK.UI
{
    [RequireComponent(typeof(UIGradient))]
    public class AnimatedGradient : BaseBehaviour
    {
        private UIGradient _gradient;
        private float _angle;
        private int _lowColorIdx;
        private int _highColorIdx;
        private float _offset;
        private float _progress;
        [SerializeField]
        private float _speed = 1f;
        [SerializeField]
        private bool _animateRotation;
        [SerializeField]
        private bool _animateColors;

        private static readonly Color[] _colorSequence;

        static AnimatedGradient()
        {
            _colorSequence = new Color[] {
                new Color(1f, 0f, 0f),
                new Color(1f, 1f, 0f),
                new Color(0f, 1f, 0f),
                new Color(0f, 1f, 1f),
                new Color(0f, 0f, 1f),
                new Color(1f, 0f, 1f)
            };
        }

        private void Start()
        {
            _gradient = GetComponent<UIGradient>();
            //m_Gradient.direction = UIGradient.Direction.Angle;
            _angle = _gradient.rotation;
            _offset = -1f;
            _lowColorIdx = 0;
            _highColorIdx = 1;

            if (_animateColors)
            {
                UpdateColors();
            }
        }

        private void Update()
        {
            _progress += Time.deltaTime * _speed;
            if (_animateColors)
            {
                if (_progress >= 1f)
                {
                    _progress = 0f;

                    _lowColorIdx = _highColorIdx;
                    _highColorIdx = (_highColorIdx + 1) % _colorSequence.Length;

                    //m_Angle = -180f;
                    _offset = -1f;

                    UpdateColors();
                }

                _offset = Mathf.Lerp(-1f, 1f, _progress);
                _gradient.offset = _offset;
            }

            if (_animateRotation)
            {
                _angle += Time.deltaTime * _speed;
                if (_angle >= 180f)
                    _angle -= 360f;

                _gradient.rotation = _angle;
            }
        }

        private void UpdateColors()
        {
            _gradient.color2 = _colorSequence[_lowColorIdx];
            _gradient.color1 = _colorSequence[_highColorIdx];
        }
    }
}
