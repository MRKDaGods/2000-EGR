using System;
using UnityEngine;

namespace MRK.UI
{
    public class TextRenderer : BaseBehaviour
    {
        private string _text;
        private float _curTime;
        private float _maxTime;
        [SerializeField]
        private AnimationCurve[] _curves;
        private int _curveIdx;
        private GUIStyle _style;
        [SerializeField]
        private Font _font;
        private bool _recreateStyle;

        private static TextRenderer _instance;

        private void Awake()
        {
            _instance = this;
        }

        private void InternalRender(string txt, float time, int curveIdx)
        {
            if (curveIdx >= _curves.Length)
            {
                MRKLogger.Log("Curve does not exist");
                return;
            }

            _text = txt;
            _curTime = 0f;
            _maxTime = time;
            _curveIdx = curveIdx;
            _recreateStyle = true;
        }

        public static void Render(string txt, float time, int curveIdx)
        {
            _instance.InternalRender(txt, time, curveIdx);
        }

        public static void Modify(Action<GUIStyle> callback)
        {
            _instance._recreateStyle = false;
            callback(_instance._style);
        }

        private void Update()
        {
            if (_curTime < _maxTime)
                _curTime += Time.deltaTime;
        }

        private void OnGUI()
        {
            if (_style == null || _recreateStyle)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.RoundToInt(100f.ScaleY()),
                    font = _font
                };
            }

            if (_curTime >= _maxTime)
                return;

            GUI.Label(new Rect(0f, Screen.height * _curves[_curveIdx].Evaluate(_curTime / _maxTime),
                Screen.width, _style.CalcHeight(new GUIContent(_text), Screen.width)), _text, _style);
        }
    }
}
