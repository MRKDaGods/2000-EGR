using System;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface {
    public class EGRMapInterfaceComponentLocationOverlay : EGRMapInterfaceComponent {
        Image m_LocationPinSprite;
        Action<Vector2d> m_Callback;

        public override EGRMapInterfaceComponentType ComponentType => EGRMapInterfaceComponentType.LocationOverlay;

        public override void OnComponentInit(EGRScreenMapInterface mapInterface) {
            base.OnComponentInit(mapInterface);

            m_LocationPinSprite = mapInterface.MapInterfaceResources.LocationPinSprite;
            m_LocationPinSprite.gameObject.SetActive(false);
        }

        public void ChooseLocationOnMap(Action<Vector2d> callback) {
            m_LocationPinSprite.gameObject.SetActive(true);

            RectTransform rectTransform = m_LocationPinSprite.rectTransform;
            Vector2 screenPoint = new Vector2(Screen.width / 2f, Screen.height / 2f + rectTransform.rect.height / 2f);
            rectTransform.position = EGRPlaceMarker.ScreenToMarkerSpace(screenPoint);

            m_Callback = callback;
        }

        public void Finish() {
            if (m_Callback != null) {
                //get pos from middle spos i guess
                Vector3 pos = new Vector3(Screen.width / 2f, Screen.height / 2f, Client.ActiveCamera.transform.position.y);
                Vector3 wPos = Client.ActiveCamera.ScreenToWorldPoint(pos);
                Vector2d geo = Client.FlatMap.WorldToGeoPosition(wPos);
                m_Callback(geo);
            }

            m_LocationPinSprite.gameObject.SetActive(false);
        }
    }
}
