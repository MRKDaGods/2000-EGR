namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.REGACC)]
    public class PacketOutRegisterAccount : Packet {
        string m_Name;
        string m_Email;
        string m_Password;

        public PacketOutRegisterAccount(string fullname, string email, string password) : base(PacketNature.Out, PacketType.REGACC) {
            m_Name = fullname;
            m_Email = email;
            m_Password = password;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(m_Name);
            stream.WriteString(m_Email);
            stream.WriteString(m_Password);
        }
    }
}