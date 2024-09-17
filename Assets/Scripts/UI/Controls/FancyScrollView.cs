using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI.Extensions.EasingCore;

namespace MRK.UI
{
    public class FancyScrollViewContext
    {
        public int SelectedIndex = -1;
        public Action<int> OnCellClicked;
        public FancyScrollView Scroll;
    }

    [Serializable]
    public class FancyScrollViewItemData
    {
        public string Text;

        public FancyScrollViewItemData(string message)
        {
            Text = message;
        }
    }

    public enum FancyScrollViewDirection
    {
        Horizontal,
        Vertical
    }

    public enum FancyScrollViewPlacement
    {
        Left,
        Right
    }

    public class FancyScrollView : FancyScrollView<FancyScrollViewItemData, FancyScrollViewContext>
    {
        [SerializeField]
        private Scroller m_Scroller;
        [SerializeField]
        private GameObject m_CellPrefab;
        private Action<int> m_OnSelectionChanged;
        [SerializeField]
        private FancyScrollViewDirection m_Direction;
        [SerializeField]
        private FancyScrollViewPlacement m_Placement;

        public event Action<int> OnDoubleSelection;

        protected override GameObject CellPrefab
        {
            get
            {
                return m_CellPrefab;
            }
        }

        public FancyScrollViewDirection Direction
        {
            get
            {
                return m_Direction;
            }
        }

        public FancyScrollViewPlacement Placement
        {
            get
            {
                return m_Placement;
            }
        }

        public IList<FancyScrollViewItemData> Items
        {
            get; private set;
        }
        public FancyScrollViewItemData SelectedItem
        {
            get; private set;
        }
        public int SelectedIndex
        {
            get
            {
                return Context.SelectedIndex;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            Context.Scroll = this;
            Context.OnCellClicked = (i) => SelectCell(i);

            m_Scroller.OnValueChanged(UpdatePosition);
            m_Scroller.OnSelectionChanged((x) => UpdateSelection(x));
        }

        private void UpdateSelection(int index, bool callEvt = true)
        {
            if (Context.SelectedIndex == index)
            {
                return;
            }

            Context.SelectedIndex = index;
            if (Items != null)
            {
                SelectedItem = Items[index];
            }

            Refresh();

            if (callEvt)
            {
                m_OnSelectionChanged?.Invoke(index);
            }
        }

        public void UpdateData(IList<FancyScrollViewItemData> items)
        {
            Items = items;
            UpdateContents(items);
            m_Scroller.SetTotalCount(items.Count);
        }

        public void OnSelectionChanged(Action<int> callback)
        {
            m_OnSelectionChanged = callback;
        }

        public void SelectNextCell()
        {
            SelectCell(Context.SelectedIndex + 1);
        }

        public void SelectPrevCell()
        {
            SelectCell(Context.SelectedIndex - 1);
        }

        public void SelectCell(int index, bool callEvt = true)
        {
            if (index < 0 || index >= ItemsSource.Count)
            {
                return;
            }

            if (callEvt)
            {
                if (Context.SelectedIndex == index)
                {
                    if (OnDoubleSelection != null)
                    {
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
