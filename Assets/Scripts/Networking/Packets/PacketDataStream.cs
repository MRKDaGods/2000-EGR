using System.IO;
using System.Text;

namespace MRK.Networking.Packets {
    public class PacketDataStream {
        byte[] m_Data;
        MemoryStream m_Stream;
        BinaryWriter m_Writer;
        BinaryReader m_Reader;
        PacketNature m_Nature;

        public byte[] Data => m_Data ?? m_Stream.GetBuffer();

        public PacketDataStream(byte[] data, PacketNature nature) {
            m_Data = data;
            m_Nature = nature;
        }

        public void Prepare() {
            m_Stream = m_Nature == PacketNature.Out ? new MemoryStream() : new MemoryStream(m_Data);

            switch (m_Nature) {

                case PacketNature.In:
                    m_Reader = new BinaryReader(m_Stream);
                    break;

                case PacketNature.Out:
                    m_Writer = new BinaryWriter(m_Stream);
                    break;

                default:
                    throw new System.Exception("Unknown stream nature");

            }
        }

        public byte ReadByte() {
            return m_Reader.ReadByte();
        }

        public byte[] ReadBytes(int count) {
            return m_Reader.ReadBytes(count);
        }

        public short ReadInt16() {
            return m_Reader.ReadInt16();
        }

        public int ReadInt32() {
            return m_Reader.ReadInt32();
        }

        public long ReadInt64() {
            return m_Reader.ReadInt64();
        }

        public ushort ReadUInt16() {
            return m_Reader.ReadUInt16();
        }

        public uint ReadUInt32() {
            return m_Reader.ReadUInt32();
        }

        public ulong ReadUInt64() {
            return m_Reader.ReadUInt64();
        }

        public float ReadSingle() {
            return m_Reader.ReadSingle();
        }

        public string ReadString() {
            int bytesCount = ReadInt32();
            if (bytesCount <= 0) {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(ReadBytes(bytesCount));
        }

        public sbyte ReadSByte() {
            return m_Reader.ReadSByte();
        }

        public bool ReadBool() {
            return m_Reader.ReadBoolean();
        }

        public double ReadDouble() {
            return m_Reader.ReadDouble();
        }

        public void WriteByte(byte b) {
            m_Writer.Write(b);
        }

        public void WriteBytes(byte[] b) {
            m_Writer.Write(b);
        }

        public void WriteInt16(short s) {
            m_Writer.Write(s);
        }

        public void WriteInt32(int i) {
            m_Writer.Write(i);
        }

        public void WriteInt64(long l) {
            m_Writer.Write(l);
        }

        public void WriteUInt16(ushort s) {
            m_Writer.Write(s);
        }

        public void WriteUInt32(uint i) {
            m_Writer.Write(i);
        }

        public void WriteUInt64(ulong l) {
            m_Writer.Write(l);
        }

        public void WriteSingle(float f) {
            m_Writer.Write(f);
        }

        public void WriteString(string s) {
            if (string.IsNullOrEmpty(s)) {
                WriteInt32(0);
                return;
            }

            //put bytes count
            int bytesCount = Encoding.UTF8.GetByteCount(s);
            WriteInt32(bytesCount);

            //put string
            WriteBytes(Encoding.UTF8.GetBytes(s));
        }

        public void WriteSByte(sbyte sb) {
            m_Writer.Write(sb);
        }

        public void WriteBool(bool b) {
            m_Writer.Write(b);
        }

        public void WriteDouble(double d) {
            m_Writer.Write(d);
        }

        public void Clean() {
            switch (m_Nature) {

                case PacketNature.In:
                    m_Reader.Close();
                    break;

                case PacketNature.Out:
                    m_Writer.Close();
                    break;

            }

            m_Stream.Close();
        }
    }
}