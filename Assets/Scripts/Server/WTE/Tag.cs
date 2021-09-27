using System;
using System.IO;

namespace MRK.Server.WTE {
    public enum TagType {
        None,
        FastFood,
        Custom,
        Burger,
        Chicken
    }

    [Serializable]
    public class Tag {
        public TagType Type;
        public string Custom;

        public void Write(BinaryWriter writer) {
            writer.Write((ushort)Type);
            writer.Write(Custom);
        }
    }
}
