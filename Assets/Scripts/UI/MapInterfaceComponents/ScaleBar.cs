using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface
{
    public class ScaleBar : Component
    {
        private Transform _parent;
        private TextMeshProUGUI _text;
        private Image _fill;

        public override ComponentType ComponentType
        {
            get
            {
                return ComponentType.ScaleBar;
            }
        }

        public bool IsActive
        {
            get
            {
                return _parent.gameObject.activeInHierarchy;
            }
        }

        public override void OnComponentInit(MapInterface mapInterface)
        {
            base.OnComponentInit(mapInterface);

            _parent = mapInterface.ScalebarParent;
            _text = _parent.Find("Text").GetComponent<TextMeshProUGUI>();
            _fill = _parent.Find("fill").GetComponent<Image>();

            SetActive(false);
        }

        public override void OnMapUpdated()
        {
            if (!IsActive)
                return;

            Vector2d minPos = Map.WorldToGeoPosition(Client.ActiveCamera.ScreenToWorldPoint(new Vector3(0f, 0f, Client.ActiveCamera.transform.localPosition.y)));
            Vector2d maxPos = Map.WorldToGeoPosition(Client.ActiveCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0f, Client.ActiveCamera.transform.localPosition.y)));
            Vector2d delta = maxPos - minPos;
            float scale = (float)MapUtils.LatLonToMeters(delta).x / (Screen.width * 0.0264583333f);
            UpdateScale(Map.Zoom, scale);
        }

        public void SetActive(bool active)
        {
            _parent.gameObject.SetActive(active);
        }

        public void UpdateScale(float curZoom, float ratio)
        {
            _fill.fillAmount = curZoom - Mathf.Floor(curZoom);

            string unit = "M";
            if (ratio < 1000f)
            {
                ratio *= 100f;
                unit = "CM";
            }
            else if (ratio > 100000f)
            {
                ratio /= 1000f;
                unit = "KM";
            }

            _text.text = $"1:{Mathf.RoundToInt(ratio)} {unit}\n{Client.FlatMap.AbsoluteZoom}";
        }
    }
}
