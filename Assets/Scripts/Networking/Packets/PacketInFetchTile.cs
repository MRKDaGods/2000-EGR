namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.TILEFETCH)]
    public class PacketInFetchTile : Packet {
        public bool Success { get; private set; }
        public MRKTileID TileID { get; private set; }
        public byte[] Data { get; private set; }

        public PacketInFetchTile() : base(PacketNature.In, PacketType.TILEFETCH) {
        }

        public override void Read(PacketDataStream stream) {
            Success = stream.ReadBool();
            TileID = stream.Read<MRKTileID>();

            if (Success) {
                int dataSize = stream.ReadInt32();
                Data = stream.ReadBytes(dataSize);
            }
        }
    }
}