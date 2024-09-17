//#define DEBUG_PLACES

using MRK.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace MRK {
    public class EGRPlaceMarker : BaseBehaviour {
        TextMeshProUGUI m_Text;
        EGRColorFade m_Fade;
        Vector3 m_OriginalScale;
        Image m_Sprite;
        static HUD ms_MapInterface;
        static Canvas ms_Canvas;
        float m_InitialMarkerWidth;
        EGRPlaceMarker m_OverlapOwner;
        EGRPlaceMarker m_ImmediateOverlapOwner; //always up to date
        readonly static Color ms_ClearWhite;
        RectTransform m_TextContainer;
        Graphic[] m_Gfx;
        (int, int) m_ZoomBounds;
        Color m_LastColor;
        float m_CreationZoom;

        public EGRPlace Place { get; private set; }
        public int TileHash { get; set; }
        public RectTransform RectTransform => (RectTransform)transform;
        public Vector3 ScreenPoint { get; private set; }
        public EGRPlaceMarker OverlapOwner {
            get => m_ImmediateOverlapOwner;
            set => m_ImmediateOverlapOwner = value;
        }
        public bool IsOverlapMaster { get; set; }
        public List<EGRPlaceMarker> Overlappers { get; private set; }
        public Vector2 LastOverlapCenter { get; set; }
        public bool OverlapCheckFlag { get; set; }

        static EGRPlaceMarker() {
            ms_ClearWhite = Color.white.AlterAlpha(0f);
        }

        public EGRPlaceMarker() {
            Overlappers = new List<EGRPlaceMarker>();
        }

        void Awake() {
            m_Text = transform.Find("TextContainer/Text").GetComponent<TextMeshProUGUI>();
            m_Sprite = transform.Find("Sprite").GetComponent<Image>();
            m_OriginalScale = transform.localScale;

            if (ms_MapInterface == null) {
                ms_MapInterface = ScreenManager.GetScreen<HUD>();
                //ms_Canvas = ScreenManager.GetLayer(ms_MapInterface);
                ms_Canvas = ScreenManager.GetScreenSpaceLayer(0);
            }

            m_TextContainer = (RectTransform)m_Text.transform.parent;
            m_InitialMarkerWidth = m_TextContainer.rect.width;

            m_Sprite.GetComponent<Button>().onClick.AddListener(OnMarkerClick);

            m_Gfx = transform.GetComponentsInChildren<Graphic>().Where(x => x.transform != transform).ToArray();

            //name = Random.Range(0, 100000000).ToString();
        }

        public void ClearOverlaps() {
            m_ImmediateOverlapOwner = null;
            IsOverlapMaster = false;
            Overlappers.Clear();

            if (m_OverlapOwner != m_ImmediateOverlapOwner)
                OverlapCheckFlag = false;
        }

        public void SetPlace(EGRPlace place) {
            if (Place == place)
                return;

            Place = place;
            OverlapCheckFlag = false;

            ClearOverlaps();

            if (Place != null) {
                if (!gameObject.activeSelf) {
                    gameObject.SetActive(true);
                }

                //name = place.Name;
                m_Text.text = Place.Name;
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(m_Text.GetPreferredValues().x + 60f, m_InitialMarkerWidth)); //38f is our label padding
                m_Sprite.sprite = ms_MapInterface.GetSpriteForPlaceType(Place.Types[Mathf.Min(2, Place.Types.Length) - 1]);

                foreach (Graphic gfx in m_Gfx)
                    gfx.color = ms_ClearWhite;

                if (m_Fade == null) {
                    m_Fade = new EGRColorFade(ms_ClearWhite, Color.white, 1.5f);
                }
                else {
                    m_Fade.Reset();
                    m_Fade.SetColors(ms_ClearWhite, Color.white);
                }

                m_ZoomBounds = Client.PlaceManager.GetPlaceZoomBoundaries(Place);
                m_CreationZoom = Client.FlatMap.Zoom;
            }
            else {
                if (gameObject.activeSelf) {
                    gameObject.SetActive(false);
                }
            }
        }

        void LateUpdate() {
            Vector3 pos = Client.FlatMap.GeoToWorldPosition(new Vector2d(Place.Latitude, Place.Longitude));
            Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos);

            if (spos.z > 0f) {
                Vector3 tempSpos = spos;
                tempSpos.y = Screen.height - tempSpos.y;
                ScreenPoint = tempSpos;

                transform.position = ScreenToMarkerSpace(spos);
            }
            else
                transform.position = ScreenToMarkerSpace(new Vector2(-1000f, -1000f));

            float zoomProg = Client.FlatMap.Zoom / 21f;
            transform.localScale = m_OriginalScale * ms_MapInterface.EvaluateMarkerScale(zoomProg);

            if (!OverlapCheckFlag) {
                //m_Text.color = ms_ClearWhite;
                //m_Sprite.color = ms_ClearWhite;

                m_Fade.Reset();
                m_Fade.SetColors(m_Text.color, m_Fade.Final);
                m_Sprite.raycastTarget = false;
                return;
            }

            if (m_OverlapOwner != m_ImmediateOverlapOwner) {
                m_OverlapOwner = m_ImmediateOverlapOwner;
                m_Fade.Reset();

                if (OverlapOwner == null) {
                    m_Fade.SetColors(ms_ClearWhite, Color.white);
                }
                else {
                    m_Fade.SetColors(m_LastColor, ms_ClearWhite);
                    m_Sprite.raycastTarget = false;
                }
            }

            if (!m_Fade.Done) {
                m_Fade.Update();
            }

            m_LastColor = m_Fade.Final.a <= Mathf.Epsilon ? m_Fade.Current : m_Fade.Current.AlterAlpha(GetAlphaFromZoom());
            foreach (Graphic gfx in m_Gfx)
                gfx.color = m_LastColor;

            if (m_OverlapOwner == null)
                m_Sprite.raycastTarget = m_LastColor.a > 0.2f;
        }

        float GetAlphaFromZoom() {
            float curZoom = Client.FlatMap.Zoom;
            float delta = (curZoom - m_CreationZoom) / (21f - m_CreationZoom);
            return ms_MapInterface.EvaluateMarkerOpacity(delta);
        }

        void OnMarkerClick() {
            Debug.Log(Place?.CID);
        }

        public static Vector3 ScreenToMarkerSpace(Vector2 spos) {
            if (ms_Canvas == null) {
                ms_Canvas = ScreenManager.Instance.GetScreenSpaceLayer(0);
            }

            Vector2 point;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ms_Canvas.transform, spos, ms_Canvas.worldCamera, out point);
            return ms_Canvas.transform.TransformPoint(point);
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
