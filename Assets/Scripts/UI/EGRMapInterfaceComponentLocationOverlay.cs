using System;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRMapInterfaceComponentLocationOverlay : EGRMapInterfaceComponent {

        public override EGRMapInterfaceComponentType ComponentType => EGRMapInterfaceComponentType.LocationOverlay;

        public void ChooseLocationOnMap(Action<Vector2d> callback) {

        }
    }
}
