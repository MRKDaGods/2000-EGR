using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public class EGRUIFancyScrollView : FancyScrollView<EGRUIFancyScrollViewItemData, EGRUIFancyScrollViewContext> {
        [SerializeField]
        Scroller m_Scroller;
        [SerializeField]
        GameObject m_CellPrefab;
        Action<int> m_OnSelectionChanged;
        [SerializeField]
        EGRUIFancyScrollViewDirection m_Direction;

        public event Action<int> OnDoubleSelection;

        protected override GameObject CellPrefab => m_CellPrefab;
        public EGRUIFancyScrollViewDirection Direction => m_Direction;

        protected override void Initialize() {
            base.Initialize();

            Context.Scroll = this;
            Context.OnCellClicked = SelectCell;

            m_Scroller.OnValueChanged(UpdatePosition);
            m_Scroller.OnSelectionChanged(UpdateSelection);
        }

        void UpdateSelection(int index) {
            if (Context.SelectedIndex == index) {
                return;
            }

            Context.SelectedIndex = index;
            Refresh();

            m_OnSelectionChanged?.Invoke(index);
        }

        public void UpdateData(IList<EGRUIFancyScrollViewItemData> items) {
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

        public void SelectCell(int index) {
            if (index < 0 || index >= ItemsSource.Count || index == Context.SelectedIndex) {
                return;
            }

            if (Context.SelectedIndex == index) {

            }

            UpdateSelection(index);
            m_Scroller.ScrollTo(index, 0.35f, Ease.OutCubic);
        }
    }
}
