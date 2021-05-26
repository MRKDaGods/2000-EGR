using MRK;

namespace MRK.UI {
    public enum EGRMapInterfaceComponentType {
        None,
        PlaceMarkers,
        ScaleBar
    }

    public abstract class EGRMapInterfaceComponent {
        public abstract EGRMapInterfaceComponentType ComponentType { get; }
        protected EGRScreenMapInterface MapInterface { get; private set; }
        protected EGRMain Client => MapInterface.Client;
        protected MRKMap Map => Client.FlatMap;
        protected EGREventManager EventManager => EGREventManager.Instance;

        public virtual void OnComponentInit(EGRScreenMapInterface mapInterface) {
            MapInterface = mapInterface;
        }

        public virtual void OnComponentShow() {
        }

        public virtual void OnComponentHide() {
        }

        public virtual void OnMapUpdated() {
        }

        public virtual void OnMapFullyUpdated() {
        }

        public virtual void OnWarmup() {
        }
    }
}
