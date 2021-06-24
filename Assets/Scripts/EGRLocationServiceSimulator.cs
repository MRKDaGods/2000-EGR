using UnityEngine;

namespace MRK {
    public class EGRLocationServiceSimulator : EGRBehaviour {
        [SerializeField]
        bool m_LocationEnabled = true;

        [SerializeField]
        Vector2d m_Coords;

        [SerializeField, Range(0f, 360f)]
        float m_Bearing;

        [SerializeField]
        bool m_ListenToMouse;

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
        }
    }
}
