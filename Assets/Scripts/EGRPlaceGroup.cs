using MRK.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DG.Tweening;
using System;

namespace MRK {
    public class EGRPlaceGroup : EGRBehaviour {
        EGRColorFade m_Fade;
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
        bool m_Freeing;
        Action m_FreeCallback;

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

            GetComponent<Button>().onClick.AddListener(OnGroupClick);
        }

        public void SetOwner(EGRPlaceMarker marker) {
            if (Owner != marker) {
                Owner = marker;
                gameObject.SetActive(marker != null);

                if (Owner != null) {
                    m_OwnerDirty = true;
                    m_Freeing = false;
                    m_FreeCallback = null;

                    if (m_Fade == null) {
                        m_Fade = new EGRColorFade(Color.clear, Color.white, 2f);
                    }
                    else {
                        m_Fade.Reset();
                        m_Fade.SetColors(Color.clear, Color.white);
                    }
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
            m_Fade.Reset();
            m_Fade.SetColors(m_Text.color, Color.white.AlterAlpha(0f));
        }

        void LateUpdate() {
            if (Owner != null) {
                if (!m_Fade.Done) {
                    m_Fade.Update();
                }
                else if (m_Freeing)
                    m_FreeCallback?.Invoke();

                if (!m_Freeing) {
                    Vector2 center = Client.PlaceManager.GetOverlapCenter(Owner);
                    center.y = Screen.height - center.y;

                    if (Owner.LastOverlapCenter.IsNotEqual(center)) {
                        Owner.LastOverlapCenter = center;
                        Vector3 target = EGRPlaceMarker.ScreenToMarkerSpace(center);

                        if ((!m_OwnerDirty && Time.time > m_TransitionEndTime) || !new Rect(0f, 0f, Screen.width, Screen.height).Contains(center)) {
                            transform.position = EGRPlaceMarker.ScreenToMarkerSpace(center);
                        }
                        else {
                            if (m_Tweener != null && m_Tweener.position < 1f) {
                                m_Tweener.Kill();

                                gameObject.transform.DOMove(target, m_TransitionEndTime - Time.time)
                                    .SetEase(Ease.OutExpo);
                                //m_Tweener.ChangeValues(transform.position, target, m_TransitionEndTime - Time.time);
                            }
                            else {
                                m_Tweener = gameObject.transform.DOMove(target, 0.5f)
                                    .SetEase(Ease.OutExpo);
                            }

                            if (m_OwnerDirty) {
                                m_OwnerDirty = false;
                                m_TransitionEndTime = Time.time + 0.5f;
                            }
                        }
                    }

                    float zoomProg = Client.FlatMap.Zoom / 21f;
                    transform.localScale = m_OriginalScale * ms_MapInterface.EvaluateMarkerScale(zoomProg) * 1.2f;

                    Color color = m_Fade.Current.AlterAlpha(ms_MapInterface.EvaluateMarkerOpacity(zoomProg) * 1.5f);
                    foreach (Graphic gfx in m_Gfx)
                        gfx.color = color;
                }
                else {
                    foreach (Graphic gfx in m_Gfx)
                        gfx.color = m_Fade.Current;
                }
            }
        }

        void OnGroupClick() {
            ms_GroupPopup.SetGroup(this);
            ms_GroupPopup.ShowScreen();
        }
    }
}
