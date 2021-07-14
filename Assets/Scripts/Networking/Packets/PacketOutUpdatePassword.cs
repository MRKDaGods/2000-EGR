namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.UPDACCPWD)]
    public class PacketOutUpdatePassword : Packet {
        string m_Token;
        string m_Pass;
        bool m_LogoutAll;

        public PacketOutUpdatePassword(string token, string pass, bool logoutAll) : base(PacketNature.Out, PacketType.UPDACCPWD) {
            m_Token = token;
            m_Pass = pass;
            m_LogoutAll = logoutAll;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(m_Token);
            stream.WriteString(MRKCryptography.Hash(m_Pass));
            stream.WriteBool(m_LogoutAll);
        }
    }
}