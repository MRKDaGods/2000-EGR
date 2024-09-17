using UnityEngine;

namespace MRK.UI
{
    [AddComponentMenu("RTL")]
    public class RTLAttribute : MonoBehaviour
    {
        private bool _isRTL;
        private bool _initiated;

        public bool IsRTL
        {
            get
            {
                return _initiated ? _isRTL : GetComponent<RTLTMPro.RTLTextMeshPro>() != null;
            }

            set
            {
                _isRTL = value;
                _initiated = true;
            }
        }
    }
}
