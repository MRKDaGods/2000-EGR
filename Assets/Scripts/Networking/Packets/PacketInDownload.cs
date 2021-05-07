namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.DWNLD)]
    public class PacketInDownload : Packet {
        public ulong ID { get; private set; }
        public int Progress { get; private set; }
        public bool Incomplete { get; private set; }
        public byte[] Data { get; private set; }

        public PacketInDownload() : base(PacketNature.In, PacketType.DWNLD) {
        }

        public override void Read(PacketDataStream stream) {
            ID = stream.ReadUInt64();
            Progress = stream.ReadInt32();
            Incomplete = stream.ReadBool();

            if (Incomplete) {
                int dataLen = stream.ReadInt32();
                Data = new byte[dataLen];
                for (int i = 0; i < dataLen; i++)
                    Data[i] = stream.ReadByte();
            }
        }
    }
}