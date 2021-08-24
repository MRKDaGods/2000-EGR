using System.Collections.Generic;
using System;

namespace MRK.Networking.Packets {
    public enum PacketNature : byte {
        None,
        In,
        Out
    }
    
    public class Packet {
        readonly static Dictionary<PacketType, Type>[] ms_PacketTable;

        public PacketNature PacketNature { get; private set; }
        public PacketType PacketType { get; private set; }
        
        static Packet() {
            ms_PacketTable = new Dictionary<PacketType, Type>[2] {
                new Dictionary<PacketType, Type>(),
                new Dictionary<PacketType, Type>()
            };
        }

        public Packet(PacketNature nature, PacketType type) {
            PacketNature = nature;
            PacketType = type;
        }

        //For out packets
        public virtual void Write(PacketDataStream stream) {
        }

        public virtual void Read(PacketDataStream stream) {
        }

        public static void RegisterIn(PacketType ptype, Type type) {
            ms_PacketTable[0][ptype] = type;
        }

        public static void RegisterOut(PacketType ptype, Type type) {
            ms_PacketTable[1][ptype] = type;
        }

        public static Packet CreatePacketInstance(PacketNature nature, PacketType type) {
            Type _type = ms_PacketTable[((int)nature) - 1][type];
            if (_type == null)
                return null;

            return (Packet)Activator.CreateInstance(_type);
        }
    }
}