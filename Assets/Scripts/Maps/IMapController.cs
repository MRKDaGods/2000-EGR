using UnityEngine;

namespace MRK.Maps
{
    public interface IMapController
    {
        public Vector3 MapRotation
        {
            get;
        }

        public Vector3 GetMapVelocity();
        public void SetCenterAndZoom(Vector2d? targetCenter, float? targetZoom);
    }
}
