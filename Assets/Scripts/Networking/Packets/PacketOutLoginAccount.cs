namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.LGNACC)]
    public class PacketOutLoginAccount : Packet {
        string m_Email;
        string m_Password;

        public PacketOutLoginAccount(string email, string password) : base(PacketNature.Out, PacketType.LGNACC) {
            m_Email = email;
            m_Password = password;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(m_Email);
            stream.WriteString(m_Password);
        }
    }
}