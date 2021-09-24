using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    [RequireComponent(typeof(RectTransform))]
    public class EGRUIReferenceFitterTMP : MRKBehaviour {
        [SerializeField]
        TextMeshProUGUI m_Reference;
        [SerializeField]
        bool m_FitWidth = true;
        [SerializeField]
        bool m_FitHeight = false;
        bool m_Running;

        void OnEnable() {
            m_Running = false;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        void OnDisable() {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        void OnTextChanged(Object o) {
            if (o == m_Reference) {
                if (!m_Running) {
                    m_Running = true;
                    StartCoroutine(UpdateSize());
                }
            }
        }

        IEnumerator UpdateSize() {
            while (CanvasUpdateRegistry.IsRebuildingGraphics()) {
                yield return new WaitForSeconds(0.05f);
            }

            Vector2 sz = rectTransform.sizeDelta;
            if (m_FitWidth)
                sz.x = m_Reference.preferredWidth;

            if (m_FitHeight)
                sz.y = m_Reference.preferredHeight;

            rectTransform.sizeDelta = sz;
            
            m_Running = false;
        }
    }
}
