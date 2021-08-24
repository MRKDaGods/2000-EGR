namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.NETINFO)]
    public class PacketOutRetrieveNetInfo : Packet {
        public PacketOutRetrieveNetInfo() : base(PacketNature.Out, PacketType.NETINFO) {
        }
    }
}
