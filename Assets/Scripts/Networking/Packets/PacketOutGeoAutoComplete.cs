namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.GEOAUTOCOMPLETE)]
    public class PacketOutGeoAutoComplete : Packet {
        string m_Query;
        Vector2d m_Proximity;

        public PacketOutGeoAutoComplete(string query, Vector2d proximity) : base(PacketNature.Out, PacketType.GEOAUTOCOMPLETE) {
            m_Query = query;
            m_Proximity = proximity;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(m_Query);
            stream.WriteDouble(m_Proximity.x);
            stream.WriteDouble(m_Proximity.y);
        }
    }
}