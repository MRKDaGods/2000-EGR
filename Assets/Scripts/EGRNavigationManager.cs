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

namespace MRK {
    public class EGRNavigationManager : EGRBehaviour {
        EGRNavigationDirections m_Directions;
        readonly ObjectPool<LineRenderer> m_LinePool;
        [SerializeField]
        Material m_LineMaterial;
        readonly List<LineRenderer> m_ActiveLines;
        int m_SelectedRoute;
        bool m_IsNavigating;
        [SerializeField]
        Gradient m_IdleGradient;
        [SerializeField]
        Gradient m_ActiveGradient;

        //temp styles
        GUIStyle m_VerticalStyle;
        GUIStyle m_LabelStyle;
        GUIStyle m_ButtonStyle;

        public EGRNavigationManager() {
            m_LinePool = new ObjectPool<LineRenderer>(() => {
                LineRenderer lr = new GameObject("LR").AddComponent<LineRenderer>();
                lr.alignment = LineAlignment.TransformZ;
                lr.useWorldSpace = true;
                lr.material = m_LineMaterial;
                lr.startWidth = lr.endWidth = 1.5f;
                lr.numCornerVertices = 45;
                lr.numCapVertices = 45;
                lr.transform.Rotate(90f, 0f, 0f);

                return lr;
            });

            m_ActiveLines = new List<LineRenderer>();
            m_SelectedRoute = -1;
        }

        void Start() {
            string json = Resources.Load<TextAsset>("Map/sampleDirs").text;
            Task.Run(async () => {
                await Task.Delay(5000);
                m_Directions = JsonConvert.DeserializeObject<EGRNavigationDirections>(json);
            });

            Client.FlatMap.OnMapUpdated += OnMapUpdated;
        }

        void OnDestroy() {
            Client.FlatMap.OnMapUpdated -= OnMapUpdated;
        }

        public void PrepareDirections() {
            if (m_ActiveLines.Count > 0) {
                foreach (LineRenderer lr in m_ActiveLines) {
                    lr.gameObject.SetActive(false);
                    m_LinePool.Free(lr);
                }

                m_ActiveLines.Clear();
            }

            int routeIdx = 0;
            foreach (EGRNavigationRoute route in m_Directions.Routes) {
                LineRenderer lr = m_LinePool.Rent();
                lr.positionCount = route.Geometry.Coordinates.Count;

                for (int i = 0; i < lr.positionCount; i++) {
                    Vector2d geoLoc = route.Geometry.Coordinates[i];
                    Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(geoLoc);
                    worldPos.y = 0.1f;
                    lr.SetPosition(i, worldPos);
                }

                lr.gameObject.SetActive(true);
                m_ActiveLines.Add(lr);

                routeIdx++;
            }

            m_IsNavigating = true;
            m_SelectedRoute = m_Directions.Routes.Count > 0 ? 0 : -1;

            UpdateLinesColor();
        }

        void OnMapUpdated() {
            if (m_ActiveLines.Count > 0 && m_IsNavigating) {
                int lrIdx = 0;
                foreach (LineRenderer lr in m_ActiveLines) {
                    for (int i = 0; i < lr.positionCount; i++) {
                        Vector2d geoLoc = m_Directions.Routes[lrIdx].Geometry.Coordinates[i];
                        Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(geoLoc);
                        worldPos.y = 0.1f;
                        lr.SetPosition(i, worldPos);
                    }

                    lrIdx++;
                }
            }
        }

        void UpdateLinesColor() {
            for (int i = 0; i < m_ActiveLines.Count; i++) {
                m_ActiveLines[i].colorGradient = m_SelectedRoute == i ? m_ActiveGradient : m_IdleGradient;
            }
        }

        void OnRouteUpdated() {
            UpdateLinesColor();
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
