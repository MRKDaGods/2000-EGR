using MRK.Navigation;
using UnityEngine;
using System;

namespace MRK {
    public class EGRRuntimeConfiguration : MonoBehaviour {
        [Serializable]
        public class GlobeSetup {
            public float UnfocusedOffset;
            public float FlatTransitionOffset;
            public float TransitionRotationLength = 1.8f;
            public float TransitionZoomOutLength = 0.5f;
            public float TransitionZoomInLength = 1.2f;
        }

        public GameObject EarthGlobe;
        public MRKMap FlatMap;
        public EGRCameraGeneral GeneralCamera;
        public EGRNavigationManager NavigationManager;
        public ParticleSystem EnvironmentEmitter;

        public Vector2d LocationSimulatorCenter;
        public GlobeSetup GlobeSettings;
    }
}
