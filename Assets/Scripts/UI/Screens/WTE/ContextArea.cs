using Coffee.UIEffects;
using DG.Tweening;
using MRK.Localization;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public partial class WTE
    {
        private class ContextArea
        {
            private FancyScrollView[] _contextualScrollView;
            private TextMeshProUGUI[] _contextualText;
            private Image _contextualBg;
            private UIGradient _contextualBgGradient;
            private ScrollSnap _scrollSnap;
            private int _lastPage;
            private TextAsset _cuisineList;
            private ScrollViewSortingLetters _cuisineLettersView;
            private readonly Dictionary<char, int> _cuisineCharTable;
            private SearchConfirmation _searchConfirmation;

            public FancyScrollView[] ContextualScrollView
            {
                get
                {
                    return _contextualScrollView;
                }
            }

            public int Page
            {
                get
                {
                    return _lastPage;
                }
            }

            public ContextArea(Transform screenspaceTrans)
            {
                _scrollSnap = screenspaceTrans.Find("SSVM").GetComponent<ScrollSnap>();
                _scrollSnap.onPageChange += OnPageChanged;

                Transform list = _scrollSnap.transform.Find("List");
                _contextualScrollView = new FancyScrollView[list.childCount];
                _contextualText = new TextMeshProUGUI[list.childCount];

                for (int i = 0; i < list.childCount; i++)
                {
                    Transform owner = list.Find($"{i + 1}"); //1 - 2 - 3
                    _contextualText[i] = owner.Find("ContextualText").GetComponent<TextMeshProUGUI>();
                    FancyScrollView sv = owner.Find("ContextualButtons")?.GetComponent<FancyScrollView>();
                    _contextualScrollView[i] = sv;

                    if (sv != null)
                    {
                        int sIdx = i;
                        sv.OnDoubleSelection += x => OnDoubleSelection(sv, sIdx);
                    }
                }

                _contextualBg = screenspaceTrans.Find("ContextualBG").GetComponent<Image>();
                _contextualBgGradient = _contextualBg.GetComponent<UIGradient>();

                _lastPage = -1;
                _scrollSnap.ChangePage(0);
                OnPageChanged(0);

                _cuisineCharTable = new Dictionary<char, int>();

                _searchConfirmation = screenspaceTrans.Find("SearchConfirmation").GetComponent<SearchConfirmation>();
            }

            public void SetupCellGradients()
            {
                //cells must've been init before doing this

                //setup cell gradients
                for (int i = 0; i < _contextualScrollView.Length; i++)
                {
                    UIGradient[] grads = _contextualScrollView[i]?.GetComponentsInChildren<UIGradient>();
                    if (grads == null)
                        continue;

                    ContextGradient grad = ms_Instance._contextGradients[i];

                    foreach (UIGradient gradient in grads)
                    {
                        gradient.color1 = grad.Third;
                        gradient.color2 = grad.Fourth;
                        gradient.color3 = grad.Fifth;
                        gradient.color4 = grad.Sixth;
                        gradient.offset = grad.Offset;
                        gradient.direction = grad.Direction;
                    }
                }
            }

            private void OnPageChanged(int page)
            {
                if (_lastPage == page || page >= ms_Instance._contextGradients.Length)
                    return;


                ContextGradient curGradient = ms_Instance._contextGradients[page];
                DOTween.To(() => _contextualBgGradient.color1, x => _contextualBgGradient.color1 = x, curGradient.First, 0.5f).SetEase(Ease.OutSine);
                DOTween.To(() => _contextualBgGradient.color2, x => _contextualBgGradient.color2 = x, curGradient.Second, 0.5f).SetEase(Ease.OutSine);

                //last page, hide WTE logo
                if (page == 2)
                {
                    ms_Instance.m_wteLogoMaskTransform.DOSizeDelta(new Vector2(0f, ms_Instance.m_wteLogoSizeDelta.Value.y), 0.5f)
                        .SetEase(Ease.OutSine);

                    if (_cuisineList == null)
                    {
                        ResourceRequest req = Resources.LoadAsync<TextAsset>("Features/wteCuisines");
                        req.completed += (op) =>
                        {
                            _cuisineList = (TextAsset)req.asset;
                            FancyScrollView scrollView = _contextualScrollView[2]; //last

                            scrollView.UpdateData(_cuisineList.text.Split('\n')
                                .Select(x => new FancyScrollViewItemData(x.Replace("\r", ""))).ToList());
                            scrollView.SelectCell(0);

                            foreach (char c in ScrollViewSortingLetters.Letters)
                            {
                                for (int i = 0; i < scrollView.Items.Count; i++)
                                {
                                    if (char.ToUpper(scrollView.Items[i].Text[0]) == c)
                                    {
                                        _cuisineCharTable[c] = i;
                                        break;
                                    }
                                }
                            }

                            SetupCellGradients();

                            ms_Instance.MessageBox.HideScreen();
                        };

                        ms_Instance.MessageBox.ShowButton(false);
                        ms_Instance.MessageBox.ShowPopup(
                            Localize(LanguageData.EGR),
                            Localize(LanguageData.LOADING_CUISINES___),
                            null,
                            ms_Instance
                        );
                    }

                    if (_cuisineLettersView == null)
                    {
                        FancyScrollView sv = _contextualScrollView[2];
                        sv.OnSelectionChanged(OnCuisineSelectionChanged);
                        _cuisineLettersView = sv.transform.parent.Find("CS").GetComponent<ScrollViewSortingLetters>();
                        _cuisineLettersView.Initialize();
                        _cuisineLettersView.LetterChanged += OnCuisineLetterChanged;
                    }
                }
                else if (_lastPage == 2)
                {
                    ms_Instance.m_wteLogoMaskTransform.DOSizeDelta(ms_Instance.m_wteLogoSizeDelta.Value, 0.5f)
                        .SetEase(Ease.OutSine);
                }

                _lastPage = page;
            }

            public void SetActive(bool active)
            {
                for (int i = 0; i < _contextualScrollView.Length; i++)
                {
                    _contextualScrollView[i]?.gameObject.SetActive(active);
                    _contextualText[i].gameObject.SetActive(active);
                }

                _contextualBg.gameObject.SetActive(active);

                if (active)
                {
                    _scrollSnap.ChangePage(0);

                    _contextualBg.DOColor(Color.white, 0.5f)
                        .ChangeStartValue(Color.white.AlterAlpha(0f))
                        .SetEase(Ease.OutSine);
                }
            }

            private void OnDoubleSelection(FancyScrollView sv, int screenIdx)
            {
                switch (screenIdx)
                {

                    case 0:
                    case 1:
                        _scrollSnap.ChangePage(screenIdx + 1);
                        break;

                    case 2:
                        _searchConfirmation.Show(new SearchConfirmation.WTEContext
                        {
                            People = _contextualScrollView[0].SelectedItem.Text,
                            Price = _contextualScrollView[1].SelectedIndex,
                            PriceStr = _contextualScrollView[1].SelectedItem.Text,
                            Cuisine = _contextualScrollView[2].SelectedItem.Text
                        });
                        break;

                }
            }

            private void OnCuisineSelectionChanged(int idx)
            {
                _cuisineLettersView.SelectLetter(_contextualScrollView[2].Items[idx].Text[0]);
            }

            private void OnCuisineLetterChanged(char c)
            {
                if (_cuisineCharTable.ContainsKey(c))
                {
                    _contextualScrollView[2].SelectCell(_cuisineCharTable[c], false);
                }
            }
        }
    }
}
