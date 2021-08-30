using UnityEngine;

namespace MRK {
    public class EGRAnimationDeviation : EGRBehaviour {
        [SerializeField]
        Vector3 m_DeviationTop;
        [SerializeField]
        Vector3 m_DeviationBottom;
        [SerializeField]
        float m_Speed;
        Vector3 m_Position;
        bool m_Direction;

        void Start() {
            m_Position = transform.localPosition;
            m_Direction = true;
        }

        void OnDisable() {
            transform.localPosition = m_Position;
        }

        void LateUpdate() {
            Vector3 dev = m_Direction ? m_DeviationTop : m_DeviationBottom;
            float adv = (m_Direction ? 1f : -1f) * m_Speed * Time.deltaTime;
            transform.localPosition += dev.ToCoefficientVector() * adv;

            //order of OPs yxz
            Vector3 subject = m_Direction ? transform.localPosition : m_Position;
            Vector3 sec = m_Direction ? m_Position : transform.localPosition;
            if (subject.y - sec.y > dev.y
                || subject.x - sec.x > dev.x
                || subject.z - sec.z > dev.z) {
                m_Direction = !m_Direction;
            }
        }
    }
}
