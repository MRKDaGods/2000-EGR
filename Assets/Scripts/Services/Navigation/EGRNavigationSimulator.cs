using UnityEngine;

namespace MRK.Navigation {
    public class EGRNavigationSimulator : EGRNavigation {
        int m_CurrentPointIdx;
        float m_SimulatedTripPercentage;
        Vector3? m_LastForward;
        Vector3 m_LastCalculatedForward;

        protected override void Prepare() {
            m_SimulatedTripPercentage = 0f;
            m_CurrentPointIdx = 0;
        }

        public override void Update() {
            m_SimulatedTripPercentage += Time.deltaTime * 0.005f;
            m_SimulatedTripPercentage = Mathf.Clamp01(m_SimulatedTripPercentage);

            int pointIdx = Mathf.FloorToInt(m_SimulatedTripPercentage * m_Route.Geometry.Coordinates.Count);
            if (pointIdx >= m_Route.Geometry.Coordinates.Count - 1) {
                Debug.Log("Nav ended");
                return;
            }

            if (m_CurrentPointIdx != pointIdx) {
                m_CurrentPointIdx = pointIdx;

                m_LastForward = m_LastCalculatedForward;
            }

            Vector3 curPointPos = Client.FlatMap.GeoToWorldPosition(m_Route.Geometry.Coordinates[pointIdx]);
            Vector3 nextPointPos = Client.FlatMap.GeoToWorldPosition(m_Route.Geometry.Coordinates[pointIdx + 1]);

            float percPerPoint = 1f / m_Route.Geometry.Coordinates.Count;
            float subPer = (m_SimulatedTripPercentage - pointIdx * percPerPoint) / percPerPoint;

            Vector3 forward = nextPointPos - curPointPos;
            if (m_LastForward.HasValue)
                forward = Vector3.Lerp(m_LastForward.Value, forward, subPer / 0.2f);

            m_LastCalculatedForward = forward;

            Quaternion lookRotation = Quaternion.LookRotation(forward);
            NavigationManager.NavigationSprite.transform.rotation = Quaternion.Euler(lookRotation.eulerAngles - Quaternion.Euler(-90f, 0f, 0f).eulerAngles);

            Vector2d realGeoPos = Vector2d.Lerp(m_Route.Geometry.Coordinates[pointIdx], m_Route.Geometry.Coordinates[pointIdx + 1], subPer);
            Vector3 pos = Client.FlatMap.GeoToWorldPosition(realGeoPos);
            Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos);

            NavigationManager.NavigationSprite.transform.position = EGRPlaceMarker.ScreenToMarkerSpace(spos);

            Client.FlatCamera.SetCenterAndZoom(realGeoPos, 18f);
            Client.ActiveCamera.transform.rotation = Quaternion.Euler(lookRotation.eulerAngles + Quaternion.Euler(90f, 0f, 0f).eulerAngles);
            //Client.ActiveCamera.transform.position = (pos - Client.ActiveCamera.transform.position).normalized;
        }
    }
}
