using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK.UI {
    public class EGRUIFancyScrollViewContent : EGRBehaviour {
        [SerializeField]
        List<EGRUIFancyScrollViewItemData> m_Data;

        void Update() {
            GetComponent<EGRUIFancyScrollView>().UpdateData(m_Data);
        }
    }
}
