using UnityEngine;
using UnityEditor;

namespace MRK.UI {
    [CustomEditor(typeof(DisplayRectTransformInfo))]
    public class EGRUIDisplayRectTransformInfoEditor : Editor {
        class GUIState {
            public bool RichTextEnabled;
            public bool Enabled;
        }

        GUIState m_CurrentState;
        string m_CurrentGroup;
        int m_GroupDepth;

        DisplayRectTransformInfo DisplayInfo => (DisplayRectTransformInfo)target;

        void SaveCurrentGUIState() {
            if (m_CurrentState == null)
                m_CurrentState = new GUIState();

            m_CurrentState.RichTextEnabled = GUI.skin.label.richText;
            m_CurrentState.Enabled = GUI.enabled;
        }

        void RestoreGUIState() {
            if (m_CurrentState == null)
                return;

            GUI.skin.label.richText = m_CurrentState.RichTextEnabled;
            GUI.enabled = m_CurrentState.Enabled;
        }

        void BeginGroup(string properties, string group) {
            m_CurrentGroup = group;

            if (m_GroupDepth > 0)
                GUILayout.Label("");

            GUILayout.Label($"{new string('\t', m_GroupDepth)}<b>{properties}</b>:");

            m_GroupDepth++;
        }

        void EndGroup() {
            m_GroupDepth--;
        }

        void DrawProperty<T>(string prop, T value) where T : struct {
            GUILayout.Label($"{new string('\t', m_GroupDepth)}{m_CurrentGroup}.<b>{prop}</b>: <b><color=white>{value}</color></b>");
        }

        public override void OnInspectorGUI() {
            if (DisplayInfo == null || DisplayInfo.rectTransform == null)
                return;

            SaveCurrentGUIState();

            GUI.skin.label.richText = true;

            BeginGroup("Properties", "rectTransform");
            {
                DrawProperty("offsetMin", DisplayInfo.rectTransform.offsetMin);
                DrawProperty("offsetMax", DisplayInfo.rectTransform.offsetMax);
                DrawProperty("sizeDelta", DisplayInfo.rectTransform.sizeDelta);
                DrawProperty("anchoredPosition", DisplayInfo.rectTransform.anchoredPosition);
            }
            EndGroup();

            BeginGroup("Edit", "rectTransform");
            {
                DisplayInfo.rectTransform.sizeDelta = EditorGUILayout.Vector2Field("\tsizeDelta", DisplayInfo.rectTransform.sizeDelta);
            }
            EndGroup();

            RestoreGUIState();
        }
    }
}