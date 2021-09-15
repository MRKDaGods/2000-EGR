using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface {
    public class EGRMapInterfaceComponentScaleBar : EGRMapInterfaceComponent {
        Transform m_Parent;
        TextMeshProUGUI m_Text;
        Image m_Fill;

        public override EGRMapInterfaceComponentType ComponentType => EGRMapInterfaceComponentType.ScaleBar;
        public bool IsActive => m_Parent.gameObject.activeInHierarchy;

        public override void OnComponentInit(EGRScreenMapInterface mapInterface) {
            base.OnComponentInit(mapInterface);

            m_Parent = mapInterface.ScalebarParent;
            m_Text = m_Parent.Find("Text").GetComponent<TextMeshProUGUI>();
            m_Fill = m_Parent.Find("fill").GetComponent<Image>();

            SetActive(false);
        }

        public override void OnMapUpdated() {
            if (!IsActive)
                return;

            Vector2d minPos = Map.WorldToGeoPosition(Client.ActiveCamera.ScreenToWorldPoint(new Vector3(0f, 0f, Client.ActiveCamera.transform.localPosition.y)));
            Vector2d maxPos = Map.WorldToGeoPosition(Client.ActiveCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0f, Client.ActiveCamera.transform.localPosition.y)));
            Vector2d delta = maxPos - minPos;
            float scale = (float)MRKMapUtils.LatLonToMeters(delta).x / (Screen.width * 0.0264583333f);
            UpdateScale(Map.Zoom, scale);
        }

        public void SetActive(bool active) {
            m_Parent.gameObject.SetActive(active);
        }

        public void UpdateScale(float curZoom, float ratio) {
            m_Fill.fillAmount = curZoom - Mathf.Floor(curZoom);

            string unit = "M";
            if (ratio < 1000f) {
                ratio *= 100f;
                unit = "CM";
            }
            else if (ratio > 100000f) {
                ratio /= 1000f;
                unit = "KM";
            }

            m_Text.text = $"1:{Mathf.RoundToInt(ratio)} {unit}\n{Client.FlatMap.AbsoluteZoom}";
        }
    }
}
