using DG.Tweening;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using static MRK.UI.EGRUI_Main.EGRScreen_MainSub03;
using System.Collections.Generic;

namespace MRK.UI {
    public class EGRScreenMainSub1 : EGRScreen {
        readonly GameObject[] m_Titles;
        int m_CurrentTitleIdx;
        ScrollRect m_Scroll;
        RectTransform m_Mask;
        float m_MaskSz;

        public EGRScreenMainSub1() {
            m_Titles = new GameObject[3];
        }

        protected override void OnScreenInit() {
            for (int i = 0; i < m_Titles.Length; i++) {
                m_Titles[i] = GetTransform((string)typeof(Others).GetField($"Title{i}", BindingFlags.Public | BindingFlags.Static).GetValue(null)).gameObject;
            }

            for (int i = 0; i < 23; i++) {
                Transform trans = GetTransform((string)typeof(Others).GetField($"zzTmp{i}", BindingFlags.Public | BindingFlags.Static).GetValue(null));
                Transform txtTrans = trans.Find("Text") ?? trans.Find("Glow/Text");
                string txt = txtTrans.GetComponent<TextMeshProUGUI>().text;
                int _i = i;

                trans.Find("Button").GetComponent<Button>().onClick.AddListener(() => {
                    Manager.MainScreen.ProcessAction(1, _i, txt);
                });
            }

            m_Scroll = GetElement<ScrollRect>("vMask/Scroll View");
            m_Scroll.verticalScrollbar.onValueChanged.AddListener(OnScrollValueChanged);

            m_Mask = GetTransform("vMask") as RectTransform;
            m_MaskSz = m_Mask.sizeDelta.y;

            m_CurrentTitleIdx = 0;
            UpdateTitleVisibility();
        }

        bool IsVisibleFrom(RectTransform rectTransform, Camera camera) {
            Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height); // Screen space bounds (assumes camera renders across the entire screen)
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);

            int visibleCorners = 0;
            Vector3 tempScreenSpaceCorner; // Cached
            for (var i = 0; i < objectCorners.Length; i++) // For each corner in rectTransform
            {
                tempScreenSpaceCorner = camera.WorldToScreenPoint(objectCorners[i]); // Transform world space position of corner to screen space
                if (screenBounds.Contains(tempScreenSpaceCorner)) // If the corner is inside the screen
                {
                    visibleCorners++;
                }
            }

            return visibleCorners == 4;
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            List<Graphic> glist = new List<Graphic>();
            foreach (GameObject go in m_Titles) {
                glist.AddRange(go.GetComponentsInChildren<Graphic>());
            }

            m_LastGraphicsBuf = glist.ToArray();

            PushGfxState(EGRGfxState.Color | EGRGfxState.Position);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, TweenMonitored(0.3f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.3f + i * 0.03f))
                        .ChangeStartValue(-1f * gfx.transform.position)
                        .SetEase(Ease.OutSine);
            }

            DOTween.To(() => m_Mask.sizeDelta.y, x=> m_Mask.sizeDelta = new Vector2(m_Mask.sizeDelta.x, x), m_MaskSz, 0.4f)
                .ChangeStartValue(-1500f)
                .SetEase(Ease.OutSine);
        }

        protected override void OnScreenShow() {
            Manager.MainScreen.ActiveScroll = m_Scroll.horizontalScrollbar;
        }

        protected override void OnScreenHide() {
            if (Manager.MainScreen.ActiveScroll == m_Scroll.horizontalScrollbar)
                Manager.MainScreen.ActiveScroll = null;
        }

        int GetDesiredTitleIdx(float pos) {
            if (pos <= 0.04794369f)
                return 2;

            if (pos <= 0.4221286f)
                return 1;

            return 0;
        }

        void UpdateTitleVisibility() {
            for (int i = 0; i < m_Titles.Length; i++) {
                m_Titles[i].SetActive(i == m_CurrentTitleIdx);
            }
        }

        void OnScrollValueChanged(float newVal) {
            int idx = GetDesiredTitleIdx(newVal);
            if (idx != m_CurrentTitleIdx) {
                m_CurrentTitleIdx = idx;
                UpdateTitleVisibility();
            }
        }
    }
}
