using UnityEngine;

namespace MRK {
    public class EGRLocationServiceSimulator : MonoBehaviour {
        [SerializeField]
        Vector2d m_Coords;

        [SerializeField, Range(0f, 360f)]
        float m_Bearing;

        public Vector2d Coords => m_Coords;
        public float Bearing => m_Bearing;
    }
}
