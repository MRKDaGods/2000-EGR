using Coffee.UIEffects;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public partial class EGRScreenWTE {
        class ContextArea {
            EGRUIFancyScrollView[] m_ContextualScrollView;
            TextMeshProUGUI[] m_ContextualText;
            Image m_ContextualBg;
            UIGradient m_ContextualBgGradient;
            ScrollSnap m_ScrollSnap;
            int m_LastPage;
            TextAsset m_CuisineList;
            EGRUIScrollViewSortingLetters m_CuisineLettersView;
            readonly Dictionary<char, int> m_CuisineCharTable;
            EGRUIWTESearchConfirmation m_SearchConfirmation;

            public EGRUIFancyScrollView[] ContextualScrollView => m_ContextualScrollView;
            public int Page => m_LastPage;

            public ContextArea(Transform screenspaceTrans) {
                m_ScrollSnap = screenspaceTrans.Find("SSVM").GetComponent<ScrollSnap>();
                m_ScrollSnap.onPageChange += OnPageChanged;

                Transform list = m_ScrollSnap.transform.Find("List");
                m_ContextualScrollView = new EGRUIFancyScrollView[list.childCount];
                m_ContextualText = new TextMeshProUGUI[list.childCount];

                for (int i = 0; i < list.childCount; i++) {
                    Transform owner = list.Find($"{i + 1}"); //1 - 2 - 3
                    m_ContextualText[i] = owner.Find("ContextualText").GetComponent<TextMeshProUGUI>();
                    EGRUIFancyScrollView sv = owner.Find("ContextualButtons")?.GetComponent<EGRUIFancyScrollView>();
                    m_ContextualScrollView[i] = sv;

                    if (sv != null) {
                        int sIdx = i;
                        sv.OnDoubleSelection += x => OnDoubleSelection(sv, sIdx);
                    }
                }

                m_ContextualBg = screenspaceTrans.Find("ContextualBG").GetComponent<Image>();
                m_ContextualBgGradient = m_ContextualBg.GetComponent<UIGradient>();

                m_LastPage = -1;
                m_ScrollSnap.ChangePage(0);
                OnPageChanged(0);

                m_CuisineCharTable = new Dictionary<char, int>();

                m_SearchConfirmation = screenspaceTrans.Find("SearchConfirmation").GetComponent<EGRUIWTESearchConfirmation>();
            }

            public void SetupCellGradients() {
                //cells must've been init before doing this

                //setup cell gradients
                for (int i = 0; i < m_ContextualScrollView.Length; i++) {
                    UIGradient[] grads = m_ContextualScrollView[i]?.GetComponentsInChildren<UIGradient>();
                    if (grads == null)
                        continue;

                    ContextGradient grad = ms_Instance.m_ContextGradients[i];

                    foreach (UIGradient gradient in grads) {
                        gradient.color1 = grad.Third;
                        gradient.color2 = grad.Fourth;
                        gradient.color3 = grad.Fifth;
                        gradient.color4 = grad.Sixth;
                        gradient.offset = grad.Offset;
                        gradient.direction = grad.Direction;
                    }
                }
            }

            void OnPageChanged(int page) {
                if (m_LastPage == page || page >= ms_Instance.m_ContextGradients.Length)
                    return;


                ContextGradient curGradient = ms_Instance.m_ContextGradients[page];
                DOTween.To(() => m_ContextualBgGradient.color1, x => m_ContextualBgGradient.color1 = x, curGradient.First, 0.5f).SetEase(Ease.OutSine);
                DOTween.To(() => m_ContextualBgGradient.color2, x => m_ContextualBgGradient.color2 = x, curGradient.Second, 0.5f).SetEase(Ease.OutSine);

                //last page, hide WTE logo
                if (page == 2) {
                    ms_Instance.m_WTELogoMaskTransform.DOSizeDelta(new Vector2(0f, ms_Instance.m_WTELogoSizeDelta.Value.y), 0.5f)
                        .SetEase(Ease.OutSine);

                    if (m_CuisineList == null) {
                        ResourceRequest req = Resources.LoadAsync<TextAsset>("Features/wteCuisines");
                        req.completed += (op) => {
                            m_CuisineList = (TextAsset)req.asset;
                            EGRUIFancyScrollView scrollView = m_ContextualScrollView[2]; //last

                            scrollView.UpdateData(m_CuisineList.text.Split('\n')
                                .Select(x => new EGRUIFancyScrollViewItemData(x.Replace("\r", ""))).ToList());
                            scrollView.SelectCell(0);

                            foreach (char c in EGRUIScrollViewSortingLetters.Letters) {
                                for (int i = 0; i < scrollView.Items.Count; i++) {
                                    if (char.ToUpper(scrollView.Items[i].Text[0]) == c) {
                                        m_CuisineCharTable[c] = i;
                                        break;
                                    }
                                }
                            }

                            SetupCellGradients();

                            ms_Instance.MessageBox.HideScreen();
                        };

                        ms_Instance.MessageBox.ShowButton(false);
                        ms_Instance.MessageBox.ShowPopup(
                            Localize(EGRLanguageData.EGR),
                            Localize(EGRLanguageData.LOADING_CUISINES___),
                            null,
                            ms_Instance
                        );
                    }

                    if (m_CuisineLettersView == null) {
                        EGRUIFancyScrollView sv = m_ContextualScrollView[2];
                        sv.OnSelectionChanged(OnCuisineSelectionChanged);
                        m_CuisineLettersView = sv.transform.parent.Find("CS").GetComponent<EGRUIScrollViewSortingLetters>();
                        m_CuisineLettersView.Initialize();
                        m_CuisineLettersView.OnLetterChanged += OnCuisineLetterChanged;
                    }
                }
                else if (m_LastPage == 2) {
                    ms_Instance.m_WTELogoMaskTransform.DOSizeDelta(ms_Instance.m_WTELogoSizeDelta.Value, 0.5f)
                        .SetEase(Ease.OutSine);
                }

                m_LastPage = page;
            }

            public void SetActive(bool active) {
                for (int i = 0; i < m_ContextualScrollView.Length; i++) {
                    m_ContextualScrollView[i]?.gameObject.SetActive(active);
                    m_ContextualText[i].gameObject.SetActive(active);
                }

                m_ContextualBg.gameObject.SetActive(active);

                if (active) {
                    m_ScrollSnap.ChangePage(0);

                    m_ContextualBg.DOColor(Color.white, 0.5f)
                        .ChangeStartValue(Color.white.AlterAlpha(0f))
                        .SetEase(Ease.OutSine);
                }
            }

            void OnDoubleSelection(EGRUIFancyScrollView sv, int screenIdx) {
                switch (screenIdx) {

                    case 0:
                    case 1:
                        m_ScrollSnap.ChangePage(screenIdx + 1);
                        break;

                    case 2:
                        m_SearchConfirmation.Show(new EGRUIWTESearchConfirmation.WTEContext {
                            People = m_ContextualScrollView[0].SelectedItem.Text,
                            Price = m_ContextualScrollView[1].SelectedIndex,
                            PriceStr = m_ContextualScrollView[1].SelectedItem.Text,
                            Cuisine = m_ContextualScrollView[2].SelectedItem.Text
                        });
                        break;

                }
            }

            void OnCuisineSelectionChanged(int idx) {
                m_CuisineLettersView.SelectLetter(m_ContextualScrollView[2].Items[idx].Text[0]);
            }

            void OnCuisineLetterChanged(char c) {
                if (m_CuisineCharTable.ContainsKey(c)) {
                    m_ContextualScrollView[2].SelectCell(m_CuisineCharTable[c], false);
                }
            }
        }
    }
}
