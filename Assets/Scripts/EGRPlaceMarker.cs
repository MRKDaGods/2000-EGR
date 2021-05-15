//#define DEBUG_PLACES

using MRK.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK {
    public class EGRPlaceMarker : EGRBehaviour {
        TextMeshProUGUI m_Text;
        EGRColorFade m_Fade;
        Vector3 m_OriginalScale;
        Image m_Sprite;
        static EGRScreenMapInterface ms_MapInterface;
        static Canvas ms_Canvas;

        public EGRPlace Place { get; private set; }

        void Awake() {
            m_Text = GetComponentInChildren<TextMeshProUGUI>();
            m_Sprite = GetComponentInChildren<Image>();
            m_OriginalScale = transform.localScale;

            if (ms_MapInterface == null) {
                ms_MapInterface = ScreenManager.GetScreen<EGRScreenMapInterface>();
                ms_Canvas = ScreenManager.GetLayer(ms_MapInterface);
            }
        }

        public void SetPlace(EGRPlace place) {
            Place = place;
            gameObject.SetActive(place != null);

            if (Place != null) {
                m_Text.text = Place.Name;
                m_Sprite.sprite = ms_MapInterface.GetSpriteForPlaceType(Place.Types[Mathf.Min(2, Place.Types.Length) - 1]);

                m_Fade = new EGRColorFade(Color.clear, Color.white, 2f);
            }
        }

        void LateUpdate() {
            Vector3 pos = Client.FlatMap.GeoToWorldPosition(new Vector2d(Place.Latitude, Place.Longitude));

            Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos);
            if (spos.z > 0f) {
                //spos.y = Screen.height - spos.y;
                //spos.y *= -1f;

                Vector2 point;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ms_Canvas.transform, spos, ms_Canvas.worldCamera, out point);
                transform.position = ms_Canvas.transform.TransformPoint(point);
            }

            transform.localScale = m_OriginalScale * ms_MapInterface.EvaluateMarkerScale(Client.FlatMap.Zoom / 21f);
            m_Sprite.color = Color.white.AlterAlpha(ms_MapInterface.EvaluateMarkerOpacity(Client.FlatMap.Zoom / 21f));

            if (!m_Fade.Done) {
                m_Fade.Update();
                //m_Text.color = m_Fade.Current;
            }
        }

#if DEBUG_PLACES
        void OnGUI() {
            if (Place == null)
                return;

            Vector3 pos = Client.FlatMap.GeoToWorldPosition(new Vector2d(Place.Latitude, Place.Longitude));
            Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos);
            if (spos.z > 0f) {
                spos.y = Screen.height - spos.y;

                GUI.Label(new Rect(spos.x, spos.y, 200f, 200f), $"<size=25>{Place.Name}</size>");
            }
        }
#endif
    }
}
