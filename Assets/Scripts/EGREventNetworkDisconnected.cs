using MRK.Networking;

namespace MRK {
    public class EGREventNetworkDisconnected : EGREvent {
        public override EGREventType EventType => EGREventType.NetworkDisconnected;
        public EGRNetwork Network { get; private set; }
        public DisconnectInfo DisconnectInfo { get; private set; }

        public EGREventNetworkDisconnected() {
        }

        public EGREventNetworkDisconnected(EGRNetwork network, DisconnectInfo disconnectInfo) {
            Network = network;
            DisconnectInfo = disconnectInfo;
        }
    }
}