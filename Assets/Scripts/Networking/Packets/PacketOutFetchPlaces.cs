namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.Out, PacketType.PLCFETCH)]
    public class PacketOutFetchPlaces : Packet {
        ulong m_CID;

        public PacketOutFetchPlaces(ulong cid) : base(PacketNature.Out, PacketType.PLCFETCH) {
            m_CID = cid;
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteUInt64(m_CID);
        }
    }
}