using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vectrosity;

namespace MRK {
    public class MRKARManager : EGRBehaviour {
        [SerializeField]
        bool m_IsListening;
        [SerializeField]
        List<Vector2d> m_Coords;
        [SerializeField]
        Texture2D m_LineTex;
        [SerializeField]
        Material m_LineMaterial;
        VectorLine m_VL;
        bool m_FixedCanvas;

        void Start() {    
            m_VL = new VectorLine("LR", new List<Vector3>(), m_LineTex, 14f, LineType.Continuous, Joins.Weld);
            m_VL.material = m_LineMaterial;
            Client.FlatMap.OnMapUpdated += OnMapUpdated;
        }

        void Update() {
            if (m_IsListening && Input.GetMouseButtonUp(0)) {
                if (!m_FixedCanvas) {
                    m_FixedCanvas = true;
                    VectorLine.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    VectorLine.canvas.worldCamera = Client.ActiveCamera;
                }

                Vector3 mousePos = Input.mousePosition;
                mousePos.z = Client.ActiveCamera.transform.position.y;

                Vector3 wPos = Client.ActiveCamera.ScreenToWorldPoint(mousePos);
                Vector2d coord = Client.FlatMap.WorldToGeoPosition(wPos);

                m_Coords.Add(coord);

                Debug.Log($"Added coord {coord}");

                m_VL.points3.Clear();
                foreach (Vector2d geoLoc in m_Coords) {
                    Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(geoLoc);
                    worldPos.y = 0.1f;
                    m_VL.points3.Add(worldPos);
                }

                m_VL.Draw();
            }
        }

        void OnMapUpdated() {
            m_VL.points3.Clear();
            foreach (Vector2d geoLoc in m_Coords) {
                Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(geoLoc);
                worldPos.y = 0.1f;
                m_VL.points3.Add(worldPos);
            }

            m_VL.Draw();
        }
    }
}
