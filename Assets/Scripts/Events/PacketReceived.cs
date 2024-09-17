using MRK.Networking;
using MRK.Networking.Packets;

namespace MRK.Events
{
    public class PacketReceived : Event
    {
        public override EventType EventType => EventType.PacketReceived;

        public EGRNetwork Network
        {
            get; private set;
        }

        public Packet Packet
        {
            get; private set;
        }

        public PacketReceived()
        {
        }

        public PacketReceived(EGRNetwork network, Packet packet)
        {
            Network = network;
            Packet = packet;
        }
    }
}
