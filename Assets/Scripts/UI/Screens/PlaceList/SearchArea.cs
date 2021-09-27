using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public partial class EGRScreenPlaceList {
        class SearchArea {
            TMP_InputField m_Input;
            bool m_InputVisible;
            RectTransform m_InputRectTransform;
            float m_InputTweenProgress;
            float m_InputHiddenOffsetMin;

            public SearchArea(Transform transform) {
                return;
                transform.GetElement<Button>("Button").onClick.AddListener(OnButtonClick);

                m_Input = transform.GetElement<TMP_InputField>("Input");
                m_InputRectTransform = (RectTransform)m_Input.transform;
                m_InputHiddenOffsetMin = m_InputRectTransform.rect.width;
            }

            void OnButtonClick() {
                m_InputVisible = !m_InputVisible;

                if (m_InputVisible)
                    Show();
                else
                    Hide();
            }

            public void Hide() {
                return;
                m_InputVisible = false;
                //upon diagnosis of the current input anchors, I concluded the following:
                //sizeDelta_HIDDEN = sizeDelta_INITIAL.x - rect_INITIAL.width
                //sizeDelta_SHOWN = sizeDelta_INITIAL.x

                DOTween.To(() => m_InputTweenProgress, x => m_InputTweenProgress = x, 1f, 0.3f)
                    .SetEase(Ease.OutSine)
                    .OnUpdate(OnInputTweenUpdate);
            }

            public void Show() {
                return;
                m_InputVisible = true;

                DOTween.To(() => m_InputTweenProgress, x => m_InputTweenProgress = x, 0f, 0.3f)
                    .SetEase(Ease.OutSine)
                    .OnUpdate(OnInputTweenUpdate);
            }

            void OnInputTweenUpdate() {
                m_InputRectTransform.offsetMin = new Vector2(Mathf.Lerp(0f, m_InputHiddenOffsetMin, m_InputTweenProgress), m_InputRectTransform.offsetMin.y);
            }
        }
    }
}
