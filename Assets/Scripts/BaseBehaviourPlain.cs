using MRK.Networking;
using MRK.UI;

namespace MRK
{
    public class BaseBehaviourPlain
    {
        public static EGR Client
        {
            get
            {
                return EGR.Instance;
            }
        }

        public EGRNetworkingClient NetworkingClient
        {
            get
            {
                return Client.NetworkingClient;
            }
        }

        public ScreenManager ScreenManager
        {
            get
            {
                return ScreenManager.Instance;
            }
        }

        public EGREventManager EventManager
        {
            get
            {
                return EGREventManager.Instance;
            }
        }
    }
}
