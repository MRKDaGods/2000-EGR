namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.STDJSONRSP)]
    public class PacketInStandardJSONResponse : Packet {
        public string Response { get; private set; }

        public PacketInStandardJSONResponse() : base(PacketNature.In, PacketType.STDJSONRSP) {
        }

        public override void Read(PacketDataStream stream) {
            Response = stream.ReadString();
        }
    }
}