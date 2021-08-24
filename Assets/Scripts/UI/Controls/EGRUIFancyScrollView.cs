using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI.Extensions.EasingCore;

namespace MRK.UI {
    public class EGRUIFancyScrollViewContext {
        public int SelectedIndex = -1;
        public Action<int> OnCellClicked;
        public EGRUIFancyScrollView Scroll;
    }

    [Serializable]
    public class EGRUIFancyScrollViewItemData {
        public string Text;

        public EGRUIFancyScrollViewItemData(string message) {
            Text = message;
        }
    }

    public enum EGRUIFancyScrollViewDirection {
        Horizontal,
        Vertical
    }

    public enum EGRUIFancyScrollViewPlacement {
        Left,
        Right
    }

    public class EGRUIFancyScrollView : FancyScrollView<EGRUIFancyScrollViewItemData, EGRUIFancyScrollViewContext> {
        [SerializeField]
        Scroller m_Scroller;
        [SerializeField]
        GameObject m_CellPrefab;
        Action<int> m_OnSelectionChanged;
        [SerializeField]
        EGRUIFancyScrollViewDirection m_Direction;
        [SerializeField]
        EGRUIFancyScrollViewPlacement m_Placement;

        public event Action<int> OnDoubleSelection;

        protected override GameObject CellPrefab => m_CellPrefab;
        public EGRUIFancyScrollViewDirection Direction => m_Direction;
        public EGRUIFancyScrollViewPlacement Placement => m_Placement;
        public IList<EGRUIFancyScrollViewItemData> Items { get; private set; }
        public EGRUIFancyScrollViewItemData SelectedItem { get; private set; }
        public int SelectedIndex => Context.SelectedIndex;

        protected override void Initialize() {
            base.Initialize();

            Context.Scroll = this;
            Context.OnCellClicked = (i) => SelectCell(i);

            m_Scroller.OnValueChanged(UpdatePosition);
            m_Scroller.OnSelectionChanged((x) => UpdateSelection(x));
        }

        void UpdateSelection(int index, bool callEvt = true) {
            if (Context.SelectedIndex == index) {
                return;
            }

            Context.SelectedIndex = index;
            if (Items != null) {
                SelectedItem = Items[index];
            }

            Refresh();

            if (callEvt) {
                m_OnSelectionChanged?.Invoke(index);
            }
        }

        public void UpdateData(IList<EGRUIFancyScrollViewItemData> items) {
            Items = items;
            UpdateContents(items);
            m_Scroller.SetTotalCount(items.Count);
        }

        public void OnSelectionChanged(Action<int> callback) {
            m_OnSelectionChanged = callback;
        }

        public void SelectNextCell() {
            SelectCell(Context.SelectedIndex + 1);
        }

        public void SelectPrevCell() {
            SelectCell(Context.SelectedIndex - 1);
        }

        public void SelectCell(int index, bool callEvt = true) {
            if (index < 0 || index >= ItemsSource.Count) {
                return;
            }

            if (callEvt) {
                if (Context.SelectedIndex == index) {
                    if (OnDoubleSelection != null) {
                        OnDoubleSelection(index);
                    }

                    return;
                }
            }

            UpdateSelection(index, callEvt);
            m_Scroller.ScrollTo(index, 0.35f, Ease.OutCubic);
        }
    }
}
