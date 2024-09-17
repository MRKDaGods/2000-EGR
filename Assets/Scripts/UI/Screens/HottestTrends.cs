using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_HottestTrends;

namespace MRK.UI
{
    public class HottestTrends : Screen
    {
        private class Line
        {
            private GameObject _object;
            private TextMeshProUGUI _rank;
            private TextMeshProUGUI _name;
            private TextMeshProUGUI _val;
            private Button _maskButton;
            private Button _more;
            private Scrollbar _scroll;
            private bool _moreShown;
            private object _tween;

            public Transform Transform
            {
                get
                {
                    return _object.transform;
                }
            }

            public Line(GameObject obj)
            {
                _object = Instantiate(obj, obj.transform.parent);
                _rank = _object.transform.Find("Scroll View/Viewport/Content/Rank").GetComponent<TextMeshProUGUI>();
                _name = _object.transform.Find("Scroll View/Viewport/Content/Name").GetComponent<TextMeshProUGUI>();
                _val = _object.transform.Find("Scroll View/Viewport/Content/Val").GetComponent<TextMeshProUGUI>();
                _maskButton = _object.transform.Find("Scroll View/Viewport/Content/MaskButton").GetComponent<Button>();
                _more = _object.transform.Find("Scroll View/Viewport/Content/Button").GetComponent<Button>();
                _scroll = _object.transform.Find("Scroll View").GetComponent<ScrollRect>().horizontalScrollbar;

                _maskButton.onClick.AddListener(OnMaskButtonClick);
            }

            private void OnMaskButtonClick()
            {
                if (_tween != null)
                    DOTween.Kill(_tween);

                float val = _moreShown ? 0f : 1f;
                _moreShown = val == 1f;

                _tween = DOTween.To(() => _scroll.value, x => _scroll.value = x, val, 0.5f)
                    .SetEase(Ease.OutBack);
            }

            public void SetData(EGRPlaceStatistics data, int index)
            {
                _rank.text = data.Rank.ToString();
                _name.text = data.Name;

                string unit = "";
                float likes = data.Likes;
                if (likes >= 1000000f)
                {
                    likes /= 1000000f;
                    unit = "M";
                }
                else if (likes >= 1000f)
                {
                    likes /= 1000f;
                    unit = "K";
                }

                string __repl(string s)
                {
                    if (s[s.Length - 1] == '.')
                        s = s.Replace(".", "");

                    return s;
                }

                string txt = $"{likes:F2}";
                _val.text = $"{__repl(txt.Substring(0, Mathf.Min(4, txt.Length)))}{unit}";

                _moreShown = false;
                _scroll.value = 0f;

                _tween = DOTween.To(() => _scroll.value, x => _scroll.value = x, 0f, 0.6f + 0.03f * index)
                    .ChangeStartValue(1.2f)
                    .SetEase(Ease.OutBack);
            }

            public void SetActive(bool active)
            {
                _object.SetActive(active);
            }
        }

        private GameObject _itemPrefab;
        private readonly List<Line> _lines;
        private TextMeshProUGUI _loadingTxt;

        public override bool CanChangeBar
        {
            get
            {
                return true;
            }
        }

        public override uint BarColor
        {
            get
            {
                return 0xB4000000;
            }
        }

        public HottestTrends()
        {
            _lines = new List<Line>();
        }

        protected override void OnScreenInit()
        {
            GetElement<Button>(Images.Blur).onClick.AddListener(OnBlurClick);

            _itemPrefab = GetTransform("VerticalLayout/Item").gameObject;
            _itemPrefab.SetActive(false);

            _loadingTxt = GetElement<TextMeshProUGUI>(Labels.Loading);
        }

        private void OnBlurClick()
        {
            HideScreen();
        }

        private IEnumerator FetchFakeTestData()
        {
            _loadingTxt.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            OnDataReceived(new List<EGRPlaceStatistics>() {
                new EGRPlaceStatistics{Rank = 1, Name = "Ammar Stores", Likes = 2192103},
                new EGRPlaceStatistics{Rank = 2, Name = "McDonald's", Likes = 999954},
                new EGRPlaceStatistics{Rank = 3, Name = "EYAD STORES", Likes = 2002},
                new EGRPlaceStatistics{Rank = 4, Name = "Salah Market", Likes = 143}
            });
        }

        private void OnDataReceived(List<EGRPlaceStatistics> stats)
        {
            _loadingTxt.gameObject.SetActive(false);

            Debug.Log(stats.Count);

            //lets see if we need to create or destroy lines?
            int delta = stats.Count - _lines.Count;
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    Line line = new Line(_itemPrefab);
                    //register gfx state, unpleasant shit would happen if not
                    foreach (Graphic gfx in line.Transform.GetComponentsInChildren<Graphic>())
                    {
                        PushGfxStateManual(gfx, GfxStates.Color);
                    }

                    _lines.Add(line);
                }
            }
            else if (delta < 0)
            {
                //set active=false
                //trailing lines
                //delta is NEGATIVE
                for (int i = _lines.Count + delta; i < _lines.Count; i++)
                {
                    _lines[i].SetActive(false);
                }
            }

            for (int i = 0; i < stats.Count; i++)
            {
                _lines[i].SetData(stats[i], i);
                _lines[i].SetActive(true);
            }
        }

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>(true);

            PushGfxState(GfxStates.Color);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                gfx.DOColor(gfx.color, 0.3f + i * 0.03f)
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine);
            }
        }

        protected override bool OnScreenHideAnim(Action callback)
        {
            base.OnScreenHideAnim(callback);

            _lastGraphicsBuf = transform.GetComponentsInChildren<Graphic>();

            SetTweenCount(_lastGraphicsBuf.Length);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                _lastGraphicsBuf[i].DOColor(Color.clear, 0.3f)
                    .SetEase(Ease.OutSine)
                    .OnComplete(OnTweenFinished);
            }

            return true;
        }

        protected override void OnScreenShow()
        {
            StartCoroutine(FetchFakeTestData());
        }

        protected override void OnScreenHide()
        {
            StopAllCoroutines();
            OnDataReceived(new List<EGRPlaceStatistics>());
            Client.ActiveEGRCamera.ResetStates();
        }
    }
}
