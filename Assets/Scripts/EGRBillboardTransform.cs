using DG.Tweening;
using UnityEngine;

namespace MRK {
    /// <summary>
    /// Keeps a transform billboarded to the currently active camera
    /// </summary>
    public class EGRBillboardTransform : MRKBehaviour {
        /// <summary>
        /// Indicates whether the look rotation should become inversed
        /// </summary>
        [SerializeField]
        bool m_Inverse = true;
        /// <summary>
        /// Indicates whether the transform must be kept at a fixed from the camera
        /// </summary>
        [SerializeField]
        bool m_FixedDistance = false;
        /// <summary>
        /// Fixed distance from camera
        /// </summary>
        [SerializeField]
        float m_Distance = 1000f;
        /// <summary>
        /// Rotation offset in euler angles
        /// </summary>
        [SerializeField]
        Vector3 m_Offset = Vector3.zero;
        /// <summary>
        /// Indicates whether transformations should be smoothed out
        /// </summary>
        [SerializeField]
        bool m_Smooth;
        /// <summary>
        /// Time taken for a smooth transformation to finish
        /// </summary>
        [SerializeField]
        float m_SmoothTime;
        /// <summary>
        /// Last look rotation
        /// </summary>
        Vector3 m_LastLookRot;
        /// <summary>
        /// Last position
        /// </summary>
        Vector3 m_LastPos;
        /// <summary>
        /// Position tween ID
        /// </summary>
        int m_PosTween;
        /// <summary>
        /// Rotation tween ID
        /// </summary>
        int m_RotTween;
        /// <summary>
        /// Target rotation in quaternion
        /// </summary>
        Quaternion m_TargetRotation;

        /// <summary>
        /// Called every frame
        /// </summary>
        void Update() {
            //positon transform appropriately
            if (m_FixedDistance) {
                //translated (distance multiplied by perspective forward direction) units and (perspective upwards multiplied by distance/2) units
                Vector3 pos = Client.ActiveCamera.transform.position + Client.ActiveCamera.transform.forward * m_Distance 
                    + Client.ActiveCamera.transform.up * m_Distance / 2f;
                //skip if position has not changed
                if (m_LastPos == pos)
                    return;

                m_LastPos = pos;

                if (m_Smooth) {
                    //kill old tween if still playing
                    if (m_PosTween.IsValidTween())
                        DOTween.Kill(m_PosTween);

                    //ease position
                    m_PosTween = transform.DOMove(pos, m_SmoothTime)
                        .SetEase(Ease.OutBack)
                        .intId = EGRTweenIDs.IntId;
                }
                else
                    transform.position = pos;
            }

            //calculate look rotation
            Vector3 lookRot = Client.ActiveCamera.transform.position - transform.position;
            if (m_Inverse)
                lookRot *= -1f;

            //skip if object has not rotated
            if (m_LastLookRot == lookRot)
                return;

            m_LastLookRot = lookRot;

            //calculate the quaternion
            Quaternion rot = Quaternion.LookRotation(lookRot);
            //offset our euler angles
            rot.eulerAngles += m_Offset;

            if (m_Smooth) {
                if (m_TargetRotation != rot) {
                    m_TargetRotation = rot;

                    //kill old tween if still playing
                    if (m_RotTween.IsValidTween()) {
                        DOTween.Kill(m_RotTween);
                    }

                    //ease rotation
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
