using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK.UI {
    public class EGRUIScrollViewSortingLetters : MRKBehaviour {
        public static string Letters;
        
        EGRUIFancyScrollView m_ScrollView;

        public event Action<char> OnLetterChanged;

        static EGRUIScrollViewSortingLetters() {
            Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        }

        void Start() {
            m_ScrollView = GetComponent<EGRUIFancyScrollView>();
        }

        public void Initialize() {
            m_ScrollView.UpdateData(Letters.Select(x => new EGRUIFancyScrollViewItemData($"{x}")).ToList());
            m_ScrollView.SelectCell(0);
            m_ScrollView.OnSelectionChanged(OnSelectionChanged);
        }

        public void SelectLetter(char c) {
            m_ScrollView.SelectCell(Letters.IndexOf(c), false);
        }

        void OnSelectionChanged(int idx) {
            if (OnLetterChanged != null) {
                OnLetterChanged(Letters[idx]);
            }
        }
    }
}
