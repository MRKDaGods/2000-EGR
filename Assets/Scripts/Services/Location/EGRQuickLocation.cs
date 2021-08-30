using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MRK {
    public class EGRQuickLocation {
        static List<EGRQuickLocation> ms_Locations;

        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("coords")]
        [JsonConverter(typeof(LonLatToVector2dConverter))]
        public Vector2d Coords { get; set; }

        public static List<EGRQuickLocation> Locations {
            get {
                if (ms_Locations == null)
                    ms_Locations = new List<EGRQuickLocation>();

                return ms_Locations;
            }
        }

        public EGRQuickLocation(string name, Vector2d coords) {
            Name = name;
            Coords = coords;
        }

        public static void ImportLocalLocations(Action callback) {
            string json = MRKPlayerPrefs.Get<string>(EGRConstants.EGR_LOCALPREFS_LOCAL_QLOCATIONS, null);
            if (json != null) {
                MRKThreadPool.Global.QueueTask(() => {
                    List<EGRQuickLocation> locs = JsonConvert.DeserializeObject<List<EGRQuickLocation>>(json);
                    if (locs.Count > 0) {
                        Locations.AddRange(locs);
                    }

                    if (callback != null) {
                        callback();
                    }
                });
            }
        }

        static void SaveLocalLocations() {
            if (ms_Locations == null)
                return;

            string json = JsonConvert.SerializeObject(ms_Locations);
            MRKPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_LOCAL_QLOCATIONS, json);
            MRKPlayerPrefs.Save();
        }

        public static void Add(string name, Vector2d coords) {
            Locations.Add(new EGRQuickLocation(name, coords));
            SaveLocalLocations();
        }
    }
}
