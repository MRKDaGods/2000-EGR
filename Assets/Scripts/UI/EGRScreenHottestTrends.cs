using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using static MRK.UI.EGRUI_Main.EGRScreen_HottestTrends;

namespace MRK.UI {
    public class EGRScreenHottestTrends : EGRScreen {
        class Line {
            GameObject m_Object;
            TextMeshProUGUI m_Rank;
            TextMeshProUGUI m_Name;
            TextMeshProUGUI m_Val;
            Button m_MaskButton;
            Button m_More;
            Scrollbar m_Scroll;
            bool m_MoreShown;
            object m_Tween;

            public Transform Transform => m_Object.transform;

            public Line(GameObject obj) {
                m_Object = Instantiate(obj, obj.transform.parent);
                m_Rank = m_Object.transform.Find("Scroll View/Viewport/Content/Rank").GetComponent<TextMeshProUGUI>();
                m_Name = m_Object.transform.Find("Scroll View/Viewport/Content/Name").GetComponent<TextMeshProUGUI>();
                m_Val = m_Object.transform.Find("Scroll View/Viewport/Content/Val").GetComponent<TextMeshProUGUI>();
                m_MaskButton = m_Object.transform.Find("Scroll View/Viewport/Content/MaskButton").GetComponent<Button>();
                m_More = m_Object.transform.Find("Scroll View/Viewport/Content/Button").GetComponent<Button>();
                m_Scroll = m_Object.transform.Find("Scroll View").GetComponent<ScrollRect>().horizontalScrollbar;

                m_MaskButton.onClick.AddListener(OnMaskButtonClick);
            }

            void OnMaskButtonClick() {
                if (m_Tween != null)
                    DOTween.Kill(m_Tween);
                
                float val = m_MoreShown ? 0f : 1f;
                m_MoreShown = val == 1f;

                m_Tween = DOTween.To(() => m_Scroll.value, x => m_Scroll.value = x, val, 0.5f)
                    .SetEase(Ease.OutBack);
            }

            public void SetData(EGRPlaceStatistics data, int index) {
                m_Rank.text = data.Rank.ToString();
                m_Name.text = data.Name;

                string unit = "";
                float likes = data.Likes;
                if (likes >= 1000000f) {
                    likes /= 1000000f;
                    unit = "M";
                }
                else if (likes >= 1000f) {
                    likes /= 1000f;
                    unit = "K";
                }

                string __repl(string s) {
                    if (s[s.Length - 1] == '.')
                        s = s.Replace(".", "");

                    return s;
                }

                string txt = $"{likes:F2}";
                m_Val.text = $"{__repl(txt.Substring(0, Mathf.Min(4, txt.Length)))}{unit}";

                m_MoreShown = false;
                m_Scroll.value = 0f;

                m_Tween = DOTween.To(() => m_Scroll.value, x => m_Scroll.value = x, 0f, 0.6f + 0.03f * index)
                    .ChangeStartValue(1.2f)
                    .SetEase(Ease.OutBack);
            }

            public void SetActive(bool active) {
                m_Object.SetActive(active);
            }
        }

        GameObject m_ItemPrefab;
        readonly List<Line> m_Lines;
        TextMeshProUGUI m_LoadingTxt;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0xB4000000;

        public EGRScreenHottestTrends() {
            m_Lines = new List<Line>();
        }

        protected override void OnScreenInit() {
            GetElement<Button>(Images.Blur).onClick.AddListener(OnBlurClick);

            m_ItemPrefab = GetTransform("VerticalLayout/Item").gameObject;
            m_ItemPrefab.SetActive(false);

            m_LoadingTxt = GetElement<TextMeshProUGUI>(Labels.Loading);
        }

        void OnBlurClick() {
            HideScreen();
        }

        IEnumerator FetchFakeTestData() {
            m_LoadingTxt.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            OnDataReceived(new List<EGRPlaceStatistics>() {
                new EGRPlaceStatistics{Rank = 1, Name = "Ammar Stores", Likes = 2192103},
                new EGRPlaceStatistics{Rank = 2, Name = "McDonald's", Likes = 999954},
                new EGRPlaceStatistics{Rank = 3, Name = "EYAD STORES", Likes = 2002},
                new EGRPlaceStatistics{Rank = 4, Name = "Salah Market", Likes = 143}
            });
        }

        void OnDataReceived(List<EGRPlaceStatistics> stats) {
            m_LoadingTxt.gameObject.SetActive(false);

            //lets see if we need to create or destroy lines?
            int delta = stats.Count - m_Lines.Count;
            if (delta > 0) {
                for (int i = 0; i < delta; i++) {
                    Line line = new Line(m_ItemPrefab);
                    //register gfx state, unpleasant shit would happen if not
                    foreach (Graphic gfx in line.Transform.GetComponentsInChildren<Graphic>()) {
                        PushGfxStateManual(gfx, EGRGfxState.Color);
                    }

                    m_Lines.Add(line);
                }
            }
            else if (delta < 0) {
                //set active=false
                //trailing lines
                //delta is NEGATIVE
                for (int i = m_Lines.Count + delta; i < m_Lines.Count; i++) {
                    m_Lines[i].SetActive(false);
                }
            }

            for (int i = 0; i < stats.Count; i++) {
                m_Lines[i].SetData(stats[i], i);
                m_Lines[i].SetActive(true);
            }
        }

        protected override void OnScreenShowAnim() {
            base.OnScreenShowAnim();

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);

            PushGfxState(EGRGfxState.Color);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                Graphic gfx = m_LastGraphicsBuf[i];

                gfx.DOColor(gfx.color, 0.3f + i * 0.03f)
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback) {
            base.OnScreenHideAnim(callback);

            m_LastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            SetTweenCount(m_LastGraphicsBuf.Length);

            for (int i = 0; i < m_LastGraphicsBuf.Length; i++) {
                m_LastGraphicsBuf[i].DOColor(Color.clear, 0.3f)
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        protected override void OnScreenShow() {
            StartCoroutine(FetchFakeTestData());
        }

        protected override void OnScreenHide() {
            StopAllCoroutines();
            OnDataReceived(new List<EGRPlaceStatistics>());
            Client.ActiveEGRCamera.ResetStates();
        }
    }
}
