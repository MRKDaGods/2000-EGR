using System.IO;
using UnityEditor;
using UnityEngine;

namespace MRK {
    public class ScriptableObjectAssetCreator : EditorWindow {
        static string ms_AssetPath = "Assets/";

        string m_Name;
        MonoScript m_Script;
        readonly MRKSelfContainedPtr<GUIContent> m_AssetPathContent;

        public ScriptableObjectAssetCreator() {
            m_AssetPathContent = new MRKSelfContainedPtr<GUIContent>(() => new GUIContent($"<b>{ms_AssetPath}</b>"));
        }

        void OnGUI() {
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("ScriptableObject");
                    m_Script = (MonoScript)EditorGUILayout.ObjectField(m_Script, typeof(MonoScript), false, GUILayout.Width(250f));
                }
                EditorGUILayout.EndHorizontal();

                Rect nameTbRect;
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Relative Path");
                    m_Name = EditorGUILayout.TextField(m_Name, GUILayout.Width(250f));

                    nameTbRect = GUILayoutUtility.GetLastRect();
                }
                EditorGUILayout.EndHorizontal();

                GUIStyle labelStyle = GUI.skin.label;
                bool oldRTState = labelStyle.richText;
                labelStyle.richText = true;
                {
                    float width = labelStyle.CalcSize(m_AssetPathContent).x;
                    nameTbRect.width = width;
                    nameTbRect.x -= width;
                    GUI.Label(nameTbRect, m_AssetPathContent);
                }
                labelStyle.richText = oldRTState;

                if (GUILayout.Button("Create")) {
                    if (m_Script != null) {
                        if (!m_Name.EndsWith(".asset")) {
                            if (!m_Name.EndsWith("/")) {
                                m_Name += '/';
                            }

                            m_Name += $"{m_Script.name}.asset";
                        }

                        Object obj = CreateInstance(m_Script.GetClass());
                        try {
                            string targetPath = $"{ms_AssetPath}{m_Name}";
                            if (!File.Exists(targetPath)) {
                                string dir = Path.GetDirectoryName(targetPath);
                                if (!Directory.Exists(dir)) {
                                    MRKSysUtils.CreateRecursiveDirectory(dir);
                                }

                                AssetDatabase.CreateAsset(obj, targetPath);
                                AssetDatabase.SaveAssets();
                                EditorUtility.FocusProjectWindow();
                                Selection.activeObject = obj;
                            }
                            else {
                                Debug.LogError("Target file already exists");
                            }
                        }
                        catch (System.Exception e) {
                            Debug.LogError(e);
                        }
                    }
                    else {
                        Debug.LogError("Script not assigned!");
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        [MenuItem("MRK/Scriptable Object Asset Creator")]
        static void CreateWindow() {
            GetWindow<ScriptableObjectAssetCreator>("SO Asset Creator").Show();
        }
    }
}
