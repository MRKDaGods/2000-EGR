using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Coffee.UIEffects;

namespace MRK.UI {
    [RequireComponent(typeof(UIGradient))]
    public class EGRUIAnimatedGradient : EGRBehaviour {
        readonly static Color[] ms_ColorSequence;

        UIGradient m_Gradient;
        float m_Angle;
        int m_LowColorIdx;
        int m_HighColorIdx;
        float m_Offset;
        float m_Progress;
        [SerializeField]
        float m_Speed = 1f;
        [SerializeField]
        bool m_AnimateRotation;
        [SerializeField]
        bool m_AnimateColors;

        static EGRUIAnimatedGradient() {
            ms_ColorSequence = new Color[] { 
                new Color(1f, 0f, 0f),
                new Color(1f, 1f, 0f),
                new Color(0f, 1f, 0f),
                new Color(0f, 1f, 1f),
                new Color(0f, 0f, 1f),
                new Color(1f, 0f, 1f) 
            };
        }

        void Start() {
            m_Gradient = GetComponent<UIGradient>();
            //m_Gradient.direction = UIGradient.Direction.Angle;
            m_Angle = m_Gradient.rotation;
            m_Offset = -1f;
            m_LowColorIdx = 0;
            m_HighColorIdx = 1;

            if (m_AnimateColors) {
                UpdateColors();
            }
        }

        void Update() {
            m_Progress += Time.deltaTime * m_Speed;
            if (m_AnimateColors) {
                if (m_Progress >= 1f) {
                    m_Progress = 0f;

                    m_LowColorIdx = m_HighColorIdx;
                    m_HighColorIdx = (m_HighColorIdx + 1) % ms_ColorSequence.Length;

                    //m_Angle = -180f;
                    m_Offset = -1f;

                    UpdateColors();
                }

                m_Offset = Mathf.Lerp(-1f, 1f, m_Progress);
                m_Gradient.offset = m_Offset;
            }

            if (m_AnimateRotation) {
                m_Angle += Time.deltaTime * m_Speed;
                if (m_Angle >= 180f)
                    m_Angle -= 360f;

                m_Gradient.rotation = m_Angle;
            }
        }

        void UpdateColors() {
            m_Gradient.color2 = ms_ColorSequence[m_LowColorIdx];
            m_Gradient.color1 = ms_ColorSequence[m_HighColorIdx];
        }
    }
}
