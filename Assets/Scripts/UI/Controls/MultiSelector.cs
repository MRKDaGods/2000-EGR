using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public class MultiSelector : MonoBehaviour
    {
        [SerializeField]
        private Button _lButton;
        [SerializeField]
        private Button _rButton;
        [SerializeField]
        private TextMeshProUGUI _text;
        [SerializeField]
        private string[] _values;
        private int _selectedIndex;
        private Coroutine _runningCoroutine;

        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }

            set
            {
                _selectedIndex = value;
                UpdateText();
            }
        }

        private void Start()
        {
            _lButton.onClick.AddListener(() => OnButtonClick(-1));
            _rButton.onClick.AddListener(() => OnButtonClick(1));
            UpdateText();
        }

        private void OnButtonClick(int delta)
        {
            _selectedIndex += delta;
            if (_selectedIndex == _values.Length)
                _selectedIndex = 0;

            if (_selectedIndex == -1)
                _selectedIndex = _values.Length - 1;

            UpdateText();
        }

        private void UpdateText()
        {
            if (_runningCoroutine != null)
            {
                StopCoroutine(_runningCoroutine);
            }

            _runningCoroutine = StartCoroutine(SetTextEnumerator((txt) => _text.text = txt, _values[_selectedIndex], 0.1f, ""));
        }

        private IEnumerator SetTextEnumerator(Action<string> set, string txt, float speed, string prohibited)
        {
            string real = "";
            List<int> linesIndices = new List<int>();
            for (int i = 0; i < txt.Length; i++)
                foreach (char p in prohibited)
                {
                    if (txt[i] == p)
                    {
                        linesIndices.Add(i);
                        break;
                    }
                }

            float timePerChar = speed / txt.Length;

            foreach (char c in txt)
            {
                bool leave = false;
                foreach (char p in prohibited)
                {
                    if (c == p)
                    {
                        real += p;
                        leave = true;
                        break;
                    }
                }

                if (leave)
                    continue;

                float secsElaped = 0f;
                while (secsElaped < timePerChar)
                {
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
