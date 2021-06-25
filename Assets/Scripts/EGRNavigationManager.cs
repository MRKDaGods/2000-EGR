using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MRK.Navigation;
using MRK.UI;
using Vectrosity;
using UnityEngine.UI;

namespace MRK {
    public abstract class EGRNavigation {
        protected EGRNavigationRoute m_Route { get; private set; }
        protected EGRMain Client => EGRMain.Instance;
        protected EGRNavigationManager NavigationManager => Client.NavigationManager;

        protected virtual void Prepare() {
        }

        public void SetRoute(EGRNavigationRoute route) {
            m_Route = route;

            Prepare();
        }

        public abstract void Update();
    }

    public class EGRNavigationManager : EGRBehaviour {
        [SerializeField]
        bool m_DrawEditorUI;
        EGRNavigationDirections m_Directions;
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
        EGRNavigation m_CurrentNavigator;
        readonly EGRNavigationLive m_LiveNavigator;
        readonly EGRNavigationSimulator m_SimulationNavigator;

        //temp styles
        GUIStyle m_VerticalStyle;
        GUIStyle m_LabelStyle;
        GUIStyle m_ButtonStyle;

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
            /* string json = Resources.Load<TextAsset>("Map/sampleDirs").text;
            Task.Run(async () => {
                await Task.Delay(5000);
                m_Directions = JsonConvert.DeserializeObject<EGRNavigationDirections>(json);
            }); */

            Client.FlatMap.OnMapUpdated += OnMapUpdated;
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
            Client.FlatMap.OnMapUpdated -= OnMapUpdated;
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

                if (active)
                    vL.rectTransform.SetAsLastSibling();
            }
        }

        void OnRouteUpdated() {
            UpdateSelectedLine(); 
        }

        public void StartNavigation(bool isPreview = true) {
            m_IsNavigating = true;
            m_IsPreview = isPreview;

            m_CurrentNavigator = isPreview ? m_SimulationNavigator : (EGRNavigation)m_LiveNavigator;
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

            //try and show the current location sprite again
            Client.LocationManager.RequestCurrentLocation(true, true, true);
        }

        void OnGUI() {
            if (!m_DrawEditorUI)
                return;

            /*if (m_VerticalStyle == null) {
                m_VerticalStyle = new GUIStyle {
                    normal = {
                        background = EGRUIUtilities.GetPlainTexture(Color.black)
                    }
                };

                m_LabelStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 42,
                    fontStyle = FontStyle.Bold
                };

                m_ButtonStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 42,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                };
            }

            if (m_IsActive) {
                GUILayout.BeginArea(new Rect(Screen.width - 600f, 0f, 600f, 700f));
                GUILayout.BeginVertical(m_VerticalStyle);
                {
                    GUILayout.Label("Navigation", m_LabelStyle);
                    GUILayout.Label($"Routes: {CurrentDirections.Value.Routes.Count}", m_LabelStyle);

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("<", m_ButtonStyle)) {
                            m_SelectedRoute--;
                            OnRouteUpdated();
                        }

                        GUILayout.Label($"<color=yellow>{m_SelectedRoute}</color>", m_ButtonStyle);

                        if (GUILayout.Button(">", m_ButtonStyle)) {
                            m_SelectedRoute++;
                            OnRouteUpdated();
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Start", m_ButtonStyle)) {
                    StartNavigation();
                }

                GUILayout.Label($"Current point: {m_CurrentPointIdx}", m_LabelStyle);
                m_SimulatedTripPercentage = GUILayout.HorizontalSlider(m_SimulatedTripPercentage, 0f, 1f, GUILayout.Height(100f));

                GUILayout.EndVertical();
                GUILayout.EndArea();
            } */
        }
    }
}
