using MRK.Networking;
using MRK.UI;
using UnityEngine;

namespace MRK {
    /// <summary>
    /// Base class of any behaviour that provides ease of access to EGR's main internals
    /// </summary>
    public class EGRBehaviour : MonoBehaviour {
        public EGRMain Client => EGRMain.Instance;
        public EGRNetworkingClient NetworkingClient => Client.NetworkingClient;
        public EGRScreenManager ScreenManager => EGRScreenManager.Instance;
        public EGREventManager EventManager => EGREventManager.Instance;
    }
}
