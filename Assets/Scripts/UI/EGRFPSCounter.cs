using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK.UI {
    public class EGRFPSCounter : MonoBehaviour {
        float m_DeltaTime;
        GUIStyle m_FPSStyle;

        void OnGUI() {
            if (m_FPSStyle == null) {
                m_FPSStyle = new GUIStyle {
                    alignment = TextAnchor.LowerLeft,
                    fontSize = 27,
                    normal =
                    {
                            textColor = Color.yellow
                        }
                };
            }

            GUI.Label(new Rect(40f, Screen.height - 50f, 50f, 50f), string.Format("{0:0.0} ms ({1:0.} fps)", m_DeltaTime * 1000f, 1f / m_DeltaTime), m_FPSStyle);
        }

        private void Update() {
            m_DeltaTime += (Time.unscaledDeltaTime - m_DeltaTime) * 0.1f;
        }
    }
}
