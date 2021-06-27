using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MRK {
    public class TestSpecialLightStripRendering : MonoBehaviour {
        class Strip {
            public Image Image;
            public float EmissionOffset;
            public float ScaleOffset;
        }

        [SerializeField]
        Image m_LinePrefab;
        [SerializeField]
        Canvas m_Canvas;
        [SerializeField]
        float m_Speed = 1f;
        [SerializeField]
        Gradient m_Spectrum;
        Camera m_Camera;
        readonly List<Strip> m_Strips;
        float m_Time;

        public TestSpecialLightStripRendering() {
            m_Strips = new List<Strip>();
        }

        void Awake() {
            m_Camera = Camera.main;
        }

        void Start() {
            m_LinePrefab.gameObject.SetActive(false);
            RectTransform canvasTransform = (RectTransform)m_Canvas.transform;
            Debug.Log(canvasTransform.rect.width + " | " + m_LinePrefab.rectTransform.rect.width);
            int hStripCount = Mathf.CeilToInt(canvasTransform.rect.width / m_LinePrefab.rectTransform.rect.width);
            Debug.Log($"Strips={hStripCount}");

            m_LinePrefab.rectTransform.sizeDelta = new Vector2(m_LinePrefab.rectTransform.sizeDelta.x, canvasTransform.rect.height);

            for (int i = 0; i < hStripCount; i++) {
                Image strip = Instantiate(m_LinePrefab, m_Canvas.transform);
                strip.rectTransform.anchoredPosition = strip.rectTransform.rect.size * new Vector2(i + 0.5f, -0.5f);
                Material stripMat = Instantiate(strip.material);
                //stripMat.color = m_Spectrum.Evaluate(Random.value);
                
                float startEmission = Random.Range(0f, 2f);
                strip.material.SetFloat("_Emission", GetPingPongedValue(startEmission));

                float startScale = Random.Range(0f, 2f);
                strip.material.mainTextureScale = new Vector2(1f, GetPingPongedValue(startScale));

                strip.material = stripMat;

                strip.gameObject.SetActive(true);

                m_Strips.Add(new Strip {
                    Image = strip,
                    EmissionOffset = startEmission,
                    ScaleOffset = startScale
                });
            }
        }

        float GetPingPongedValue(float off) {
            return Mathf.PingPong(m_Time + off, 2f);
        }

        void Update() {
            m_Time += Time.deltaTime * m_Speed;

            foreach (Strip strip in m_Strips) {
                strip.Image.material.SetFloat("_Emission", GetPingPongedValue(strip.EmissionOffset));
                strip.Image.material.mainTextureScale = new Vector2(1f, GetPingPongedValue(strip.ScaleOffset));
            }
        }
    }
}
