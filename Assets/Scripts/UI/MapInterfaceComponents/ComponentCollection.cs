using System.Collections.Generic;

namespace MRK.UI.MapInterface
{
    public class ComponentCollection : Dictionary<ComponentType, Component>
    {
        public PlaceMarkers PlaceMarkers
        {
            get
            {
                return GetComponent<PlaceMarkers>(ComponentType.PlaceMarkers);
            }
        }

        public ScaleBar ScaleBar
        {
            get
            {
                return GetComponent<ScaleBar>(ComponentType.ScaleBar);
            }
        }

        public Navigation Navigation
        {
            get
            {
                return GetComponent<Navigation>(ComponentType.Navigation);
            }
        }

        public LocationOverlay LocationOverlay
        {
            get
            {
                return GetComponent<LocationOverlay>(ComponentType.LocationOverlay);
            }
        }

        public MapButtons MapButtons
        {
            get
            {
                return GetComponent<MapButtons>(ComponentType.MapButtons);
            }
        }

        public T GetComponent<T>(ComponentType type) where T : Component
        {
            return (T)this[type];
        }

        public void OnMapUpdated()
        {
            foreach (KeyValuePair<ComponentType, Component> pair in this)
            {
                pair.Value.OnMapUpdated();
            }
        }

        public void OnMapFullyUpdated()
        {
            foreach (KeyValuePair<ComponentType, Component> pair in this)
            {
                pair.Value.OnMapFullyUpdated();
            }
        }

        public void OnComponentsShow()
        {
            foreach (KeyValuePair<ComponentType, Component> pair in this)
            {
                pair.Value.OnComponentShow();
            }
        }

        public void OnComponentsHide()
        {
            foreach (KeyValuePair<ComponentType, Component> pair in this)
            {
                pair.Value.OnComponentHide();
            }
        }

        public void OnComponentsUpdate()
        {
            foreach (Component component in Values)
            {
                component.OnComponentUpdate();
            }
        }

        public void OnComponentsWarmUp()
        {
            foreach (KeyValuePair<ComponentType, Component> pair in this)
            {
                pair.Value.OnWarmup();
            }
        }
    }
}
