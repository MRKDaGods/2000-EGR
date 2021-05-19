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
        int m_PosTween;
        int m_RotTween;
        Quaternion m_TargetRotation;

        void Update() {
            if (m_FixedDistance) {
                Vector3 pos = Client.ActiveCamera.transform.position + Client.ActiveCamera.transform.forward * m_Distance + Client.ActiveCamera.transform.up * m_Distance / 2f;
                if (m_LastPos == pos)
                    return;

                m_LastPos = pos;

                if (m_Smooth) {
                    if (m_PosTween.IsValidTween())
                        DOTween.Kill(m_PosTween);

                    m_PosTween = transform.DOMove(pos, m_SmoothTime)
                        .SetEase(Ease.OutBack)
                        .intId = EGRTweenIDs.IntId;
                }
                else
                    transform.position = pos;
            }

            Vector3 lookRot = Client.ActiveCamera.transform.position - transform.position;
            if (m_Inverse)
                lookRot *= -1f;

            if (m_LastLookRot == lookRot)
                return;

            m_LastLookRot = lookRot;

            Quaternion rot = Quaternion.LookRotation(lookRot);
            rot.eulerAngles += m_Offset;

            if (m_Smooth) {
                if (m_TargetRotation != rot) {
                    m_TargetRotation = rot;

                    if (m_RotTween.IsValidTween()) {
                        DOTween.Kill(m_RotTween);
                    }

                    m_RotTween = transform.DORotateQuaternion(m_TargetRotation, m_SmoothTime)
                        .SetEase(Ease.OutBack)
                        .intId = EGRTweenIDs.IntId;
                }
            }
            else
                transform.rotation = rot;
        }
    }
}
