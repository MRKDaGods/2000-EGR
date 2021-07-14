using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MRK.GeoJson {
    [Serializable]
    public struct EGRGeoJson {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("features")]
        public List<EGRGeoJsonFeature> Features { get; set; }
    }

    [Serializable]
    public struct EGRGeoJsonFeature {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("geometry", NullValueHandling = NullValueHandling.Ignore)]
        public EGRGeoJsonGeometry Geometry { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, string> Properties { get; set; }
    }

    [Serializable]
    public class EGRGeoJsonGeometry {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("coordinates"), JsonConverter(typeof(LonLatMultiArrayToVector2dMultiListConverter))]
        public List<List<Vector2d>> Polygons { get; set; }
    }
}
