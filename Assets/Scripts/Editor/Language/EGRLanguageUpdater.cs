using MRK.Localization;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EGRLanguageUpdater : MonoBehaviour
{
    [MenuItem("EGR/Update Language")]
    private static void Main()
    {
        TextAsset txt = Resources.Load<TextAsset>($"Lang/{Language.English}");
        Dictionary<int, string> strings = new Dictionary<int, string>();
        LanguageManager.ParseWithOccurence(txt, strings, true);

        using (FileStream fstream = new FileStream($@"{Application.dataPath}\Scripts\Localization\LanguageData.cs", FileMode.Create))
        using (StreamWriter writer = new StreamWriter(fstream))
        {
            writer.WriteLine("namespace MRK.Localization {\n\tpublic enum LanguageData {");

            static string fixStr(string s)
            {
                string chars = "!@#$%^&*()-+=~`'\":;/.,><[]{}|\\ ?";
                foreach (char c in chars)
                {
                    s = s.Replace(c, '_');
                }

                return s;
            }

            foreach (KeyValuePair<int, string> pair in strings)
            {
                writer.WriteLine($"\t\t//{pair.Value}");
                writer.WriteLine($"\t\t{fixStr(pair.Value)} = {pair.Key},\n");
            }

            writer.WriteLine("\t\t__LANG_DATA_MAX");
            writer.WriteLine("\t}\n}");

            writer.Close();
        }
    }
}
