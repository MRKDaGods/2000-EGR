namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.CDNRESOURCE)]
    public class PacketInRequestCDNResource : Packet {
        public EGRStandardResponse Response { get; private set; }
        public byte[] Resource { get; private set; }

        public PacketInRequestCDNResource() : base(PacketNature.In, PacketType.CDNRESOURCE) {
        }

        public override void Read(PacketDataStream stream) {
            Response = (EGRStandardResponse)stream.ReadByte();

            if (Response == EGRStandardResponse.SUCCESS) {
                int size = stream.ReadInt32();
                Resource = stream.ReadBytes(size);
            }
        }
    }
}
