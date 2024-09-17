using System;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface
{
    public class LocationOverlay : Component
    {
        private Image _locationPinSprite;
        private Action<Vector2d> _callback;

        public override ComponentType ComponentType
        {
            get
            {
                return ComponentType.LocationOverlay;
            }
        }

        public override void OnComponentInit(MapInterface mapInterface)
        {
            base.OnComponentInit(mapInterface);

            _locationPinSprite = mapInterface.MapInterfaceResources.LocationPinSprite;
            _locationPinSprite.gameObject.SetActive(false);
        }

        public void ChooseLocationOnMap(Action<Vector2d> callback)
        {
            _locationPinSprite.gameObject.SetActive(true);

            RectTransform rectTransform = _locationPinSprite.rectTransform;
            Vector2 screenPoint = new Vector2(Screen.width / 2f, Screen.height / 2f + rectTransform.rect.height / 2f);
            rectTransform.position = EGRPlaceMarker.ScreenToMarkerSpace(screenPoint);

            _callback = callback;
        }

        public void Finish()
        {
            if (_callback != null)
            {
                //get pos from middle spos i guess
                Vector3 pos = new Vector3(Screen.width / 2f, Screen.height / 2f, Client.ActiveCamera.transform.position.y);
                Vector3 wPos = Client.ActiveCamera.ScreenToWorldPoint(pos);
                Vector2d geo = Client.FlatMap.WorldToGeoPosition(wPos);
                _callback(geo);
            }

            _locationPinSprite.gameObject.SetActive(false);
        }
    }
}
