using System.Collections.Generic;
using UnityEngine;

namespace MRK.UI {
    public class EGRUIUtilities {
        static readonly Dictionary<Color, Texture2D> ms_TextureCache;

        static EGRUIUtilities() {
            ms_TextureCache = new Dictionary<Color, Texture2D>();
        }

        public static Texture2D GetPlainTexture(Color color) {
            Texture2D _tex;
            if (ms_TextureCache.TryGetValue(color, out _tex))
                return _tex;

            _tex = new Texture2D(1, 1);
            _tex.SetPixel(0, 0, color);
            _tex.Apply();

            if (ms_TextureCache.Keys.Count > 2000) //dumb move but ok
                ms_TextureCache.Clear();

            ms_TextureCache[color] = _tex;
            return _tex;
        }

        public static Texture2D GetPlainTexture(float r, float g, float b, float a) {
            return GetPlainTexture(new Color(r, g, b, a));
        }
    }
}
