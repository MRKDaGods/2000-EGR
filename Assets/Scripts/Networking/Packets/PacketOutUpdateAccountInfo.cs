namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.UPDACC)]
    public class PacketOutUpdateAccountInfo : Packet {
        string m_Token;
        string m_Name;
        string m_Email;
        sbyte m_Gender;

        public PacketOutUpdateAccountInfo(string token, string fullname, string email, sbyte gender) : base(PacketNature.Out, PacketType.UPDACC) {
            m_Token = token;
            m_Name = fullname;
            m_Email = email;
            m_Gender = gender;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(m_Token);
            stream.WriteString(m_Name);
            stream.WriteString(m_Email);
            stream.WriteSByte(m_Gender);
        }
    }
}