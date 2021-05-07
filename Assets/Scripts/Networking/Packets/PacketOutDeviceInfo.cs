namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.DEVINFO)]
    public class PacketOutDeviceInfo : Packet {
        string m_Hwid;

        public PacketOutDeviceInfo(string hwid) : base(PacketNature.Out, PacketType.DEVINFO) {
            m_Hwid = hwid;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(m_Hwid);
        }
    }
}