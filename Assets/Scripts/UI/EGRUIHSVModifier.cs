using Coffee.UIEffects;
using UnityEngine;

namespace MRK.UI {
    [RequireComponent(typeof(UIHsvModifier))]
    public class EGRUIHSVModifier : MRKBehaviour {
        UIHsvModifier m_Modifier;
        float m_AnimDelta;

        void Start() {
            m_Modifier = GetComponent<UIHsvModifier>();
        }

        void Update() {
            m_AnimDelta += Time.deltaTime * 0.2f;
            if (m_AnimDelta > 0.5f)
                m_AnimDelta = -0.5f;

            m_Modifier.hue = m_AnimDelta;
        }
    }
}
