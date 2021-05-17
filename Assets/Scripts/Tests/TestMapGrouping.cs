using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace MRK {
    public class TestMapGrouping : MonoBehaviour {
        class Place {
            public float X;
            public float Y;

            public Vector2[] Points;
            public float Slope;
            public float Intercept;
        }

        [SerializeField]
        Button m_Button;
        [SerializeField]
        UILineRenderer m_Sample;
        Place[] m_Places;
        readonly ObjectPool<UILineRenderer> m_Lines;
        RectTransform rt;
        [SerializeField]
        float radius = 367f;
        [SerializeField]
        Material m_Material;

        public TestMapGrouping() {
            m_Lines = new ObjectPool<UILineRenderer>(() => {
                return Instantiate(m_Sample, m_Sample.transform.parent);
            });
        }

        void Start() {
            m_Button.onClick.AddListener(OnButtonClick);

            //create places
            rt = m_Button.transform as RectTransform;

            m_Places = new Place[6];
            for (int i = 0; i < m_Places.Length; i++) {
                Place p = new Place();
                p.X = rt.anchoredPosition.x + (rt.sizeDelta.x / 2f) * Random.Range(-1f, 1f);
                p.Y = rt.anchoredPosition.y + (rt.sizeDelta.y / 2f) * Random.Range(-1f, 1f);
                m_Places[i] = p;
                //TestMapGrouping img = Instantiate(m_Sample, m_Sample.transform.parent);
                //(img.transform as RectTransform).anchoredPosition = new Vector2(p.X, p.Y);
            }

            m_Sample.gameObject.SetActive(false);
        }

        bool IsInBetween(float val, float v0, float v1) {
            float min = Mathf.Min(v0, v1);
            float max = Mathf.Max(v0, v1);

            return val > min && val < max;
        }

        void OnButtonClick() {
            m_Button.gameObject.SetActive(false);

            float radialSpace = 360f / m_Places.Length;

            for (int i = 0; i < m_Places.Length; i++) {
                Place p = m_Places[i];

                float angle = i * radialSpace * Mathf.Deg2Rad;
                
                p.Points = new Vector2[2] {
                    new Vector2(Mathf.Cos(angle) * 100f, Mathf.Sin(angle) * 100f), //new Vector2(p.X - rt.rect.x, p.Y - rt.rect.y),
                    new Vector2(Mathf.Cos(angle) * 300f, Mathf.Sin(angle) * 300f)
                };

                //y = mx+c
                p.Slope = (p.Points[1].y - p.Points[0].y) / (p.Points[1].x - p.Points[0].x);
                //c = y - mx
                p.Intercept = p.Points[0].y - p.Slope * p.Points[0].x;

                UILineRenderer lr = m_Lines.Rent();
                lr.rectTransform.position = m_Button.transform.position;
                lr.gameObject.name = i.ToString();

                lr.Points = p.Points;

                lr.gameObject.SetActive(true);
            }

            /*for (int i = 0; i < m_Places.Length; i++) {
                Place p = m_Places[i];
                for (int j = 0; j < m_Places.Length; j++) {
                    if (i == j)
                        continue;

                    //A=m, B=1, c=intercept
                    Place other = m_Places[j];
                    float ix = (other.Intercept - p.Intercept) / (p.Slope - other.Slope);
                    float iy = (p.Intercept * other.Slope - other.Intercept * p.Slope) / (p.Slope - other.Slope);

                    if (!IsInBetween(ix, other.Points[0].x, other.Points[1].x) || !IsInBetween(ix, p.Points[0].x, p.Points[1].x)) {
                        continue;
                    }

                    if (!IsInBetween(iy, other.Points[0].y, other.Points[1].y) || !IsInBetween(ix, p.Points[0].x, p.Points[1].x)) {
                        continue;
                    }

                    Debug.Log($"intersection between L{i} - L{j} is ({ix},{iy})");
                }

                UILineRenderer lr = m_Lines.Rent();
                lr.rectTransform.position = m_Button.transform.position;
                lr.gameObject.name = i.ToString();

                lr.Points = p.Points;

                lr.gameObject.SetActive(true);
            } */
        }

        void OnGUI() {
            //EGRGL.SetLineMaterial(m_Material);

            //EGRGL.DrawCircle(new Vector2(Screen.width / 2f, Screen.height / 2f), radius, Color.white, 50);
            //EGRGL.DrawLine(new Vector2(), new Vector2(500f, 500f), Color.yellow, 2f);
        }
    }
}
