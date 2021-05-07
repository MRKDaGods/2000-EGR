using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRK;
using System.IO;
using UnityEditor;

public class EGRLanguageUpdater : MonoBehaviour {
    [MenuItem("EGR/Update Language")]
    static void Main() {
        TextAsset txt = Resources.Load<TextAsset>($"Lang/{EGRLanguage.English}");
        Dictionary<int, string> strings = new Dictionary<int, string>();
        EGRLanguageManager.Parse(txt, strings, true);

        using (FileStream fstream = new FileStream($@"{Application.dataPath}\Scripts\EGRLanguageData.cs", FileMode.Create))
        using (StreamWriter writer = new StreamWriter(fstream)) {
            writer.WriteLine("namespace MRK {\n\tpublic enum EGRLanguageData {");

            string fixStr(string s) {
                string chars = "!@#$%^&*()-+=~`'\":;/.,><[]{}|\\ ?";
                foreach (char c in chars)
                    s = s.Replace(c, '_');

                return s;
            }

            foreach (KeyValuePair<int, string> pair in strings) {
                writer.WriteLine($"\t\t//{pair.Value}");
                writer.WriteLine($"\t\t{fixStr(pair.Value)} = {pair.Key},\n");
            }

            writer.WriteLine("\t\t__LANG_DATA_MAX");
            writer.WriteLine("\t}\n}");

            writer.Close();
        }
    }
}
