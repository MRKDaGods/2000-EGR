using System;
using UnityEngine;

namespace MRK.UI.MapInterface {
    public enum EGRUIMapButtonID {
        None,
        Settings,
        Trending,
        CurrentLocation,
        Navigation,
        BackToEarth,
        FieldOfView
    }

    [Serializable]
    public class EGRUIMapButtonInfo {
        public EGRUIMapButtonID ID;
        public EGRLanguageData Name;
        public Sprite Sprite;
    }
}
