using Coffee.UIEffects;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface {
    public partial class EGRMapInterfaceComponentNavigation {
        class Top {
            RectTransform m_Transform;
            TMP_InputField m_From;
            TMP_InputField m_To;
            Button[] m_Profiles;
            int m_SelectedProfileIndex;
            static Color ms_SelectedProfileColor;
            static Color ms_IdleProfileColor;
            readonly UIHsvModifier[] m_ValidationModifiers;
            float m_InitialY;

            public string From => m_From.text;
            public string To => m_To.text;
            public TMP_InputField FromInput => m_From;
            public TMP_InputField ToInput => m_To;
            public byte SelectedProfile => (byte)m_SelectedProfileIndex;

            static Top() {
                ms_SelectedProfileColor = new Color(0.4588235294117647f, 0.6980392156862745f, 1f, 1f);
                ms_IdleProfileColor = new Color(0.5176470588235294f, 0.5176470588235294f, 0.5176470588235294f, 1f);
            }

            public Top(RectTransform transform) {
                m_Transform = transform;

                m_From = m_Transform.Find("Main/Places/From").GetComponent<TMP_InputField>();
                m_To = m_Transform.Find("Main/Places/To").GetComponent<TMP_InputField>();

                m_From.text = m_To.text = "";
                m_From.onSelect.AddListener((val) => OnSelect(0));
                m_To.onSelect.AddListener((val) => OnSelect(1));
                m_From.onValueChanged.AddListener((val) => OnTextChanged(0, val));
                m_To.onValueChanged.AddListener((val) => OnTextChanged(1, val));

                m_Profiles = m_Transform.Find("Main/Places/Profiles").GetComponentsInChildren<Button>();
                for (int i = 0; i < m_Profiles.Length; i++) {
                    int _i = i;
                    m_Profiles[i].onClick.AddListener(() => OnProfileClicked(_i));
                }

                m_SelectedProfileIndex = 0;
                UpdateSelectedProfile();

                m_ValidationModifiers = new UIHsvModifier[2]{
                    m_From.GetComponent<UIHsvModifier>(),
                    m_To.GetComponent<UIHsvModifier>()
                };

                foreach (UIHsvModifier modifier in m_ValidationModifiers) {
                    modifier.enabled = false;
                }

                m_InitialY = m_Transform.anchoredPosition.y;
                transform.anchoredPosition = new Vector3(m_Transform.anchoredPosition.x, m_InitialY + m_Transform.rect.height); //initially
            }

            void OnProfileClicked(int index) {
                m_SelectedProfileIndex = index;
                UpdateSelectedProfile();
            }

            void UpdateSelectedProfile() {
                for (int i = 0; i < m_Profiles.Length; i++) {
                    m_Profiles[i].GetComponent<Image>().color = m_SelectedProfileIndex == i ? ms_SelectedProfileColor : ms_IdleProfileColor;
                }
            }

            void OnTextChanged(int idx, string value) {
                ms_Instance.m_AutoComplete.SetContext(idx, value);

                //invalidate
                SetValidationState(idx, false);
            }

            void OnSelect(int idx) {
                ms_Instance.m_AutoComplete.SetAutoCompleteState(true, idx == 0, /*idx == 1*/ true);
                TMP_InputField active = idx == 0 ? m_From : m_To;
                ms_Instance.m_AutoComplete.SetActiveInput(active);

                OnTextChanged(idx, active.text);
            }

            public void Show(bool clear = true) {
                if (clear) {
                    m_From.text = m_To.text = "";
                }

                m_Transform.DOAnchorPosY(m_InitialY, 0.3f)
                    .ChangeStartValue(new Vector3(0f, m_InitialY + m_Transform.rect.height))
                    .SetEase(Ease.OutSine);
            }

            public void Hide() {
                m_Transform.DOAnchorPosY(m_InitialY + m_Transform.rect.height, 0.3f)
                    .SetEase(Ease.OutSine);
            }

            public void SetInputActive(int idx) {
                (idx == 0 ? m_From : m_To).ActivateInputField();
            }

            public void SetValidationState(int idx, bool state) {
                m_ValidationModifiers[idx].enabled = state;

                if (!state) {
                    if (idx == 0)
                        ms_Instance.FromCoords = null;
                    else
                        ms_Instance.ToCoords = null;
                }
            }

            public bool IsValid(int idx) {
                return m_ValidationModifiers[idx].enabled;
            }
        }
    }
}