using MRK.Networking;

namespace MRK.Events
{
    public class NetworkConnected : Event
    {
        public override EventType EventType
        {
            get
            {
                return EventType.NetworkConnected;
            }
        }

        public EGRNetwork Network
        {
            get; private set;
        }

        public NetworkConnected()
        {
        }

        public NetworkConnected(EGRNetwork network)
        {
            Network = network;
        }
    }
}
