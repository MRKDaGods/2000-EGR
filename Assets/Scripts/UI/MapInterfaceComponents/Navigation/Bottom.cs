using Coffee.UIEffects;
using DG.Tweening;
using MRK.Navigation;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface {
    public partial class EGRMapInterfaceComponentNavigation {
        class Bottom {
            class Route {
                public GameObject Object;
                public TextMeshProUGUI Text;
                public Button Button;
                public int Index;
            }

            static readonly Color SELECTED_ROUTE_COLOR;
            static readonly Color IDLE_ROUTE_COLOR;

            readonly RectTransform m_Transform;
            readonly TextMeshProUGUI m_Distance;
            readonly TextMeshProUGUI m_Time;
            readonly Button m_Start;
            readonly GameObject m_RoutePrefab;
            float m_StartAnimDelta;
            readonly UIHsvModifier m_StartAnimHSV;
            float m_InitialY;
            readonly UITransitionEffect m_BackAnim;
            readonly ObjectPool<Route> m_RoutePool;
            readonly List<Route> m_CurrentRoutes;
            EGRNavigationDirections m_CurrentDirs;

            static Bottom() {
                SELECTED_ROUTE_COLOR = new Color(0.0509803921568627f, 0.0509803921568627f, 0.0509803921568627f);
                IDLE_ROUTE_COLOR = new Color(0.2117647058823529f, 0.2117647058823529f, 0.2117647058823529f);
            }

            public Bottom(RectTransform transform) {
                m_Transform = transform;

                m_Distance = m_Transform.Find("Main/Info/Distance").GetComponent<TextMeshProUGUI>();
                m_Time = m_Transform.Find("Main/Info/Time").GetComponent<TextMeshProUGUI>();

                m_Start = m_Transform.Find("Main/Destination/Button").GetComponent<Button>();
                m_Start.onClick.AddListener(OnStartClick);
                m_StartAnimHSV = m_Start.transform.Find("Sep").GetComponent<UIHsvModifier>();

                m_RoutePrefab = m_Transform.Find("Routes/Route").gameObject;
                m_RoutePrefab.gameObject.SetActive(false);

                Transform back = m_Transform.Find("Back");
                back.GetComponent<Button>().onClick.AddListener(OnBackClick);
                m_BackAnim = back.GetComponent<UITransitionEffect>();
                m_BackAnim.effectFactor = 0f;

                m_InitialY = m_Transform.anchoredPosition.y;
                transform.anchoredPosition = new Vector3(m_Transform.anchoredPosition.x, m_InitialY - m_Transform.rect.height); //initially

                m_RoutePool = new ObjectPool<Route>(() => {
                    Route route = new Route();
                    route.Object = Object.Instantiate(m_RoutePrefab, m_RoutePrefab.transform.parent);
                    route.Text = route.Object.transform.Find("Text").GetComponent<TextMeshProUGUI>();
                    route.Button = route.Object.GetComponent<Button>();
                    route.Button.onClick.AddListener(() => OnRouteClick(route));

                    route.Object.SetActive(false);
                    return route;
                });

                m_CurrentRoutes = new List<Route>();
            }

            public void Update() {
                if (m_Start.gameObject.activeInHierarchy) {
                    m_StartAnimDelta += Time.deltaTime * 0.2f;
                    if (m_StartAnimDelta > 0.5f)
                        m_StartAnimDelta = -0.5f;

                    m_StartAnimHSV.hue = m_StartAnimDelta;
                }
            }

            public void Show() {
                m_Transform.DOAnchorPosY(m_InitialY, 0.3f)
                    .ChangeStartValue(new Vector3(0f, m_InitialY - m_Transform.rect.height))
                    .SetEase(Ease.OutSine);
            }

            public void Hide() {
                m_Transform.DOAnchorPosY(m_InitialY - m_Transform.rect.height, 0.3f)
                    .SetEase(Ease.OutSine);
            }

            void OnBackClick() {
                if (ms_Instance.Hide()) {
                    DOTween.To(() => m_BackAnim.effectFactor, x => m_BackAnim.effectFactor = x, 0f, 0.3f);
                }
            }

            public void ShowBackButton() {
                DOTween.To(() => m_BackAnim.effectFactor, x => m_BackAnim.effectFactor = x, 1f, 0.7f);
            }

            public void ClearDirections() {
                if (m_CurrentRoutes.Count > 0) {
                    foreach (Route r in m_CurrentRoutes) {
                        r.Object.SetActive(false);
                        m_RoutePool.Free(r);
                    }

                    m_CurrentRoutes.Clear();
                }
            }

            public void SetDirections(EGRNavigationDirections dirs) {
                ClearDirections();

                m_CurrentDirs = dirs;

                int idx = 0;
                foreach (EGRNavigationRoute route in dirs.Routes) {
                    Route r = m_RoutePool.Rent();
                    r.Index = idx;
                    r.Text.text = $"ROUTE {(idx++) + 1}";
                    r.Object.SetActive(true);

                    m_CurrentRoutes.Add(r);
                }

                ms_Instance.Client.NavigationManager.PrepareDirections();

                if (idx > 0) {
                    SetCurrentRoute(m_CurrentRoutes[0]);
                }
            }

            void SetCurrentRoute(Route route) {
                EGRNavigationRoute r = m_CurrentDirs.Routes[route.Index];

                for (int i = 0; i < m_CurrentRoutes.Count; i++) {
                    m_CurrentRoutes[i].Button.GetComponent<Image>().color = i == route.Index ? SELECTED_ROUTE_COLOR : IDLE_ROUTE_COLOR;
                }

                string dUnits = "M";
                double dist = r.Distance;
                if (r.Distance > 1000d) {
                    dUnits = "KM";
                    dist /= 1000d;
                }

                //upper round
                dist = Mathd.CeilToInt(dist);
                m_Distance.text = $"{dist} {dUnits}";

                string units = "S";
                double dur = r.Duration;
                string timeStr = string.Empty;
                if (r.Duration > 3600d) {
                    units = "HR";
                    dur /= 3600d;

                    int noHrs = Mathd.FloorToInt(dur);
                    int minutes = Mathd.CeilToInt((dur - noHrs) * 60d);
                    timeStr = $"{noHrs} HR {minutes} MIN";
                }
                else if (r.Duration > 60d) {
                    units = "MIN";
                    dur /= 60d;
                    dur = Mathd.CeilToInt(dur);
                }

                m_Time.text = timeStr.Length == 0 ? $"{dur} {units}" : timeStr;

                ms_Instance.Client.NavigationManager.SelectedRouteIndex = route.Index;
            }

            void OnRouteClick(Route route) {
                SetCurrentRoute(route);
            }

            void OnStartClick() {
                ms_Instance.Start();
            }

            public void SetStartText(string txt) {
                m_Start.GetComponentInChildren<TextMeshProUGUI>().text = txt;
            }
        }
    }
}