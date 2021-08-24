namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.WTEQUERY)]
    public class PacketOutWTEQuery : Packet {
        byte m_People;
        int m_Price;
        string m_Cuisine;

        public PacketOutWTEQuery(byte people, int price, string cuisine) : base(PacketNature.Out, PacketType.WTEQUERY) {
            m_People = people;
            m_Price = price;
            m_Cuisine = cuisine;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteByte(m_People);
            stream.WriteInt32(m_Price);
            stream.WriteString(m_Cuisine);
        }
    }
}