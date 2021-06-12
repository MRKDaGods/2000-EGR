using System.Collections.Generic;
using System;

namespace MRK.Networking.Packets {
    public enum PacketNature : byte {
        None,
        In,
        Out
    }

    public enum PacketType : ushort {
        None = 0x00,

        //xor key
        XKEY = 0x01,
        //device info
        DEVINFO = 0x02,
        //download request
        DWNLDREQ = 0x03,
        //download
        DWNLD = 0x04,

        //register account
        REGACC = 0x10,
        //login
        LGNACC = 0x11,
        //login with token
        LGNACCTK = 0x12,
        //login with device
        LGNACCDEV = 0x13,
        //logout
        LGNOUT = 0x14,
        //update account info
        UPDACC = 0x15,
        //update account password
        UPDACCPWD = 0x16,

        //standard response
        STDRSP = 0x20,
        //test packet
        TEST = 0x21,
        //standard json response
        STDJSONRSP = 0x22,

        //fetch places
        PLCFETCH = 0x30,
        //fetch tile
        TILEFETCH = 0x31,
        //fetch place id
        PLCIDFETCH = 0x32,
        //fetch place v2
        PLCFETCHV2 = 0x33,

        //geo autocomplete
        GEOAUTOCOMPLETE = 0x40,
        //query directions
        QUERYDIRS = 0x41,

        MAX
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