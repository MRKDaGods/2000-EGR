using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRK;
using UnityEngine;
using UnityEngine.UI;
using Vectrosity;
using MRK.GeoJson;
using Newtonsoft.Json;

namespace MRK.UI {
    public class EGRScreenQuickLocations : EGRScreen {
        EGRGeoJson? m_GeoJson;
        readonly List<VectorLine> m_Lines;
        [SerializeField]
        Material m_LineMaterial;
        [SerializeField]
        Texture2D m_LineTexture;
        GameObject m_LinesParent;
        string m_RawGeoJson;

        public EGRScreenQuickLocations() {
            m_Lines = new List<VectorLine>();
        }

        protected override void OnScreenInit() {
        }

        protected override void OnScreenShow() {
            //StartCoroutine(OnShowCoroutine());
        }

        IEnumerator OnShowCoroutine() {
            if (!m_GeoJson.HasValue) {
                Reference<bool> awaitRef = ReferencePool<bool>.Default.Rent();
                ResourceRequest req = Resources.LoadAsync<TextAsset>("Map/countries");
                while (!req.isDone)
                    yield return new WaitForSeconds(0.1f);

                string json = ((TextAsset)req.asset).text;

                new System.Threading.Thread(() => {
                    m_GeoJson = JsonConvert.DeserializeObject<EGRGeoJson>(json);

                        awaitRef.Value = true;
                }).Start();

                m_LinesParent = new GameObject("Lines Parent");

                while (!awaitRef.Value)
                    yield return new WaitForSeconds(0.1f);

                Debug.Log("e");
                ReferencePool<bool>.Default.Free(awaitRef);
            }

            int polyGen = 0;
            foreach (EGRGeoJsonFeature feature in m_GeoJson.Value.Features) {
                if (feature.Geometry == null || feature.Geometry.Polygons.Count == 0)
                    continue;

                foreach (List<Vector2d> poly in feature.Geometry.Polygons) {
                    VectorLine vl = new VectorLine("LR", new List<Vector3>(), m_LineTexture, 1f, LineType.Continuous, Joins.Weld);
                    vl.material = m_LineMaterial;
                    vl.color = Color.yellow;

                    foreach (Vector2d geoPoint in poly) {
                        Vector3 wPos = MRKMapUtils.GeoToWorldGlobePosition(geoPoint.x, geoPoint.y, 1505f);
                        wPos += Client.GlobalMap.transform.position;
                        vl.points3.Add(wPos);
                    }

                    vl.points3.Add(vl.points3[0]); //loop

                    GameObject subLR = new GameObject("subLR");
                    subLR.transform.parent = m_LinesParent.transform;
                    vl.drawTransform = subLR.transform;
                    vl.Draw3D();      

                    m_Lines.Add(vl);

                    polyGen++;
                    if (polyGen % 50 == 0) {
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }

            m_LinesParent.transform.position = Client.GlobalMap.transform.position;
            m_LinesParent.transform.rotation = Client.GlobalMap.transform.rotation;

            //m_Lines.ForEach(x => x.Draw3DAuto());
        }
    }
}
