using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public class MRKMapUtils {
		const int TILE_SIZE = 1024;
		const int EARTH_RADIUS = 6378137;
		const double INITIAL_RESOLUTION = 2 * Math.PI * EARTH_RADIUS / TILE_SIZE;
		const double ORIGIN_SHIFT = 2 * Math.PI * EARTH_RADIUS / 2;
		public const double LATITUDE_MAX = 85.0511;
		public const double LONGITUDE_MAX = 180;
		public const double WEBMERC_MAX = 20037508.342789244;

		readonly static Dictionary<int, RectD> ms_TileBoundsCache;

		static MRKMapUtils() {
			ms_TileBoundsCache = new Dictionary<int, RectD>();
        }

		public static MRKTileID CoordinateToTileId(Vector2d coord, int zoom) {
			double lat = coord.x;
			double lng = coord.y;

			// See: http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames
			int x = (int)Math.Floor((lng + 180.0) / 360.0 * Math.Pow(2.0, zoom));
			int y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0)
					+ 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom));

			return new MRKTileID(zoom, x, y);
		}

		public static RectD TileBounds(MRKTileID unwrappedTileId) {
			int hash = unwrappedTileId.GetHashCode();
			if (ms_TileBoundsCache.ContainsKey(hash))
				return ms_TileBoundsCache[hash];

			Vector2d min = PixelsToMeters(new Vector2d(unwrappedTileId.X * TILE_SIZE, unwrappedTileId.Y * TILE_SIZE), unwrappedTileId.Z);
			Vector2d max = PixelsToMeters(new Vector2d((unwrappedTileId.X + 1) * TILE_SIZE, (unwrappedTileId.Y + 1) * TILE_SIZE), unwrappedTileId.Z);
			RectD rect = new RectD(min, max - min);

			if (ms_TileBoundsCache.Count > 10000)
				ms_TileBoundsCache.Clear();

			ms_TileBoundsCache[hash] = rect;
			return rect;
		}

		static double Resolution(int zoom) {
			return INITIAL_RESOLUTION / (1 << zoom);
		}

		static Vector2d PixelsToMeters(Vector2d p, int zoom) {
			double res = Resolution(zoom);
			return new Vector2d(p.x * res - ORIGIN_SHIFT, -(p.y * res - ORIGIN_SHIFT));
		}

		public static Vector2d LatLonToMeters(double lat, double lon) {
			var posx = lon * ORIGIN_SHIFT / 180;
			var posy = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
			posy = posy * ORIGIN_SHIFT / 180;
			return new Vector2d(posx, posy);
		}

		public static Vector2d LatLonToMeters(Vector2d v) {
			return LatLonToMeters(v.x, v.y);
		}

		public static Vector2d MetersToLatLon(Vector2d m) {
			var vx = (m.x / ORIGIN_SHIFT) * 180;
			var vy = (m.y / ORIGIN_SHIFT) * 180;
			vy = 180 / Math.PI * (2 * Math.Atan(Math.Exp(vy * Math.PI / 180)) - Math.PI / 2);
			return new Vector2d(vy, vx);
		}

		public static Vector2d GeoFromGlobePosition(Vector3 point, float radius) {
			float latitude = Mathf.Asin(point.y / radius);
			float longitude = Mathf.Atan2(point.z, point.x);
			return new Vector2d(latitude * Mathf.Rad2Deg, longitude * Mathf.Rad2Deg);
		}

		public static Vector2d GeoToWorldPosition(double lat, double lon, Vector2d refPoint, float scale = 1) {
			var posx = lon * ORIGIN_SHIFT / 180;
			var posy = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
			posy = posy * ORIGIN_SHIFT / 180;
			return new Vector2d((posx - refPoint.x) * scale, (posy - refPoint.y) * scale);
		}

		static Vector2d MetersToPixels(Vector2d m, int zoom) {
			var res = Resolution(zoom);
			var pix = new Vector2d(((m.x + ORIGIN_SHIFT) / res), ((-m.y + ORIGIN_SHIFT) / res));
			return pix;
		}

		static Vector2 PixelsToTile(Vector2d p) {
			var t = new Vector2((int)Math.Ceiling(p.x / (double)TILE_SIZE) - 1, (int)Math.Ceiling(p.y / (double)TILE_SIZE) - 1);
			return t;
		}

		public static Vector2 MetersToTile(Vector2d m, int zoom) {
			var p = MetersToPixels(m, zoom);
			return PixelsToTile(p);
		}
	}
}
