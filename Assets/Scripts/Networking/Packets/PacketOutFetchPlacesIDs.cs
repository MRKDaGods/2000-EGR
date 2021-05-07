namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.PLCIDFETCH)]
    public class PacketOutFetchPlacesIDs : Packet {
        ulong m_Ctx;
        double m_MinLat;
        double m_MinLng;
        double m_MaxLat;
        double m_MaxLng;
        int m_ZoomLvl;

        public PacketOutFetchPlacesIDs(ulong ctx, double minLat, double minLng, double maxLat, double maxLng, int zoomLvl) : base(PacketNature.Out, PacketType.PLCIDFETCH) {
            m_Ctx = ctx;
            m_MinLat = minLat;
            m_MinLng = minLng;
            m_MaxLat = maxLat;
            m_MaxLng = maxLng;
            m_ZoomLvl = zoomLvl;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteUInt64(m_Ctx);
            stream.WriteDouble(m_MinLat);
            stream.WriteDouble(m_MinLng);
            stream.WriteDouble(m_MaxLat);
            stream.WriteDouble(m_MaxLng);
            stream.WriteInt32(m_ZoomLvl);
        }
    }
}