using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace MRK {
    public enum EGRQuickLocationType {
        Home,
        Work,
        Other
    }

    public class EGRQuickLocation : MRKBehaviourPlain {
        const string B64_PREFIX = "MRKb64";

        static List<EGRQuickLocation> ms_Locations;
        static MRKRegistryBinary m_Registry;

        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("coords")]
        [JsonConverter(typeof(LonLatToVector2dConverter))]
        public Vector2d Coords { get; set; }
        [JsonProperty("type")]
        public EGRQuickLocationType Type { get; set; }
        [JsonProperty("added")]
        public long CreationDate { get; set; }

        public static List<EGRQuickLocation> Locations {
            get {
                if (ms_Locations == null)
                    ms_Locations = new List<EGRQuickLocation>();

                return ms_Locations;
            }
        }

        static EGRQuickLocation() {
            m_Registry = new MRKRegistryBinary();
        }

        public EGRQuickLocation() {
        }

        public EGRQuickLocation(string name, Vector2d coords) {
            Name = name;
            Coords = coords;
            Type = EGRQuickLocationType.Other;
            CreationDate = DateTime.UtcNow.Ticks;
        }

        public TimeSpan Period() {
            return new TimeSpan(DateTime.UtcNow.Ticks - CreationDate);
        }

        static MRKRegistryBinarySequence[] GetLocationLoadSequence() {
            return new MRKRegistryBinarySequence[] {
                MRKRegistryBinarySequence.String("Name"),
                MRKRegistryBinarySequence.Double("Lat"),
                MRKRegistryBinarySequence.Double("Lng"),
                MRKRegistryBinarySequence.Byte("Type"),
                MRKRegistryBinarySequence.Long("Date")
            };
        }

        public static void ImportLocalLocations(Action callback) {
            string b64 = MRKPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_LOCAL_QLOCATIONS, null);
            if (b64 != null && b64.StartsWith(B64_PREFIX)) {
                MRKThreadPool.Global.QueueTask(() => {
                    byte[] bytes = Convert.FromBase64String(b64.Substring(B64_PREFIX.Length));
                    using (MemoryStream stream = new MemoryStream(bytes))
                    using (BinaryReader reader = new BinaryReader(stream)) {
                        m_Registry.Load(reader, GetLocationLoadSequence());
                        reader.Close();
                    }

                    List<EGRQuickLocation> locs = m_Registry.GetAll((reg) => {
                        EGRQuickLocation loc = new EGRQuickLocation();
                        loc.Name = (string)reg["Name"];
                        loc.Coords = new Vector2d((double)reg["Lat"], (double)reg["Lng"]);
                        loc.Type = (EGRQuickLocationType)(byte)reg["Type"];
                        loc.CreationDate = (long)reg["Date"];
                        return loc;
                    });
                    
                    if (locs != null && locs.Count > 0) {
                        Locations.AddRange(locs);
                    }

                    if (callback != null) {
                        callback();
                    }
                });
            }
        }

        static MRKRegistryBinaryReverseSequence[] GetLocationSaveSequence() {
            return new MRKRegistryBinaryReverseSequence[] {
                MRKRegistryBinaryReverseSequence.String("Name"),
                MRKRegistryBinaryReverseSequence.Double("Lat"),
                MRKRegistryBinaryReverseSequence.Double("Lng"),
                MRKRegistryBinaryReverseSequence.Byte("Type"),
                MRKRegistryBinaryReverseSequence.Long("Date")
            };
        }

        public static void SaveLocalLocations(Action callback = null) {
            if (ms_Locations == null)
                return;

            MRKThreadPool.Global.QueueTask(() => {
                m_Registry.SetAll(ms_Locations, (reg, loc) => {
                    reg["Name"] = loc.Name;
                    reg["Lat"] = loc.Coords.x;
                    reg["Lng"] = loc.Coords.y;
                    reg["Type"] = (byte)loc.Type;
                    reg["Date"] = loc.CreationDate;
                });

                string b64;
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    m_Registry.Save(writer, GetLocationSaveSequence());

                    byte[] buf = stream.GetBuffer();
                    b64 = B64_PREFIX + Convert.ToBase64String(buf);
                    writer.Close();
                }

                Client.Runnable.RunOnMainThread(() => {
                    MRKPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_LOCAL_QLOCATIONS, b64);
                    MRKPlayerPrefs.Save();

                    if (callback != null) {
                        callback();
                    }
                });
            });
        }

        public static void Add(string name, Vector2d coords) {
            Locations.Add(new EGRQuickLocation(name, coords));
            SaveLocalLocations();
        }

        public static void Delete(EGRQuickLocation loc) {
            if (Locations.Remove(loc)) {
                SaveLocalLocations();
            }
        }
    }
}
