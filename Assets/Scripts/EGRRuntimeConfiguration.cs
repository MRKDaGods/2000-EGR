using UnityEngine;

namespace MRK {
    public class EGRRuntimeConfiguration : MonoBehaviour {
        public GameObject EarthGlobe;
        public MRKMap FlatMap;
        public EGRCameraGeneral GeneralCamera;
        public EGRNavigationManager NavigationManager;
        public EGRLocationService LocationService;
        public ParticleSystem EnvironmentEmitter;

        public Vector2d LocationSimulatorCenter;
    }
}
