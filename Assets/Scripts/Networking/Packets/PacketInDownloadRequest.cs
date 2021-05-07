namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.DWNLDREQ)]
    public class PacketInDownloadRequest : Packet {
        public ulong ID { get; private set; }
        public int Sections { get; private set; }

        public PacketInDownloadRequest() : base(PacketNature.In, PacketType.DWNLDREQ) {
        }

        public override void Read(PacketDataStream stream) {
            ID = stream.ReadUInt64();
            Sections = stream.ReadInt32();
        }
    }
}