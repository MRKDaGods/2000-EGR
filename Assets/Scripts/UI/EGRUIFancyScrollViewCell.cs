﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace MRK.UI {
    class EGRUIFancyScrollViewCell : FancyCell<EGRUIFancyScrollViewItemData, EGRUIFancyScrollViewContext> {
        [SerializeField] 
        Animator m_Animator;
        [SerializeField] 
        TextMeshProUGUI m_Text;
        [SerializeField] 
        Image m_Background;
        [SerializeField]
        Button m_Button;
        float m_CurrentPosition;

        static class AnimatorHash {
            public static readonly int Scroll = Animator.StringToHash("scroll");
        }

        public override void Initialize() {
            m_Button.onClick.AddListener(() => Context.OnCellClicked?.Invoke(Index));
            
            m_Background.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 
                ((RectTransform)transform).rect.height);
        }

        public override void UpdateContent(EGRUIFancyScrollViewItemData itemData) {
            m_Text.text = itemData.Message;

            var selected = Context.SelectedIndex == Index;
            m_Background.color = selected
                ? new Color32(255, 0, 0, 100)
                : new Color32(0, 0, 0, 77);
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