namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.NETINFO)]
    public class PacketInRetrieveNetInfo : Packet {
        public int CDNPort { get; private set; }
        public string CDNKey { get; private set; }

        public PacketInRetrieveNetInfo() : base(PacketNature.In, PacketType.NETINFO) {
        }

        public override void Read(PacketDataStream stream) {
            CDNPort = stream.ReadInt32();
            CDNKey = stream.ReadString();
        }
    }
}
