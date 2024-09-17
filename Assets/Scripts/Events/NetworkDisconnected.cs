using MRK.Networking;

namespace MRK.Events
{
    public class NetworkDisconnected : Event
    {
        public override EventType EventType
        {
            get
            {
                return EventType.NetworkDisconnected;
            }
        }

        public EGRNetwork Network
        {
            get; private set;
        }

        public DisconnectInfo DisconnectInfo
        {
            get; private set;
        }

        public NetworkDisconnected()
        {
        }

        public NetworkDisconnected(EGRNetwork network, DisconnectInfo disconnectInfo)
        {
            Network = network;
            DisconnectInfo = disconnectInfo;
        }
    }
}