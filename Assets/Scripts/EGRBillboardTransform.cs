using DG.Tweening;
using UnityEngine;

namespace MRK {
    public class EGRBillboardTransform : EGRBehaviour {
        [SerializeField]
        bool m_Inverse = true;
        [SerializeField]
        bool m_FixedDistance = false;
        [SerializeField]
        float m_Distance = 1000f;
        [SerializeField]
        Vector3 m_Offset = Vector3.zero;
        [SerializeField]
        bool m_Smooth;
        [SerializeField]
        float m_SmoothTime;
        Vector3 m_LastLookRot;
        Vector3 m_LastPos;
        object m_PosTween;
        object m_RotTween;

        void Update() {
            if (m_FixedDistance) {
                Vector3 pos = Client.ActiveCamera.transform.position + Client.ActiveCamera.transform.forward * m_Distance + Client.ActiveCamera.transform.up * m_Distance / 2f;
                if (m_LastPos == pos)
                    return;

                if (m_Smooth) {
                    if (m_PosTween != null)
                        DOTween.Kill(m_PosTween);

                    m_PosTween = transform.DOMove(pos, m_SmoothTime)
                        .SetEase(Ease.OutBack);
                }
                else
                    transform.position = pos;
            }

            Vector3 lookRot = Client.ActiveCamera.transform.position - transform.position;
            if (m_Inverse)
                lookRot *= -1f;

            if (m_LastLookRot == lookRot)
                return;

            Quaternion rot = Quaternion.LookRotation(lookRot);
            rot.eulerAngles += m_Offset;

            if (m_Smooth) {
                if (m_RotTween != null)
                    DOTween.Kill(m_RotTween);

                m_RotTween = transform.DORotateQuaternion(rot, m_SmoothTime)
                    .SetEase(Ease.OutBack);
            }
            else
                transform.rotation = rot;
        }
    }
}
