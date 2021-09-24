using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MRK {
    public struct MRKRegistryBinarySequence {
        public string Name { get; private set; }
        public Func<BinaryReader, object> Action { get; private set; }

        public MRKRegistryBinarySequence(string name, Func<BinaryReader, object> action) {
            Name = name;
            Action = action;
        }

        public static MRKRegistryBinarySequence Float(string name) => new MRKRegistryBinarySequence(name, br => br.ReadSingle());
        public static MRKRegistryBinarySequence Long(string name) => new MRKRegistryBinarySequence(name, br => br.ReadInt64());
        public static MRKRegistryBinarySequence String(string name) => new MRKRegistryBinarySequence(name, br => br.ReadString());
        public static MRKRegistryBinarySequence Double(string name) => new MRKRegistryBinarySequence(name, br => br.ReadDouble());
        public static MRKRegistryBinarySequence Byte(string name) => new MRKRegistryBinarySequence(name, br => br.ReadByte());
    }

    public struct MRKRegistryBinaryReverseSequence {
        public string Name { get; private set; }
        public Action<BinaryWriter, object> Action { get; private set; }

        public MRKRegistryBinaryReverseSequence(string name, Action<BinaryWriter, object> action) {
            Name = name;
            Action = action;
        }

        public static MRKRegistryBinaryReverseSequence Float(string name) => new MRKRegistryBinaryReverseSequence(name, (bw, o) => bw.Write((float)o));
        public static MRKRegistryBinaryReverseSequence Long(string name) => new MRKRegistryBinaryReverseSequence(name, (bw, o) => bw.Write((long)o));
        public static MRKRegistryBinaryReverseSequence String(string name) => new MRKRegistryBinaryReverseSequence(name, (bw, o) => bw.Write((string)o));
        public static MRKRegistryBinaryReverseSequence Double(string name) => new MRKRegistryBinaryReverseSequence(name, (bw, o) => bw.Write((double)o));
        public static MRKRegistryBinaryReverseSequence Byte(string name) => new MRKRegistryBinaryReverseSequence(name, (bw, o) => bw.Write((byte)o));
    }

    public class MRKRegistryBinary : List<MRKRegistry<string, object>> {
        public new MRKRegistry<string, object> this[int idx] {
            get {
                if (idx < Count) {
                    return ((List<MRKRegistry<string, object>>)this)[idx];
                }

                if (idx == Count) {
                    //alloc
                    MRKRegistry<string, object> reg = new MRKRegistry<string, object>();
                    Add(reg);
                    return reg;
                }

                return null;
            }
        }

        public void Load(BinaryReader reader, params MRKRegistryBinarySequence[] sequence) {
            Clear();

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                string base64 = reader.ReadString();
                byte[] raw = Convert.FromBase64String(base64);

                using (MemoryStream stream = new MemoryStream(raw))
                using (BinaryReader br = new BinaryReader(stream)) {
                    foreach (MRKRegistryBinarySequence seq in sequence) {
                        this[i][seq.Name] = seq.Action(br);
                    }

                    br.Close();
                }
            }
        }

        public void Save(BinaryWriter writer, params MRKRegistryBinaryReverseSequence[] sequence) {
            writer.Write(Count);
            foreach (MRKRegistry<string, object> reg in this) {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(stream)) {
                    foreach (KeyValuePair<string, object> pair in reg) {
                        MRKRegistryBinaryReverseSequence[] seq = sequence.Where(x => x.Name == pair.Key).ToArray();
                        if (seq.Length == 0)
                            continue;

                        seq[0].Action(bw, pair.Value);
                    }

                    byte[] buf = stream.GetBuffer();
                    string base64 = Convert.ToBase64String(buf);
                    writer.Write(base64);
                    bw.Close();
                }
            }
        }

        public List<T> GetAll<T>(Func<MRKRegistry<string, object>, T> func) {
            if (func == null || Count == 0)
                return null;

            List<T> list = new List<T>();
            foreach (MRKRegistry<string, object> reg in this) {
                list.Add(func(reg));
            }

            return list;
        }

        public void SetAll<T>(List<T> list, Action<MRKRegistry<string, object>, T> action) {
            if (list == null || action == null)
                return;

            Clear();

            int idx = 0;
            foreach (T obj in list) {
                MRKRegistry<string, object> reg = this[idx++];
                action(reg, obj);
            }
        }
    }
}
