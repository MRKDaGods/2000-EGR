using UnityEngine;

namespace MRK {
    public class EGRLocationServiceSimulator : MonoBehaviour {
        [SerializeField]
        bool m_LocationEnabled = true;

        [SerializeField]
        Vector2d m_Coords;

        [SerializeField, Range(0f, 360f)]
        float m_Bearing;

        public bool LocationEnabled => m_LocationEnabled;
        public Vector2d Coords => m_Coords;
        public float Bearing => m_Bearing;
    }
}
