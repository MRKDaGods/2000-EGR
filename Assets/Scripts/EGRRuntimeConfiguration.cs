using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public class EGRRuntimeConfiguration : MRKBehaviour {
        public GameObject EarthGlobe;
        public MRKMap FlatMap;
        public EGRCameraGeneral GeneralCamera;
        public EGRNavigationManager NavigationManager;
        public EGRLocationService LocationService;
        public ParticleSystem EnvironmentEmitter;
    }
}
