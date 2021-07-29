using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using DG.Tweening;

namespace MRK.UI {
    class EGRUIFancyScrollViewCell : FancyCell<EGRUIFancyScrollViewItemData, EGRUIFancyScrollViewContext> {
        static class AnimatorHash {
            public static readonly int Scroll = Animator.StringToHash("scroll");
        }

        [SerializeField]
        Animator m_Animator;
        [SerializeField]
        RuntimeAnimatorController m_VController;
        [SerializeField]
        RuntimeAnimatorController m_HController;
        [SerializeField]
        TextMeshProUGUI m_Text;
        [SerializeField] 
        Image m_Background;
        [SerializeField]
        Button m_Button;
        [SerializeField]
        bool m_ShouldResize = true;
        float m_CurrentPosition;

        public override void Initialize() {
            m_Animator.runtimeAnimatorController = Context.Scroll.Direction == EGRUIFancyScrollViewDirection.Horizontal ? m_HController : m_VController;

            m_Button.onClick.AddListener(() => Context.OnCellClicked?.Invoke(Index));

            if (m_ShouldResize) {
                if (Context.Scroll.Direction == EGRUIFancyScrollViewDirection.Horizontal) {
                    m_Background.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                        ((RectTransform)transform).rect.height);
                }
                else {
                    m_Background.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                        ((RectTransform)transform).rect.width);
                }
            }
        }

        public override void UpdateContent(EGRUIFancyScrollViewItemData itemData) {
            m_Text.text = itemData.Text;

            var selected = Context.SelectedIndex == Index;

            m_Background.DOColor(selected ? Color.white : Color.black.AlterAlpha(0.5f), 0.3f)
                .SetEase(Ease.OutSine);

            //m_Background.color = selected ? Color.white : Color.black.AlterAlpha(0.5f);
        }

        public override void UpdatePosition(float position) {
            m_CurrentPosition = position;

            if (m_Animator.isActiveAndEnabled) {
                m_Animator.Play(AnimatorHash.Scroll, -1, position);
            }

            m_Animator.speed = 0;
        }

        void OnEnable() {
            UpdatePosition(m_CurrentPosition);
        }
    }
}
