namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.XKEY)]
    public class PacketInXorKey : Packet {
        public string XorKey { get; private set; }

        public PacketInXorKey() : base(PacketNature.In, PacketType.XKEY) {
        }

        public override void Read(PacketDataStream stream) {
            XorKey = stream.ReadString();
        }
    }
}