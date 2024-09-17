using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace MRK.UI
{
    internal class FancyScrollViewCell : FancyCell<FancyScrollViewItemData, FancyScrollViewContext>
    {
        private static class AnimatorHash
        {
            public static readonly int Scroll = Animator.StringToHash("scroll");
        }

        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private RuntimeAnimatorController _vlController;
        [SerializeField]
        private RuntimeAnimatorController _vrController;
        [SerializeField]
        private RuntimeAnimatorController _hController;
        [SerializeField]
        private TextMeshProUGUI _text;
        [SerializeField]
        private Image _background;
        [SerializeField]
        private Button _button;
        [SerializeField]
        private bool _shouldResize = true;
        private float _currentPosition;

        public override void Initialize()
        {
            _animator.runtimeAnimatorController = Context.Scroll.Direction == FancyScrollViewDirection.Horizontal ? _hController :
                Context.Scroll.Placement == FancyScrollViewPlacement.Left ? _vlController : _vrController;

            _button.onClick.AddListener(() => Context.OnCellClicked?.Invoke(Index));

            if (_shouldResize)
            {
                if (Context.Scroll.Direction == FancyScrollViewDirection.Horizontal)
                {
                    _background.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                        ((RectTransform)transform).rect.height);
                }
                else
                {
                    _background.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                        ((RectTransform)transform).rect.width);
                }
            }
        }

        public override void UpdateContent(FancyScrollViewItemData itemData)
        {
            _text.text = itemData.Text;

            var selected = Context.SelectedIndex == Index;

            _background.DOColor(selected ? Color.white : Color.black.AlterAlpha(0.5f), 0.3f)
                .SetEase(Ease.OutSine);

            //m_Background.color = selected ? Color.white : Color.black.AlterAlpha(0.5f);
        }

        public override void UpdatePosition(float position)
        {
            _currentPosition = position;

            if (_animator.isActiveAndEnabled)
            {
                _animator.Play(AnimatorHash.Scroll, -1, position);
            }

            _animator.speed = 0;
        }

        private void OnEnable()
        {
            UpdatePosition(_currentPosition);
        }
    }
}
