using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public enum EGRLocationError {
        None,
        NotEnabled,
        TimeOut
    }

    public class EGRLocationService {
        bool m_Initialized;

        public EGRLocationError LastError { get; private set; }

        public IEnumerator Initialize() {
            LastError = EGRLocationError.None;

            if (!Input.location.isEnabledByUser) {
                LastError = EGRLocationError.NotEnabled;
                yield break;
            }


        }

        public Vector2d GetCurrentLocation() {
            LocationInfo info = Input.location.lastData;
            return new Vector2d(info.latitude, info.longitude);
        }
    }
}
