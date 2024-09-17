using MRK.Events;

namespace MRK.UI.MapInterface
{
    public enum ComponentType
    {
        None,
        PlaceMarkers,
        ScaleBar,
        Navigation,
        LocationOverlay,
        MapButtons
    }

    public abstract class Component
    {
        public abstract ComponentType ComponentType
        {
            get;
        }

        protected MapInterface MapInterface
        {
            get; private set;
        }

        protected EGR Client
        {
            get
            {
                return MapInterface.Client;
            }
        }

        protected Map Map
        {
            get
            {
                return Client.FlatMap;
            }
        }

        protected ScreenManager ScreenManager
        {
            get
            {
                return Client.ScreenManager;
            }
        }

        protected EventManager EventManager
        {
            get
            {
                return EventManager.Instance;
            }
        }

        public virtual void OnComponentInit(HUD mapInterface)
        {
            MapInterface = mapInterface;
        }

        public virtual void OnComponentShow()
        {
        }

        public virtual void OnComponentHide()
        {
        }

        public virtual void OnComponentUpdate()
        {
        }

        public virtual void OnMapUpdated()
        {
        }

        public virtual void OnMapFullyUpdated()
        {
        }

        public virtual void OnWarmup()
        {
        }
    }
}
