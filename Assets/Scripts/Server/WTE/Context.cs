using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MRK.Server.WTE {
    public class Context : ScriptableObject {
        [Range(1, 20)]
        public int Version;
        public string ContextName;
        public List<Place> Places;

        public void Write(BinaryWriter writer) {
            writer.Write(ContextName);
            writer.Write(0x8721FFBA);
            writer.Write(Version);

            writer.Write(Places.Count);
            foreach (Place p in Places) {
                p.Write(writer);
            }
        }
    }
}
