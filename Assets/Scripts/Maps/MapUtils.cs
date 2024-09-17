using System;
using System.Collections.Generic;
using UnityEngine;

namespace MRK
{
    public class MapUtils
    {
        private const int TileSize = 1024;
        private const int EarthRadius = 6378137;
        private const double InitialResolution = 2 * Math.PI * EarthRadius / TileSize;
        private const double OriginShift = 2 * Math.PI * EarthRadius / 2;
        public const double LatitudeMax = 85.0511;
        public const double LongitudeMax = 180;
        public const double WebMercatorMax = 20037508.342789244;

        private static readonly Dictionary<int, Rectd> _tileBoundsCache;

        static MapUtils()
        {
            _tileBoundsCache = new Dictionary<int, Rectd>();
        }

        public static TileID CoordinateToTileId(Vector2d coord, int zoom)
        {
            double lat = coord.x;
            double lng = coord.y;

            // See: http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames
            int x = (int)Math.Floor((lng + 180.0) / 360.0 * Math.Pow(2.0, zoom));
            int y = (int)Math.Floor((1.0 - (Math.Log(Math.Tan(lat * Math.PI / 180.0)
                    + (1.0 / Math.Cos(lat * Math.PI / 180.0))) / Math.PI)) / 2.0 * Math.Pow(2.0, zoom));

            return new TileID(zoom, x, y);
        }

        public static Rectd TileBounds(TileID unwrappedTileId)
        {
            int hash = unwrappedTileId.GetHashCode();
            if (_tileBoundsCache.ContainsKey(hash))
            {
                return _tileBoundsCache[hash];
            }

            Vector2d min = PixelsToMeters(new Vector2d(unwrappedTileId.X * TileSize, unwrappedTileId.Y * TileSize), unwrappedTileId.Z);
            Vector2d max = PixelsToMeters(new Vector2d((unwrappedTileId.X + 1) * TileSize, (unwrappedTileId.Y + 1) * TileSize), unwrappedTileId.Z);
            Rectd rect = new Rectd(min, max - min);

            if (_tileBoundsCache.Count > 10000)
            {
                _tileBoundsCache.Clear();
            }

            _tileBoundsCache[hash] = rect;
            return rect;
        }

        private static double Resolution(int zoom)
        {
            return InitialResolution / (1 << zoom);
        }

        private static Vector2d PixelsToMeters(Vector2d p, int zoom)
        {
            double res = Resolution(zoom);
            return new Vector2d((p.x * res) - OriginShift, -((p.y * res) - OriginShift));
        }

        public static Vector2d LatLonToMeters(double lat, double lon)
        {
            var posx = lon * OriginShift / 180;
            var posy = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
            posy = posy * OriginShift / 180;
            return new Vector2d(posx, posy);
        }

        public static Vector2d LatLonToMeters(Vector2d v)
        {
            return LatLonToMeters(v.x, v.y);
        }

        public static Vector2d MetersToLatLon(Vector2d m)
        {
            var vx = (m.x / OriginShift) * 180;
            var vy = (m.y / OriginShift) * 180;
            vy = 180 / Math.PI * ((2 * Math.Atan(Math.Exp(vy * Math.PI / 180))) - (Math.PI / 2));
            return new Vector2d(vy, vx);
        }

        public static Vector2d GeoFromGlobePosition(Vector3 point, float radius)
        {
            float latitude = Mathf.Asin(point.y / radius);
            float longitude = Mathf.Atan2(point.z, point.x);
            return new Vector2d(latitude * Mathf.Rad2Deg, longitude * Mathf.Rad2Deg);
        }

        public static Vector2d GeoToWorldPosition(double lat, double lon, Vector2d refPoint, float scale = 1)
        {
            var posx = lon * OriginShift / 180;
            var posy = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
            posy = posy * OriginShift / 180;
            return new Vector2d((posx - refPoint.x) * scale, (posy - refPoint.y) * scale);
        }

        private static Vector2d MetersToPixels(Vector2d m, int zoom)
        {
            var res = Resolution(zoom);
            var pix = new Vector2d((m.x + OriginShift) / res, ((-m.y + OriginShift) / res));
            return pix;
        }

        private static Vector2 PixelsToTile(Vector2d p)
        {
            var t = new Vector2((int)Math.Ceiling(p.x / (double)TileSize) - 1, (int)Math.Ceiling(p.y / (double)TileSize) - 1);
            return t;
        }

        public static Vector2 MetersToTile(Vector2d m, int zoom)
        {
            var p = MetersToPixels(m, zoom);
            return PixelsToTile(p);
        }

        public static Vector3 GeoToWorldGlobePosition(double lat, double lon, float radius)
        {
            double xPos = radius * Math.Cos(Mathf.Deg2Rad * lat) * Math.Cos(Mathf.Deg2Rad * lon);
            double zPos = radius * Math.Cos(Mathf.Deg2Rad * lat) * Math.Sin(Mathf.Deg2Rad * lon);
            double yPos = radius * Math.Sin(Mathf.Deg2Rad * lat);

            return new Vector3((float)xPos, (float)yPos, (float)zPos);

            /*float rad = Mathf.Deg2Rad * angle;
			Matrix4x4 matrix = new Matrix4x4 {
				m00 = Mathf.Cos(rad), m01 = 0, m02 = Mathf.Sin(rad), m03 = 0f,

				m10 = 0, m11 = 1f, m12 = 0f, m13 = 0f,

				m20 = -Mathf.Sin(rad), m21 = 0f, m22 = Mathf.Cos(rad), m23 = 0f,

				m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f
			}; */

            //return matrix.MultiplyPoint3x4(new Vector3((float)xPos, (float)yPos, (float)zPos));
        }
    }
}
