using DG.Tweening;
using UnityEngine;

namespace MRK
{
    /// <summary>
    /// Keeps a transform billboarded to the currently active camera
    /// </summary>
    public class BillboardTransform : BaseBehaviour
    {
        /// <summary>
        /// Indicates whether the look rotation should become inversed
        /// </summary>
        [SerializeField]
        private bool _inverse = true;
        /// <summary>
        /// Indicates whether the transform must be kept at a fixed from the camera
        /// </summary>
        [SerializeField]
        private bool _fixedDistance = false;
        /// <summary>
        /// Fixed distance from camera
        /// </summary>
        [SerializeField]
        private float _distance = 1000f;
        /// <summary>
        /// Rotation offset in euler angles
        /// </summary>
        [SerializeField]
        private Vector3 _offset = Vector3.zero;
        /// <summary>
        /// Indicates whether transformations should be smoothed out
        /// </summary>
        [SerializeField]
        private bool _smooth;
        /// <summary>
        /// Time taken for a smooth transformation to finish
        /// </summary>
        [SerializeField]
        private float _smoothTime;

        /// <summary>
        /// Last look rotation
        /// </summary>
        private Vector3 _lastLookRot;

        /// <summary>
        /// Last position
        /// </summary>
        private Vector3 _lastPos;

        /// <summary>
        /// Position tween ID
        /// </summary>
        private int _posTween;

        /// <summary>
        /// Rotation tween ID
        /// </summary>
        private int _rotTween;

        /// <summary>
        /// Target rotation in quaternion
        /// </summary>
        private Quaternion _targetRotation;

        /// <summary>
        /// Called every frame
        /// </summary>
        private void Update()
        {
            //positon transform appropriately
            if (_fixedDistance)
            {
                //translated (distance multiplied by perspective forward direction) units and (perspective upwards multiplied by distance/2) units
                Vector3 pos = Client.ActiveCamera.transform.position + (Client.ActiveCamera.transform.forward * _distance)
                    + (Client.ActiveCamera.transform.up * _distance / 2f);

                //skip if position has not changed
                if (_lastPos == pos)
                {
                    return;
                }

                _lastPos = pos;

                if (_smooth)
                {
                    //kill old tween if still playing
                    if (_posTween.IsValidTween())
                    {
                        DOTween.Kill(_posTween);
                    }

                    //ease position
                    _posTween = transform.DOMove(pos, _smoothTime)
                        .SetEase(Ease.OutBack)
                        .intId = EGRTweenIDs.IntId;
                }
                else
                {
                    transform.position = pos;
                }
            }

            //calculate look rotation
            Vector3 lookRot = Client.ActiveCamera.transform.position - transform.position;
            if (_inverse)
            {
                lookRot *= -1f;
            }

            //skip if object has not rotated
            if (_lastLookRot == lookRot)
            {
                return;
            }

            _lastLookRot = lookRot;

            //calculate the quaternion
            Quaternion rot = Quaternion.LookRotation(lookRot);
            //offset our euler angles
            rot.eulerAngles += _offset;

            if (_smooth)
            {
                if (_targetRotation != rot)
                {
                    _targetRotation = rot;

                    //kill old tween if still playing
                    if (_rotTween.IsValidTween())
                    {
                        DOTween.Kill(_rotTween);
                    }

                    //ease rotation
                    _rotTween = transform.DORotateQuaternion(_targetRotation, _smoothTime)
                        .SetEase(Ease.OutBack)
                        .intId = EGRTweenIDs.IntId;
                }
            }
            else
            {
                transform.rotation = rot;
            }
        }
    }
}
