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

namespace MRK {
    public class EGRNavigationManager : EGRBehaviour {
        EGRNavigationDirections m_Directions;
        readonly ObjectPool<VectorLine> m_LinePool;
        [SerializeField]
        Material m_LineMaterial;
        readonly List<VectorLine> m_ActiveLines;
        int m_SelectedRoute;
        bool m_IsNavigating;
        [SerializeField]
        Texture2D m_IdleLineTexture;
        [SerializeField]
        Texture2D m_ActiveLineTexture;
        [SerializeField]
        float m_IdleLineWidth;
        [SerializeField]
        float m_ActiveLineWidth;

        //temp styles
        GUIStyle m_VerticalStyle;
        GUIStyle m_LabelStyle;
        GUIStyle m_ButtonStyle;

        public EGRNavigationManager() {
            m_LinePool = new ObjectPool<VectorLine>(() => {
                VectorLine vL = new VectorLine("LR", new List<Vector3>(), m_IdleLineTexture, 14f, LineType.Continuous, Joins.Weld);
                vL.material = m_LineMaterial;

                return vL;
            });

            m_ActiveLines = new List<VectorLine>();
            m_SelectedRoute = -1;
        }

        void Start() {
            string json = Resources.Load<TextAsset>("Map/sampleDirs").text;
            Task.Run(async () => {
                await Task.Delay(5000);
                m_Directions = JsonConvert.DeserializeObject<EGRNavigationDirections>(json);
            });

            Client.FlatMap.OnMapUpdated += OnMapUpdated;
            //VectorLine.SetCanvasCamera(Client.ActiveCamera);
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

            int routeIdx = 0;
            foreach (EGRNavigationRoute route in m_Directions.Routes) {
                VectorLine lr = m_LinePool.Rent();
                lr.points3.Clear();

                for (int i = 0; i < route.Geometry.Coordinates.Count; i++) {
                    Vector2d geoLoc = route.Geometry.Coordinates[i];
                    Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(geoLoc);
                    worldPos.y = 0.1f;
                    lr.points3.Add(worldPos);
                }

                lr.active = true;
                m_ActiveLines.Add(lr);
                lr.Draw();
                routeIdx++;
            }

            m_IsNavigating = true;
            m_SelectedRoute = m_Directions.Routes.Count > 0 ? 0 : -1;

            UpdateSelectedLine();

            Client.FlatMap.SetNavigationTileset();
        }

        void OnMapUpdated() {
            if (m_ActiveLines.Count > 0 && m_IsNavigating) {
                int lrIdx = 0;
                foreach (VectorLine lr in m_ActiveLines) {
                    lr.points3.Clear();

                    EGRNavigationRoute route = m_Directions.Routes[lrIdx];

                    for (int i = 0; i < route.Geometry.Coordinates.Count; i++) {
                        Vector2d geoLoc = route.Geometry.Coordinates[i];
                        Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(geoLoc);
                        worldPos.y = 0.1f;
                        lr.points3.Add(worldPos);
                    }

                    lr.Draw();
                    lrIdx++;
                }
            }
        }

        void Update() {
            //foreach (VectorLine vl in m_ActiveLines)
            //   vl.Draw3D();
        }

        void UpdateSelectedLine() {
            for (int i = 0; i < m_ActiveLines.Count; i++) {
                VectorLine vL = m_ActiveLines[i];
                bool active = m_SelectedRoute == i;
                vL.SetColor(active ? Color.green : Color.white);
                vL.lineWidth = active ? m_ActiveLineWidth : m_IdleLineWidth;
                vL.texture = active ? m_ActiveLineTexture : m_IdleLineTexture;
            }

            VectorLine.canvas.renderMode = RenderMode.ScreenSpaceCamera;
            VectorLine.canvas.worldCamera = Client.ActiveCamera;
        }

        void OnRouteUpdated() {
            UpdateSelectedLine();
        }

        void OnGUI() {
            if (m_VerticalStyle == null) {
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
                    richText = true
                };
            }

            if (m_IsNavigating) {
                GUILayout.BeginArea(new Rect(Screen.width - 600f, 0f, 600f, 700f));
                GUILayout.BeginVertical(m_VerticalStyle);
                {
                    GUILayout.Label("Navigation", m_LabelStyle);
                    GUILayout.Label($"Routes: {m_Directions.Routes.Count}", m_LabelStyle);

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
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }
    }
}
