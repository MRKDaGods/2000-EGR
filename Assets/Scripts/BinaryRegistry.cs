using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MRK
{
    public struct RegistryBinarySequence
    {
        public string Name
        {
            get; private set;
        }

        public Func<BinaryReader, object> Action
        {
            get; private set;
        }

        public RegistryBinarySequence(string name, Func<BinaryReader, object> action)
        {
            Name = name;
            Action = action;
        }

        public static RegistryBinarySequence Float(string name)
        {
            return new RegistryBinarySequence(name, br => br.ReadSingle());
        }

        public static RegistryBinarySequence Long(string name)
        {
            return new RegistryBinarySequence(name, br => br.ReadInt64());
        }

        public static RegistryBinarySequence String(string name)
        {
            return new RegistryBinarySequence(name, br => br.ReadString());
        }

        public static RegistryBinarySequence Double(string name)
        {
            return new RegistryBinarySequence(name, br => br.ReadDouble());
        }

        public static RegistryBinarySequence Byte(string name)
        {
            return new RegistryBinarySequence(name, br => br.ReadByte());
        }
    }

    public struct RegistryBinaryReverseSequence
    {
        public string Name
        {
            get; private set;
        }

        public Action<BinaryWriter, object> Action
        {
            get; private set;
        }

        public RegistryBinaryReverseSequence(string name, Action<BinaryWriter, object> action)
        {
            Name = name;
            Action = action;
        }

        public static RegistryBinaryReverseSequence Float(string name)
        {
            return new RegistryBinaryReverseSequence(name, (bw, o) => bw.Write((float)o));
        }

        public static RegistryBinaryReverseSequence Long(string name)
        {
            return new RegistryBinaryReverseSequence(name, (bw, o) => bw.Write((long)o));
        }

        public static RegistryBinaryReverseSequence String(string name)
        {
            return new RegistryBinaryReverseSequence(name, (bw, o) => bw.Write((string)o));
        }

        public static RegistryBinaryReverseSequence Double(string name)
        {
            return new RegistryBinaryReverseSequence(name, (bw, o) => bw.Write((double)o));
        }

        public static RegistryBinaryReverseSequence Byte(string name)
        {
            return new RegistryBinaryReverseSequence(name, (bw, o) => bw.Write((byte)o));
        }
    }

    public class BinaryRegistry : List<Registry<string, object>>
    {
        public new Registry<string, object> this[int idx]
        {
            get
            {
                if (idx < Count)
                {
                    return ((List<Registry<string, object>>)this)[idx];
                }

                if (idx == Count)
                {
                    //alloc
                    Registry<string, object> reg = new Registry<string, object>();
                    Add(reg);
                    return reg;
                }

                return null;
            }
        }

        public void Load(BinaryReader reader, params RegistryBinarySequence[] sequence)
        {
            Clear();

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string base64 = reader.ReadString();
                byte[] raw = Convert.FromBase64String(base64);

                using (MemoryStream stream = new MemoryStream(raw))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    foreach (RegistryBinarySequence seq in sequence)
                    {
                        this[i][seq.Name] = seq.Action(br);
                    }

                    br.Close();
                }
            }
        }

        public void Save(BinaryWriter writer, params RegistryBinaryReverseSequence[] sequence)
        {
            writer.Write(Count);
            foreach (Registry<string, object> reg in this)
            {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(stream))
                {
                    foreach (KeyValuePair<string, object> pair in reg)
                    {
                        RegistryBinaryReverseSequence[] seq = sequence.Where(x => x.Name == pair.Key).ToArray();
                        if (seq.Length == 0)
                        {
                            continue;
                        }

                        seq[0].Action(bw, pair.Value);
                    }

                    byte[] buf = stream.GetBuffer();
                    string base64 = Convert.ToBase64String(buf);
                    writer.Write(base64);
                    bw.Close();
                }
            }
        }

        public List<T> GetAll<T>(Func<Registry<string, object>, T> func)
        {
            if (func == null || Count == 0)
            {
                return null;
            }

            List<T> list = new List<T>();
            foreach (Registry<string, object> reg in this)
            {
                list.Add(func(reg));
            }

            return list;
        }

        public void SetAll<T>(List<T> list, Action<Registry<string, object>, T> action)
        {
            if (list == null || action == null)
            {
                return;
            }

            Clear();

            int idx = 0;
            foreach (T obj in list)
            {
                Registry<string, object> reg = this[idx++];
                action(reg, obj);
            }
        }
    }
}
