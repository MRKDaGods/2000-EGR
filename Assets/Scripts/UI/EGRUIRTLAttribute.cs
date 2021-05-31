using UnityEngine;

namespace MRK.UI {
    [AddComponentMenu("RTL")]
    public class EGRUIRTLAttribute : MonoBehaviour {
        bool m_IsRTL;
        bool m_Initiated;

        public bool IsRTL {
            get => m_Initiated ? m_IsRTL : GetComponent<RTLTMPro.RTLTextMeshPro>() != null;
            set {
                m_IsRTL = value;
                m_Initiated = true;
            }
        }
    }
}
