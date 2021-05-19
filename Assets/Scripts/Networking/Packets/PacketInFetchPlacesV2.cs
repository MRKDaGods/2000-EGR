using System.Collections.Generic;

namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.PLCFETCHV2)]
    public class PacketInFetchPlacesV2 : Packet {
        public int Hash { get; private set; }
        public HashSet<EGRPlace> Places { get; private set; }
        public string TileHash { get; private set; }

        public PacketInFetchPlacesV2() : base(PacketNature.In, PacketType.PLCFETCHV2) {
        }

        public override void Read(PacketDataStream stream) {
            Hash = stream.ReadInt32();

            int len = stream.ReadInt32();
            Places = new HashSet<EGRPlace>();

            for (int i = 0; i < len; i++) {
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

                Places.Add(new EGRPlace(name, type, cid, addr, lat, lng, ex, chain, types));
            }

            TileHash = stream.ReadString();
        }
    }
}