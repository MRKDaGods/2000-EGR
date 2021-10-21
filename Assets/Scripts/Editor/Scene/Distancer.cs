using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Distancer : EditorWindow {
    class SavedPos {
        public Vector3 Pos;
        public Transform Owner;
        public float? RelativeDistance;
    }

    [SerializeField]
    Transform[] m_Transforms;
    readonly List<SavedPos> m_SavedPositions;
    float m_DistanceFactor;
    bool m_LiveUpdate;
    SerializedObject m_SerializedObj;
    bool m_Saved;
    Transform m_ParentTransform;

    public Distancer() {
        m_SavedPositions = new List<SavedPos>();
    }

    void OnEnable() {
        if (m_SerializedObj == null) {
            m_SerializedObj = new SerializedObject(this);
        }
    }

    void OnDisable() {
        //reset positions
        if (!m_Saved) {
            ResetPositions();
        }
    }

    void OnGUI() {
        m_SerializedObj.UpdateIfRequiredOrScript();

        GUILayout.BeginVertical();

        m_ParentTransform = (Transform)EditorGUILayout.ObjectField("Parent transform", m_ParentTransform, typeof(Transform), true);
        if (m_ParentTransform != null && GUILayout.Button("Import children")) {
            m_Transforms = new Transform[m_ParentTransform.childCount];
            for (int i = 0; i < m_ParentTransform.childCount; i++) {
                m_Transforms[i] = m_ParentTransform.GetChild(i);
            }
        }

        //render transforms
        {
            SerializedProperty transforms = m_SerializedObj.FindProperty("m_Transforms");
            EditorGUILayout.PropertyField(transforms, true);
        }

        //render positions
        {
            GUILayout.Label("Saved Positions");
            GUI.enabled = false;

            foreach (SavedPos v in m_SavedPositions) {
                EditorGUILayout.Vector3Field(v.Owner.name, v.Pos);
            }

            GUI.enabled = true;
        }

        if (GUILayout.Button("Save posiitons")) {
            //save transform positions
            if (m_SavedPositions.Count > 0) {
                //reset
                ResetPositions();
                m_SavedPositions.Clear();
            }

            m_Transforms.Select(x => new SavedPos { Pos = x.position, Owner = x }).ToList().ForEach(v => m_SavedPositions.Add(v));
        }

        EditorGUILayout.Space();
        m_DistanceFactor = EditorGUILayout.FloatField("Distance factor", m_DistanceFactor);
        m_LiveUpdate = EditorGUILayout.Toggle("Live update", m_LiveUpdate);
        m_Saved = EditorGUILayout.Toggle("Save", m_Saved);

        if (m_LiveUpdate || GUILayout.Button("Distance em")) {
            ApplyDistancing();
        }

        if (GUILayout.Button("Backup")) {
            if (m_SavedPositions.Count > 0) {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(m_SavedPositions.Count);

                    foreach (SavedPos v in m_SavedPositions) {
                        writer.Write(v.Pos.x);
                        writer.Write(v.Pos.y);
                        writer.Write(v.Pos.z);
                    }

                    byte[] data = stream.GetBuffer();
                    string b64 = System.Convert.ToBase64String(data);
                    PlayerPrefs.SetString("MRK_EDITOR_DISTANCER", b64);

                    writer.Close();
                }
            }
        }

        if (GUILayout.Button("Restore")) {
            string b64 = PlayerPrefs.GetString("MRK_EDITOR_DISTANCER", null);
            if (b64 != null) {
                byte[] data = System.Convert.FromBase64String(b64);
                using (MemoryStream stream = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(stream)) {
                    int count = reader.ReadInt32();
                    if (count != m_SavedPositions.Count) {
                        Debug.LogError("Count mismatch");
                        goto __close;
                    }

                    for (int i = 0; i <  count; i++) {
                        m_SavedPositions[i].Pos = new Vector3 {
                            x = reader.ReadSingle(),
                            y = reader.ReadSingle(),
                            z = reader.ReadSingle()
                        };
                    }

                    __close:
                    reader.Close();
                }
            }
        }

        GUILayout.EndVertical();

        m_SerializedObj.ApplyModifiedProperties();
    }

    void ResetPositions() {
        m_SavedPositions.ForEach(x => x.Owner.position = x.Pos);
    }

    void ApplyDistancing() {
        // O -> O -> O -> O
        //<    <    <    <

        if (m_Transforms == null || m_Transforms.Length < 2) {
            return;
        }

        float accumulatedDistance = 0f;
        for (int i = 0; i < m_SavedPositions.Count; i++) {
            if (i == 0) //skip 1st element
                continue;

            SavedPos relative = m_SavedPositions[i - 1];
            SavedPos current = m_SavedPositions[i];
            Vector3 dirFromRelative = current.Pos - relative.Pos;

            if (!current.RelativeDistance.HasValue) {
                current.RelativeDistance = dirFromRelative.magnitude;
            }

            //normalize dir
            dirFromRelative.Normalize();

            Vector3 rawOrthographicIncrement = dirFromRelative * (current.RelativeDistance.Value * m_DistanceFactor);
            current.Owner.position = current.Pos + rawOrthographicIncrement + (dirFromRelative * accumulatedDistance);

            accumulatedDistance += rawOrthographicIncrement.magnitude;
        }
    }

    [MenuItem("MRK/Scene/Distancer")]
    static void Main() {
        GetWindow<Distancer>("Distancer").Show();
    }
}
