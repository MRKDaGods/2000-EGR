using System.Collections.Generic;

namespace MRK.UI.MapInterface {
    public class EGRMapInterfaceComponentCollection : Dictionary<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> {
        public EGRMapInterfaceComponentPlaceMarkers PlaceMarkers => GetComponent<EGRMapInterfaceComponentPlaceMarkers>(EGRMapInterfaceComponentType.PlaceMarkers);
        public EGRMapInterfaceComponentScaleBar ScaleBar => GetComponent<EGRMapInterfaceComponentScaleBar>(EGRMapInterfaceComponentType.ScaleBar);
        public EGRMapInterfaceComponentNavigation Navigation => GetComponent<EGRMapInterfaceComponentNavigation>(EGRMapInterfaceComponentType.Navigation);
        public EGRMapInterfaceComponentLocationOverlay LocationOverlay => GetComponent<EGRMapInterfaceComponentLocationOverlay>(EGRMapInterfaceComponentType.LocationOverlay);
        public EGRMapInterfaceComponentMapButtons MapButtons => GetComponent<EGRMapInterfaceComponentMapButtons>(EGRMapInterfaceComponentType.MapButtons);

        public T GetComponent<T>(EGRMapInterfaceComponentType type) where T : EGRMapInterfaceComponent {
            return (T)this[type];
        }

        public void OnMapUpdated() {
            foreach (KeyValuePair<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> pair in this) {
                pair.Value.OnMapUpdated();
            }
        }

        public void OnMapFullyUpdated() {
            foreach (KeyValuePair<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> pair in this) {
                pair.Value.OnMapFullyUpdated();
            }
        }

        public void OnComponentsShow() {
            foreach (KeyValuePair<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> pair in this) {
                pair.Value.OnComponentShow();
            }
        }

        public void OnComponentsHide() {
            foreach (KeyValuePair<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> pair in this) {
                pair.Value.OnComponentHide();
            }
        }

        public void OnComponentsUpdate() {
            foreach (EGRMapInterfaceComponent component in Values) {
                component.OnComponentUpdate();
            }
        }

        public void OnComponentsWarmUp() {
            foreach (KeyValuePair<EGRMapInterfaceComponentType, EGRMapInterfaceComponent> pair in this) {
                pair.Value.OnWarmup();
            }
        }
    }
}
