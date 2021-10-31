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
        GameObject m_Halo;

        public static EGRPlanet Sun { get; private set; }

        public EGRPlanetType PlanetType => m_PlanetType;

        void Awake() {
            if (m_PlanetType == EGRPlanetType.Sun) {
                Sun = this;
            }

            m_Halo = transform.Find("Halo").gameObject;
        }

        void Start() {
            //random move
            if (m_PlanetType == EGRPlanetType.Sun || m_PlanetType == EGRPlanetType.Earth)
                return;

            //rotate a random value around sun INITIALLY
            transform.RotateAround(Sun.transform.position, Vector3.up, UnityEngine.Random.value * 360f);
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

        public void SetHaloActiveState(bool active) {
            m_Halo.SetActive(active);
        }
    }
}
