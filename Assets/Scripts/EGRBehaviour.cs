using MRK.UI;
using UnityEngine;

namespace MRK {
    public class EGRBehaviour : MonoBehaviour {
        public EGRMain Client => EGRMain.Instance;
        public EGRScreenManager ScreenManager => EGRScreenManager.Instance;
        public EGREventManager EventManager => EGREventManager.Instance;
    }
}
