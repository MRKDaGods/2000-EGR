using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MRK.Localization
{
    public enum Language
    {
        English,
        Arabic,

        MAX
    }

    public class LanguageManager
    {
        private readonly Dictionary<int, string> _strings;

        public Language CurrentLanguage
        {
            get; private set;
        }

        public string this[LanguageData data]
        {
            get
            {
                return _strings[(int)data];
            }
        }

        public LanguageManager()
        {
            _strings = new Dictionary<int, string>();
        }

        public void Init()
        {
            CurrentLanguage = (Language)PlayerPrefs.GetInt(EGRConstants.EGR_LOCALPREFS_SETTINGS_LANGUAGE, 0);
            Parse(Resources.Load<TextAsset>($"Lang/{CurrentLanguage}"), _strings);
        }

        public static void Parse(TextAsset asset, Dictionary<int, string> buf, bool editor = false)
        {
            if (asset == null || buf == null)
            {
                return;
            }

            string[] lines = asset.text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim(' ', '\r', '\n', '\t');

                if (line.StartsWith("#"))
                {
                    continue;
                }

                int idx = line.IndexOf('-');
                string numStr = line.Substring(0, idx).Replace(" ", "");
                int id = int.Parse(numStr);

                string txt = line.Substring(idx + 1).TrimStart(' ');
                if (!editor)
                {
                    txt = txt.Replace("$n", "\n");
                }

                buf[id] = txt;
            }
        }

        public static void ParseWithOccurence(TextAsset asset, Dictionary<int, string> buf, bool editor = false)
        {
            if (asset == null || buf == null)
            {
                return;
            }

            Parse(asset, buf, editor);

            //O(n^2)
            HashSet<int> visited = new HashSet<int>();
            HashSet<KeyValuePair<int, string>> modifications = new HashSet<KeyValuePair<int, string>>();
            foreach (var pair in buf)
            {
                if (visited.Contains(pair.Key))
                {
                    continue;
                }

                visited.Add(pair.Key);
                var occs = buf.Where(x => x.Value == pair.Value).ToArray();
                if (occs.Length > 1)
                {
                    int occIdx = 0;
                    foreach (var occ in occs)
                    {
                        if (occ.Key == pair.Key)
                        {
                            occIdx++;
                            continue;
                        }

                        visited.Add(occ.Key);
                        modifications.Add(new KeyValuePair<int, string>(occ.Key, $"{occ.Value}_{occIdx++}"));
                    }
                }
            }

            foreach (var modification in modifications)
            {
                buf[modification.Key] = modification.Value;
            }
        }

        public static string Localize(LanguageData data)
        {
            return EGR.Instance.LanguageManager[data];
        }
    }
}
