using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Vectrosity;

namespace MRK.Navigation {
    public class EGRNavigationManager : BaseBehaviour {
        readonly ObjectPool<VectorLine> m_LinePool;
        [SerializeField]
        Material m_LineMaterial;
        readonly List<VectorLine> m_ActiveLines;
        int m_SelectedRoute;
        bool m_IsActive;
        [SerializeField]
        Texture2D m_IdleLineTexture;
        [SerializeField]
        Texture2D m_ActiveLineTexture;
        [SerializeField]
        float m_IdleLineWidth;
        [SerializeField]
        float m_ActiveLineWidth;
        bool m_FixedCanvas;
        [SerializeField]
        Color m_ActiveLineColor;
        [SerializeField]
        Color m_IdleLineColor;
        [SerializeField]
        Image m_NavSprite;
        bool m_IsPreview;
        bool m_IsNavigating;
        EGRNavigator m_CurrentNavigator;
        readonly EGRNavigationLive m_LiveNavigator;
        readonly EGRNavigationSimulator m_SimulationNavigator;

        public EGRNavigationDirections? CurrentDirections { get; private set; }
        public EGRNavigationRoute CurrentRoute => CurrentDirections.Value.Routes[m_SelectedRoute];
        public int SelectedRouteIndex {
            get => m_SelectedRoute;
            set {
                m_SelectedRoute = value;
                UpdateSelectedLine();
            }
        }
        public bool IsNavigating => m_IsNavigating;
        public Image NavigationSprite => m_NavSprite;

        public EGRNavigationManager() {
            m_LinePool = new ObjectPool<VectorLine>(() => {
                VectorLine vL = new VectorLine("LR", new List<Vector3>(), m_IdleLineTexture, 14f, LineType.Continuous, Joins.Weld);
                vL.material = m_LineMaterial;

                return vL;
            });

            m_ActiveLines = new List<VectorLine>();
            m_SelectedRoute = -1;

            m_LiveNavigator = new EGRNavigationLive();
            m_SimulationNavigator = new EGRNavigationSimulator();
        }

        void Start() {
            Client.FlatMap.MapUpdated += OnMapUpdated;
            m_NavSprite.gameObject.SetActive(false);
        }

        public void SetCurrentDirections(string json, Action callback) {
            Task.Run(async () => {
                await Task.Delay(100);
                CurrentDirections = JsonConvert.DeserializeObject<EGRNavigationDirections>(json);

                if (callback != null) {
                    Client.Runnable.RunOnMainThread(callback);
                }
            });
        }

        void OnDestroy() {
            Client.FlatMap.MapUpdated -= OnMapUpdated;
        }

        public void PrepareDirections() {
            if (m_ActiveLines.Count > 0) {
                foreach (VectorLine lr in m_ActiveLines) {
                    lr.active = false;
                    m_LinePool.Free(lr);
                }

                m_ActiveLines.Clear();
            }

            double minX = double.PositiveInfinity;
            double minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double maxY = double.NegativeInfinity;

            int routeIdx = 0;
            foreach (EGRNavigationRoute route in CurrentDirections.Value.Routes) {
                VectorLine lr = m_LinePool.Rent();
                lr.points3.Clear();

                foreach (EGRNavigationStep step in route.Legs[0].Steps) {
                    for (int i = 0; i < step.Geometry.Coordinates.Count; i++) {
                        Vector2d geoLoc = step.Geometry.Coordinates[i];

                        minX = Mathd.Min(minX, geoLoc.x);
                        minY = Mathd.Min(minY, geoLoc.y);
                        maxX = Mathd.Max(maxX, geoLoc.x);
                        maxY = Mathd.Max(maxY, geoLoc.y);

                        Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(geoLoc);
                        worldPos.y = 0.1f;
                        lr.points3.Add(worldPos);
                    }
                }

                lr.active = true;
                m_ActiveLines.Add(lr);
                lr.Draw();
                routeIdx++;
            }

            m_IsActive = true;
            m_SelectedRoute = CurrentDirections.Value.Routes.Count > 0 ? 0 : -1;

            UpdateSelectedLine();

            //Client.FlatMap.SetNavigationTileset();
            Client.FlatMap.FitToBounds(new Vector2d(minX, minY), new Vector2d(maxX, maxY));
        }

        void OnMapUpdated() {
            if (m_ActiveLines.Count > 0 && m_IsActive) {
                int lrIdx = 0;
                foreach (VectorLine lr in m_ActiveLines) {
                    lr.points3.Clear();

                    EGRNavigationRoute route = CurrentDirections.Value.Routes[lrIdx];

                    foreach (EGRNavigationStep step in route.Legs[0].Steps) {
                        for (int i = 0; i < step.Geometry.Coordinates.Count; i++) {
                            Vector2d geoLoc = step.Geometry.Coordinates[i];
                            Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(geoLoc);
                            worldPos.y = 0.1f;
                            lr.points3.Add(worldPos);
                        }
                    }

                    lr.Draw();
                    lrIdx++;
                }
            }
        }

        void Update() {
            if (!m_IsNavigating)
                return;

            m_CurrentNavigator.Update();

            //camera
            Client.FlatCamera.SetCenterAndZoom(m_CurrentNavigator.LastKnownCenter, 19f);
            Client.FlatCamera.SetRotation(new Vector3(0f, m_CurrentNavigator.LastKnownBearing));
        }

        void UpdateSelectedLine() {
            if (!m_FixedCanvas) {
                m_FixedCanvas = true;
                VectorLine.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                VectorLine.canvas.worldCamera = Client.ActiveCamera;
            }

            for (int i = 0; i < m_ActiveLines.Count; i++) {
                VectorLine vL = m_ActiveLines[i];
                bool active = m_SelectedRoute == i;
                vL.SetColor(active ? m_ActiveLineColor : m_IdleLineColor);
                vL.SetWidth(active ? m_ActiveLineWidth : m_IdleLineWidth);
                vL.texture = active ? m_ActiveLineTexture : m_IdleLineTexture;

                if (active) {
                    vL.rectTransform.SetAsLastSibling();
                }
            }
        }

        public void StartNavigation(bool isPreview = true) {
            m_IsNavigating = true;
            m_IsPreview = isPreview;

            m_CurrentNavigator = isPreview ? m_SimulationNavigator : (EGRNavigator)m_LiveNavigator;
            m_CurrentNavigator.SetRoute(CurrentRoute);

            m_NavSprite.gameObject.SetActive(true);
        }

        public void ExitNavigation() {
            foreach (VectorLine vL in m_ActiveLines) {
                vL.points3.Clear();
                vL.active = false;
                m_LinePool.Free(vL);
            }

            m_ActiveLines.Clear();

            m_IsActive = false;

            m_IsNavigating = false;
            m_NavSprite.gameObject.SetActive(false);
        }
    }
}
