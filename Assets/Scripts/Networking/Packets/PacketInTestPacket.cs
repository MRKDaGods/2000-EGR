namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.TEST)]
    public class PacketInTestPacket : Packet {
        public int ByteLength { get; private set; }
        public byte[] Bytes { get; private set; }
        public string ReadStr { get; private set; }

        public PacketInTestPacket() : base(PacketNature.In, PacketType.TEST) {
        }

        public override void Read(PacketDataStream stream) {
            ByteLength = stream.ReadInt32();
            Bytes = new byte[ByteLength];
            for (int i = 0; i < ByteLength; i++)
                Bytes[i] = stream.ReadByte();

            ReadStr = stream.ReadString();
        }
    }
}