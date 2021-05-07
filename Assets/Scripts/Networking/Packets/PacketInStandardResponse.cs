namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.STDRSP)]
    public class PacketInStandardResponse : Packet {
        public EGRStandardResponse Response { get; private set; }

        public PacketInStandardResponse() : this(EGRStandardResponse.NONE) {
        }

        public PacketInStandardResponse(EGRStandardResponse resp) : base(PacketNature.In, PacketType.STDRSP) {
            Response = resp;
        }

        public override void Read(PacketDataStream stream) {
            Response = (EGRStandardResponse)stream.ReadByte();
        }
    }
}