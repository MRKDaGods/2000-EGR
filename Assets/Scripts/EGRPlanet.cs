using System;
using UnityEngine;

namespace MRK {
    public enum EGRPlanetType {
        None,
        Sun,
        Mercury,
        Venus,
        Earth,
        Mars,
        Jupiter,
        Saturn,
        Uranus,
        Neptune
    }

    public class EGRPlanet : MRKBehaviour {
        [SerializeField]
        EGRPlanetType m_PlanetType;
        [SerializeField]
        float m_RotationSpeed;

        public static EGRPlanet Sun { get; private set; }

        void Awake() {
            if (m_PlanetType == EGRPlanetType.Sun) {
                Sun = this;
            }
        }

        void Update() {
            if (m_PlanetType == EGRPlanetType.Sun)
                return;

            transform.RotateAround(Sun.transform.position, Vector3.up, m_RotationSpeed * Time.deltaTime);
        }

        void OnValidate() {
            EGRPlanetType pt;
            if (Enum.TryParse(name, out pt)) {
                m_PlanetType = pt;
            }
        }
    }
}
