using UnityEngine;
using System;

namespace MRK.UI {
    public class EGRUITextRenderer : MRKBehaviour {
        string m_Text;
        float m_CurTime;
        float m_MaxTime;
        [SerializeField]
        AnimationCurve[] m_Curves;
        int m_CurveIdx;
        GUIStyle m_Style;
        [SerializeField]
        Font m_Font;
        bool m_RecreateStyle;

        static EGRUITextRenderer ms_Instance;

        void Awake() {
            ms_Instance = this;
        }

        void InternalRender(string txt, float time, int curveIdx) {
            if (curveIdx >= m_Curves.Length) {
                MRKLogger.Log("Curve does not exist");
                return;
            }

            m_Text = txt;
            m_CurTime = 0f;
            m_MaxTime = time;
            m_CurveIdx = curveIdx;
            m_RecreateStyle = true;
        }

        public static void Render(string txt, float time, int curveIdx) {
            ms_Instance.InternalRender(txt, time, curveIdx);
        }

        public static void Modify(Action<GUIStyle> callback) {
            ms_Instance.m_RecreateStyle = false;
            callback(ms_Instance.m_Style);
        }

        void Update() {
            if (m_CurTime < m_MaxTime)
                m_CurTime += Time.deltaTime;
        }

        void OnGUI() {
            if (m_Style == null || m_RecreateStyle) {
                m_Style = new GUIStyle(GUI.skin.label) {
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.RoundToInt(100f.ScaleY()),
                    font = m_Font
                };
            }

            if (m_CurTime >= m_MaxTime)
                return;

            GUI.Label(new Rect(0f, Screen.height * m_Curves[m_CurveIdx].Evaluate(m_CurTime / m_MaxTime),
                Screen.width, m_Style.CalcHeight(new GUIContent(m_Text), Screen.width)), m_Text, m_Style);
        }
    }
}
