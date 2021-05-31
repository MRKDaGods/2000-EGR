using MRK.UI;
using System;
using UnityEditor;
using UnityEngine;
using TMPro;
using RTLTMPro;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(EGRUIRTLAttribute))]
public class EGRUIRTLEditor : Editor {
    enum RTLType {
        None,
        TMProUGUI,
        TMPro,

        MAX
    }

    static readonly Type[] ms_RTLTypes;
    RTLType? m_DetectedType;
    object m_Component;
    readonly Stack<bool> m_StateStack;

    EGRUIRTLAttribute m_RTLAttribute => (EGRUIRTLAttribute)target;

    static EGRUIRTLEditor() {
        ms_RTLTypes = new Type[(int)(RTLType.MAX - 1)] {
            typeof(TextMeshProUGUI),
            typeof(TextMeshPro)
        };
    }

    public EGRUIRTLEditor() {
        m_StateStack = new Stack<bool>();
    }

    RTLType GetDetectedType() {
        if (!m_DetectedType.HasValue) {
            for (int i = 0; i < ms_RTLTypes.Length; i++) {
                object comp = ((EGRUIRTLAttribute)target).GetComponent(ms_RTLTypes[i]);
                if (comp != null) {
                    m_Component = comp;
                    m_DetectedType = (RTLType)(i + 1);
                    break;
                }
            }

            if (m_Component == null)
                m_DetectedType = RTLType.None;
        }

        return m_DetectedType.Value;
    }

    void ConvertRTLTMProUGUI() {
        TextMeshProUGUI txt = (TextMeshProUGUI)m_Component;
        string text = txt.text;
        TMP_FontAsset font = txt.font;
        float fontSize = txt.fontSize;
        float fontSizeMin = txt.fontSizeMin;
        float fontSizeMax = txt.fontSizeMax;
        FontStyles fontStyles = txt.fontStyle;
        bool autoSize = txt.enableAutoSizing;
        Color vertexColor = txt.color;
        TextAlignmentOptions alignment = txt.alignment;
        bool wrapping = txt.enableWordWrapping;
        TextOverflowModes overflow = txt.overflowMode;
        Vector4 margin = txt.margin;
        bool richText = txt.richText;
        bool raycastTarget = txt.raycastTarget;
        float chSpacing = txt.characterSpacing;
        float lineSpacing = txt.lineSpacing;
        float wordSpacing = txt.wordSpacing;
        float paraSpacing = txt.paragraphSpacing;

        GameObject obj = txt.gameObject;
        DestroyImmediate(txt);

        RTLTextMeshPro rtlTxt = obj.AddComponent<RTLTextMeshPro>();
        rtlTxt.text = text;
        rtlTxt.font = font;
        rtlTxt.fontSize = fontSize;
        rtlTxt.fontSizeMin = fontSizeMin;
        rtlTxt.fontSizeMax = fontSizeMax;
        rtlTxt.fontStyle = fontStyles;
        rtlTxt.enableAutoSizing = autoSize;
        rtlTxt.color = vertexColor;
        rtlTxt.alignment = alignment;
        rtlTxt.enableWordWrapping = wrapping;
        rtlTxt.overflowMode = overflow;
        rtlTxt.margin = margin;
        rtlTxt.richText = richText;
        rtlTxt.raycastTarget = raycastTarget;
        rtlTxt.characterSpacing = chSpacing;
        rtlTxt.lineSpacing = lineSpacing;
        rtlTxt.wordSpacing = wordSpacing;
        rtlTxt.paragraphSpacing = paraSpacing;
        rtlTxt.ForceFix = false;
        rtlTxt.Farsi = false;
        rtlTxt.PreserveNumbers = true;
        rtlTxt.FixTags = true;

        m_RTLAttribute.IsRTL = true;
    }

    void RevertRTLTMProUGUI() {
        RTLTextMeshPro txt = (RTLTextMeshPro)m_Component;
        string text = txt.text;
        TMP_FontAsset font = txt.font;
        float fontSize = txt.fontSize;
        float fontSizeMin = txt.fontSizeMin;
        float fontSizeMax = txt.fontSizeMax;
        FontStyles fontStyles = txt.fontStyle;
        bool autoSize = txt.enableAutoSizing;
        Color vertexColor = txt.color;
        TextAlignmentOptions alignment = txt.alignment;
        bool wrapping = txt.enableWordWrapping;
        TextOverflowModes overflow = txt.overflowMode;
        Vector4 margin = txt.margin;
        bool richText = txt.richText;
        bool raycastTarget = txt.raycastTarget;
        float chSpacing = txt.characterSpacing;
        float lineSpacing = txt.lineSpacing;
        float wordSpacing = txt.wordSpacing;
        float paraSpacing = txt.paragraphSpacing;

        GameObject obj = txt.gameObject;
        DestroyImmediate(txt);

        TextMeshProUGUI rtlTxt = obj.AddComponent<TextMeshProUGUI>();
        rtlTxt.text = new string(text.Reverse().ToArray());
        rtlTxt.font = font;
        rtlTxt.fontSize = fontSize;
        rtlTxt.fontSizeMin = fontSizeMin;
        rtlTxt.fontSizeMax = fontSizeMax;
        rtlTxt.fontStyle = fontStyles;
        rtlTxt.enableAutoSizing = autoSize;
        rtlTxt.color = vertexColor;
        rtlTxt.alignment = alignment;
        rtlTxt.enableWordWrapping = wrapping;
        rtlTxt.overflowMode = overflow;
        rtlTxt.margin = margin;
        rtlTxt.richText = richText;
        rtlTxt.raycastTarget = raycastTarget;
        rtlTxt.characterSpacing = chSpacing;
        rtlTxt.lineSpacing = lineSpacing;
        rtlTxt.wordSpacing = wordSpacing;
        rtlTxt.paragraphSpacing = paraSpacing;

        m_RTLAttribute.IsRTL = false;
    }

    public override void OnInspectorGUI() {
        m_StateStack.Push(GUI.skin.label.richText);
        GUI.skin.label.richText = true;

        GUILayout.Label($"Detected Type: <b><color=white>{GetDetectedType()}</color></b>");
        GUILayout.Label($"RTL State: <b><color=white>{m_RTLAttribute.IsRTL}</color></b>");

        m_StateStack.Push(GUI.enabled);
        GUI.enabled = !m_RTLAttribute.IsRTL;

        if (GUILayout.Button("Convert to RTL")) {
            if (m_Component != null) {
                switch (m_DetectedType) {

                    case RTLType.TMProUGUI:
                        ConvertRTLTMProUGUI();
                        break;

                    default:
                        Debug.LogError("No conversion found for " + m_DetectedType);
                        break;
                }
            }
        }

        GUI.enabled = !GUI.enabled;

        if (GUILayout.Button("Convert to TMPro")) {
            if (m_Component != null) {
                switch (m_DetectedType) {

                    case RTLType.TMProUGUI:
                        RevertRTLTMProUGUI();
                        break;

                    default:
                        Debug.LogError("No revert conversion found for " + m_DetectedType);
                        break;
                }
            }
        }

        GUI.enabled = m_StateStack.Pop();
        GUI.skin.label.richText = m_StateStack.Pop();
    }
}

