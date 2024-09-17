using FuzzySharp;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MRK.UI
{
    public partial class PlaceList
    {
        private class SearchArea
        {
            private readonly TMP_InputField _input;
            private readonly PlaceItem _stationaryItem;

            public SearchArea(Transform transform)
            {
                _input = transform.GetElement<TMP_InputField>("Textbox");
                _input.onValueChanged.AddListener(OnInputTextChanged);

                _stationaryItem = new PlaceItem(null, true);
            }

            private void OnInputTextChanged(string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    Instance.ClearFocusedItems();
                    return;
                }

                _stationaryItem.SetInfo(str, null);
                Instance.SetFocusedItems(Process.ExtractSorted<PlaceItem>(_stationaryItem, Instance._items, item => item.Name)
                    .Select(res => res.Value)
                    .ToList());
            }

            public void Clear()
            {
                _input.SetTextWithoutNotify(string.Empty);
            }
        }
    }
}
