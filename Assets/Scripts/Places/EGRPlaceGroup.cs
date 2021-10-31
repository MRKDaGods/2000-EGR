using MRK.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DG.Tweening;
using System;

namespace MRK {
    public class EGRPlaceGroup : MRKBehaviour {
        Image m_Sprite;
        bool m_OwnerDirty;
        Vector3 m_OriginalScale;
        Graphic[] m_Gfx;
        static EGRScreenMapInterface ms_MapInterface;
        TextMeshProUGUI m_Text;
        float m_InitialTextWidth;
        RectTransform m_TextContainer;
        static EGRPopupPlaceGroup ms_GroupPopup;
        float m_TransitionEndTime;
        Tweener m_Tweener;
        [SerializeField]
        bool m_Freeing;
        Action m_FreeCallback;
        Vector2 m_LastCenter;
        [SerializeField]
        Vector3 m_InitialPosition;
        int m_CreationZoom;

        public EGRPlaceMarker Owner { get; private set; }

        void Awake() {
            m_Sprite = transform.Find("Sprite").GetComponent<Image>();
            m_Gfx = transform.GetComponentsInChildren<Graphic>().Where(x => x.transform != transform).ToArray();
            m_OriginalScale = transform.localScale;

            m_Text = transform.Find("TextContainer/Text").GetComponent<TextMeshProUGUI>();
            m_TextContainer = (RectTransform)m_Text.transform.parent;
            m_InitialTextWidth = m_TextContainer.rect.width;

            if (ms_MapInterface == null) {
                ms_MapInterface = ScreenManager.GetScreen<EGRScreenMapInterface>();
            }

            if (ms_GroupPopup == null) {
                ms_GroupPopup = ScreenManager.GetPopup<EGRPopupPlaceGroup>();
            }

            m_Sprite.GetComponent<Button>().onClick.AddListener(OnGroupClick);

            m_InitialPosition = EGRPlaceMarker.ScreenToMarkerSpace(new Vector2(-500f, -500f));
            transform.position = m_InitialPosition;
        }

        public void SetOwner(EGRPlaceMarker marker) {
            if (Owner != marker) {
                Owner = marker;
                gameObject.SetActive(marker != null);

                if (Owner != null) {
                    m_OwnerDirty = true;
                    m_Freeing = false;
                    m_FreeCallback = null;
                    m_CreationZoom = Client.FlatMap.AbsoluteZoom;
                }
            }

            UpdateText();
        }

        void UpdateText() {
            if (Owner == null) {
                m_Text.text = "";
                return;
            }

            string txt = Owner.Place.Name;
            foreach (EGRPlaceMarker marker in Owner.Overlappers) {
                txt += $", {marker.Place.Name}";
            }

            m_Text.text = txt;
            m_TextContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(m_Text.GetPreferredValues().x + 60f, m_InitialTextWidth));
        }

        public void Free(Action callback) {
            m_Freeing = true;
            m_FreeCallback = callback;

            //capture the geo location of center when Free was called
            //m_FreeGeoLocation = Client.FlatMap.WorldToGeoPosition(Client.ActiveCamera.ScreenToWorldPoint(new Vector3(m_LastCenter.x, 
            //    m_LastCenter.y, Client.ActiveCamera.transform.position.z)));

            //m_OutsideScreenspace = Client.ActiveCamera.ScreenToViewportPoint(m_LastCenter).sqrMagnitude < 0.3 * 0.3f;
        }

        void LateUpdate() {
            if (Owner != null) {
                if (!m_Freeing) {
                    Vector2 center = Owner.ScreenPoint; //Client.PlaceManager.GetOverlapCenter(Owner);
                    center.y = Screen.height - center.y;

                    if (Owner.LastOverlapCenter.IsNotEqual(center)) {
                        Owner.LastOverlapCenter = center;
                        m_LastCenter = center;
                        transform.position = EGRPlaceMarker.ScreenToMarkerSpace(center);
                    }

                    float zoomProg = Client.FlatMap.Zoom / 21f;
                    transform.localScale = m_OriginalScale * ms_MapInterface.EvaluateMarkerScale(zoomProg) * 1.2f;

                    float distance = Mathf.Abs(Client.FlatMap.Zoom - m_CreationZoom);
                    float opacity = distance > 5f ? 1f - Mathf.Clamp((distance - 5f) / 5f, 0f, 1f) : 1f;

                    Color color = Color.white.AlterAlpha(opacity); // ms_MapInterface.EvaluateMarkerOpacity(opacity) * 1.5f);
                    foreach (Graphic gfx in m_Gfx)
                        gfx.color = color;

                    m_Sprite.raycastTarget = color.a > 0.2f;
                }
                else {
                    foreach (Graphic gfx in m_Gfx)
                        gfx.color = Color.clear;

                    m_FreeCallback?.Invoke();
                }
            }
        }

        void OnGroupClick() {
            ms_GroupPopup.SetGroup(this);
            ms_GroupPopup.ShowScreen();
        }
    }
}
