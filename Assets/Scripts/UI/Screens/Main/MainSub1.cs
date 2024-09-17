using DG.Tweening;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_MainSub03;

namespace MRK.UI
{
    public class MainSub1 : Screen
    {
        private class Title
        {
            public class GraphicBuffer
            {
                public object Tween;
                public Graphic Gfx;
            }

            private Transform _transform;
            private GraphicBuffer[] _graphicBuffer;
            private bool _active;

            public GraphicBuffer[] GraphicBuffers
            {
                get
                {
                    return _graphicBuffer;
                }
            }

            public Title(Transform trans)
            {
                _transform = trans;

                Graphic[] gfx = _transform.GetComponentsInChildren<Graphic>(true);
                _graphicBuffer = new GraphicBuffer[gfx.Length];
                for (int i = 0; i < gfx.Length; i++)
                    _graphicBuffer[i] = new GraphicBuffer { Gfx = gfx[i], Tween = null };
            }

            public void SetActive(bool active, bool force = false)
            {
                if (_active == active && !force)
                    return;

                _active = active;

                foreach (GraphicBuffer gfx in _graphicBuffer)
                {
                    if (gfx.Tween != null)
                    {
                        DOTween.Kill(gfx.Tween);
                    }

                    gfx.Tween = gfx.Gfx.DOFade(active ? 1f : 0f, 0.3f);
                }
            }
        }

        private readonly Title[] _titles;
        private int _currentTitleIdx;
        private ScrollRect _scroll;
        private RectTransform _mask;
        private float _maskSz;

        public MainSub1()
        {
            _titles = new Title[3];
        }

        protected override void OnScreenInit()
        {
            for (int i = 0; i < _titles.Length; i++)
            {
                _titles[i] = new Title(GetTransform((string)typeof(Others).GetField($"Title{i}", BindingFlags.Public | BindingFlags.Static).GetValue(null)));
            }

            for (int i = 0; i < 23; i++)
            {
                Transform trans = GetTransform((string)typeof(Others).GetField($"zzTmp{i}", BindingFlags.Public | BindingFlags.Static).GetValue(null));
                Transform txtTrans = trans.Find("Text") ?? trans.Find("Glow/Text");
                string txt = txtTrans.GetComponent<TextMeshProUGUI>().text;
                int _i = i;

                trans.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    ScreenManager.MainScreen.ProcessAction(1, _i, txt);
                });
            }

            _scroll = GetElement<ScrollRect>("vMask/Scroll View");
            _scroll.verticalScrollbar.onValueChanged.AddListener(OnScrollValueChanged);

            _mask = GetTransform("vMask") as RectTransform;
            _maskSz = _mask.sizeDelta.y;

            _currentTitleIdx = 0;
            UpdateTitleVisibility();
        }

        private bool IsVisibleFrom(RectTransform rectTransform, Camera camera)
        {
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

        protected override void OnScreenShowAnim()
        {
            base.OnScreenShowAnim();

            List<Graphic> glist = new List<Graphic>();
            foreach (Title title in _titles)
            {
                foreach (Title.GraphicBuffer buf in title.GraphicBuffers)
                {
                    glist.Add(buf.Gfx);
                }
            }

            _lastGraphicsBuf = glist.ToArray();

            PushGfxState(GfxStates.Color | GfxStates.Position);

            for (int i = 0; i < _lastGraphicsBuf.Length; i++)
            {
                Graphic gfx = _lastGraphicsBuf[i];

                /* gfx.DOColor(gfx.color, TweenMonitored(0.3f))
                    .ChangeStartValue(Color.clear)
                    .SetEase(Ease.OutSine); */

                gfx.transform.DOMoveX(gfx.transform.position.x, TweenMonitored(0.3f + i * 0.03f))
                        .ChangeStartValue(-1f * gfx.transform.position)
                        .SetEase(Ease.OutSine);
            }

            DOTween.To(() => _mask.sizeDelta.y, x => _mask.sizeDelta = new Vector2(_mask.sizeDelta.x, x), _maskSz, 0.4f)
                .ChangeStartValue(-1500f)
                .SetEase(Ease.OutSine);

            UpdateTitleVisibility(true);
        }

        protected override void OnScreenShow()
        {
            ScreenManager.MainScreen.ActiveScroll = _scroll.horizontalScrollbar;
        }

        protected override void OnScreenHide()
        {
            if (ScreenManager.MainScreen.ActiveScroll == _scroll.horizontalScrollbar)
                ScreenManager.MainScreen.ActiveScroll = null;
        }

        private int GetDesiredTitleIdx(float pos)
        {
            if (pos <= 0.04794369f)
                return 2;

            if (pos <= 0.4221286f)
                return 1;

            return 0;
        }

        private void UpdateTitleVisibility(bool force = false)
        {
            for (int i = 0; i < _titles.Length; i++)
            {
                _titles[i].SetActive(i == _currentTitleIdx, force);
            }
        }

        private void OnScrollValueChanged(float newVal)
        {
            int idx = GetDesiredTitleIdx(newVal);
            if (idx != _currentTitleIdx)
            {
                _currentTitleIdx = idx;
                UpdateTitleVisibility();
            }
        }
    }
}
