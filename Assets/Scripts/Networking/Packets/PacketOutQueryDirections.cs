namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.QUERYDIRS)]
    public class PacketOutQueryDirections : Packet {
        Vector2d m_From;
        Vector2d m_To;
        byte m_Profile;

        public PacketOutQueryDirections(Vector2d from, Vector2d to, byte profile) : base(PacketNature.Out, PacketType.QUERYDIRS) {
            m_From = from;
            m_To = to;
            m_Profile = profile;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteDouble(m_From.x);
            stream.WriteDouble(m_From.y);
            stream.WriteDouble(m_To.x);
            stream.WriteDouble(m_To.y);
            stream.WriteByte(m_Profile);
        }
    }
}