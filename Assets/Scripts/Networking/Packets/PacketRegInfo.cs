using System;

namespace MRK.Networking.Packets {
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PacketRegInfo : Attribute {
        public PacketNature PacketNature { get; private set; }
        public PacketType PacketType { get; private set; }

        public PacketRegInfo(PacketNature nature, PacketType type) {
            PacketNature = nature;
            PacketType = type;
        }
    }
}
