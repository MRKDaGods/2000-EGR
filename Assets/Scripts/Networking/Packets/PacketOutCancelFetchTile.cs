namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.TILEFETCHCANCEL)]
    public class PacketOutCancelFetchTile : Packet {
        string m_Tileset;
        int m_Hash;
        bool m_Low;

        public PacketOutCancelFetchTile(string tileset, int hash, bool low) : base(PacketNature.Out, PacketType.TILEFETCHCANCEL) {
            m_Tileset = tileset;
            m_Hash = hash;
            m_Low = low;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(m_Tileset);
            stream.WriteInt32(m_Hash);
            stream.WriteBool(m_Low);
        }
    }
}