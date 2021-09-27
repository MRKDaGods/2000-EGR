using System;
using System.Collections.Generic;
using System.IO;

namespace MRK.Server.WTE {
    [Serializable]
    public class Place {
        public string Name;
        public ulong CID;
        public List<Tag> Tags;
        public List<Pricing> Pricing;

        public void Write(BinaryWriter writer) {
            writer.Write(Name);
            writer.Write(CID);

            //
            // 2000EGYPT-WTE-DB V10 SPEC
            //

            //write 2 cats
            writer.Write(2);

            //write first category [tags]
            writer.Write((ushort)1u); //PlaceTags = 1
            writer.Write(Tags.Count);
            foreach (Tag tag in Tags) {
                //write type again
                writer.Write((ushort)1u);
                tag.Write(writer);
            }

            //write second category [pricing]
            writer.Write((ushort)2u); //PlacePricing = 2
            writer.Write(Pricing.Count);
            foreach (Pricing pricing in Pricing) {
                //write type again
                writer.Write((ushort)2u);
                pricing.Write(writer);
            }
        }
    }
}
