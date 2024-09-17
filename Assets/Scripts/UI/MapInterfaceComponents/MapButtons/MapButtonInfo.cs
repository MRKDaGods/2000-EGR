using MRK.Localization;
using System;
using UnityEngine;

namespace MRK.UI.MapInterface
{
    public enum MapButtonID
    {
        None,
        Settings,
        Trending,
        CurrentLocation,
        Navigation,
        BackToEarth,
        FieldOfView
    }

    [Serializable]
    public class MapButtonInfo
    {
        public MapButtonID ID;
        public LanguageData Name;
        public Sprite Sprite;
    }
}
