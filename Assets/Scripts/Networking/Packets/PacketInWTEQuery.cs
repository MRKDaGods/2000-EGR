using System.Collections.Generic;

namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.WTEQUERY)]
    public class PacketInWTEQuery : Packet {
        public List<EGRWTEProxyPlace> Places { get; private set; }

        public PacketInWTEQuery() : base(PacketNature.In, PacketType.WTEQUERY) {
        }

        public override void Read(PacketDataStream stream) {
            int pCount = stream.ReadInt32();
            Places = new List<EGRWTEProxyPlace>(pCount);

            for (int i = 0; i < pCount; i++) {
                string name = stream.ReadString();
                List<string> tags = stream.ReadList((stream) => stream.ReadString());
                float genMin = stream.ReadSingle();
                float genMax = stream.ReadSingle();
                Places.Add(new EGRWTEProxyPlace {
                    Name = name,
                    Tags = tags,
                    GeneralMinimum = genMin,
                    GeneralMaximum = genMax
                });
            }
        }
    }
}