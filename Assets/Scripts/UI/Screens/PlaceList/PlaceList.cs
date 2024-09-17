using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MRK.Localization;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public partial class PlaceList : AnimatedAlpha
    {
        private SearchArea _searchArea;
        private readonly ObjectPool<PlaceItem> _placeItemPool;
        private GameObject _placeItemPrefab;
        private TextMeshProUGUI _resultLabel;
        private readonly List<PlaceItem> _items;

        private static PlaceList Instance
        {
            get; set;
        }

        public PlaceList()
        {
            _placeItemPool = new ObjectPool<PlaceItem>(() =>
            {
                Transform trans = Instantiate(_placeItemPrefab, _placeItemPrefab.transform.parent).transform;
                PlaceItem item = new PlaceItem(trans);
                return item;
            }, true, OnItemHide);

            _items = new List<PlaceItem>();
        }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();

            Instance = this;

            _searchArea = new SearchArea(Body.Find("Search"));
            _placeItemPrefab = Body.Find("Scroll View/Viewport/Content/Item").gameObject;
            _placeItemPrefab.SetActive(false);

            _resultLabel = Body.GetElement<TextMeshProUGUI>("Results");

            Body.GetElement<Button>("Top/Back").onClick.AddListener(OnBackClick);
        }

        protected override void OnScreenShow()
        {
            _searchArea.Clear();
            SetResultsText(0);
        }

        protected override void OnScreenHide()
        {
            SetPlaces(null);
        }

        private void SetResultsText(int n)
        {
            _resultLabel.text = string.Format(Localize(LanguageData._b__0___b__RESULTS), n);
        }

        public void SetPlaces(List<EGRWTEProxyPlace> places)
        {
            _placeItemPool.FreeAll();
            _items.Clear();

            if (places != null)
            {
                foreach (EGRWTEProxyPlace place in places)
                {
                    PlaceItem item = _placeItemPool.Rent();
                    item.SetInfo(place.Name, place.Tags.StringifyList(", "), place.CID);
                    item.SetActive(true);
                    _items.Add(item);

                    Debug.Log("Added " + place.Name);
                }

                SetResultsText(places.Count);
            }
        }

        private void OnBackClick()
        {
            HideScreen();
        }

        private void OnItemHide(PlaceItem item)
        {
            item.SetActive(false);
        }

        private void ClearFocusedItems()
        {
            foreach (PlaceItem place in _items)
            {
                place.SetActive(true);
                place.Transform.SetSiblingIndex(_placeItemPool.GetIndex(place));
            }
        }

        private void SetFocusedItems(List<PlaceItem> items)
        {
            foreach (PlaceItem place in _items)
            {
                int idx = items.FindIndex(p => p == place);
                if (idx != -1)
                {
                    place.SetActive(true);
                    place.Transform.SetSiblingIndex(idx);
                }
                else
                {
                    place.SetActive(false);
                }
            }
        }
    }
}