namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.TILEFETCH)]
    public class PacketInFetchTile : Packet {
        public EGRStandardResponse Response { get; private set; }
        public ulong DownloadID { get; private set; }

        public PacketInFetchTile() : base(PacketNature.In, PacketType.TILEFETCH) {
        }

        public override void Read(PacketDataStream stream) {
            Response = (EGRStandardResponse)stream.ReadByte();
            if (Response == EGRStandardResponse.SUCCESS) {
                DownloadID = stream.ReadUInt64();
            }
        }
    }
}