//#define DEBUG_PLACES

using MRK.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MRK {
    public class EGRPlaceMarker : EGRBehaviour {
        TextMeshProUGUI m_Text;
        EGRColorFade m_Fade;
        Vector3 m_OriginalScale;
        Image m_Sprite;
        static EGRScreenMapInterface ms_MapInterface;
        static Canvas ms_Canvas;
        float m_InitialMarkerWidth;

        public EGRPlaceMarker Owner;
        public bool GroupMaster;
        public List<EGRPlaceMarker> Overlappers = new List<EGRPlaceMarker>();

        public EGRPlace Place { get; private set; }
        public int TileHash { get; set; }
        public RectTransform RectTransform => (RectTransform)transform;
        public Vector3 ScreenPoint { get; private set; }

        void Awake() {
            m_Text = GetComponentInChildren<TextMeshProUGUI>();
            m_Sprite = GetComponentInChildren<Image>();
            m_OriginalScale = transform.localScale;

            if (ms_MapInterface == null) {
                ms_MapInterface = ScreenManager.GetScreen<EGRScreenMapInterface>();
                ms_Canvas = ScreenManager.GetLayer(ms_MapInterface);
            }

            m_InitialMarkerWidth = m_Text.rectTransform.rect.width;
        }

        public void ClearOverlaps() {
            Owner = null;
            GroupMaster = false;
            Overlappers.Clear();
        }

        public void SetPlace(EGRPlace place) {
            Place = place;
            gameObject.SetActive(place != null);

            ClearOverlaps();

            if (Place != null) {
                name = place.Name;
                m_Text.text = Place.Name;
                m_Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(m_Text.preferredWidth, m_InitialMarkerWidth));
                m_Sprite.sprite = ms_MapInterface.GetSpriteForPlaceType(Place.Types[Mathf.Min(2, Place.Types.Length) - 1]);

                m_Fade = new EGRColorFade(Color.clear, Color.white, 2f);
            }
        }

        void LateUpdate() {
            Vector3 pos = Client.FlatMap.GeoToWorldPosition(new Vector2d(Place.Latitude, Place.Longitude));

            Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos);
            if (spos.z > 0f) {
                Vector3 tempSpos = spos;
                tempSpos.y = Screen.height - tempSpos.y;
                ScreenPoint = tempSpos;

                Vector2 point;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ms_Canvas.transform, spos, ms_Canvas.worldCamera, out point);
                transform.position = ms_Canvas.transform.TransformPoint(point);
            }

            transform.localScale = m_OriginalScale * ms_MapInterface.EvaluateMarkerScale(Client.FlatMap.Zoom / 21f);
            m_Sprite.color = Color.white.AlterAlpha(Mathf.Lerp(0f, ms_MapInterface.EvaluateMarkerOpacity(Client.FlatMap.Zoom / 21f), m_Fade.Delta - (Owner != null ? 0.5f : 0f)));

            if (Owner != null) {
                if (m_Fade.Done)
                    m_Fade.Reset();
            }

            if (!m_Fade.Done) {
                m_Fade.Update();
                //m_Text.color = m_Fade.Current;
            }
        }

        /*void FindOverlaps(EGRPlaceMarker prev) {
            if (HasGroup || Previous != null)
                return;

            Previous = prev;
            if (prev != null)
                HasGroup = true;

            foreach (EGRPlaceMarker marker in ms_MapInterface.ActiveMarkers) {
                if (marker == this || marker.HasGroup || marker == Previous)
                    continue;

                //overlap
                if (!marker.HasGroup && marker.RectTransform.RectOverlaps(RectTransform)) {
                    //marker.OVERLAPS = true;

                    OVERLAPS = true;
                    Next = marker;
                    marker.FindOverlaps(this);

                    break;
                }
            }
        }*/

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
