namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.TILEFETCH)]
    public class PacketOutFetchTile : Packet {
        string m_Tileset;
        MRKTileID m_ID;

        public PacketOutFetchTile(string tileset, MRKTileID id) : base(PacketNature.Out, PacketType.TILEFETCH) {
            m_Tileset = tileset;
            m_ID = id;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(m_Tileset);
            stream.WriteInt32(m_ID.Z);
            stream.WriteInt32(m_ID.X);
            stream.WriteInt32(m_ID.Y);
        }
    }
}