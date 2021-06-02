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
