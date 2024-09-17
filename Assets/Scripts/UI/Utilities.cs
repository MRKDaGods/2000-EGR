using System.Collections.Generic;
using UnityEngine;

namespace MRK.UI
{
    public class Utilities
    {
        private static readonly Dictionary<int, Texture2D> _textureCache;

        static Utilities()
        {
            _textureCache = new Dictionary<int, Texture2D>();
        }

        public static Texture2D GetPlainTexture(Color color)
        {
            Texture2D _tex;
            int hash = color.GetHashCode();
            if (_textureCache.TryGetValue(hash, out _tex))
            {
                return _tex;
            }

            _tex = new Texture2D(1, 1);
            _tex.SetPixel(0, 0, color);
            _tex.Apply();

            if (_textureCache.Keys.Count > 2000) //dumb move but ok
            {
                _textureCache.Clear();
            }

            _textureCache[hash] = _tex;
            return _tex;
        }

        public static Texture2D GetPlainTexture(float r, float g, float b, float a)
        {
            return GetPlainTexture(new Color(r, g, b, a));
        }
    }
}
