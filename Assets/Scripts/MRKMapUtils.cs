using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public class MRKMapUtils {
		const int TILE_SIZE = 256;
		const int EARTH_RADIUS = 6378137;
		const double INITIAL_RESOLUTION = 2 * Math.PI * EARTH_RADIUS / TILE_SIZE;
		const double ORIGIN_SHIFT = 2 * Math.PI * EARTH_RADIUS / 2;
		public const double LATITUDE_MAX = 85.0511;
		public const double LONGITUDE_MAX = 180;
		public const double WEBMERC_MAX = 20037508.342789244;

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
			var min = PixelsToMeters(new Vector2d(unwrappedTileId.X * TILE_SIZE, unwrappedTileId.Y * TILE_SIZE), unwrappedTileId.Z);
			var max = PixelsToMeters(new Vector2d((unwrappedTileId.X + 1) * TILE_SIZE, (unwrappedTileId.Y + 1) * TILE_SIZE), unwrappedTileId.Z);
			return new RectD(min, max - min);
		}

		static double Resolution(int zoom) {
			return INITIAL_RESOLUTION / Math.Pow(2, zoom);
		}

		static Vector2d PixelsToMeters(Vector2d p, int zoom) {
			var res = Resolution(zoom);
			var met = new Vector2d();
			met.x = (p.x * res - ORIGIN_SHIFT);
			met.y = -(p.y * res - ORIGIN_SHIFT);
			return met;
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
	}
}
