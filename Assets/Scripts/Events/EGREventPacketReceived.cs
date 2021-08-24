using MRK.Networking;
using MRK.Networking.Packets;

namespace MRK {
    public class EGREventPacketReceived : EGREvent {
        public override EGREventType EventType => EGREventType.PacketReceived;
        public EGRNetwork Network { get; private set; }
        public Packet Packet { get; private set; }

        public EGREventPacketReceived() {
        }

        public EGREventPacketReceived(EGRNetwork network, Packet packet) {
            Network = network;
            Packet = packet;
        }
    }
}
