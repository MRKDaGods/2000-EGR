namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.PLCFETCHV2)]
    public class PacketOutFetchPlacesV2 : Packet {
        int m_Hash;
        double m_MinLat;
        double m_MinLng;
        double m_MaxLat;
        double m_MaxLng;
        int m_ZoomLvl;

        public PacketOutFetchPlacesV2(int hash, double minLat, double minLng, double maxLat, double maxLng, int zoomLvl) : base(PacketNature.Out, PacketType.PLCFETCHV2) {
            m_Hash = hash;
            m_MinLat = minLat;
            m_MinLng = minLng;
            m_MaxLat = maxLat;
            m_MaxLng = maxLng;
            m_ZoomLvl = zoomLvl;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteInt32(m_Hash);
            stream.WriteString(""); //tile hash
            stream.WriteDouble(m_MinLat);
            stream.WriteDouble(m_MinLng);
            stream.WriteDouble(m_MaxLat);
            stream.WriteDouble(m_MaxLng);
            stream.WriteInt32(m_ZoomLvl);
        }
    }
}