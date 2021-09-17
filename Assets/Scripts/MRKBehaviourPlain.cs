using MRK.Networking;
using MRK.UI;

namespace MRK {
    public class MRKBehaviourPlain {
        public EGRMain Client => EGRMain.Instance;
        public EGRNetworkingClient NetworkingClient => Client.NetworkingClient;
        public EGRScreenManager ScreenManager => EGRScreenManager.Instance;
        public EGREventManager EventManager => EGREventManager.Instance;
    }
}
