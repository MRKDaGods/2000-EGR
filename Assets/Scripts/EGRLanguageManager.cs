﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public enum EGRLanguage {
        English,
        Arabic,

        MAX
    }

    public class EGRLanguageManager {
        readonly Dictionary<int, string> m_Strings;

        public EGRLanguage CurrentLanguage { get; private set; }
        public string this[EGRLanguageData data] => m_Strings[(int)data];

        public EGRLanguageManager() {
            m_Strings = new Dictionary<int, string>();
        }

        public void Init() {
            CurrentLanguage = (EGRLanguage)PlayerPrefs.GetInt(EGRConstants.EGR_LOCALPREFS_LANGUAGE, 0);
            Parse(Resources.Load<TextAsset>($"Lang/{CurrentLanguage}"), m_Strings);
        }

        public static void Parse(TextAsset asset, Dictionary<int, string> buf, bool editor = false) {
            string[] lines = asset.text.Split('\n');
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i].Trim(' ', '\r', '\n', '\t');

                if (line.StartsWith("#"))
                    continue;

                int idx = line.IndexOf('-');
                string numStr = line.Substring(0, idx).Replace(" ", "");
                int id = int.Parse(numStr);

                string txt = line.Substring(idx + 1).TrimStart(' ');
                if (!editor) {
                    txt = txt.Replace("$n", "\n");
                }

                buf[id] = txt;
            }
        }

        public static string Localize(EGRLanguageData data) {
            return EGRMain.Instance.LanguageManager[data];
        }
    }
}