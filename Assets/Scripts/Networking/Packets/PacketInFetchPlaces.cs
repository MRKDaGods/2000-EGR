namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.PLCFETCH)]
    public class PacketInFetchPlaces : Packet {
        public EGRPlace Place { get; private set; }

        public PacketInFetchPlaces() : base(PacketNature.In, PacketType.PLCFETCH) {
        }

        public override void Read(PacketDataStream stream) {
            string name = stream.ReadString();
            string type = stream.ReadString();
            ulong cid = stream.ReadUInt64();
            string addr = stream.ReadString();
            double lat = stream.ReadDouble();
            double lng = stream.ReadDouble();

            int exLen = stream.ReadInt32();
            string[] ex = new string[exLen];
            for (int j = 0; j < exLen; j++)
                ex[j] = stream.ReadString();

            ulong chain = stream.ReadUInt64();

            int typeLen = stream.ReadInt32();
            EGRPlaceType[] types = new EGRPlaceType[typeLen];
            for (int j = 0; j < typeLen; j++)
                types[j] = (EGRPlaceType)stream.ReadUInt16();

            Place = new EGRPlace(name, type, cid, addr, lat, lng, ex, chain, types);
        }
    }
}