using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MRK {
    [Serializable]
    public struct EGRGeoAutoCompleteGeometry {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonConverter(typeof(LonLatToVector2dConverter))]
        [JsonProperty("coordinates")]
        public Vector2d Coordinates { get; set; }
    }

    [Serializable]
    public struct EGRGeoAutoCompleteFeature {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("place_name")]
        public string PlaceName { get; set; }

        [JsonProperty("relevance")]
        public double Relevance { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }

        [JsonProperty("center")]
        [JsonConverter(typeof(LonLatToVector2dConverter))]
        public Vector2d Center { get; set; }

        [JsonProperty("geometry")]
        public EGRGeoAutoCompleteGeometry Geometry { get; set; }

        [JsonProperty("address", NullValueHandling = NullValueHandling.Ignore)]
        public string Address { get; set; }

        [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
        public List<Dictionary<string, string>> Context { get; set; }
    }

    [Serializable]
    public struct EGRGeoAutoComplete {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("query")]
        public List<string> Query { get; set; }

        [JsonProperty("features")]
        public List<EGRGeoAutoCompleteFeature> Features { get; set; }
    }
}
