using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.IO;

using Screen = MRK.UI.EGRScreen;
using System.Text;

public class EGRUIUpdater : MonoBehaviour {
    static string GetPath(Transform transform) {
        string path = transform.name;
        while (transform.parent != null) {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    [MenuItem("EGR/Addz")]
    static void CopyPath() {
        //uncomment when needed
        return;

        GameObject go = Selection.activeGameObject;

        if (go == null) {
            return;
        }

        foreach (Button b in go.transform.GetComponentsInChildren<Button>()) {
            Image img = b.gameObject.AddComponent<Image>();
            img.color = Color.clear;
        }
    }

    [MenuItem("EGR/Update UI")]
    static void Main() {
        Debug.LogError("DEPRECATED");
        return;

        Scene activeScene = SceneManager.GetActiveScene();
        using (FileStream fstream = new FileStream($@"{Application.dataPath}\Scripts\UI\Gen\EGRUI_{activeScene.name}.cs", FileMode.Create)) {
            StringBuilder sb = new StringBuilder();

            sb.Append($"namespace MRK.UI {{\n\tpublic class EGRUI_{activeScene.name} {{\n\t\t ");

            Screen[] screens = GameObject.Find("EGRDefaultCanvas").transform.GetComponentsInChildren<Screen>();

            foreach (Screen screen in screens) {
                string name = screen.ScreenName;

                string pref = screen is MRK.UI.EGRPopup ? "Popup" : "Screen";
                sb.Append($"public class EGR{pref}_{name} {{ //{name}\n\t\t\tpublic static string SCREEN_NAME = \"{name}\";\n\n\t\t\t");

                List<string> textBoxes = new List<string>();
                List<string> buttons = new List<string>();
                List<string> labels = new List<string>();
                List<string> images = new List<string>();
                List<string> toggles = new List<string>();
                List<string> others = new List<string>();
                List<int> strips = new List<int>();
                List<string> fnames = new List<string>();

                foreach (Transform transform in screen.GetComponentsInChildren<Transform>()) {
                    string tname = transform.name;
                    string _name = GetPath(transform);
                    int stripIdx;
                    if (tname.StartsWith("tb")) {
                        textBoxes.Add(_name);
                        stripIdx = 2;
                    }

                    else if (tname.StartsWith("b")) {
                        buttons.Add(_name);
                        stripIdx = 1;
                    }

                    else if (tname.StartsWith("txt")) {
                        labels.Add(_name);
                        stripIdx = 3;
                    }

                    else if (tname.StartsWith("img")) {
                        images.Add(_name);
                        stripIdx = 3;
                    }

                    else if (tname.StartsWith("tog")) {
                        toggles.Add(_name);
                        stripIdx = 3;
                    }

                    else if (tname.StartsWith("o")) {
                        others.Add(_name);
                        stripIdx = 1;
                    }

                    else
                        continue;

                    fnames.Add(tname);
                    strips.Add(stripIdx);
                }

                List<string> old = new List<string>();
                List<int> newIndicies = new List<int>();
                foreach (string s in fnames)
                    old.Add(s);

                fnames.Sort();
                buttons.Sort();
                images.Sort();
                others.Sort();
                textBoxes.Sort();
                toggles.Sort();
                labels.Sort();

                for (int i = 0; i < fnames.Count; i++) {
                    int oldIndex = -1;
                    for (int j = 0; j < old.Count; j++) {
                        if (fnames[i] == old[j]) {
                            oldIndex = j;
                            break;
                        }
                    }

                    newIndicies.Insert(i, strips[oldIndex]);
                }

                int _idx = 0;
                string getName() {
                    string ret = fnames[_idx];
                    _idx++;

                    return ret;
                }

                int idx = 0;
                int getStrip() {
                    int ret = newIndicies[idx];
                    idx++;

                    return ret;
                }

                sb.Append("public class Buttons {\n\t\t\t\t");

                foreach (string b in buttons)
                    sb.Append($"public static string {getName().Substring(getStrip())} = \"/{b}\";\n\t\t\t");

                sb.Append("}\n\t\t\t");

                sb.Append("public class Images {\n\t\t\t\t");

                foreach (string i in images)
                    sb.Append($"public static string {getName().Substring(getStrip())} = \"/{i}\";\n\t\t\t");

                sb.Append("}\n\t\t\t");

                sb.Append("public class Others {\n\t\t\t\t");

                foreach (string o in others)
                    sb.Append($"public static string {getName().Substring(getStrip())} = \"/{o}\";\n\t\t\t");

                sb.Append("}\n\t\t\t");

                sb.Append("public class Textboxes {\n\t\t\t\t");

                foreach (string tb in textBoxes)
                    sb.Append($"public static string {getName().Substring(getStrip())} = \"/{tb}\";\n\t\t\t");

                sb.Append("}\n\t\t\t");

                sb.Append("public class Toggles {\n\t\t\t\t");

                foreach (string t in toggles)
                    sb.Append($"public static string {getName().Substring(getStrip())} = \"/{t}\";\n\t\t\t");

                sb.Append("}\n\t\t\t");

                sb.Append("public class Labels {\n\t\t\t\t");

                foreach (string l in labels)
                    sb.Append($"public static string {getName().Substring(getStrip())} = \"/{l}\";\n\t\t\t");

                sb.Append("}\n\t\t\t");

                sb.Append("\t\t}");
            }

            sb.Append("\t}\n}\n");

            byte[] _b = Encoding.ASCII.GetBytes(sb.ToString());
            fstream.Write(_b, 0, _b.Length);
            fstream.Close();

            Debug.Log("UI Updated for " + activeScene.name);
        }
    }
}