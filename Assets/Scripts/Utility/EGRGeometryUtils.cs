using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRK {
	public static class EGRGeometryUtils {
		public static List<Vector2d> Decode(string encodedPath, int precision = 5) {
			int len = encodedPath.Length;

			double factor = Math.Pow(10, precision);

			List<Vector2d> path = new List<Vector2d>();
			int index = 0;
			int lat = 0;
			int lng = 0;

			while (index < len) {
				int result = 1;
				int shift = 0;
				int b;
				do {
					b = encodedPath[index++] - 63 - 1;
					result += b << shift;
					shift += 5;
				}
				while (b >= 0x1f);
				lat += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

				result = 1;
				shift = 0;
				do {
					b = encodedPath[index++] - 63 - 1;
					result += b << shift;
					shift += 5;
				}
				while (b >= 0x1f);
				lng += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

				path.Add(new Vector2d(y: lng / factor, x: lat / factor));
			}

			return path;
		}

		public static string Encode(List<Vector2d> path, int precision = 5) {
			long lastLat = 0;
			long lastLng = 0;

			StringBuilder result = new StringBuilder();

			double factor = Math.Pow(10, precision);

			foreach (Vector2d point in path) {
				var lat = (long)Math.Round(point.x * factor);
				var lng = (long)Math.Round(point.y * factor);

				Encode(lat - lastLat, result);
				Encode(lng - lastLng, result);

				lastLat = lat;
				lastLng = lng;
			}

			return result.ToString();
		}

		static void Encode(long variable, StringBuilder result) {
			variable = variable < 0 ? ~(variable << 1) : variable << 1;
			while (variable >= 0x20) {
				result.Append((char)((int)((0x20 | (variable & 0x1f)) + 63)));
				variable >>= 5;
			}

			result.Append((char)((int)(variable + 63)));
		}
	}
}
