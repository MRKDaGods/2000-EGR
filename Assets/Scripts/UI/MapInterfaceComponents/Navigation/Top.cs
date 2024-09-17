using Coffee.UIEffects;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface
{
    public partial class Navigation
    {
        private class Top
        {
            private RectTransform _transform;
            private TMP_InputField _from;
            private TMP_InputField _to;
            private Button[] _profiles;
            private int _selectedProfileIndex;
            private readonly UIHsvModifier[] _validationModifiers;
            private float _initialY;

            private static Color _selectedProfileColor;
            private static Color _idleProfileColor;

            public string From
            {
                get
                {
                    return _from.text;
                }
            }

            public string To
            {
                get
                {
                    return _to.text;
                }
            }

            public TMP_InputField FromInput
            {
                get
                {
                    return _from;
                }
            }

            public TMP_InputField ToInput
            {
                get
                {
                    return _to;
                }
            }

            public byte SelectedProfile
            {
                get
                {
                    return (byte)_selectedProfileIndex;
                }
            }

            static Top()
            {
                _selectedProfileColor = new Color(0.4588235294117647f, 0.6980392156862745f, 1f, 1f);
                _idleProfileColor = new Color(0.5176470588235294f, 0.5176470588235294f, 0.5176470588235294f, 1f);
            }

            public Top(RectTransform transform)
            {
                _transform = transform;

                _from = _transform.Find("Main/Places/From").GetComponent<TMP_InputField>();
                _to = _transform.Find("Main/Places/To").GetComponent<TMP_InputField>();

                _from.text = _to.text = "";
                _from.onSelect.AddListener((val) => OnSelect(0));
                _to.onSelect.AddListener((val) => OnSelect(1));
                _from.onValueChanged.AddListener((val) => OnTextChanged(0, val));
                _to.onValueChanged.AddListener((val) => OnTextChanged(1, val));

                _profiles = _transform.Find("Main/Places/Profiles").GetComponentsInChildren<Button>();
                for (int i = 0; i < _profiles.Length; i++)
                {
                    int _i = i;
                    _profiles[i].onClick.AddListener(() => OnProfileClicked(_i));
                }

                _selectedProfileIndex = 0;
                UpdateSelectedProfile();

                _validationModifiers = new UIHsvModifier[2]{
                    _from.GetComponent<UIHsvModifier>(),
                    _to.GetComponent<UIHsvModifier>()
                };

                foreach (UIHsvModifier modifier in _validationModifiers)
                {
                    modifier.enabled = false;
                }

                _initialY = _transform.anchoredPosition.y;
                transform.anchoredPosition = new Vector3(_transform.anchoredPosition.x, _initialY + _transform.rect.height); //initially
            }

            private void OnProfileClicked(int index)
            {
                _selectedProfileIndex = index;
                UpdateSelectedProfile();
            }

            private void UpdateSelectedProfile()
            {
                for (int i = 0; i < _profiles.Length; i++)
                {
                    _profiles[i].GetComponent<Image>().color = _selectedProfileIndex == i ? _selectedProfileColor : _idleProfileColor;
                }
            }

            private void OnTextChanged(int idx, string value)
            {
                _instance._autoComplete.SetContext(idx, value);

                //invalidate
                SetValidationState(idx, false);
            }

            private void OnSelect(int idx)
            {
                _instance._autoComplete.SetAutoCompleteState(true, idx == 0, /*idx == 1*/ true);
                TMP_InputField active = idx == 0 ? _from : _to;
                _instance._autoComplete.SetActiveInput(active);

                OnTextChanged(idx, active.text);
            }

            public void Show(bool clear = true)
            {
                if (clear)
                {
                    _from.text = _to.text = "";
                }

                _transform.DOAnchorPosY(_initialY, 0.3f)
                    .ChangeStartValue(new Vector3(0f, _initialY + _transform.rect.height))
                    .SetEase(Ease.OutSine);
            }

            public void Hide()
            {
                _transform.DOAnchorPosY(_initialY + _transform.rect.height, 0.3f)
                    .SetEase(Ease.OutSine);
            }

            public void SetInputActive(int idx)
            {
                (idx == 0 ? _from : _to).ActivateInputField();
            }

            public void SetValidationState(int idx, bool state)
            {
                _validationModifiers[idx].enabled = state;

                if (!state)
                {
                    if (idx == 0)
                        _instance.FromCoords = null;
                    else
                        _instance.ToCoords = null;
                }
            }

            public bool IsValid(int idx)
            {
                return _validationModifiers[idx].enabled;
            }
        }
    }
}