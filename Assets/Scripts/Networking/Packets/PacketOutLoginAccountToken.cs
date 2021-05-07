namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.LGNACCTK)]
    public class PacketOutLoginAccountToken : Packet {
        string m_Token;

        public PacketOutLoginAccountToken(string token) : base(PacketNature.Out, PacketType.LGNACCTK) {
            m_Token = token;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(m_Token);
        }
    }
}