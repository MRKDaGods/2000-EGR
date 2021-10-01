using FuzzySharp;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MRK.UI {
    public partial class EGRScreenPlaceList {
        class SearchArea {
            TMP_InputField m_Input;
            PlaceItem m_StationaryItem;

            public SearchArea(Transform transform) {
                m_Input = transform.GetElement<TMP_InputField>("Textbox");
                m_Input.onValueChanged.AddListener(OnInputTextChanged);

                m_StationaryItem = new PlaceItem(null, true);
            }

            void OnInputTextChanged(string str) {
                if (string.IsNullOrEmpty(str)) {
                    Instance.ClearFocusedItems();
                    return;
                }

                m_StationaryItem.SetInfo(str, null);
                Instance.SetFocusedItems(Process.ExtractSorted<PlaceItem>(m_StationaryItem, Instance.m_Items, item => item.Name)
                    .Select(res => res.Value)
                    .ToList());
            }

            public void Clear() {
                m_Input.SetTextWithoutNotify(string.Empty);
            }
        }
    }
}
