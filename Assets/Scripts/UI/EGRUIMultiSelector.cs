using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRUIMultiSelector : MonoBehaviour {
        [SerializeField]
        Button m_LButton;
        [SerializeField]
        Button m_RButton;
        [SerializeField]
        TextMeshProUGUI m_Text;
        [SerializeField]
        string[] m_Values;
        int m_SelectedIndex;
        Coroutine m_RunningCoroutine;

        public int SelectedIndex {
            get {
                return m_SelectedIndex;
            }

            set {
                m_SelectedIndex = value;
                UpdateText();
            }
        }

        void Start() {
            m_LButton.onClick.AddListener(() => OnButtonClick(-1));
            m_RButton.onClick.AddListener(() => OnButtonClick(1));
            UpdateText();
        }

        void OnButtonClick(int delta) {
            m_SelectedIndex += delta;
            if (m_SelectedIndex == m_Values.Length)
                m_SelectedIndex = 0;

            if (m_SelectedIndex == -1)
                m_SelectedIndex = m_Values.Length - 1;

            UpdateText();
        }

        void UpdateText() {
            if (m_RunningCoroutine != null) {
                StopCoroutine(m_RunningCoroutine);
            }

            m_RunningCoroutine = StartCoroutine(SetTextEnumerator((txt) => m_Text.text = txt, m_Values[m_SelectedIndex], 0.1f, ""));
        }

        IEnumerator SetTextEnumerator(Action<string> set, string txt, float speed, string prohibited) {
            string real = "";
            List<int> linesIndices = new List<int>();
            for (int i = 0; i < txt.Length; i++)
                foreach (char p in prohibited) {
                    if (txt[i] == p) {
                        linesIndices.Add(i);
                        break;
                    }
                }

            float timePerChar = speed / txt.Length;

            foreach (char c in txt) {
                bool leave = false;
                foreach (char p in prohibited) {
                    if (c == p) {
                        real += p;
                        leave = true;
                        break;
                    }
                }

                if (leave)
                    continue;

                float secsElaped = 0f;
                while (secsElaped < timePerChar) {
                    yield return new WaitForSeconds(0.02f);
                    secsElaped += 0.02f;

                    string renderedTxt = real + EGRUtils.GetRandomString(txt.Length - real.Length);
                    foreach (int index in linesIndices)
                        renderedTxt = renderedTxt.ReplaceAt(index, prohibited[prohibited.IndexOf(txt[index])]);

                    set(renderedTxt);
                }

                real += c;
            }

            set(txt);
        }
    }
}
