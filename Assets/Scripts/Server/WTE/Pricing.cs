using System;
using System.IO;
using UnityEngine;

namespace MRK.Server.WTE {
    public enum PricingType {
        None,
        GeneralMinimum,
        GeneralMaximum,
        Custom
    }

    [Serializable]
    public class Pricing {
        public PricingType Type;
        public string CustomType;
        public float Value = 0.99f;
        [Range(1, 20)]
        public int PeopleCount = 1;

        public void Write(BinaryWriter writer) {
            writer.Write((ushort)Type);
            writer.Write(CustomType);
            writer.Write(Value);
            writer.Write((byte)PeopleCount - 1);
        }
    }
}
