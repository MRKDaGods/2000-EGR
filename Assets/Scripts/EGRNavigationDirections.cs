using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRK.Navigation {
	[Serializable]
	public struct EGRNavigationAnnotation {
		[JsonProperty("distance")]
		public double[] Distance { get; set; }

		[JsonProperty("duration")]
		public double[] Duration { get; set; }

		[JsonProperty("speed")]
		public string[] Speed { get; set; }

		[JsonProperty("congestion")]
		public string[] Congestion { get; set; }
	}

	[Serializable]
	public struct EGRNavigationDirections {
		[JsonProperty("routes")]
		public List<EGRNavigationRoute> Routes { get; set; }

		[JsonProperty("waypoints")]
		public List<EGRNavigationWaypoint> Waypoints { get; set; }

		[JsonProperty("code")]
		public string Code { get; set; }
	}

	[Serializable]
	public struct EGRNavigationIntersection {
		[JsonProperty("out", Order = 0)]
		public int Out { get; set; }

		[JsonProperty("entry", Order = 1)]
		public List<bool> Entry { get; set; }

		[JsonProperty("bearings", Order = 2)]
		public List<int> Bearings { get; set; }

		[JsonProperty("location", Order = 3)]
		[JsonConverter(typeof(LonLatToVector2dConverter))]
		public Vector2d Location { get; set; }

		[JsonProperty("in", Order = 4, NullValueHandling = NullValueHandling.Ignore)]
		public int? In { get; set; }
	}

	[Serializable]
	public struct EGRNavigationLeg {
		[JsonProperty("steps")]
		public List<EGRNavigationStep> Steps { get; set; }

		[JsonProperty("summary")]
		public string Summary { get; set; }

		[JsonProperty("duration")]
		public double Duration { get; set; }

		[JsonProperty("distance")]
		public double Distance { get; set; }

		[JsonProperty("annotation")]
		public EGRNavigationAnnotation Annotation { get; set; }
	}

	[Serializable]
	public struct EGRNavigationManeuver {
		[JsonProperty("bearing_after")]
		public int BearingAfter { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("modifier")]
		public string Modifier { get; set; }

		[JsonProperty("bearing_before")]
		public int BearingBefore { get; set; }

		[JsonProperty("Location")]
		[JsonConverter(typeof(LonLatToVector2dConverter))]
		public Vector2d Location { get; set; }

		[JsonProperty("instruction")]
		public string Instruction { get; set; }
	}

	[Serializable]
	public struct EGRNavigationGeometry {
		[JsonProperty("coordinates")]
		[JsonConverter(typeof(LonLatArrayToVector2dListConverter))]
		public List<Vector2d> Coordinates { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }
    }

	[Serializable]
	public struct EGRNavigationRoute {
		[JsonProperty("legs")]
		public List<EGRNavigationLeg> Legs { get; set; }

		[JsonProperty("geometry")]
		public EGRNavigationGeometry Geometry { get; set; }

		[JsonProperty("duration")]
		public double Duration { get; set; }

		[JsonProperty("distance")]
		public double Distance { get; set; }

		[JsonProperty("weight")]
		public float Weight { get; set; }

		[JsonProperty("weight_name")]
		public string WeightName { get; set; }
	}

	[Serializable]
	public struct EGRNavigationStep {
		[JsonProperty("intersections")]
		public List<EGRNavigationIntersection> Intersections { get; set; }

		[JsonProperty("geometry")]
		public EGRNavigationGeometry Geometry { get; set; }

		[JsonProperty("maneuver")]
		public EGRNavigationManeuver Maneuver { get; set; }

		[JsonProperty("duration")]
		public double Duration { get; set; }

		[JsonProperty("distance")]
		public double Distance { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("mode")]
		public string Mode { get; set; }
	}

	[Serializable]
	public struct EGRNavigationWaypoint {
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("location")]
		[JsonConverter(typeof(LonLatToVector2dConverter))]
		public Vector2d Location { get; set; }
	}
}
