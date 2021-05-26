using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MRK {
    public static class EGRExtensions {
        public static Color AlterAlpha(this Color color, float alpha) {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static Color Inverse(this Color color) {
            return new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);
        }

        public static bool Approx(this Vector2 vec, Vector2 other) {
            return Mathf.Abs(vec.x - other.x) <= 20f
                && Mathf.Abs(vec.y - other.y) <= 20f;
        }

        public static float ScaleX(this float f) {
            return Screen.width / 1080f * f;
        }

        public static float ScaleY(this float f) {
            return Screen.height / 1920f * f;
        }

        public static string ReplaceAt(this string input, int index, char newChar) {
            char[] chars = input.ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }

        public static bool IsNotEqual(this Vector2d vec, Vector2d other) {
            return Mathd.Abs(vec.x - other.x) > Mathd.Epsilon || Mathd.Abs(vec.y - other.y) > Mathd.Epsilon;
        }

        public static bool IsNotEqual(this Vector2 vec, Vector2 other) {
            return Mathf.Abs(vec.x - other.x) > Mathf.Epsilon || Mathf.Abs(vec.y - other.y) > Mathf.Epsilon;
        }

        public static bool ParentHasGfx(this Graphic gfx, params Type[] excluded) {
            Transform trans = gfx.transform;
            while ((trans = trans.parent) != null) {
                if (trans.GetComponent<Graphic>() != null && trans.GetComponent<Mask>() == null) {
                    if (excluded != null && excluded.Length > 0) {
                        bool found = false;
                        foreach (Type type in excluded) {
                            if (trans.GetComponent(type) != null) {
                                found = true;
                                break;
                            }
                        }

                        if (found)
                            continue;
                    }

                    return true;
                }
            }

            return false;
        }

        public static bool GfxHasScrollView(this Graphic gfx) {
            return gfx.transform.GetComponent<ScrollRect>() != null || gfx.transform.GetComponent<Mask>() != null;
        }

        public static bool EldersHaveTransform(this Transform trans, Transform target) {
            while ((trans = trans.parent) != null) {
                if (trans == target)
                    return true;
            }

            return false;
        }

        public static bool ToBool(this int i) {
            return i == 1;
        }

        public static int ToInt(this bool b) {
            return b ? 1 : 0;
        }

		public static Vector3 ToVector3xz(this Vector2 v) {
			return new Vector3(v.x, 0, v.y);
		}

		public static Vector3 ToVector3xz(this Vector2d v) {
			return new Vector3((float)v.x, 0, (float)v.y);
		}

		public static Vector2 ToVector2xz(this Vector3 v) {
			return new Vector2(v.x, v.z);
		}

		public static Vector2d ToVector2d(this Vector3 v) {
			return new Vector2d(v.x, v.z);
		}

		public static Vector3 Perpendicular(this Vector3 v) {
			return new Vector3(-v.z, v.y, v.x);
		}

		public static void MoveToGeocoordinate(this Transform t, double lat, double lng, Vector2d refPoint, float scale = 1) {
			t.position = MRKMapUtils.GeoToWorldPosition(lat, lng, refPoint, scale).ToVector3xz();
		}

		public static void MoveToGeocoordinate(this Transform t, Vector2d latLon, Vector2d refPoint, float scale = 1) {
			t.MoveToGeocoordinate(latLon.x, latLon.y, refPoint, scale);
		}

		public static Vector3 AsUnityPosition(this Vector2 latLon, Vector2d refPoint, float scale = 1) {
			return MRKMapUtils.GeoToWorldPosition(latLon.x, latLon.y, refPoint, scale).ToVector3xz();
		}

		public static Vector2d GetGeoPosition(this Transform t, Vector2d refPoint, float scale = 1) {
			var pos = refPoint + (t.position / scale).ToVector2d();
			return MRKMapUtils.MetersToLatLon(pos);
		}

		public static Vector2d GetGeoPosition(this Vector3 position, Vector2d refPoint, float scale = 1) {
			var pos = refPoint + (position / scale).ToVector2d();
			return MRKMapUtils.MetersToLatLon(pos);
		}

		public static Vector2d GetGeoPosition(this Vector2 position, Vector2d refPoint, float scale = 1) {
			return position.ToVector3xz().GetGeoPosition(refPoint, scale);
		}

        public static ulong NextULong(this System.Random rng) {
            byte[] buf = new byte[8];
            rng.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        public static bool RectOverlaps(this RectTransform a, RectTransform b) {
            return a.WorldRect().Overlaps(b.WorldRect());
        }

        public static bool RectOverlaps(this RectTransform a, RectTransform b, bool allowInverse) {
            return a.WorldRect().Overlaps(b.WorldRect(), allowInverse);
        }

        public static Rect WorldRect(this RectTransform rectTransform) {
            Vector2 sizeDelta = rectTransform.sizeDelta;
            float rectTransformWidth = sizeDelta.x * rectTransform.lossyScale.x;
            float rectTransformHeight = sizeDelta.y * rectTransform.lossyScale.y;

            Vector3 position = rectTransform.position;
            return new Rect(position.x - rectTransformWidth / 2f, position.y - rectTransformHeight / 2f, rectTransformWidth, rectTransformHeight);
        }

        public static bool IsValidTween(this int i) {
            return i != -999;
        }
    }
}
