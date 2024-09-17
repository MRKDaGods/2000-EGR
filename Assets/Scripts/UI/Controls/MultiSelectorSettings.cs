using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public class MultiSelectorSettings : BaseBehaviour
    {
        [SerializeField]
        private GameObject[] _options;
        [SerializeField]
        private string _activeMarkerName;
        private int _selectedIndex;
        private GameObject[] _activeMarkers;

        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }
            set
            {
                _selectedIndex = value;
                UpdateActiveMarker();
            }
        }

        private void Start()
        {
            _activeMarkers = new GameObject[_options.Length];
            for (int i = 0; i < _activeMarkers.Length; i++)
            {
                _activeMarkers[i] = _options[i].transform.Find(_activeMarkerName).gameObject;

                int _i = i;
                _options[i].GetComponent<Button>().onClick.AddListener(() => OnButtonClicked(_i));
            }

            UpdateActiveMarker();
        }

        private void UpdateActiveMarker()
        {
            if (_activeMarkers == null)
                return;

            for (int i = 0; i < _activeMarkers.Length; i++)
            {
                _activeMarkers[i].SetActive(_selectedIndex == i);
            }
        }

        private void OnButtonClicked(int idx)
        {
            _selectedIndex = idx;
            UpdateActiveMarker();
        }
    }
}
