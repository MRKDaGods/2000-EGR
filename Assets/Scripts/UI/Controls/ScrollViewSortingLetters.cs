using System;
using System.Linq;

namespace MRK.UI
{
    public class ScrollViewSortingLetters : BaseBehaviour
    {
        private FancyScrollView _scrollView;

        public static string Letters;

        public event Action<char> LetterChanged;

        static ScrollViewSortingLetters()
        {
            Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        }

        private void Start()
        {
            _scrollView = GetComponent<FancyScrollView>();
        }

        public void Initialize()
        {
            _scrollView.UpdateData(Letters.Select(x => new FancyScrollViewItemData($"{x}")).ToList());
            _scrollView.SelectCell(0);
            _scrollView.OnSelectionChanged(OnSelectionChanged);
        }

        public void SelectLetter(char c)
        {
            _scrollView.SelectCell(Letters.IndexOf(c), false);
        }

        private void OnSelectionChanged(int idx)
        {
            if (LetterChanged != null)
            {
                LetterChanged(Letters[idx]);
            }
        }
    }
}
