namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.CDNRESOURCE)]
    public class PacketOutRequestCDNResource : Packet {
        readonly string m_Resource;
        readonly byte[] m_Signature;

        public PacketOutRequestCDNResource(string resource, byte[] sig) : base(PacketNature.Out, PacketType.CDNRESOURCE) {
            m_Resource = resource;
            m_Signature = sig;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(m_Resource);
            stream.WriteBytes(m_Signature);
        }
    }
}
