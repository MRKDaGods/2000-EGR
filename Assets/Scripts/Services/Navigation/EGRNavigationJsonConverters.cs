using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MRK {
    public class LonLatToVector2dConverter : CustomCreationConverter<Vector2d> {
        public override bool CanWrite => true;
        public static LonLatToVector2dConverter Instance { get; private set; }

        public LonLatToVector2dConverter() {
            Instance = this;
        }

        public override Vector2d Create(Type objectType) {
            throw new NotImplementedException();
        }

        public Vector2d Create(Type objectType, JArray val) {
            return new Vector2d((double)val[1], (double)val[0]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            Vector2d val = (Vector2d)value;

            Array valAsArray = val.ToArray();
            Array.Reverse(valAsArray);

            serializer.Serialize(writer, valAsArray);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            JArray coordinates = JArray.Load(reader);
            return Create(objectType, coordinates);
        }
    }

    public class LonLatArrayToVector2dListConverter : CustomCreationConverter<List<Vector2d>> {
        public override bool CanWrite => false;

        public override List<Vector2d> Create(Type objectType) {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            List<Vector2d> list = new List<Vector2d>();

            JArray coordinates = JArray.Load(reader);
            for (int i = 0; i < coordinates.Count; i++) {
                JArray val = (JArray)coordinates[i];
                list.Add(new Vector2d((double)val[1], (double)val[0]));
            }

            return list;
        }
    }

    public class LonLatMultiArrayToVector2dMultiListConverter : CustomCreationConverter<List<List<Vector2d>>> {
        public override bool CanWrite => false;

        public override List<List<Vector2d>> Create(Type objectType) {
            throw new NotImplementedException();
        }

        List<Vector2d> ParsePolygon(JArray arr, JArray proot = null, JArray root = null) {
            List<Vector2d> v = new List<Vector2d>();

            bool useParent = false;
            for (int i = 0; i < arr.Count; i++) {
                try {
                    JArray val = (JArray)arr[i];
                    v.Add(new Vector2d((double)val[1], (double)val[0]));
                }
                catch {
                    //invalid cast, weird case?
                    useParent = true;
                    break;
                }
            }

            if (useParent) {
                for (int i = 0; i < proot.Count; i++) {
                    JArray val = (JArray)proot[i];
                    v.Add(new Vector2d((double)val[1], (double)val[0]));
                }
            }

            return v;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            List<List<Vector2d>> polygons = new List<List<Vector2d>>();

            JArray root = JArray.Load(reader);
            if (root.Count == 1) {
                //normal polygon
                polygons.Add(ParsePolygon((JArray)root[0]));
                return polygons;
            }

            for (int i = 0; i < root.Count; i++) {
                JArray polyRoot = (JArray)root[i];
                polygons.Add(ParsePolygon((JArray)polyRoot[0], polyRoot, root));
            }

            return polygons;
        }
    }

    public class PolylineToVector2dListConverter : CustomCreationConverter<List<Vector2d>> {
		public override bool CanWrite => true;

		public override List<Vector2d> Create(Type objectType) {
			throw new NotImplementedException();
		}

		public List<Vector2d> Create(Type objectType, string polyLine) {
			return EGRGeometryUtils.Decode(polyLine);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			List<Vector2d> val = (List<Vector2d>)value;
			serializer.Serialize(writer, EGRGeometryUtils.Encode(val));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			JToken polyLine = JToken.Load(reader);
			return Create(objectType, (string)polyLine);
		}
	}
}
