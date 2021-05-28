using MRK.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK {
    public class EGRPlaceGroup : EGRBehaviour {
        EGRColorFade m_Fade;
        Image m_Sprite;
        bool m_OwnerDirty;
        Vector3 m_OriginalScale;
        Graphic[] m_Gfx;
        static EGRScreenMapInterface ms_MapInterface;
        TextMeshProUGUI m_Text;

        public EGRPlaceMarker Owner { get; private set; }

        void Awake() {
            m_Sprite = transform.Find("Sprite").GetComponent<Image>();
            m_Gfx = transform.GetComponentsInChildren<Graphic>();
            m_OriginalScale = transform.localScale;

            m_Text = transform.Find("TextContainer/Text").GetComponent<TextMeshProUGUI>();

            if (ms_MapInterface == null) {
                ms_MapInterface = ScreenManager.GetScreen<EGRScreenMapInterface>();
            }
        }

        public void SetOwner(EGRPlaceMarker marker) {
            if (Owner == marker)
                return;

            Owner = marker;
            gameObject.SetActive(marker != null);

            if (Owner != null) {
                m_OwnerDirty = true;

                if (m_Fade == null) {
                    m_Fade = new EGRColorFade(Color.clear, Color.white, 2f);
                }
                else {
                    m_Fade.Reset();
                    m_Fade.SetColors(Color.clear, Color.white);
                }
            }

            /*if (!ms_ScreenRect.HasValue) {
                ms_ScreenRect = new Rect(0f, 0f, Screen.width, Screen.height);
            }

            if (Owner != null) {
                int hw = Screen.width / 2;
                int hh = Screen.height / 2;

                int rawX = Random.Range(-hw, hw);
                int rawY = Random.Range(-hh, hh);

                int pivotX;
                if (rawX > 0)
                    pivotX = 0;
                else if (rawX < 0)
                    pivotX = Screen.width;
                else
                    pivotX = hw;

                int pivotY;
                if (rawY > 0)
                    pivotY = 0;
                else if (rawY < 0)
                    pivotY = Screen.height;
                else
                    pivotY = hh;

                m_InitialPosition = EGRPlaceMarker.ScreenToMarkerSpace(new Vector2(pivotX + rawX, pivotY + rawY));
                m_InitialDirty = true;
            } */
        }

        void LateUpdate() {
            if (Owner != null) {
                if (m_OwnerDirty) {
                    m_OwnerDirty = false;

                    //m_Fade.Reset();
                }

                Vector2 center = Client.PlaceManager.GetOverlapCenter(Owner);
                center.y = Screen.height - center.y;
                transform.position = EGRPlaceMarker.ScreenToMarkerSpace(center);

                if (!m_Fade.Done) {
                    m_Fade.Update();
                }

                float zoomProg = Client.FlatMap.Zoom / 21f;
                transform.localScale = m_OriginalScale * ms_MapInterface.EvaluateMarkerScale(zoomProg) * 1.2f;

                Color color = m_Fade.Current.AlterAlpha(ms_MapInterface.EvaluateMarkerOpacity(zoomProg) * 1.2f);
                foreach (Graphic gfx in m_Gfx)
                    gfx.color = color;

                /*if (Time.time - m_LastPositionTime > 0.2f) {
                    //m_LastPositionTime = Time.time;

                    Vector2 center = Client.PlaceManager.GetOverlapCenter(Owner);
                    center.y = Screen.height - center.y;

                    if (m_LastCenter.IsNotEqual(center)) {
                        m_LastCenter = center;
                        m_TargetPosition = EGRPlaceMarker.ScreenToMarkerSpace(center);

                        if (!ms_ScreenRect.Value.Contains(center) && m_InitialDirty) {
                            transform.position = EGRPlaceMarker.ScreenToMarkerSpace(center);
                            return;
                        }

                        if (m_PosTween.IsValidTween()) {
                            //DOTween.Kill(m_PosTween);
                        }

                        var tween = gameObject.transform.DOMove(m_TargetPosition, m_InitialDirty ? 0.5f : 0.05f)
                            .SetEase(Ease.OutSine);

                        if (m_InitialDirty) {
                            m_InitialDirty = false;
                            tween.ChangeStartValue(m_InitialPosition);
                        }

                        tween.intId = EGRTweenIDs.IntId;
                    }
                } */
            }
        }
    }
}
