using UnityEngine;

namespace MRK {
    public class EGRLocationServiceSimulator : MRKBehaviour {
        [SerializeField]
        bool m_LocationEnabled = true;
        [SerializeField]
        Vector2d m_Coords;
        [SerializeField, Range(0f, 360f)]
        float m_Bearing;
        [SerializeField]
        bool m_ListenToMouse;
        [SerializeField]
        bool m_ListenToKB;
        [SerializeField]
        float m_Speed = 1f;

        public bool LocationEnabled => m_LocationEnabled;
        public Vector2d Coords => m_Coords;
        public float Bearing => m_Bearing;

        void Update() {
            if (m_ListenToMouse && Input.GetMouseButtonDown(0)) {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = Client.ActiveCamera.transform.position.y;

                Vector3 wPos = Client.ActiveCamera.ScreenToWorldPoint(mousePos);
                m_Coords = Client.FlatMap.WorldToGeoPosition(wPos);

                Debug.Log($"Updated coords to {m_Coords}");
            }

            if (m_ListenToKB) {
                float horizontal = Input.GetAxis("Horizontal") * m_Speed;
                float vertical = Input.GetAxis("Vertical") * m_Speed;

                //assume that forward is +z (NORTH)
                Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(m_Coords);
                worldPos.x += horizontal;
                worldPos.z += vertical;

                m_Coords = Client.FlatMap.WorldToGeoPosition(worldPos);
            }
        }
    }
}
