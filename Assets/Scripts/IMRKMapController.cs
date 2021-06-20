using UnityEngine;

namespace MRK {
    public interface IMRKMapController {
        public Vector2 MapRotation { get; }

        public Vector3 GetMapVelocity();
        public void SetCenterAndZoom(Vector2d targetCenter, float targetZoom);
    }
}
