using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ReferenceFitterTMP : BaseBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _reference;
        [SerializeField]
        private bool _fitWidth = true;
        [SerializeField]
        private bool _fitHeight = false;
        private bool _running;

        private void OnEnable()
        {
            _running = false;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        private void OnTextChanged(Object o)
        {
            if (o == _reference)
            {
                if (!_running)
                {
                    _running = true;
                    StartCoroutine(UpdateSize());
                }
            }
        }

        private IEnumerator UpdateSize()
        {
            while (CanvasUpdateRegistry.IsRebuildingGraphics())
            {
                yield return new WaitForSeconds(0.05f);
            }

            Vector2 sz = rectTransform.sizeDelta;
            if (_fitWidth)
            {
                sz.x = _reference.preferredWidth;
            }

            if (_fitHeight)
            {
                sz.y = _reference.preferredHeight;
            }

            rectTransform.sizeDelta = sz;

            _running = false;
        }
    }
}
