using MRK.Networking;

namespace MRK {
    public class EGREventNetworkConnected : EGREvent {
        public override EGREventType EventType => EGREventType.NetworkConnected;
        public EGRNetwork Network { get; private set; }

        public EGREventNetworkConnected() {
        }

        public EGREventNetworkConnected(EGRNetwork network) {
            Network = network;
        }
    }
}
