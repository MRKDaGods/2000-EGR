using Coffee.UIEffects;
using DG.Tweening;
using MRK.Navigation;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface
{
    public partial class Navigation
    {
        private class Bottom
        {
            private class Route
            {
                public GameObject Object;
                public TextMeshProUGUI Text;
                public Button Button;
                public int Index;
            }

            private readonly RectTransform _transform;
            private readonly TextMeshProUGUI _distance;
            private readonly TextMeshProUGUI _time;
            private readonly Button _start;
            private readonly GameObject _routePrefab;
            private float _startAnimDelta;
            private readonly UIHsvModifier _startAnimHSV;
            private float _initialY;
            private readonly UITransitionEffect _backAnim;
            private readonly ObjectPool<Route> _routePool;
            private readonly List<Route> _currentRoutes;
            private EGRNavigationDirections _currentDirs;

            private static readonly Color SelectedRouteColor;
            private static readonly Color IdleRouteColor;

            static Bottom()
            {
                SelectedRouteColor = new Color(0.0509803921568627f, 0.0509803921568627f, 0.0509803921568627f);
                IdleRouteColor = new Color(0.2117647058823529f, 0.2117647058823529f, 0.2117647058823529f);
            }

            public Bottom(RectTransform transform)
            {
                _transform = transform;

                _distance = _transform.Find("Main/Info/Distance").GetComponent<TextMeshProUGUI>();
                _time = _transform.Find("Main/Info/Time").GetComponent<TextMeshProUGUI>();

                _start = _transform.Find("Main/Destination/Button").GetComponent<Button>();
                _start.onClick.AddListener(OnStartClick);
                _startAnimHSV = _start.transform.Find("Sep").GetComponent<UIHsvModifier>();

                _routePrefab = _transform.Find("Routes/Route").gameObject;
                _routePrefab.gameObject.SetActive(false);

                Transform back = _transform.Find("Back");
                back.GetComponent<Button>().onClick.AddListener(OnBackClick);
                _backAnim = back.GetComponent<UITransitionEffect>();
                _backAnim.effectFactor = 0f;

                _initialY = _transform.anchoredPosition.y;
                transform.anchoredPosition = new Vector3(_transform.anchoredPosition.x, _initialY - _transform.rect.height); //initially

                _routePool = new ObjectPool<Route>(() =>
                {
                    Route route = new Route
                    {
                        Object = Object.Instantiate(_routePrefab, _routePrefab.transform.parent)
                    };
                    route.Text = route.Object.transform.Find("Text").GetComponent<TextMeshProUGUI>();
                    route.Button = route.Object.GetComponent<Button>();
                    route.Button.onClick.AddListener(() => OnRouteClick(route));

                    route.Object.SetActive(false);
                    return route;
                });

                _currentRoutes = new List<Route>();
            }

            public void Update()
            {
                if (_start.gameObject.activeInHierarchy)
                {
                    _startAnimDelta += Time.deltaTime * 0.2f;
                    if (_startAnimDelta > 0.5f)
                        _startAnimDelta = -0.5f;

                    _startAnimHSV.hue = _startAnimDelta;
                }
            }

            public void Show()
            {
                _transform.DOAnchorPosY(_initialY, 0.3f)
                    .ChangeStartValue(new Vector3(0f, _initialY - _transform.rect.height))
                    .SetEase(Ease.OutSine);
            }

            public void Hide()
            {
                _transform.DOAnchorPosY(_initialY - _transform.rect.height, 0.3f)
                    .SetEase(Ease.OutSine);
            }

            private void OnBackClick()
            {
                if (_instance.Hide())
                {
                    DOTween.To(() => _backAnim.effectFactor, x => _backAnim.effectFactor = x, 0f, 0.3f);
                }
            }

            public void ShowBackButton()
            {
                DOTween.To(() => _backAnim.effectFactor, x => _backAnim.effectFactor = x, 1f, 0.7f);
            }

            public void ClearDirections()
            {
                if (_currentRoutes.Count > 0)
                {
                    foreach (Route r in _currentRoutes)
                    {
                        r.Object.SetActive(false);
                        _routePool.Free(r);
                    }

                    _currentRoutes.Clear();
                }
            }

            public void SetDirections(EGRNavigationDirections dirs)
            {
                ClearDirections();

                _currentDirs = dirs;

                int idx = 0;
                foreach (EGRNavigationRoute route in dirs.Routes)
                {
                    Route r = _routePool.Rent();
                    r.Index = idx;
                    r.Text.text = $"ROUTE {(idx++) + 1}";
                    r.Object.SetActive(true);

                    _currentRoutes.Add(r);
                }

                _instance.Client.NavigationManager.PrepareDirections();

                if (idx > 0)
                {
                    SetCurrentRoute(_currentRoutes[0]);
                }
            }

            private void SetCurrentRoute(Route route)
            {
                EGRNavigationRoute r = _currentDirs.Routes[route.Index];

                for (int i = 0; i < _currentRoutes.Count; i++)
                {
                    _currentRoutes[i].Button.GetComponent<Image>().color = i == route.Index ? SelectedRouteColor : IdleRouteColor;
                }

                string dUnits = "M";
                double dist = r.Distance;
                if (r.Distance > 1000d)
                {
                    dUnits = "KM";
                    dist /= 1000d;
                }

                //upper round
                dist = Mathd.CeilToInt(dist);
                _distance.text = $"{dist} {dUnits}";

                string units = "S";
                double dur = r.Duration;
                string timeStr = string.Empty;
                if (r.Duration > 3600d)
                {
                    units = "HR";
                    dur /= 3600d;

                    int noHrs = Mathd.FloorToInt(dur);
                    int minutes = Mathd.CeilToInt((dur - noHrs) * 60d);
                    timeStr = $"{noHrs} HR {minutes} MIN";
                }
                else if (r.Duration > 60d)
                {
                    units = "MIN";
                    dur /= 60d;
                    dur = Mathd.CeilToInt(dur);
                }

                _time.text = timeStr.Length == 0 ? $"{dur} {units}" : timeStr;

                _instance.Client.NavigationManager.SelectedRouteIndex = route.Index;
            }

            private void OnRouteClick(Route route)
            {
                SetCurrentRoute(route);
            }

            private void OnStartClick()
            {
                _instance.Start();
            }

            public void SetStartText(string txt)
            {
                _start.GetComponentInChildren<TextMeshProUGUI>().text = txt;
            }
        }
    }
}