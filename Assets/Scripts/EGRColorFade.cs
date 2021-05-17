using UnityEngine;

namespace MRK {
    class EGRColorFade {
        Color m_Initial;
        Color m_Final;
        float m_Delta;
        float m_Speed;

        public bool Done => m_Delta >= 1f;// || Current == m_Final;
        public Color Current { get; private set; }
        public float Delta => m_Delta;

        public EGRColorFade(Color i, Color f, float s = 1f) {
            m_Initial = i;
            m_Final = f;
            m_Delta = 0f;
            m_Speed = s;
        }

        public void Update() {
            m_Delta += Time.deltaTime * m_Speed;
            m_Delta = Mathf.Clamp01(m_Delta);
            Current = Color.Lerp(m_Initial, m_Final, m_Delta);
        }

        public void Reset() {
            m_Delta = 0f;
        }
    }
}
