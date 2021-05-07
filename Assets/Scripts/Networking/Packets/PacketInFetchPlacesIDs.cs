using System.Collections.Generic;

namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.PLCIDFETCH)]
    public class PacketInFetchPlacesIDs : Packet {
        public ulong Ctx { get; private set; }
        public List<ulong> IDs { get; private set; }

        public PacketInFetchPlacesIDs() : base(PacketNature.In, PacketType.PLCIDFETCH) {
        }

        public override void Read(PacketDataStream stream) {
            Ctx = stream.ReadUInt64();
            int count = stream.ReadInt32();
            IDs = new List<ulong>(count);

            for (int i = 0; i < count; i++) {
                IDs.Add(stream.ReadUInt64());
            }
        }
    }
}