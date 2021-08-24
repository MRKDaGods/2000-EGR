using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRUIMultiSelectorSettings : EGRBehaviour {
        [SerializeField]
        GameObject[] m_Options;
        [SerializeField]
        string m_ActiveMarkerName;
        int m_SelectedIndex;
        GameObject[] m_ActiveMarkers;

        public int SelectedIndex { 
            get {
                return m_SelectedIndex;
            }
            set {
                m_SelectedIndex = value;
                UpdateActiveMarker();
            }
        }

        void Start() {
            m_ActiveMarkers = new GameObject[m_Options.Length];
            for (int i = 0; i < m_ActiveMarkers.Length; i++) {
                m_ActiveMarkers[i] = m_Options[i].transform.Find(m_ActiveMarkerName).gameObject;

                int _i = i;
                m_Options[i].GetComponent<Button>().onClick.AddListener(() => OnButtonClicked(_i));
            }

            UpdateActiveMarker();
        }

        void UpdateActiveMarker() {
            if (m_ActiveMarkers == null)
                return;

            for (int i = 0; i < m_ActiveMarkers.Length; i++) {
                m_ActiveMarkers[i].SetActive(m_SelectedIndex == i);
            }
        }

        void OnButtonClicked(int idx) {
            m_SelectedIndex = idx;
            UpdateActiveMarker();
        }
    }
}
